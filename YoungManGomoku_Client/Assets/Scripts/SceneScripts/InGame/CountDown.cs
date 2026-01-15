using System;
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
    private long _startTime;
    private float _originFontSize;
    private float _minFontSize;
    private bool _isStartPrinted;

    private void Awake()
    {
        _countDownText = GetComponent<TextMeshProUGUI>();
        _audioSource = GetComponent<AudioSource>();
        _originFontSize = _countDownText.fontSize;
        _minFontSize = _originFontSize * minScale;

        NetworkManager.Instance.OnRequestFailed += OnRequestFailed;
        EventManager.Instance.OnGameEnd += OnGameEnd;
    }

    private void Start()
    {
        _startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _isStartPrinted = false;
        DoTextAnim("Ready?");
        _audioSource.PlayOneShot(countSound);
    }

    private void Update()
    {
        long elapsedMS = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTime;

        if (elapsedMS < 1200L) return;

        if (_isStartPrinted && elapsedMS < 2200L) return;

        _tween.Kill();
        _tween = null;

        if (elapsedMS < 2200L)
        {
            DoTextAnim("Start!!");
            _audioSource.PlayOneShot(startSound);
            _isStartPrinted = true;
            return;
        }

        // Start!! 연출을 건너뛰고 2.5초 이상 경과한 경우
        if (_isStartPrinted is false)
        {
            _audioSource.PlayOneShot(startSound);
        }

        enabled = false;
        _countDownText.enabled = false;
        EventManager.Instance.StartGame();
    }

    private void OnDisable() => _tween?.Kill();
    
    private void DoTextAnim(string text)
    {
        _countDownText.text = text;
        _countDownText.fontSize = _minFontSize;

        _tween = DOTween.To(getter: () => _countDownText.fontSize,
            setter: size => _countDownText.fontSize = size,
            endValue: _originFontSize, duration: 1f).SetEase(Ease.OutSine);
    }

    private void OnRequestFailed(RequestError e) => gameObject.SetActive(false);
    private void OnGameEnd() => gameObject.SetActive(false);
}
