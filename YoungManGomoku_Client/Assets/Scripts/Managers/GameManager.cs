using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action OnGameStart;
    public event Action OnGameEnd;
    
    public event Action OnGameWin;
    public event Action OnGameLose;

    public Board BoardInform { get; private set; }

    private bool isPlayerBlack;

    private void Awake()
    {
        Instance = this;
        BoardInform = new Board();

        OnGameWin += () => OnGameEnd!.Invoke();
        OnGameLose += () => OnGameEnd!.Invoke();
        
        isPlayerBlack = true; // 테스트용 임시 초기화, 이후 서버에서 받아온 정보로 결정
    }

    private void Start()
    {
        BoardInform.BlackWin += () => (isPlayerBlack ? OnGameWin : OnGameLose)!.Invoke();
        BoardInform.WhiteWin += () => (isPlayerBlack ? OnGameLose : OnGameWin)!.Invoke();
        enabled = false;
    }

    public void StartGame()
    {
        enabled = true;
        OnGameStart!.Invoke();
    }
}
