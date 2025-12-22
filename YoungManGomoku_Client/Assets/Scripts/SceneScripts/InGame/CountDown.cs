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
    private int _elapsedSecond;
    private float _originFontSize;
    private float _minFontSize;

    private void Awake()
    {
        _countDownText = GetComponent<TextMeshProUGUI>();
        _audioSource = GetComponent<AudioSource>();
        _elapsedSecond = 0;
        _originFontSize = _countDownText.fontSize;
        _minFontSize = _originFontSize * minScale;

        NetworkManager.Instance.OnRequestFailed += OnRequestFailed;
        EventManager.Instance.OnGameEnd += OnGameEnd;
    }

    private void Start()
    {
        _startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        DoTextAnim("3");
        _audioSource.PlayOneShot(countSound);
    }

    private void Update()
    {
        int elapsed = Mathf.FloorToInt((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTime) / 1000f);
        
        if (elapsed > _elapsedSecond)
        {
            _tween.Kill();
            _countDownText.fontSize = _originFontSize;
            
            if (elapsed < 3)
            {
                DoTextAnim((3 - elapsed).ToString());
                _audioSource.PlayOneShot(countSound);
            }
            else if (elapsed == 3)
            {
                DoTextAnim("Start!!");
                _audioSource.PlayOneShot(startSound);
            }
            else
            {
                if (_elapsedSecond < 3)
                {
                    // Start!! 연출을 건너뛴 경우 사운드만 재생
                    _audioSource.PlayOneShot(startSound);
                }
                
                _countDownText.enabled = false;
                EventManager.Instance.StartGame();
                enabled = false;
                Destroy(gameObject, t: 3f);
            }
            
            _elapsedSecond = elapsed;
        }
    }

    private void OnDestroy()
    {
        _tween?.Kill();
        NetworkManager.Instance.OnRequestFailed -= OnRequestFailed;
        EventManager.Instance.OnGameEnd -= OnGameEnd;
    }

    private void DoTextAnim(string text)
    {
        _countDownText.text = text;
        _countDownText.fontSize = _minFontSize;

        _tween = DOTween.To(getter: () => _countDownText.fontSize,
            setter: size => _countDownText.fontSize = size,
            endValue: _originFontSize, duration: 1f).SetEase(Ease.OutSine);
    }

    private void OnRequestFailed(RequestError e) => Destroy(gameObject);
    private void OnGameEnd() => Destroy(gameObject);
}
