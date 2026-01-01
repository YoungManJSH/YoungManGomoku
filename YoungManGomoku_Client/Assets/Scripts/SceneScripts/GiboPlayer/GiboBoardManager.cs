using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class GiboBoardManager : MonoBehaviour
{
    private readonly struct ForbiddenRecords
    {
        private readonly HashSet<(int row, int col)>[] _records;
        private readonly HashSet<(int row, int col)> _empty;

        public ForbiddenRecords(int lastTurn)
        {
            // 6수까지는 금수가 존재할 수 없음
            if (lastTurn > 6)
            {
                _records = new HashSet<(int row, int col)>[lastTurn - 6];

                for (int i = 0; i < _records.Length; ++i)
                {
                    _records[i] = new HashSet<(int row, int col)>();
                }
            }
            else _records = null;

            _empty = new HashSet<(int row, int col)>();
        }

        /// <summary>7수 미만은 금수가 존재할 수 없으므로 항상 비어있는 것이 반환됨</summary>
        public HashSet<(int row, int col)> this[int turn]
            => turn < 7 ? _empty : _records[turn - 7];
    }
    
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
    public bool IsMarkForbidden { get; private set; }

    /// <summary> 기보 불러오기 실패 </summary>
    public event Action OnReadFailed;

    /// <summary> 기보 불러오기 성공 </summary>
    public event Action OnReadSucceed;

    /// <summary> 흑돌 금수 위치 시뮬레이션 완료, 매개변수는 성공 여부 </summary>
    public event Action<bool> OnSimulationCompleted;

    /// <summary> 재생 중인 턴 위치가 바뀜, 매개변수: 바뀐 턴 </summary>
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
    private ForbiddenRecords _forbiddenRecords;
    private int _nowTurn;

    private void Awake()
    {
        recentMark.gameObject.SetActive(false);
        _boardRect = GetComponent<RectTransform>();
        _boardImage = GetComponent<Image>();
        _audioSource = GetComponent<AudioSource>();
        _boardImage.sprite = BoardGenerator.GenerateBoard(boardData);
        IsMarkForbidden = false;
    }

    private void Start() => StartAsync().Cancel();

    /// <summary> Start 단계에서 비동기로 진행할 초기화 로직 </summary>
    private async Awaitable StartAsync()
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
        
        await PlaceStones();
        OnReadSucceed?.Invoke();
        waitingText.enabled = false;
        recentMark.gameObject.SetActive(true);
        
        // 백그라운드에서 비동기로 처리 후 이벤트 호출
        OnSimulationCompleted?.Invoke(await TrySimulation());
    }

    /// <summary> 기보 재생을 위한 gameObject 생성 </summary>
    private async Awaitable PlaceStones()
    {
        /* EndOfFrameAsync: Canvas Scaler 반영이 끝나고 렌더링 직전에 연산 */
        await Awaitable.EndOfFrameAsync();
        
        const float center = Board.MaxCoord / 2f; // 중앙 좌표값
        float cellSize = boardData.CellSize * _boardRect.rect.width / boardData.TotalPixel;

        /* 현재 단계에서 분할 루프 방식은 사용하지 않음.
         * Why? 아래 반복문들은 모두 상수 시간 복잡도 [O(1)]이며,
         * 테스트 과정에서 순식간에 완료되는 것을 확인하였음. */
        
        // Anchor, Pivot 모두 중앙일 때를 전제한 좌표 계산
        for (int i = 0; i < _moveStoneData.Length; ++i)
        {
            _recordStones[i] = Instantiate(i % 2 == 0 ? blackPrefab : whitePrefab, _boardRect);
            _recordStones[i].anchoredPosition =
                new Vector2(_moveStoneData[i].col - center, center - _moveStoneData[i].row) * cellSize;
            _recordStones[i].gameObject.SetActive(false);
        }

        /* 모든 좌표에 금수 마크 생성 후 비활성화
         * 이 부분은 성능 최적화보다 구현 편의 및 유지보수 난이도 선택
         * (씬 로드 시 한 번만 실행되는 구간이므로) */
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
    }

    /// <summary>백그라운드에서 비동기로 금수 위치 시뮬레이션</summary>
    /// <returns>
    /// <para>true: 시뮬레이션 완료, 흑돌 금수 정보를 구하였음</para>
    /// <para>false: 비정상적인 착수 정보가 확인되어 시뮬레이션을 중단하였음</para>
    /// </returns>
    private async Awaitable<bool> TrySimulation()
    {
        // 순수 C# 로직이고 약간 무거운 연산들이므로 백그라운드로...
        await Awaitable.BackgroundThreadAsync();
        
        Board simulator = new Board();
        bool isEnded = false;
        void Ended() => isEnded = true;
        
        simulator.OnBlackGomoku += Ended;
        simulator.OnWhiteGomoku += Ended;
        simulator.OnBlackUnmovable += Ended;
        // OverMaxTurn 이벤트는 파일 읽기 단계에서 걸러지므로 체크 불필요

        _forbiddenRecords = new ForbiddenRecords(LastTurn);
        
        #region 착수 데이터를 토대로 시뮬레이션하며 계산
        
        int turn = 0;
        
        while(turn < LastTurn)
        {
            if (simulator.TryMoveStone(_moveStoneData[turn].row, _moveStoneData[turn].col))
            { ++turn; }
            else break; // 유효하지 않은 착수 정보가 있는 상황

            if (isEnded && turn < LastTurn) break; // 종료 이벤트가 발생했는데 LastTurn이 아닌 경우
            
            if (simulator.NowTurn < 7) continue; // 7수 이후부터 금수가 생길 수 있음
            
            for (int row = 0; row < Board.BoardSize; ++row)
            {
                for (int col = 0; col < Board.BoardSize; ++col)
                {
                    if (simulator[row, col] != StoneColorType.Empty)
                        continue;

                    /* 백돌 차례일 때 흑돌 금수 정보는 simulator가 갖고 있지 않음
                     * 따라서 흑돌 턴일 때는 갖고 오고, 백돌 턴일 때만 따로 계산 */
                    JudgeType judgeType = simulator.NowTurn % 2 == 0
                        ? simulator.BlackJudge((row, col))
                        : JudgeMove.JudgeBlackMove(simulator, row, col);

                    if (judgeType == JudgeType.Forbidden)
                        _forbiddenRecords[turn].Add((row, col));
                }
            }
        }
        #endregion
        
        // 시뮬레이션 끝났으면 메인 스레드 복귀 후 결과 반환
        await Awaitable.MainThreadAsync();
        
        /* 중간에 시뮬레이션이 break 된 경우 turn < LastTurn
         * 오목, 금수패 이벤트 이후에도 착수 데이터 있음 or 유효하지 않은 착수 데이터 */
        if (turn < LastTurn)
            Debug.LogWarning($"{(isEnded ? "종료 이벤트 이후 착수 정보 존재" : "유효하지 않은 착수 정보 존재")}");
        
        return turn == LastTurn;
    }

    /// <summary> 시간 간격을 두고 연속적으로 턴 이동 </summary>
    /// <param name="token"> 캔슬 토큰 </param>
    /// <param name="delay"> 시간 간격 (초 단위) </param>
    /// <param name="moveCount"> 이동할 개수, 입력하지 않으면 끝까지 </param>
    /// <param name="isNext">
    /// <para>true[기본]: 다음 수순으로 이동(재생)</para>
    /// <para>false: 이전 수순으로 이동(되감기)</para>
    /// </param>
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
        catch (OperationCanceledException) {}
    }

    public void MoveNextTurn()
    {
        if (_nowTurn >= LastTurn) return;
        
        _recordStones[_nowTurn].gameObject.SetActive(true);
        recentMark.anchoredPosition = _recordStones[_nowTurn].anchoredPosition;
        _audioSource.PlayOneShot(stoneSound);
        ++_nowTurn;
        OnTurnChanged?.Invoke(_nowTurn);

        if (IsMarkForbidden is false) return;
        
        /* 아래 로직에서 HashSet 차집합 구하기는 GC 자폭임
         * 최적화 하려면 TrySimulation 단계에서 차집합까지 미리 구해 캐싱해 두어야 함.
         * 하지만 아래 로직을 그대로 돌려도 퍼포먼스에 하자가 없으므로 일단 그대로 둠. */
        
        /* 순서 주의! 활성화 → 비활성화 순서라면 겹치는 좌표가 비활성화 됨 */
        // 이전 턴 금수 마크를 비활성화
        foreach (var coord in _forbiddenRecords[_nowTurn - 1])
        {
            _forbiddenMarks[coord.row, coord.col].gameObject.SetActive(false);
        }
        // 현재 턴 금수 마크 활성화
        foreach (var coord in _forbiddenRecords[_nowTurn])
        {
            _forbiddenMarks[coord.row, coord.col].gameObject.SetActive(true);
        }
    }

    public void MovePrevTurn()
    {
        if (_nowTurn <= 0) return;
        
        --_nowTurn;
        _recordStones[_nowTurn].gameObject.SetActive(false);
        recentMark.anchoredPosition = _recordStones[_nowTurn].anchoredPosition;
        _audioSource.PlayOneShot(turnBackSound);
        OnTurnChanged?.Invoke(_nowTurn);
        
        if (IsMarkForbidden is false) return;
        
        /* 순서 주의! 활성화 → 비활성화 순서라면 겹치는 좌표가 비활성화 됨 */
        // 되감기 전 금수 마크를 비활성화
        foreach (var coord in _forbiddenRecords[_nowTurn + 1])
        {
            _forbiddenMarks[coord.row, coord.col].gameObject.SetActive(false);
        }
        // 현재 턴 금수 마크 활성화
        foreach (var coord in _forbiddenRecords[_nowTurn])
        {
            _forbiddenMarks[coord.row, coord.col].gameObject.SetActive(true);
        }
    }

    public void ToggleMarkForbidden()
    {
        IsMarkForbidden = !IsMarkForbidden;

        if (_nowTurn == 0) return;
        
        foreach (var coord in _forbiddenRecords[_nowTurn - 1])
        {
            _forbiddenMarks[coord.row, coord.col].gameObject.SetActive(IsMarkForbidden);
        }
    }
}