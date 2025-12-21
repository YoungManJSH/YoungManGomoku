using System;
using System.Threading;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    [SerializeField] private float minScale;
    [SerializeField] private AudioClip countSound;
    [SerializeField] private AudioClip startSound;
    
    private TextMeshProUGUI _countDownText;
    private AudioSource _audioSource;
    private TweenerCore<float, float, FloatOptions> _tween;
    private CancellationTokenSource _cancelToken;

    private void Awake()
    {
        _countDownText = GetComponent<TextMeshProUGUI>();
        _audioSource = GetComponent<AudioSource>();
        _cancelToken = new CancellationTokenSource();

        NetworkManager.Instance.OnRequestFailed += OnRequestFailed;
        EventManager.Instance.OnGameEnd += OnGameEnd;
    }

    private void Start() => StartCountDown(_cancelToken.Token).Cancel();

    private void OnDestroy()
    {
        _cancelToken?.Cancel();
        _cancelToken?.Dispose();
        NetworkManager.Instance.OnRequestFailed -= OnRequestFailed;
        EventManager.Instance.OnGameEnd -= OnGameEnd;
    }

    private async Awaitable StartCountDown(CancellationToken ctn)
    {
        try
        {
            float originSize = _countDownText.fontSize;
            float minSize = originSize * minScale;

            for (int count = 3; count > 0; --count)
            {
                _countDownText.text = count.ToString();
                _countDownText.fontSize = minSize;

                _tween = DOTween.To(getter: () => _countDownText.fontSize,
                    setter: size => _countDownText.fontSize = size,
                    endValue: originSize, duration: 1f).SetEase(Ease.OutSine);

                _audioSource.PlayOneShot(countSound);
                await Awaitable.WaitForSecondsAsync(1f, ctn);
                _tween.Kill();
            }

            _countDownText.text = "Start!!";
            _countDownText.fontSize = minSize;
            _tween = DOTween.To(getter: () => _countDownText.fontSize, setter: size => _countDownText.fontSize = size,
                endValue: originSize, duration: 1f).SetEase(Ease.OutSine);

            _audioSource.PlayOneShot(startSound);
            await Awaitable.WaitForSecondsAsync(1f, ctn);
            _tween.Kill(complete: true);

            _countDownText.enabled = false;
            EventManager.Instance.StartGame();
            Destroy(gameObject, t: 1f);
        }
        catch (OperationCanceledException)
        {
            _tween.Kill();
            _tween = null;
        }
    }

    private void OnRequestFailed(RequestError e) => Destroy(gameObject);
    private void OnGameEnd() => Destroy(gameObject);
}
