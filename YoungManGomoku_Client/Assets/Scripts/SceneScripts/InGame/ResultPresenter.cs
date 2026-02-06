using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class ResultPresenter : MonoBehaviour
{
    [Header("재대결 요청 가능 시간(초)")]
    public const float RematchWaitingTime = 10f;
    
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
    [SerializeField] private GameResultTexts gameResultTexts;
    [SerializeField] private string disconnectedText;

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
        _prevTime = Mathf.CeilToInt(RematchWaitingTime);
        rematchTimer.text = _prevTime.ToString();
        _isRematchPossible = true;

        NetworkManager.Instance.OnRequestFailed += OnRequestFailed;
        
        EventManager em = EventManager.Instance;
        em.OnServerReplyFailed += OnServerReplyFailed;
        em.OnGameEnd += async() => await OnGameEnd();
        _endSound = drawSound; // 승패가 아닌 모든 경우에 무승부 사운드로 처리
        em.OnGameWin += () => _endSound = winSound;
        em.OnGameLose += () => _endSound = loseSound;
        
        em.OnPlayerGomoku += () => gameResultTexts.ApplyText(mainText, detailText, GameEndCode.GomokuWin);
        em.OnOppositeGomoku += () => gameResultTexts.ApplyText(mainText, detailText, GameEndCode.GomokuLose);
        em.OnBlackUnmovable += () => gameResultTexts.ApplyText(mainText, detailText, GameEndCode.BlackUnmovable);
        em.OnOppositeSurrender += () => gameResultTexts.ApplyText(mainText, detailText, GameEndCode.SurrenderWin);
        em.OnPlayerSurrender += () => gameResultTexts.ApplyText(mainText, detailText, GameEndCode.SurrenderLose);
        em.OnOppositeTimeOut += () => gameResultTexts.ApplyText(mainText, detailText, GameEndCode.TimeOutWin);
        em.OnPlayerTimeOut += () => gameResultTexts.ApplyText(mainText, detailText, GameEndCode.TimeOutLose);
        em.OnOppositeDisconnectedWin += () =>
        {
            gameResultTexts.ApplyText(mainText, detailText, GameEndCode.DisconnectedWin);
            DisableRematch();
        };
        em.OnPlayerDisconnectedLose += () =>
        {
            gameResultTexts.ApplyText(mainText, detailText, GameEndCode.DisconnectedLose);
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
        float remainTime =
            RematchWaitingTime - (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _gameEndTime) / 1000f;
        
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
        
        /* RematchFailed는 재대결 가능 상태에서만 발생함
         * 재대결 가능 상태는 게임이 정상적으로 끝난 상황이므로,
         * 리플레이 버튼 역시 활성화된 상태여야 함.
         * 따라서 재대결 수락 요청을 보낼 때 비활성화 된 버튼을 다시 활성화 시켜야 함. */
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

    private void OnRequestFailed(RequestError error)
    {
        if (SceneLoadManager.NowScene != SceneLoadManager.SceneType.IngameScene ||
            EventManager.Instance.IsGameEnd) return;
        
        Debug.LogError($"{error.Result}({error.StatusCode}): {error.Message}, {error.ResponseBody}");
        mainText.text = "통신 실패"; // 서버 통신 자체를 실패한 상황
        detailText.text = disconnectedText;
        DisableRematch();
        DisableReplay();
    }

    private void OnServerReplyFailed()
    {
        mainText.text = "통신 에러"; // 서버의 응답에 논리적인 결함이 있음을 의미
        detailText.text = disconnectedText;
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
