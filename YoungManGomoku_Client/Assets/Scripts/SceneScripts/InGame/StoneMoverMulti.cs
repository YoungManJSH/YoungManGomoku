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
    private string _idToken;
    private UserTimer _playerTimer;
    private UserTimer _oppositeTimer;

    protected override void OnAwake()
    {
        _networkManager = NetworkManager.Instance;
        _eventManager = EventManager.Instance;
        _isPlayerTurn = GameManager.Instance.IsPlayerBlack;
        _idToken = GameManager.Instance.IdToken;
        _playerTimer = GameManager.Instance.PlayerTimer;
        _oppositeTimer = GameManager.Instance.OppositeTimer;
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
        NowPreview = GameManager.Instance.IsPlayerBlack ?
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
                GameEndCode endCode = await _networkManager.RequestMyTurn(_idToken);

                if (endCode != GameEndCode.None)
                {
                    _eventManager.HandleGameEndCode(endCode);
                }
            }
            else
            {
                var placeStoneDto = new CS_PlaceStoneDTO(_idToken, _playerTimer.SyncData, PrevCoord.row, PrevCoord.col);
                var opponentMove = await _networkManager.RequestPlaceStone(placeStoneDto);

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
