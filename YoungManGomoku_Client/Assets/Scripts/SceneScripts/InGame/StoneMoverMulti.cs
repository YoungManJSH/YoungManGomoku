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
        EventManager.OnTakeBackRequested += _ => DisableUpdate();
        EventManager.OnTakeBack += _ => enabled = _isPlayerTurn;

#if UNITY_ANDROID
        confirmButton.ButtonImageChange(_gameManager.IsPlayerBlack);
        _confirmButton = confirmButton.GetComponent<Button>();
        _confirmButton.interactable = false;
#endif
    }
    
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
            // 레이스 컨디션으로 게임 종료 응답보다 착수 응답이 늦은 경우
            if (EventManager.IsGameEnd) return;

            _isPlayerTurn = !_isPlayerTurn;
            // 내 턴이고 클라이언트 오목판 정보가 게임 종료 상황이 아니면 입력 활성화 
            enabled = _isPlayerTurn && IsGameEndInBoard is false;
            
            /* [오목, 금수패, 무승부 상황에 대한 레이스 컨디션 방어]
             * 위 상황을 만드는 착수 정보가 게임 종료 응답보다 먼저 올 경우,
             * 게임 종료 상황인데 순간적으로 착수 입력이 활성화 되어있는 시간이 생김.
             * 위와 같은 이슈로 인해 오목 연출이 틀어지는 것이 확인되었으며,
             * 광클을 한다면 상황에 맞지 않는 착수 요청 역시 보낼 수 있을 것으로 사료됨.
             * 연출은 클라이언트에서 일임하고, 게임 결과는 서버에서 일임하는 구조이므로
             * 위와 같이 레이스 컨디션을 양쪽으로 방어하는 것이 불가피해 보임. */
            
#if UNITY_ANDROID
            // 모바일용 착수 확인 버튼 활성화 여부는 본 스크립트와 동기화
            _confirmButton.interactable = enabled;
#endif
            
            #region 상대방 착수 정보 요청 및 처리
            if (_isPlayerTurn is false)
            {
                #region 내 착수 정보 DTO를 조립
                _placeStoneDTO.MyTimer = _playerTimer.SyncData;
                _placeStoneDTO.Row = (byte)NowCoord.row;
                _placeStoneDTO.Col = (byte)NowCoord.col;
                #endregion
                
                // 내 착수 정보를 송신 & 상대방 착수 정보를 요청
                EventManager.UpdateLastRequestTime();
                SC_OpponentPlaceStoneDTO opponentMove =
                    await _networkManager.RequestPlaceStone(_placeStoneDTO);

                if (opponentMove == null)
                {
                    // 나머지는 OnRequestFailed 이벤트로 처리됨
                    Debug.LogError("Error in Receive Opponent Move");
                    EventManager.ServerReplyFailed();
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

                // 상대방 착수 정보 적용
                MoveStone((opponentMove.Row, opponentMove.Col));
            }
            #endregion
        }
        catch (Exception e)
        {
            Debug.LogError($"Multi Stone Mover Error, in Turn Change Logic: {e}");
            EventManager.ServerReplyFailed();
        }
    }
}
