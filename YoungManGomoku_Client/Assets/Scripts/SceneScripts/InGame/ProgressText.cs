using TMPro;
using UnityEngine;

public class ProgressText : MonoBehaviour
{
    private TextMeshProUGUI _progressText;
    
    private void Awake()
    {
        _progressText = GetComponent<TextMeshProUGUI>();

        GameManager gm = GameManager.Instance;
        gm.BoardInform.OnTurnChanged += OnTurnChanged;
        
        gm.BoardInform.BlackWin += () => OnGameOver(gm.IsPlayerBlack ? "승리" : "패배");
        gm.BoardInform.WhiteWin += () => OnGameOver(gm.IsPlayerBlack ? "패배" : "승리");
        
        EventManager em = EventManager.Instance;
        em.OnPlayerSurrender += () => OnGameOver("기권패");
        em.OnOppositeSurrender += () => OnGameOver("기권승");
        em.OnPlayerTimeOut += () => OnGameOver("시간패");
        em.OnOppositeTimeOut += () => OnGameOver("시간승");
        em.OnOppositeDisconnectedWin += () => OnGameOver("접속끊김승");
        em.OnGameDraw += () => OnGameOver("무승부");
    }
    
    private void OnTurnChanged(int turn)
        => _progressText.text = $"{turn}수 진행 중";

    private void OnGameOver(string gameResult)
        => _progressText.text = $"{(GameManager.Instance.IsPlayerBlack ? "흑" : "백")} {GameManager.Instance.BoardInform.NowTurn}수 {gameResult}";
}
