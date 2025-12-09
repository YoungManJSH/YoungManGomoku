using TMPro;
using UnityEngine;

public class ProgressText : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private EventManager eventManager;
    
    private TextMeshProUGUI _progressText;
    
    private void Awake()
    {
        _progressText = GetComponent<TextMeshProUGUI>();

        eventManager.OnPlayerSurrender += () => OnGameOver("기권패");
        eventManager.OnOppositeSurrender += () => OnGameOver("기권승");
        eventManager.OnOppositeDisconnectedWin += () => OnGameOver("접속끊김승");
    }

    public void OnBoardGenerated(Board board, bool isPlayerBlack, GameManager.TimeController playerTime,  GameManager.TimeController oppositeTime)
    {
        board.OnTurnChanged += OnTurnChanged;
        board.BlackWin += () => OnGameOver(isPlayerBlack ? "승리" : "패배");
        board.WhiteWin += () => OnGameOver(isPlayerBlack ? "패배" : "승리");
        playerTime.OnTimeLose += () => OnGameOver("시간패");
        oppositeTime.OnTimeLose += () => OnGameOver("시간승"); // 추후 수정, 시간패는 서버 처리 받아야 함
    }
    
    private void OnTurnChanged(int turn)
        => _progressText.text = $"{turn}수 진행 중";

    private void OnGameOver(string gameResult) 
        => _progressText.text = $"{(gameManager.IsPlayerBlack ? "흑" : "백")} {gameManager.BoardInform.NowTurn}수 {gameResult}";
}
