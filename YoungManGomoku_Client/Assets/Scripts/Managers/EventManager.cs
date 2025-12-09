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

    private GameManager _gameManager;

    private void Awake()
    {
        _gameManager = GetComponent<GameManager>();
    }

    private void Start()
    {
        // 각각의 이벤트들은 다른 곳의 Awake 단계에서 구독이 완료되어야 함.
        // 이벤트 델리게이트는 Immutable이므로 이벤트끼리 연결하는 순서를 유의하지 않으면
        // 최신화되지 않은 개체가 구독될 수 있음.
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
