using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class GiboBoardManager : MonoBehaviour
{
    [SerializeField] private BoardImageData boardData;
    [SerializeField] private RectTransform blackPrefab;
    [SerializeField] private RectTransform whitePrefab;
    [SerializeField] private RectTransform recentMark;
    [SerializeField] private AudioClip stoneSound;
    [SerializeField] private AudioClip turnBackSound;

    public int LastTurn => _recordData.Length;
    public string Result => _result;
    public event Action OnReadFailed;
    public event Action OnReadSucceed;
    public event Action<int> OnTurnChanged;
    
    private RectTransform _boardRect;
    private Image _boardImage;
    private AudioSource _audioSource;
    
    private (int row, int col)[] _recordData;
    private DateTime _giboDateTime;
    private BasicPlayerData _blackData;
    private BasicPlayerData _whiteData;
    private string _result;
    private RectTransform[] _recordStones;
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
        if (GiboFileManager.TryReadGiboFile(out _recordData, out _giboDateTime,
                out _blackData, out _whiteData, out _result) is false)
        {
            OnReadFailed?.Invoke();
            return;
        }
        
        _recordStones = new RectTransform[_recordData.Length];
        _nowTurn = 0;
        PlaceStones().Cancel();
    }

    private async Awaitable PlaceStones()
    {
        // Canvas Scaler 반영이 끝난 뒤 돌 배치
        await Awaitable.EndOfFrameAsync();
        
        float cellSize = boardData.CellSize * _boardRect.rect.width / boardData.TotalPixel;
        
        for (int i = 0; i < _recordData.Length; ++i)
        {
            _recordStones[i] = Instantiate(i % 2 == 0 ? blackPrefab : whitePrefab, _boardRect);
            _recordStones[i].anchoredPosition = new Vector2(_recordData[i].col - Board.MaxCoord / 2f,
                Board.MaxCoord / 2f - _recordData[i].row) * cellSize;
            _recordStones[i].gameObject.SetActive(false);
        }

        OnReadSucceed?.Invoke();
        recentMark.gameObject.SetActive(true);
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
                moveTurn.Invoke();
                await Awaitable.WaitForSecondsAsync(delay, token);
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
