using System;
using UnityEngine;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.TypeEnum.InGame;

/// <summary> 멀티플레이 착수 제어 클래스 </summary>
public class StoneMoverMulti : StoneMover
{
    private bool _isPlayerTurn;
    private NetworkManager _networkManager;
    private EventManager _eventManager;
    private GameManager _gameManager;
    private UserTimer _playerTimer;
    private UserTimer _oppositeTimer;
    private CS_PlaceStoneDTO _placeStoneDto;

    protected override void OnAwake()
    {
        _networkManager = NetworkManager.Instance;
        _eventManager = EventManager.Instance;
        _gameManager = GameManager.Instance;
        
        _isPlayerTurn = _gameManager.IsPlayerBlack;
        _playerTimer = _gameManager.PlayerTimer;
        _oppositeTimer = _gameManager.OppositeTimer;
        _placeStoneDto = new CS_PlaceStoneDTO(_gameManager.IdToken, default, 0, 0);
        
        OnStoneMove += OnTurnChanged;
    }

    private void OnDisable() => NowPreview.SetActive(false);

    public override void MoveConfirmed()
    {
#if UNITY_ANDROID
        if (NowPreview.activeSelf)
        {
            NowPreview.SetActive(false);
            MoveStone(PrevCoord);
        }
#endif
    }
    
    protected override void MessageBoxClosed() => enabled = _isPlayerTurn;
    
    protected override void CreatePreview()
    {
        NowPreview = _gameManager.IsPlayerBlack ?
            Instantiate(blackStone, BlackParent) : Instantiate(whiteStone, WhiteParent);
        
        NowPreview.GetComponent<SpriteRenderer>().color = PreviewColor;
        NowPreview.name = "Preview";
        NowPreview.SetActive(false);
    }

    private async void OnTurnChanged(bool _)
    {
        try
        {
            if (_eventManager.IsGameEnd) return;

            _isPlayerTurn = !_isPlayerTurn;
            enabled = _isPlayerTurn;

            if (_isPlayerTurn)
            {
                GameEndCode endCode =
                    await _networkManager.RequestMyTurn(_gameManager.IdToken);

                if (endCode != GameEndCode.None)
                {
                    _eventManager.HandleGameEndCode(endCode);
                }
            }
            else
            {
                _placeStoneDto.MyTimer = _playerTimer.SyncData;
                _placeStoneDto.Row = (byte)PrevCoord.row;
                _placeStoneDto.Col = (byte)PrevCoord.col;
                
                var opponentMove =
                    await _networkManager.RequestPlaceStone(_placeStoneDto);

                _oppositeTimer.SynchroTimer(opponentMove.OpponentTimer);

                if (opponentMove.GameEndCode != GameEndCode.None)
                {
                    _eventManager.HandleGameEndCode(opponentMove.GameEndCode);
                }

                MoveStone((opponentMove.Row, opponentMove.Col));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Multi Stone Mover Error, in Turn Change Logic : {e}");
        }
    }
}
