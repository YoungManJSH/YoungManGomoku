using System;
using UnityEngine;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class EventManager : MonoBehaviour
{
    [SerializeField] private BoardSweeper oppositeHand;
    
    public event Action OnGameStart;
    public event Action OnGameEnd;
    
    public event Action OnGameWin;
    public event Action OnGameLose;
    public event Action OnGameDraw;
    
    public event Action OnPlayerGomoku;
    public event Action OnOppositeGomoku;
    public event Action OnBlackUnmovable;
    
    public event Action OnPlayerSurrender;
    public event Action OnOppositeSurrender;
    public event Action OnStartSweeping;

    public event Action OnPlayerTimeOut;
    public event Action OnOppositeTimeOut;

    public event Action<int> OnPlayerByoyomiPurchase;
    public event Action<int> OnOppositeByoyomiPurchase;
    
    public event Action OnOppositeDisconnectedWin;
    public event Action OnPlayerDisconnectedLose;
    public event Action OnTakeBack;
    public event Action OnServerReplyFailed;

    public static EventManager Instance { get; private set; }
    public bool IsGameEnd { get; private set; }
    
    private GameManager _gameManager;
    private NetworkManager _networkManager;
    private CS_InGameRequestDTO _ingameRequestDTO;
    private CS_WaitForEventDTO _waitForEventDTO;
    
    // 다른 오브젝트들의 Awake가 일어나기 전에 이 Awake가 먼저 실행되어야 함!
    // 프로젝트 세팅 - Script Execution Order에서 이 스크립트를 -1로 설정하였음.
    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
        _gameManager = GetComponent<GameManager>();
        _networkManager = GetComponent<NetworkManager>();
        _networkManager.OnRequestFailed += OnRequestFailed;
        _ingameRequestDTO = new CS_InGameRequestDTO(_gameManager.IdToken, IngameRequestType.None);
        _waitForEventDTO = new CS_WaitForEventDTO(_gameManager.IdToken, takeback: false);
        
        IsGameEnd = false;
    }

    private void OnDestroy() => Instance = null;

    private void Start()
    {
        var matchResult = PlayerDataFromWebServer.Instance.MatchResultDTO;
        
        if (matchResult.MatchingSuccess is false ||
            matchResult.MyStoneColorType is StoneColorType.Empty)
        {
            OnServerReplyFailed!.Invoke();
            OnGameEnd!.Invoke();
            return;
        }
        
        // Action 개체는 Immutable이므로 구독 순서에 유의할 것!!
        OnServerReplyFailed += OnGameEnd;
        OnGameWin += OnGameEnd;
        OnGameLose += OnGameEnd;
        OnGameDraw += OnGameEnd;

        OnPlayerGomoku += OnGameWin;
        OnOppositeGomoku += OnGameLose;
        OnBlackUnmovable += _gameManager.IsPlayerBlack ? OnGameLose : OnGameWin;
        
        OnPlayerSurrender += OnGameLose;
        OnOppositeSurrender += OnGameWin;

        OnPlayerTimeOut += OnGameLose;
        OnOppositeTimeOut += OnGameWin;
        
        OnOppositeDisconnectedWin += OnGameWin;
        OnPlayerDisconnectedLose += OnGameLose;
    }

    public async void StartGame()
    {
        try
        {
            SC_ResponseStringDTO reply =
                await _networkManager.RequestGameStartAnnounce(_gameManager.IdToken, timeOutSeconds: 3);

            if (reply == null)
            {
                // 나머지는 OnRequestFailed 이벤트로 처리됨
                Debug.LogError("게임 시작 응답으로 null이 들어옴");
                return;
            }
            
            if (reply.IsSuccess is false)
            {
                ServerReplyFailed();
                return;
            }
            
            OnGameStart!.Invoke();
            HandleIngameEvent();
        }
        catch (Exception e)
        {
            Debug.LogError($"게임 시작 요청 실패: {e}");
            ServerReplyFailed();
        }
    }
    
    public async void PlayerSurrendered()
    {
        try
        {
            _ingameRequestDTO.IngameRequest = IngameRequestType.Surrender;
            SC_ResponseStringDTO reply =
                await _networkManager.RequestIngameAction(_ingameRequestDTO, timeOutSeconds: 3);
            
            if (reply == null)
            {
                // 나머지는 OnRequestFailed 이벤트로 처리됨
                Debug.LogError("기권 요청 응답으로 null이 왔음");
                ServerReplyFailed();
                return;
            }

            if (reply.IsSuccess is false)
            {
                // 시간패, 상대방의 기권과 레이스 컨디션이 발생할 경우 여기로 올 수도?
                Debug.LogWarning("기권 요청 응답의 IsSuccess가 false...?");
            }
            
            /* 다른 요청과는 달리 인게임 요청 DTO의 멤버를 None으로 돌리지 않음
             * 기권 요청은 어떠한 경로로 흘러가든 게임이 끝나게 되기 때문 */
        }
        catch (Exception e)
        {
            Debug.LogError($"기권 요청 에러: {e}");
            ServerReplyFailed();
        }
    }

    public async void RequestTakeBack()
    {
        // TODO: 타이머 정지시킬 거면 해당 처리 추가하기!
        try
        {
            _ingameRequestDTO.IngameRequest = IngameRequestType.TakeBack;
            SC_ResponseStringDTO reply = // 상대방의 승인을 기다려야 하므로 넉넉하게 대기
                await _networkManager.RequestIngameAction(_ingameRequestDTO, timeOutSeconds: 15);
            
            if (reply == null)
            {
                // 나머지는 OnRequestFailed 이벤트로 처리됨
                Debug.LogError("무르기 요청 응답으로 null이 왔음");
                ServerReplyFailed();
                return;
            }

            if (reply.IsSuccess)
            {
                Debug.Log("무르기 요청이 승인됨!");
                OnTakeBack!.Invoke();
                // TODO: OnTakeBack 이벤트에 승인 UI 호출도 추가 구독...
            }
            else
            {
                Debug.Log("무르기 요청이 거부됨! - 상대방의 거부 or 구매 거부");
                // TODO: 거부됐다는 UI 호출이 있어야 할 듯..? 이벤트 새로 파자
            }
            
            // 요청-응답이 완료되었으면 DTO 멤버 초기화 (잘못된 사용을 미연에 방지)
            _ingameRequestDTO.IngameRequest = IngameRequestType.None;
        }
        catch (Exception e)
        {
            Debug.LogError($"무르기 요청 에러: {e}");
            ServerReplyFailed();
        }
    }
    
    public async void RequestPurchaseByoyomi()
    {
        try
        {
            _ingameRequestDTO.IngameRequest = IngameRequestType.PurchaseByoyomi;
            SC_ResponseStringDTO reply =
                await _networkManager.RequestIngameAction(_ingameRequestDTO, timeOutSeconds: 3);
            
            if (reply == null)
            {
                // 나머지는 OnRequestFailed 이벤트로 처리됨
                Debug.LogError("초읽기 구매 요청 응답으로 null이 왔음");
                ServerReplyFailed();
                return;
            }

            if (reply.IsSuccess)
            {
                Debug.Log("초읽기 구매가 승인됨!");
                OnPlayerByoyomiPurchase!.Invoke(_gameManager.ByoyomiPurchaseAmount);
            }
            else
            {
                /* 레이스 컨디션으로 게임 종료가 중간에 끼어들면 여기 들어올 수 있음
                 * 그밖의 경우는 클라이언트가 요청을 잘못한 것이므로 로직 확인할 것 */ 
                Debug.LogWarning("초읽기 구매가 거부됨! (게임 종료 레이스 컨디션이 아니라면 클라이언트 요청이 잘못된 것)");
            }
            
            // 요청-응답이 완료되었으면 DTO 멤버 초기화 (잘못된 사용을 미연에 방지)
            _ingameRequestDTO.IngameRequest = IngameRequestType.None;
        }
        catch (Exception e)
        {
            Debug.LogError($"초읽기 구매 요청 에러: {e}");
            ServerReplyFailed();
        }
    }
    
    public void ServerReplyFailed()
    {
        IsGameEnd = true;
        OnServerReplyFailed!.Invoke();
    }

    public void StartSweeping() => OnStartSweeping!.Invoke();
    
    public void HandleGameEndCode(GameEndCode gameEndCode)
    {
        if (IsGameEnd) return; // 중복 호출 방어
        
        switch (gameEndCode)
        {
            case GameEndCode.None:
                Debug.LogWarning("GameEndCode None, but Handling Function Called!!");
                return; // IsGameEnd를 true로 만들지 않고 바로 return
            case GameEndCode.GomokuWin:
                OnPlayerGomoku!.Invoke();
                break;
            case GameEndCode.GomokuLose:
                OnOppositeGomoku!.Invoke();
                break;
            case GameEndCode.BlackUnmovable:
                OnBlackUnmovable!.Invoke();
                break;
            case GameEndCode.SurrenderWin:
                oppositeHand.Sweeping();
                OnOppositeSurrender!.Invoke();
                break;
            case GameEndCode.SurrenderLose:
                OnPlayerSurrender!.Invoke();
                break;
            case GameEndCode.TimeOutWin:
                OnOppositeTimeOut!.Invoke();
                break;
            case GameEndCode.TimeOutLose:
                OnPlayerTimeOut!.Invoke();
                break;
            case GameEndCode.DisconnectedWin:
                OnOppositeDisconnectedWin!.Invoke();
                break;
            case GameEndCode.DisconnectedLose:
                OnPlayerDisconnectedLose!.Invoke();
                break;
            case GameEndCode.Draw:
                OnGameDraw!.Invoke();
                break;
            default:
                Debug.LogError("정의되지 않은 GameEndCode!");
                ServerReplyFailed();
                break;
        }

        IsGameEnd = true;
    }

    private async void HandleIngameEvent()
    {
        try
        {
            SC_WaitEventDTO response =
                await _networkManager.RequestWaitForEvent(_waitForEventDTO);
            
            if (response == null)
            {
                // 나머지는 OnRequestFailed 이벤트로 처리됨
                Debug.LogError("인게임 이벤트 응답으로 null이 들어옴");
                ServerReplyFailed();
                return;
            }

            if (response.GameEndCode != GameEndCode.None)
            {
                HandleGameEndCode(response.GameEndCode);
                return;
            }

            // 여기부터는 게임 종료 이벤트가 아닌 경우
            
            _waitForEventDTO.IsTakeBackable = false; // 무르기 거부가 기본 세팅
            
            switch (response.OpponentRequest)
            {
                case IngameRequestType.None:
                    Debug.LogWarning("아무 동작도 없는 인게임 이벤트 응답!");
                    if (IsGameEnd) return; // 게임이 끝났으면 완전 종료
                    break; // 게임이 끝나지 않았으면 switch문만 종료
                case IngameRequestType.Surrender:
                    Debug.LogError("서버가 바보인 듯?");
                    ServerReplyFailed();
                    return;
                case IngameRequestType.TakeBack:
                    // TODO: 상대방의 무르기 요청에 대해 승인 확인 대화상자 핑퐁
                    // 승인, 거부 처리 후 IsTakeBackable 조정
                    // 승인이면 OnTakeBack 이벤트 Invoke
                    break;
                case IngameRequestType.PurchaseByoyomi:
                    OnOppositeByoyomiPurchase!.Invoke(_gameManager.ByoyomiPurchaseAmount);
                    break;
                default:
                    Debug.LogError("정의되지 않은 인게임 이벤트 종류");
                    ServerReplyFailed();
                    return;
            }
            
            HandleIngameEvent(); // 다음 인게임 이벤트 응답을 받기 위해 재호출
        }
        catch (Exception e)
        {
            Debug.LogError($"인게임 이벤트 대기 요청 실패: {e}");
            ServerReplyFailed();
        }
    }
    
    private void OnRequestFailed(RequestError _)
    {
        // 게임이 끝난 상황에서는 해당 이벤트 처리가 불필요
        if (IsGameEnd) return;
        
        IsGameEnd = true;
        OnGameEnd!.Invoke();
    }
}
