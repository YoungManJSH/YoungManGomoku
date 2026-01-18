using System;
using System.Threading;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToastBoxController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageUI;
    [SerializeField] private StoneMover stoneMover;
    [SerializeField] private string forbiddenMoveString;
    [SerializeField] private string waitingTakeBackString;
    [SerializeField] private string takeBackConfirmString;
    [SerializeField] private string takeBackRejectedString;
    [SerializeField] private string waitingRematchString;
    [SerializeField] private string rematchFailedString;
    [SerializeField] private float toastTime;
    [SerializeField] private float fadeDuration;

    private const int COUNTS = 3;
    
    private TweenerCore<Color, Color, ColorOptions>[] _openAnims;
    private TweenerCore<Color, Color, ColorOptions>[] _closeAnims;
    private CancellationTokenSource _cts;
    private string[] _waitingTakeBackStrings;
    private string[] _waitingRematchStrings;
    private bool _isMyTakeback;
    
    private void Awake()
    {
        Image background = GetComponent<Image>();
        
        _closeAnims = new TweenerCore<Color, Color, ColorOptions>[2];
        _closeAnims[0] = background.DOFade(endValue: 0f, fadeDuration).SetAutoKill(false).Pause();
        _closeAnims[1] = messageUI.DOFade(endValue: 0f, fadeDuration)
            .OnComplete(() => gameObject.SetActive(false)).SetAutoKill(false).Pause();

        float bgAlpha = background.color.a;
        float messageAlpha = messageUI.alpha;
        background.color = Color.clear;
        messageUI.alpha = 0f;
        
        _openAnims = new TweenerCore<Color, Color, ColorOptions>[2];
        _openAnims[0] = background.DOFade(endValue:bgAlpha, fadeDuration).SetAutoKill(false).Pause();
        _openAnims[1] = messageUI.DOFade(endValue:messageAlpha, fadeDuration).SetAutoKill(false).Pause();
        
        #region 대기용 문자열 소스 생성
        _waitingTakeBackStrings = new string[COUNTS];
        _waitingRematchStrings = new string[COUNTS];

        for (int i = 0; i < COUNTS; ++i)
        {
            var dots = new string('·', i + 1);
            _waitingTakeBackStrings[i] = $"{dots}{waitingTakeBackString}{dots}";
            _waitingRematchStrings[i] = $"{dots}{waitingRematchString}{dots}";
        }
        #endregion

        stoneMover.OnForbiddenMoveRejected += OnForbiddenMove;
        
        EventManager em = EventManager.Instance;
        em.OnTakeBackRequested += OnTakeBackRequested;
        em.OnTakeBack += OnTakeBack;
        em.OnWaitingRematch += OnWaitingRematch;
        em.OnRematchFailed += OnRematchFailed;

        gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        foreach (var openAnim in _openAnims)
            openAnim.Kill();
        
        foreach (var closeAnim in _closeAnims)
            closeAnim.Kill();
    }
    
    private void OnTakeBackRequested(bool isMyRequest)
    {
        _isMyTakeback = isMyRequest;
        
        if (isMyRequest)
        {
            StartStringChange(_waitingTakeBackStrings).Cancel();
            gameObject.SetActive(true);
        }
    }
    
    private void OnWaitingRematch()
    {
        StartStringChange(_waitingRematchStrings).Cancel();
        gameObject.SetActive(true);
    }

    private async void OpenToastMessage(string message)
    {
        try
        {
            RestartAnims();

            messageUI.text = message;
            gameObject.SetActive(true);

            _cts = new CancellationTokenSource();
            await Awaitable.WaitForSecondsAsync(toastTime, _cts.Token);

            foreach (var closeAnim in _closeAnims)
                closeAnim.Restart();
        }
        catch (Exception e)
        {
            if (e is OperationCanceledException) return;

            Debug.LogError($"토스트 메시지 박스 에러{message}: {e}");
        }
    }

    private void OnForbiddenMove()
        => OpenToastMessage(forbiddenMoveString);

    private void OnTakeBack(bool isAccepted)
    {
        if (isAccepted)
            OpenToastMessage(takeBackConfirmString);
        // 무르기 거절 시에는 내 요청이었을 때만 거절 메시지 출력
        else if (_isMyTakeback)
            OpenToastMessage(takeBackRejectedString);
    }

    private void OnRematchFailed()
        => OpenToastMessage(rematchFailedString);
    
    private async Awaitable StartStringChange(string[] strings)
    {
        try
        {
            RestartAnims();
            _cts = new CancellationTokenSource();
            
            while (true)
            {
                foreach (string str in strings)
                {
                    messageUI.text = str;
                    await Awaitable.WaitForSecondsAsync(0.5f, _cts.Token);
                }
            }
        }
        catch (OperationCanceledException) { }
    }
    
    /// <summary>기존 진행 중이던 연출을 취소 후 재시작</summary>
    private void RestartAnims()
    {
        foreach (var closeAnim in _closeAnims)
            closeAnim.Pause();
        
        foreach (var openAnim in _openAnims)
            openAnim.Restart();

        if (_cts == null) return;
        
        _cts.Cancel();
        _cts = null;
    }
}
