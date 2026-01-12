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
    [SerializeField] private string waitingTakeBackString;
    [SerializeField] private string takeBackConfirmString;
    [SerializeField] private string takeBackRejectedString;
    [SerializeField] private string waitingRematchString;
    [SerializeField] private string rematchFailedString;
    [SerializeField] private float toastTime;

    private const int COUNTS = 3;
    
    private TweenerCore<Color, Color, ColorOptions>[] _toastOpens;
    private TweenerCore<Color, Color, ColorOptions>[] _toastCloses;
    private CancellationTokenSource _cts;
    private string[] _waitingTakeBackStrings;
    private string[] _waitingRematchStrings;
    private bool _isMyTakeback;
    
    private void Awake()
    {
        Image background = GetComponent<Image>();
        
        _toastCloses = new TweenerCore<Color, Color, ColorOptions>[2];
        _toastCloses[0] = background.DOFade(endValue: 0f, duration: 0.5f).SetAutoKill(false).Pause();
        _toastCloses[1] = messageUI.DOFade(endValue:0f, duration:0.5f)
            .OnComplete(() => gameObject.SetActive(false)).SetAutoKill(false).Pause();

        float bgAlpha = background.color.a;
        float messageAlpha = messageUI.alpha;
        background.color = Color.clear;
        messageUI.alpha = 0f;
        
        _toastOpens = new TweenerCore<Color, Color, ColorOptions>[2];
        _toastOpens[0] = background.DOFade(endValue:bgAlpha, duration:0.5f).SetAutoKill(false).Pause();
        _toastOpens[1] = messageUI.DOFade(endValue:messageAlpha, duration:0.5f).SetAutoKill(false).Pause();
        
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
        
        EventManager em = EventManager.Instance;
        em.OnTakeBackRequested += OnTakeBackRequested;
        em.OnTakeBack += OnTakeBack;
        em.OnWaitingRematch += OnWaitingRematch;
        em.OnRematchFailed += OnRematchFailed;

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        StopAwaitable();

        foreach (var closeAnim in _toastCloses)
            closeAnim.Pause();
        
        foreach (var openAnim in _toastOpens)
            openAnim.Restart();
    }

    private void OnDestroy()
    {
        foreach (var openAnim in _toastOpens)
            openAnim.Kill();
        
        foreach (var closeAnim in _toastCloses)
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
    

    private async void OnTakeBack(bool isAccepted)
    {
        try
        {
            StopAwaitable();

            if (isAccepted)
                messageUI.text = takeBackConfirmString;
            else if (_isMyTakeback)
                messageUI.text = takeBackRejectedString;

            if (isAccepted || _isMyTakeback)
            {
                gameObject.SetActive(true);
                
                _cts = new  CancellationTokenSource();
                await Awaitable.WaitForSecondsAsync(toastTime, _cts.Token);
                
                foreach (var closeAnim in _toastCloses)
                    closeAnim.Restart();
            }
        }
        catch (Exception e)
        {
            if (e is not OperationCanceledException)
                Debug.LogError($"토스트 메시지 박스 에러: {e}");
        }
    }

    private void OnRematchFailed()
    {
        /* case1: OnWaitingRematch 중에 응답받은 경우
         * case2: 재대결 요청 전에 먼저 응답받은 경우 */
        
        /* TODO: 투명도 조절 애니메이션 넣기
         * 3초 정도 메시지 띄웠다가 사라지도록 조정하기 */ 
    }
    
    private async Awaitable StartStringChange(string[] strings)
    {
        try
        {
            _cts?.Dispose();
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
    
    /// <summary>Awaitable을 활용한 연출의 취소</summary>
    private void StopAwaitable()
    {
        if (_cts == null) return;
        
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }
}
