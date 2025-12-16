using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public event Action OnGameStart;
    public event Action OnGameEnd;
    
    public event Action OnGameWin;
    public event Action OnGameLose;
    public event Action OnGameDraw;
    
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

    public static EventManager Instance { get; private set; }
    
    private GameManager _gameManager;
    
    // 다른 오브젝트들의 Awake가 일어나기 전에 이 Awake가 먼저 실행되어야 함!
    // 프로젝트 세팅 - Script Execution Order에서 이 스크립트를 -1로 설정하였음.
    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
        _gameManager = GetComponent<GameManager>();
    }

    private void OnDestroy() => Instance = null;

    private void Start()
    {
        // Action 개체는 Immutable이므로 구독 순서에 유의할 것!!
        OnGameWin += OnGameEnd;
        OnGameLose += OnGameEnd;
        OnGameDraw += OnGameEnd;

        OnPlayerSurrender += OnGameLose;
        OnOppositeSurrender += OnGameWin;

        _gameManager.PlayerTimer.OnTimeOut += OnPlayerTimeOut;
        // 상대방의 시간패는 클라에서 판단하지 않으므로 이벤트 구독 X
        OnPlayerTimeOut += OnGameLose;
        OnOppositeTimeOut += OnGameWin;
        
        OnOppositeDisconnectedWin += OnGameWin;
        OnPlayerDisconnectedLose += OnGameLose;

        _gameManager.BoardInform.BlackWin += _gameManager.IsPlayerBlack ? OnGameWin : OnGameLose;
        _gameManager.BoardInform.WhiteWin += _gameManager.IsPlayerBlack ? OnGameLose : OnGameWin;
        _gameManager.BoardInform.OverMaxTurn += OnGameDraw;
    }

    public void StartGame() => OnGameStart!.Invoke();

    public void StartSweeping() => OnStartSweeping!.Invoke();
    
    public void PlayerSurrendered() => OnPlayerSurrender!.Invoke();

    public void OppositeSurrendered() => OnOppositeSurrender!.Invoke();

    public void PlayerByoyomiPurchase() => OnPlayerByoyomiPurchase!.Invoke(_gameManager.ByoyomiPurchaseAmount);

    public void OppositeByoyomiPurchase() => OnOppositeByoyomiPurchase!.Invoke(_gameManager.ByoyomiPurchaseAmount);

    public void TakeBack() => OnTakeBack!.Invoke();
}
