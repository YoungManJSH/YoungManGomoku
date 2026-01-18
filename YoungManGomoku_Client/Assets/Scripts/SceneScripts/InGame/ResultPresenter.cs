using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ResultPresenter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mainText;
    [SerializeField] private TextMeshProUGUI detailText;
    [SerializeField] private Button rematchButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI rematchText;
    [SerializeField] private TextMeshProUGUI rematchTimer;

    [Header("게임 종료 시 재생할 효과음")]
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioClip drawSound;
    
    [Header("결과창에 표시할 텍스트")]
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
    
    [Header("재대결 요청 가능 시간(초)")]
    [SerializeField] private float idlingTime;

    private AudioSource _audioSource;
    private AudioClip _endSound;
    private TextMeshProUGUI _replayText;
    private int _prevTime;
    private long _gameEndTime;
    private bool _isRematchPossible;
    
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _replayText = replayButton.GetComponentInChildren<TextMeshProUGUI>();
        _prevTime = Mathf.CeilToInt(idlingTime);
        rematchTimer.text = _prevTime.ToString();
        _isRematchPossible = true;

        NetworkManager.Instance.OnRequestFailed += OnRequestFailed;
        
        EventManager em = EventManager.Instance;
        em.OnServerReplyFailed += OnServerReplyFailed;
        em.OnGameEnd += async() => await OnGameEnd();
        em.OnGameWin += OnGameWin;
        em.OnGameLose += OnGameLose;
        em.OnGameDraw += OnGameDraw;
        
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

        em.OnWaitingRematch += DisableRematch;
        em.OnWaitingRematch += DisableReplay;
        em.OnRematchFailed += OnRematchFailed;
        
        rematchButton.onClick.AddListener(AcceptRematch);
        replayButton.onClick.AddListener(OpenReplay);
        closeButton.onClick.AddListener(OnCloseButtonClick);
        
        gameObject.SetActive(false);
    }

    private void Update()
    {
        float remainTime = idlingTime -
                           (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _gameEndTime) / 1000f;
        
        if (remainTime <= 0f)
        {
            RejectRematch();
            return;
        }
        
        int remainTimeToInt = Mathf.CeilToInt(remainTime);
        
        if (remainTimeToInt != _prevTime)
        {
            rematchTimer.text = remainTimeToInt.ToString();
            _prevTime = remainTimeToInt;
        }

        if (Input.GetButtonDown("Submit"))
        {
            AcceptRematch();
            return;
        }

        if (Input.GetButtonDown("Cancel"))
        {
            OnCloseButtonClick();
        }
    }

    public void RejectRematch()
    {
        if (_isRematchPossible)
        {
            EventManager.Instance.RequestRematch(isAccept: false);
            DisableRematch();
        }
    }
    
    private void AcceptRematch()
        => EventManager.Instance.RequestRematch(isAccept: true);
    
    private void OnCloseButtonClick()
    {
        RejectRematch();
        gameObject.SetActive(false);
    }
    
    private void OpenReplay()
    {
        RejectRematch();
        SceneLoadManager.LoadScene(SceneLoadManager.SceneType.GiboPlayScene).Cancel();
    }
    
    private void DisableRematch()
    {
        _isRematchPossible = false;
        _prevTime = 0;
        rematchTimer.enabled = false;
        rematchText.color = new Color(1f, 1f, 1f, 0.5f);
        rematchButton.interactable = false;
        enabled = false;
    }

    private void DisableReplay()
    {
        _replayText.color = new Color(1f, 1f, 1f, 0.5f);
        replayButton.interactable = false;
    }

    private void OnRematchFailed()
    {
        DisableRematch();
        
        // 리플레이 버튼 활성화
        _replayText.color = Color.white;
        replayButton.interactable = true;
    }

    private async Awaitable OnGameEnd()
    {
        _gameEndTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        await Awaitable.WaitForSecondsAsync(0.5f);
        
        gameObject.SetActive(true);
        _audioSource.PlayOneShot(_endSound);
        BlinkText(mainText).Cancel();
    }

    private void OnGameWin()
    {
        mainText.text = "승리";
        _endSound = winSound;
    }

    private void OnGameLose()
    {
        mainText.text = "패배";
        _endSound = loseSound;
    }

    private void OnGameDraw()
    {
        mainText.text = "무승부";
        detailText.text = drawText;
        _endSound = drawSound;
    }

    private void OnRequestFailed(RequestError error)
    {
        if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene ||
            EventManager.Instance.IsGameEnd) return;
        
        Debug.LogError($"{error.Result}({error.StatusCode}): {error.Message}, {error.ResponseBody}");
        mainText.text = "통신 실패";
        detailText.text = disconnectedText;
        _endSound = drawSound;
        DisableRematch();
        DisableReplay();
    }

    private void OnServerReplyFailed()
    {
        mainText.text = "통신 에러";
        detailText.text = disconnectedText;
        _endSound = drawSound;
        DisableRematch();
        DisableReplay();
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
