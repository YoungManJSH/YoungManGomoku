using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public event Action OnGameStart;
    public event Action OnGameEnd;
    
    public event Action OnGameWin;
    public event Action OnGameLose;
    
    public event Action OnPlayerSurrender;
    public event Action OnOppositeSurrender;
    
    public event Action OnOppositeDisconnectedWin;

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

    private void Start()
    {
        OnGameWin += OnGameEnd;
        OnGameLose += OnGameEnd;

        OnPlayerSurrender += OnGameLose;
        OnOppositeSurrender += OnGameWin;

        _gameManager.PlayerTime.OnTimeLose += OnGameLose;
        _gameManager.OppositeTime.OnTimeLose += OnGameWin; // 추후 수정, 상대방 시간패 처리는 서버에서 받아야 함

        OnOppositeDisconnectedWin += OnGameWin;

        _gameManager.BoardInform.BlackWin += _gameManager.IsPlayerBlack ? OnGameWin : OnGameLose;
        _gameManager.BoardInform.WhiteWin += _gameManager.IsPlayerBlack ? OnGameLose : OnGameWin;
    }

    public void StartGame() => OnGameStart!.Invoke();

    public void PlayerSurrendered() => OnPlayerSurrender!.Invoke();
}
