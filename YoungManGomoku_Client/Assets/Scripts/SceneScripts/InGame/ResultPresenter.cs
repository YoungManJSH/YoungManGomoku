using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ResultPresenter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mainText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private Button rematchButton;
    [SerializeField] private TextMeshProUGUI rematchText;
    [SerializeField] private TextMeshProUGUI rematchTimer;
    
    [SerializeField] private string playerGomokuText;
    [SerializeField] private string oppositeGomokuText;
    [SerializeField] private string playerUnmovableText;
    [SerializeField] private string oppositeUnmovableText;
    [SerializeField] private string surrenderWinText;
    [SerializeField] private string surrenderLoseText;
    [SerializeField] private string timeWinText;
    [SerializeField] private string timeLoseText;
    [SerializeField] private string oppositeDisconnectedText;
    [SerializeField] private string playerDisconnectedText;
    [SerializeField] private string drawText;
    [SerializeField] private string disconnectedText;
    [SerializeField] private float idlingTime;
    
    private int _prevTime;
    private long _gameEndTime;
    
    private void Awake()
    {
        _prevTime = Mathf.CeilToInt(idlingTime);
        rematchTimer.text = _prevTime.ToString();

        NetworkManager.Instance.OnRequestFailed += OnRequestFailed;
        
        EventManager em = EventManager.Instance;
        em.OnGameEnd += OnGameEnd().Cancel;
        em.OnGameWin += () => mainText.text = "승리";
        em.OnGameLose += () => mainText.text = "패배";
        em.OnGameDraw += () => mainText.text = "무승부";
        em.OnGameDraw += () => detailText.text = drawText;
        
        em.OnPlayerGomoku += () => detailText.text = playerGomokuText;
        em.OnOppositeGomoku += () => detailText.text = oppositeGomokuText;
        em.OnBlackUnmovable += ()
            => detailText.text = GameManager.Instance.IsPlayerBlack ? playerUnmovableText : oppositeUnmovableText;
        em.OnOppositeSurrender += () => detailText.text = surrenderWinText;
        em.OnPlayerSurrender += () => detailText.text = surrenderLoseText;
        em.OnOppositeTimeOut += () => detailText.text = timeWinText;
        em.OnPlayerTimeOut += () => detailText.text = timeLoseText;
        em.OnOppositeDisconnectedWin += () =>
        {
            detailText.text = oppositeDisconnectedText;
            DisableRematch();
        };
        em.OnPlayerDisconnectedLose += () =>
        {
            detailText.text = playerDisconnectedText;
            DisableRematch();
        };

        em.OnServerReplyFailed += () =>
        {
            mainText.text = "통신 실패";
            detailText.text = disconnectedText;
            DisableRematch();
        };
        
        gameObject.SetActive(false);
    }

    private void Update()
    {
        float remainTime = idlingTime -
                           (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _gameEndTime) / 1000f;
        
        if (remainTime <= 0f)
        {
            DisableRematch();
            
            /*TODO: 추후 재대국 신청 취소 처리*/
            
            return;
        }
        
        int remainTimeToInt = Mathf.CeilToInt(remainTime);
        
        if (remainTimeToInt != _prevTime)
        {
            rematchTimer.text = remainTimeToInt.ToString();
            _prevTime = remainTimeToInt;
        }
    }
    
    public void OpenReplay()
    {
        /*TODO: 추후 재대국 신청 취소 처리*/
        SceneLoadManager.LoadScene(SceneLoadManager.SceneType.GiboPlayScene);
    }

    public void AcceptRematch()
    {
        /*TODO: 추후 재대국 신청 수락 동작*/
        SceneLoadManager.LoadScene(SceneLoadManager.SceneType.IngameScene);
    }

    private void DisableRematch()
    {
        _prevTime = 0;
        rematchTimer.enabled = false;
        rematchText.color = new Color(1f, 1f, 1f, 0.5f);
        rematchButton.interactable = false;
        enabled = false;
    }
    
    private async Awaitable OnGameEnd()
    {
        _gameEndTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        await Awaitable.WaitForSecondsAsync(0.5f);
        gameObject.SetActive(true);
        BlinkText(mainText).Cancel();
    }

    private void OnRequestFailed(RequestError error)
    {
        Debug.LogError($"{error.Result}({error.StatusCode}): {error.Message}, {error.ResponseBody}");
        mainText.text = "통신 실패";
        detailText.text = disconnectedText;
        DisableRematch();
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
