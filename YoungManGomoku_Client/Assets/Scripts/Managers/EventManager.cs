using System;
using UnityEngine;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class EventManager : MonoBehaviour
{
    private const long HEARTBEAT_TERM = 10_000L; // 10초
    
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
    
    public event Action OnPlayerTimeOut;
    public event Action OnOppositeTimeOut;

    public event Action<int> OnPlayerByoyomiPurchase;
    public event Action<int> OnOppositeByoyomiPurchase;
    
    public event Action OnOppositeDisconnectedWin;
    public event Action OnPlayerDisconnectedLose;

    /// <summary>
    /// <para>본인 혹은 상대방의 무르기 요청 시점에 발생</para>
    /// <para>매개변수 true: 나의 요청</para>
    /// <para>매개변수 false: 상대방의 요청</para>
    /// </summary>
    public event Action<bool> OnTakeBackRequested;
    /// <summary>
    /// <para>무르기 요청 이후 수락/거절이 결정되었을 때 발생</para>
    /// <para>매개변수 true: 요청이 수락됨, 무르기 진행</para>
    /// <para>매개변수 false: 요청이 거절됨, 게임 재개</para>
    /// </summary>
    public event Action<bool> OnTakeBack;

    /// <summary>재대결 수락 요청을 보내는 시점에 발생</summary>
    public event Action OnWaitingRematch;
    /// <summary>재대결 불성립 응답을 받았을 때 발생</summary>
    public event Action OnRematchFailed;
    /// <summary>서버의 응답에 결함이 있을 때 발생 (게임 중단)</summary>
    public event Action OnServerReplyFailed;

    public static EventManager Instance { get; private set; }
    public bool IsGameEnd { get; private set; }
    
    private GameManager _gameManager;
    private NetworkManager _networkManager;
    private CS_InGameRequestDTO _ingameRequestDTO;
    private long _lastRequestTime;
    
    /* 다른 오브젝트들의 Awake가 일어나기 전에 이 Awake가 먼저 실행되어야 함!
     * 프로젝트 세팅 - Script Execution Order에서 이 스크립트를 -1로 설정하였음. */
    private void Awake()
    {
        if (Instance != null) Destroy(Instance);
        Instance = this;
        _gameManager = GetComponent<GameManager>();
        _networkManager = GetComponent<NetworkManager>();
        _networkManager.OnRequestFailed += OnRequestFailed;
        _ingameRequestDTO = new CS_InGameRequestDTO(_gameManager.IdToken, IngameRequestType.None);
        _lastRequestTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		
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
        
        enabled = false;
    }

    private void Update()
    {
        long nowTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        if (nowTime - _lastRequestTime > HEARTBEAT_TERM)
        {
            _networkManager.RequestHeartbeat(_gameManager.IdToken, timeOutSeconds: 5).Cancel();
            _lastRequestTime = nowTime;
        }
    }
    
    /// <summary>마지막 요청 시각을 현재로 업데이트 (Heartbeat 구현용)</summary>
    public void UpdateLastRequestTime()
        => _lastRequestTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    
    /// <summary> 게임 시작 요청 </summary>
    public async void StartGame()
    {
        try
        {
            SC_ResponseStringDTO reply =
                await _networkManager.RequestGameStartAnnounce(_gameManager.IdToken, timeOutSeconds: 5);

            if (reply == null)
            {
                // 나머지는 OnRequestFailed 이벤트로 처리됨
                Debug.LogError("게임 시작 응답으로 null이 들어옴");
                ServerReplyFailed();
                return;
            }
            
            if (reply.IsSuccess is false)
            {
                ServerReplyFailed();
                return;
            }
            
            OnGameStart!.Invoke();
            HandleIngameEvent();
            HandleGameResult();
            
            enabled = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"게임 시작 요청 실패: {e}");
            ServerReplyFailed();
        }
    }
    
    /// <summary> 기권 요청 </summary>
    public async void RequestSurrender()
    {
        try
        {
            _ingameRequestDTO.IngameRequest = IngameRequestType.Surrender;
            SC_ResponseStringDTO reply =
                await _networkManager.RequestIngameAction(_ingameRequestDTO, timeOutSeconds: 5);
            
            // 게임 종료 응답과 레이스 컨디션 발생 시 아래 케이스 가능
            if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene)
                return;
            
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
            if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene)
                return;
            
            Debug.LogError($"기권 요청 에러: {e}");
            ServerReplyFailed();
        }
    }

    /// <summary> 무르기 요청, 무르기 성사 여부는 별도로 처리됨 </summary>
    public async void RequestTakeBack()
    {
        try
        {
            OnTakeBackRequested!.Invoke(true);

            UpdateLastRequestTime();
            _ingameRequestDTO.IngameRequest = IngameRequestType.TakeBack;
            SC_ResponseStringDTO reply =
                await _networkManager.RequestIngameAction(_ingameRequestDTO, timeOutSeconds: 5);
            
            // 게임 종료 응답과 레이스 컨디션 발생 시 아래 케이스 가능
            if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene)
                return;
            
            if (reply == null)
            {
                // 나머지는 OnRequestFailed 이벤트로 처리됨
                Debug.LogError("무르기 요청 응답으로 null이 왔음");
                ServerReplyFailed();
                return;
            }

            if (reply.IsSuccess)
            {
                Debug.Log("무르기 요청이 확인됨!");
                // 상대방의 승인 응답은 인게임 이벤트 응답으로 받음
            }
            else
            {
                /* 레이스 컨디션으로 게임 종료가 중간에 끼어들면 여기 들어올 수 있음
                 * 그밖의 경우는 클라이언트가 요청을 잘못한 것이므로 로직 확인할 것 */
                Debug.LogWarning("무르기 요청이 거부됨! (게임 종료 레이스 컨디션이 아니라면 클라이언트 요청이 잘못된 것)");
                
                if (IsGameEnd is false)
                    OnTakeBack!.Invoke(false);
            }
            
            // 요청-응답이 완료되었으면 DTO 멤버 초기화 (잘못된 사용을 미연에 방지)
            _ingameRequestDTO.IngameRequest = IngameRequestType.None;
        }
        catch (Exception e)
        {
            if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene)
                return;
            
            Debug.LogError($"무르기 요청 에러: {e}");
            ServerReplyFailed();
        }
    }
    
    /// <summary> 초읽기 구매 요청 </summary>
    public async void RequestPurchaseByoyomi()
    {
        try
        {
            UpdateLastRequestTime();
            _ingameRequestDTO.IngameRequest = IngameRequestType.PurchaseByoyomi;
            SC_ResponseStringDTO reply =
                await _networkManager.RequestIngameAction(_ingameRequestDTO, timeOutSeconds: 5);
            
            // 게임 종료 응답과 레이스 컨디션 발생 시 아래 케이스 가능
            if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene)
                return;
            
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
            if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene)
                return;
            
            Debug.LogError($"초읽기 구매 요청 에러: {e}");
            ServerReplyFailed();
        }
    }

    /// <summary>재대결 수락 여부 전송</summary>
    /// <param name="isAccept">수락 여부</param>
    public void RequestRematch(bool isAccept)
    {
        try
        {
            if (isAccept) OnWaitingRematch!.Invoke();
            
            CS_PermitDTO accept = new CS_PermitDTO(_gameManager.IdToken, isAccept);
            _networkManager.RequestRematch(accept, timeOutSeconds: 5).Cancel();
        }
        catch (Exception e)
        {
            Debug.LogError($"재대결 {(isAccept ? "수락" : "거절")} 요청 에러: {e}");
            /* 이 요청은 예외 발생 시 별도 처리 없이 지나가도 무방함
             * Why? 재대결이 성사되지 않을뿐이고 서버가 알아서 처리할 영역이기 때문 */
        }
    }
    
    /// <summary> 서버의 응답에 결함이 있을 경우 실행 </summary>
    public void ServerReplyFailed()
    {
        if (IsGameEnd) return;

        enabled = false;
        IsGameEnd = true;
        OnServerReplyFailed!.Invoke();
        _networkManager.RequestCloseSession(_gameManager.IdToken, timeOutSeconds: 5).Cancel(); //일방적 통보
        // 터지는 상황에서는 재로그인이 필요함
        SceneLoadManager.LoadScene(SceneLoadManager.SceneType.TitleScene, seconds: 2f).Cancel();
    }
    
    /// <summary>게임 종료 결과를 받아서 처리</summary>
    private async void HandleGameResult()
    {
        try
        {
            GameRecord result =
                await _networkManager.RequestGameResult(_gameManager.IdToken);

            if (result.EndCode == GameEndCode.None)
            {
                Debug.LogError("게임 종료 코드로 None이 응답됨!");
                ServerReplyFailed();
                return;
            }

            HandleGameEndCode(result.EndCode);
            PlayerDataFromWebServer.Instance.PlayerData.UpdateData(ref result);
        }
        catch (Exception e)
        {
            Debug.LogError($"게임 결과 요청 및 응답 처리 실패: {e}");
            ServerReplyFailed();
        }
    }
    
    /// <summary>GameEndCode에 따라 게임 종료 이벤트 실행</summary>
    private void HandleGameEndCode(GameEndCode gameEndCode)
    {
        if (IsGameEnd) return; // 중복 호출 방어

        bool rematchPossible = true;
        
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
                rematchPossible = false;
                break;
            case GameEndCode.DisconnectedLose:
                OnPlayerDisconnectedLose!.Invoke();
                rematchPossible = false;
                break;
            case GameEndCode.Draw:
                OnGameDraw!.Invoke();
                break;
            default:
                Debug.LogError("정의되지 않은 GameEndCode!");
                ServerReplyFailed();
                return; // 이후 과정 생략, 바로 return
        }

        enabled = false;
        IsGameEnd = true;

        if (rematchPossible) WaitForRematch();
    }
    
    /// <summary>인게임 이벤트 대기 요청-응답 사이클 관리 함수</summary>
    private async void HandleIngameEvent()
    {
        try
        {
            UpdateLastRequestTime();
            SC_WaitEventDTO response =
                await _networkManager.RequestWaitForEvent(_gameManager.IdToken);

            /* 1. ServerReplyFailed → 나가기 → 응답 도착인 경우
             * 2. 찰나의 순간에 게임 종료 응답과 레이스 컨디션이 발생할 가능성도 이론적으로 존재함 */
            if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene)
                return;
            
            if (response == null)
            {
                // 나머지는 OnRequestFailed 이벤트로 처리됨
                Debug.LogError("인게임 이벤트 응답으로 null이 들어옴");
                ServerReplyFailed();
                return;
            }
            
            switch (response.OpponentRequest)
            {
                case IngameRequestType.None:
                    if (IsGameEnd) return; // 게임이 끝났으면 완전 종료
                    
                    Debug.LogWarning("비어있는 인게임 이벤트가 응답됨!");
                    await Awaitable.WaitForSecondsAsync(0.5f);
                    if (IsGameEnd)
                        return; // 레이스 컨디션 이슈를 고려하여 0.5초 대기 후 다시 체크
                    break; // 게임이 끝나지 않았으면 switch문만 종료
                
                case IngameRequestType.Surrender:
                    Debug.Log("상대방의 기권 이벤트가 응답됨!");
                    // 승패 처리는 게임 결과 요청에서 일괄적으로
                    return;
                
                case IngameRequestType.TakeBack:
                    OnTakeBackRequested!.Invoke(false);
                    break;
                
                case IngameRequestType.TakeBackResult:
                    OnTakeBack!.Invoke(response.IsTakeBackSuccess);
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
            if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene)
                return;
            
            Debug.LogError($"인게임 이벤트 대기 요청 실패: {e}");
            ServerReplyFailed();
        }
    }
    
    /// <summary>게임 종료 후 재대결 성사 여부 응답 요청</summary>
    private async void WaitForRematch()
    {
        try
        {
            SC_RematchResultDTO result =
                await _networkManager.RequestRematchResult(_gameManager.IdToken,
                    timeOutSeconds: Mathf.RoundToInt(ResultPresenter.RematchWaitingTime) + 1);
            
            // 유저가 인게임 씬을 이미 떠났다면 이 응답은 무효임
            if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene)
                return;
            
            if (result == null)
            {
                // 나머지는 OnRequestFailed 이벤트로 처리됨
                Debug.LogError("재대결 결과 응답으로 null이 왔음");
                OnRematchFailed!.Invoke();
                return;
            }

            if (result.IsRematchSuccess)
            {
                Debug.Log("재대결이 성사되었음!");
                PlayerDataFromWebServer.Instance.MatchResultDTO.ApplyRematchInform(result);
                SceneLoadManager.LoadScene(SceneLoadManager.SceneType.IngameScene).Cancel();
            }
            else
            {
                Debug.Log("재대결이 성사되지 않았음!");
                OnRematchFailed!.Invoke();
            }
        }
        catch (Exception e)
        {
            if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene)
                return;
            
            Debug.LogError($"재대결 대기 요청 실패: {e}");
            OnRematchFailed!.Invoke();
        }
    }
    
    private void OnRequestFailed(RequestError _)
    {
        // 게임이 끝난 상황에서는 해당 이벤트 처리가 불필요
        if (IsGameEnd) return;

        enabled = false;
        IsGameEnd = true;
        OnGameEnd!.Invoke();
    }
}