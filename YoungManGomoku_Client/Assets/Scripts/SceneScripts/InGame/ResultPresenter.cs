using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultPresenter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mainText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private Button rematchButton;
    [SerializeField] private TextMeshProUGUI rematchText;
    [SerializeField] private TextMeshProUGUI rematchTimer;
    
    [SerializeField] private string blackWinText;
    [SerializeField] private string whiteWinText;
    [SerializeField] private string surrenderWinText;
    [SerializeField] private string surrenderLoseText;
    [SerializeField] private string timeWinText;
    [SerializeField] private string timeLoseText;
    [SerializeField] private string oppositeDisconnectedText;
    [SerializeField] private string playerDisconnectedText;
    [SerializeField] private string drawText;
    [SerializeField] private float idlingTime;
    
    private float _remainTime;
    private int _prevTime;
    
    private void Awake()
    {
        _remainTime = idlingTime;
        _prevTime = Mathf.CeilToInt(idlingTime);
        rematchTimer.text = _prevTime.ToString();
        
        GameManager gm = GameManager.Instance;
        gm.BoardInform.BlackWin += () => detailText.text = blackWinText;
        gm.BoardInform.WhiteWin += () => detailText.text = whiteWinText;
        
        EventManager em = EventManager.Instance;
        em.OnGameEnd += OnGameEnd;
        em.OnGameWin += () => mainText.text = "승리";
        em.OnGameLose += () => mainText.text = "패배";
        em.OnGameDraw += () => mainText.text = "무승부";
        
        em.OnGameDraw += () => detailText.text = drawText;
        em.OnOppositeSurrender += () => detailText.text = surrenderWinText;
        em.OnPlayerSurrender += () => detailText.text = surrenderLoseText;
        em.OnOppositeTimeOut += () => detailText.text = timeWinText;
        em.OnPlayerTimeOut += () => detailText.text = timeLoseText;
        em.OnOppositeDisconnectedWin += () =>
        {
            _remainTime = 0f;
            rematchTimer.enabled = false;
            detailText.text = oppositeDisconnectedText;
        };
        em.OnPlayerDisconnectedLose += () =>
        {
            _remainTime = 0f;
            rematchTimer.enabled = false;
            detailText.text = playerDisconnectedText;
        };
        
        gameObject.SetActive(false);
    }

    private void Update()
    {
        _remainTime -= Time.deltaTime;
        
        if (_remainTime <= 0)
        {
            _prevTime = 0;
            rematchTimer.text = "0";
            rematchTimer.color = new Color(1f, 1f, 1f, 0.5f);
            rematchText.color = new Color(1f, 1f, 1f, 0.5f);
            rematchButton.interactable = false;
            
            /*TODO: 추후 재대국 신청 취소 처리*/
            return;
        }
        
        int remainTimeToInt = Mathf.CeilToInt(_remainTime);
        
        if (remainTimeToInt != _prevTime)
        {
            rematchTimer.text = remainTimeToInt.ToString();
            _prevTime = remainTimeToInt;
        }
    }

    public void OpenReplay()
    {
        /*TODO: 추후 재대국 신청 취소 처리*/
        SceneManager.LoadScene("Scenes/3.InGame/GiboPlayScene");
    }

    public void AcceptRematch()
    {
        /*TODO: 추후 재대국 신청 수락 동작*/
        SceneManager.LoadScene("Scenes/3.InGame/InGameScene");
    }

    private void OnGameEnd()
    {
        gameObject.SetActive(true);
        BlinkText(mainText).Cancel();
    }
    
    private async Awaitable BlinkText(TextMeshProUGUI text)
    {
        for (int i = 0; i < 3; ++i)
        {
            text.enabled = true;
            await Awaitable.WaitForSecondsAsync(0.5f);
            text.enabled = false;
            await Awaitable.WaitForSecondsAsync(0.2f);
        }

        text.enabled = true;
    }
}
