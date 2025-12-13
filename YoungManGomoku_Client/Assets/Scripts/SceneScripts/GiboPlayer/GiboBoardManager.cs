using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class GiboBoardManager : MonoBehaviour
{
    [SerializeField] private BoardImageData boardData;
    [SerializeField] private RectTransform blackPrefab;
    [SerializeField] private RectTransform whitePrefab;

    public int LastTurn => _recordData.Length; 
    public string Result => _result;
    public event Action OnReadFailed;
    public event Action OnReadSucceed;
    public event Action<int> OnTurnChanged;
    
    private RectTransform _boardRect;
    private Image _boardImage;
    
    private (int row, int col)[] _recordData;
    private DateTime _giboDateTime;
    private BasicPlayerData _blackData;
    private BasicPlayerData _whiteData;
    private string _result;
    private RectTransform[] _recordStones;
    private int _nowTurn;
    
    private void Awake()
    {
        _boardRect = GetComponent<RectTransform>();
        _boardImage = GetComponent<Image>();
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
    }
    
    public async Awaitable AutoPlay(CancellationToken token)
    {
        try
        {
            while (_nowTurn < LastTurn)
            {
                await Awaitable.WaitForSecondsAsync(0.5f, token);
                MoveNextTurn();
            }
        }
        catch (OperationCanceledException) { }
    }

    public void MoveNextTurn()
    {
        if (_nowTurn >= LastTurn) return;
        _recordStones[_nowTurn].gameObject.SetActive(true);
        ++_nowTurn;
        OnTurnChanged?.Invoke(_nowTurn);
    }

    public void MovePrevTurn()
    {
        if (_nowTurn <= 0) return;
        --_nowTurn;
        _recordStones[_nowTurn].gameObject.SetActive(false);
        OnTurnChanged?.Invoke(_nowTurn);
    }

    public void MoveNext10Turn()
    {
        for (int i = 0; i < 10; ++i)
        {
            if (_nowTurn == LastTurn) break;
            MoveNextTurn();
        }
    }

    public void MovePrev10Turn()
    {
        for (int i = 0; i < 10; ++i)
        {
            if (_nowTurn == 0) break;
            MovePrevTurn();
        }
    }

    public void MoveLastTurn()
    {
        while (_nowTurn < LastTurn)
        {
            MoveNextTurn();
        }
    }

    public void MoveZeroTurn()
    {
        while (_nowTurn > 0)
        {
            MovePrevTurn();
        }
    }
}
