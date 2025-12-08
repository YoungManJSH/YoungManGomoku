using TMPro;
using UnityEngine;

public class ProgressText : MonoBehaviour
{
    private TextMeshProUGUI progressText;

    private void Awake()
    {
        progressText = GetComponent<TextMeshProUGUI>();
    }
    
    private void Start()
    {
        var gm = GameManager.Instance;
        
        gm.BoardInform.OnTurnChanged += OnTurnChanged;
        gm.BoardInform.BlackWin += () => OnGameOver(gm.IsPlayerBlack ? "승리" : "패배");
        gm.BoardInform.WhiteWin += () => OnGameOver(gm.IsPlayerBlack ? "패배" : "승리");
        gm.OnPlayerSurrender += () => OnGameOver("기권패");
        gm.OnOppositeSurrender += () => OnGameOver("기권승");
        gm.PlayerTime.OnTimeLose += () => OnGameOver("시간패");
        gm.OppositeTime.OnTimeLose += () => OnGameOver("시간승"); // 추후 수정, 시간패는 서버 처리 받아야 함
        gm.OnOppositeDisconnectedWin += () => OnGameOver("접속끊김승");
    }
    
    private void OnTurnChanged(int turn)
        => progressText.text = $"{turn}수 진행 중";

    private void OnGameOver(string gameResult)
    {
        var gm = GameManager.Instance;
        progressText.text = $"{(gm.IsPlayerBlack ? "흑" : "백")} {gm.BoardInform.NowTurn}수 {gameResult}";
    }
}
