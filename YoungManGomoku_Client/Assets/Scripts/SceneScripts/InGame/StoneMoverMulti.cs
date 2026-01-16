using System;
using UnityEngine;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.ServerToClient;
#if UNITY_ANDROID
using UnityEngine.UI;
#endif

/// <summary> 멀티플레이 착수 제어 클래스 </summary>
public class StoneMoverMulti : StoneMover
{
    private bool _isPlayerTurn;
    private NetworkManager _networkManager;
    private GameManager _gameManager;
    private UserTimer _playerTimer;
    private UserTimer _oppositeTimer;
    private CS_PlaceStoneDTO _placeStoneDTO;
#if UNITY_ANDROID
    private Button _confirmButton;
#endif

    protected override void OnAwake()
    {
        _networkManager = NetworkManager.Instance;
        _gameManager = GameManager.Instance;
        
        _isPlayerTurn = _gameManager.IsPlayerBlack;
        _playerTimer = _gameManager.PlayerTimer;
        _oppositeTimer = _gameManager.OppositeTimer;
        _placeStoneDTO = new CS_PlaceStoneDTO(_gameManager.IdToken, default, 0, 0);
        
        OnStoneMove += OnTurnChanged;
        eventManager.OnTakeBackRequested += _ => DisableUpdate();
        eventManager.OnTakeBack += _ => enabled = _isPlayerTurn;

#if UNITY_ANDROID
        confirmButton.ButtonImageChange(_gameManager.IsPlayerBlack);
        _confirmButton = confirmButton.GetComponent<Button>();
        _confirmButton.interactable = false;
#endif
    }

    private void OnDisable() => NowPreview.SetActive(false);
    
    protected override void MessageBoxClosed() => enabled = _isPlayerTurn;
    
    protected override void CreatePreview()
    {
        NowPreview = (_gameManager.IsPlayerBlack ?
            Instantiate(blackStone, BlackParent) :
            Instantiate(whiteStone, WhiteParent)).gameObject;
        
        NowPreview.GetComponent<SpriteRenderer>().color = PreviewColor;
        NowPreview.name = "Preview";
        NowPreview.SetActive(false);
    }

    private async void OnTurnChanged(bool _)
    {
        try
        {
            if (eventManager.IsGameEnd) return;

            _isPlayerTurn = !_isPlayerTurn;
            enabled = _isPlayerTurn && gameEndInBoard is false;
            
#if UNITY_ANDROID
            _confirmButton.interactable = _isPlayerTurn;
#endif
            
            if (_isPlayerTurn is false) // 내가 착수를 완료한 상황
            {
                _placeStoneDTO.MyTimer = _playerTimer.SyncData;
                _placeStoneDTO.Row = (byte)NowCoord.row;
                _placeStoneDTO.Col = (byte)NowCoord.col;

                eventManager.UpdateLastRequestTime();
                SC_OpponentPlaceStoneDTO opponentMove =
                    await _networkManager.RequestPlaceStone(_placeStoneDTO);

                if (opponentMove == null)
                {
                    // 나머지는 OnRequestFailed 이벤트로 처리됨
                    Debug.LogError("Error in Receive Opponent Move");
                    eventManager.ServerReplyFailed();
                    return;
                }

                _oppositeTimer.SynchroTimer(opponentMove.OpponentTimer);
                
                /* 게임 종료 상황용 예외 처리
                 * 유효하지 않은 좌표를 돌려받을 경우 착수 처리 생략 */
                if ((opponentMove.Row, opponentMove.Col) == NowCoord ||
                    opponentMove.Row > Board.MaxCoord || opponentMove.Col > Board.MaxCoord)
                {
                    Debug.Log("유효하지 않은 착수 좌표가 응답되었음, 게임 종료 상황이면 정상");
                    return;
                }

                MoveStone((opponentMove.Row, opponentMove.Col));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Multi Stone Mover Error, in Turn Change Logic : {e}");
            eventManager.ServerReplyFailed();
        }
    }
}
