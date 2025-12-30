using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class GiboBoardManager : MonoBehaviour
{
    [SerializeField] private BoardImageData boardData;
    [SerializeField] private RectTransform blackPrefab;
    [SerializeField] private RectTransform whitePrefab;
    [SerializeField] private RectTransform forbiddenPrefab;
    [SerializeField] private RectTransform recentMark;
    [SerializeField] private TextMeshProUGUI waitingText;
    [SerializeField] private AudioClip stoneSound;
    [SerializeField] private AudioClip turnBackSound;

    public int LastTurn => _moveStoneData.Length;
    public DateTime GiboDateTime => _giboDateTime;
    public BasicPlayerData BlackData => _blackData;
    public BasicPlayerData WhiteData => _whiteData;
    public string Result => _result;
    public event Action OnReadFailed;
    public event Action OnReadSucceed;
    public event Action<int> OnTurnChanged;
    
    private RectTransform _boardRect;
    private Image _boardImage;
    private AudioSource _audioSource;
    
    private (int row, int col)[] _moveStoneData;
    private DateTime _giboDateTime;
    private BasicPlayerData _blackData;
    private BasicPlayerData _whiteData;
    private string _result;
    private RectTransform[] _recordStones;
    private RectTransform[,] _forbiddenMarks;
    private HashSet<(int row, int col)>[] _forbiddenRecords;
    private int _nowTurn;
    
    private void Awake()
    {
        recentMark.gameObject.SetActive(false);
        _boardRect = GetComponent<RectTransform>();
        _boardImage = GetComponent<Image>();
        _audioSource = GetComponent<AudioSource>();
        _boardImage.sprite = BoardGenerator.GenerateBoard(boardData);
    }

    private void Start()
    {
        if (GiboFileManager.TryReadGiboFile(out _moveStoneData, out _giboDateTime,
                out _blackData, out _whiteData, out _result) is false)
        {
            waitingText.enabled = false;
            OnReadFailed?.Invoke();
            return;
        }
        
        _recordStones = new RectTransform[_moveStoneData.Length];
        _forbiddenMarks = new RectTransform[Board.BoardSize, Board.BoardSize];
        _nowTurn = 0;
        PlaceStones().Cancel();
    }

    private async Awaitable PlaceStones()
    {
        // Canvas Scaler 반영이 끝난 뒤 돌 배치
        await Awaitable.EndOfFrameAsync();
        
        const float center = Board.MaxCoord / 2f; // 중앙 좌표값
        float cellSize = boardData.CellSize * _boardRect.rect.width / boardData.TotalPixel;
        
        // Anchor, Pivot 모두 중앙일 때를 전제한 좌표 계산
        for (int i = 0; i < _moveStoneData.Length; ++i)
        {
            _recordStones[i] = Instantiate(i % 2 == 0 ? blackPrefab : whitePrefab, _boardRect);
            _recordStones[i].anchoredPosition =
                new Vector2(_moveStoneData[i].col - center, center - _moveStoneData[i].row) * cellSize;
            _recordStones[i].gameObject.SetActive(false);
        }

        /* 이 부분은 성능 최적화보다 구현 편의 및 유지보수 난이도 선택
         * 상수 시간 복잡도이고 씬 로드 시 한 번만 실행되는 구간이므로 */
        for (int row = 0; row < Board.BoardSize; ++row)
        {
            for (int col = 0; col < Board.BoardSize; ++col)
            {
                _forbiddenMarks[row, col] = Instantiate(forbiddenPrefab, _boardRect);
                _forbiddenMarks[row, col].anchoredPosition =
                    new Vector2(col - center, center - row) * cellSize;
                _forbiddenMarks[row, col].gameObject.SetActive(false);
            }
        }

        OnReadSucceed?.Invoke();
        waitingText.enabled = false;
        recentMark.gameObject.SetActive(true);
    }

    private bool TryGenerateForbiddenRecords()
    {
        _forbiddenRecords = new HashSet<(int row, int col)>[LastTurn];
        Board simulator = new Board();
        
        // 착수 데이터를 토대로 시뮬레이션
        for (int turn = 0; turn < LastTurn; ++turn)
        {
            if (simulator.TryMoveStone(_moveStoneData[turn].row, _moveStoneData[turn].col) is false)
            {
                // TODO: false 반환 시 정상적이지 않은 착수데이터 있음 알리기
                Debug.LogError("착수 데이터 시뮬레이션 실패!");
                return false;
            }
            
            _forbiddenRecords[turn] = new HashSet<(int row, int col)>();
            
            if (turn < 7) continue; // 7수 이후부터 금수가 생길 수 있음

            // TODO: 반복문 안에 if문 가능할 듯? 혹은 삼항연산자
            if (turn % 2 == 0) // 흑돌 차례인 경우 simulator에 이미 계산된 값이 있음
            {
                for (int row = 0; row < Board.BoardSize; ++row)
                {
                    for (int col = 0; col < Board.BoardSize; ++col)
                    {
                        if (simulator.BlackJudge((row, col)) == JudgeType.Forbidden)
                            _forbiddenRecords[turn].Add((row, col));
                    }
                }
            }
            else // 백돌 차례인 경우 흑돌 금수 정보 별도로 계산
            {
                for (int row = 0; row < Board.BoardSize; ++row)
                {
                    for (int col = 0; col < Board.BoardSize; ++col)
                    {
                        if (simulator[row, col] != StoneColorType.Empty) continue;
                        
                        if (JudgeMove.JudgeBlackMove(simulator, row, col) == JudgeType.Forbidden)
                            _forbiddenRecords[turn].Add((row, col));
                    }
                }
            }
        }

        // TODO: 금수 표시 버튼 활성화시키고 어쩌고저쩌고...
        return true;
    }
    
    public async Awaitable MoveTurnWithDelay(CancellationToken token, float delay,
        int moveCount = Board.BoardSize * Board.BoardSize, bool isNext = true)
    {
        try
        {
            Action moveTurn = isNext ? MoveNextTurn : MovePrevTurn;
            int iteration = Math.Min(moveCount, isNext ? LastTurn - _nowTurn : _nowTurn);

            for (int i = 0; i < iteration; ++i)
            {
                await Awaitable.WaitForSecondsAsync(delay, token);
                moveTurn.Invoke();
            }
        }
        catch (OperationCanceledException) { }
    }
    
    public void MoveNextTurn()
    {
        if (_nowTurn >= LastTurn) return;
        _recordStones[_nowTurn].gameObject.SetActive(true);
        recentMark.anchoredPosition = _recordStones[_nowTurn].anchoredPosition;
        _audioSource.PlayOneShot(stoneSound);
        ++_nowTurn;
        OnTurnChanged?.Invoke(_nowTurn);
    }

    public void MovePrevTurn()
    {
        if (_nowTurn <= 0) return;
        --_nowTurn;
        _recordStones[_nowTurn].gameObject.SetActive(false);
        recentMark.anchoredPosition = _recordStones[_nowTurn].anchoredPosition;
        _audioSource.PlayOneShot(turnBackSound);
        OnTurnChanged?.Invoke(_nowTurn);
    }
}
