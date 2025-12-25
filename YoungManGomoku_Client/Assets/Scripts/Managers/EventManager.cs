using System;
using UnityEngine;
using YoungManGomoku_Protocol.ClientToServer;
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
    
    // 다른 오브젝트들의 Awake가 일어나기 전에 이 Awake가 먼저 실행되어야 함!
    // 프로젝트 세팅 - Script Execution Order에서 이 스크립트를 -1로 설정하였음.
    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
        _gameManager = GetComponent<GameManager>();
        _networkManager = GetComponent<NetworkManager>();
        _networkManager.OnRequestFailed += _ => OnGameEnd!.Invoke();
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
            var response = await _networkManager.RequestGameStartAnnounce(_gameManager.IdToken);

            if (response == null || response.IsSuccess is false)
            {
                OnServerReplyFailed!.Invoke();
                return;
            }
            
            OnGameStart!.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            OnServerReplyFailed!.Invoke();
        }
    }
    
    public void StartSweeping() => OnStartSweeping!.Invoke();

    public async void PlayerSurrendered()
    {
        var surrenderRequest = new CS_InGameRequestDTO(_gameManager.IdToken, IngameRequest.Surrender);
        var reply = await _networkManager.RequestIngameAction(surrenderRequest);

        Debug.Assert(reply.GameEndCode != GameEndCode.None);
        HandleGameEndCode(reply.GameEndCode);
    }

    public void PlayerByoyomiPurchase() => OnPlayerByoyomiPurchase!.Invoke(_gameManager.ByoyomiPurchaseAmount);

    public void OppositeByoyomiPurchase() => OnOppositeByoyomiPurchase!.Invoke(_gameManager.ByoyomiPurchaseAmount);

    public void TakeBack() => OnTakeBack!.Invoke();

    public void HandleGameEndCode(GameEndCode gameEndCode)
    {
        if (IsGameEnd) return;
        
        switch (gameEndCode)
        {
            case GameEndCode.None:
                Debug.LogWarning("GameEndCode None, but Handling Function Called!!");
                return;
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
                Debug.LogError("Invalid GameEndCode");
                return;
        }

        IsGameEnd = true;
    }
}
