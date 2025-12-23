using TMPro;
using UnityEngine;

public class ProgressText : MonoBehaviour
{
    private TextMeshProUGUI _progressText;
    
    private void Awake()
    {
        _progressText = GetComponent<TextMeshProUGUI>();

        NetworkManager.Instance.OnRequestFailed += OnRequestFailed;
        
        GameManager gm = GameManager.Instance;
        gm.BoardInform.OnTurnChanged += OnTurnChanged;
        
        EventManager em = EventManager.Instance;
        em.OnPlayerGomoku += () => OnGameOver("승리");
        em.OnOppositeGomoku += () => OnGameOver("패배");
        em.OnBlackUnmovable += () => OnGameOver(gm.IsPlayerBlack ? "금수패" : "금수승");
        em.OnPlayerSurrender += () => OnGameOver("기권패");
        em.OnOppositeSurrender += () => OnGameOver("기권승");
        em.OnPlayerTimeOut += () => OnGameOver("시간패");
        em.OnOppositeTimeOut += () => OnGameOver("시간승");
        em.OnOppositeDisconnectedWin += () => OnGameOver("접속끊김승");
        em.OnGameDraw += () => OnGameOver("무승부");
        
        /* TODO: 서버 연결 시 주석 해제
        if (matchResult.MatchingSuccess is false ||
            matchResult.MyStoneColorType is StoneColorType.Empty)
        {
            _progressText.text = "-매칭 실패-";
        } */
    }
    
    private void OnTurnChanged(int turn)
        => _progressText.text = $"{turn}수 진행 중";

    private void OnGameOver(string gameResult)
        => _progressText.text = $"{(GameManager.Instance.IsPlayerBlack ? "흑" : "백")} {GameManager.Instance.BoardInform.NowTurn}수 {gameResult}";

    private void OnRequestFailed(RequestError _)
        => _progressText.text = "-통신 오류-";
}
