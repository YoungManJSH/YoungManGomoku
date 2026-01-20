using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountDown : MonoBehaviour
{
    [SerializeField] private float minScale;
    [SerializeField] private AudioClip countSound;
    [SerializeField] private AudioClip startSound;
    [SerializeField] private Image playerStoneColor;
    [SerializeField] private Sprite whiteStoneSprite;
    [SerializeField] private IngameBoardScaler boardScaler;
    
    private TextMeshProUGUI _countDownText;
    private AudioSource _audioSource;
    private TweenerCore<float, float, FloatOptions> _tween;
    private Sequence _stoneSequence;
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
        
        EventManager.Instance.OnGameEnd += OnGameEnd;
        boardScaler.OnBoardScaled += ResizeStoneObject;

        if (GameManager.Instance.IsPlayerBlack is false)
        {
            playerStoneColor.sprite = whiteStoneSprite;
        }
    }

    private void Start()
    {
        _startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _isStartPrinted = false;
        DoTextAnim("Ready?");
        _audioSource.PlayOneShot(countSound);
        
        _stoneSequence = DOTween.Sequence();
        _stoneSequence.Append(playerStoneColor.rectTransform
            .DOScale(endValue:1.5f, duration: 0.4f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
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
            
            _stoneSequence.Kill();
            _stoneSequence = DOTween.Sequence();

            _stoneSequence.Append(playerStoneColor.rectTransform
                .DORotate(new Vector3(0, 0, 360f), duration: 1f, RotateMode.FastBeyond360).
                SetEase(Ease.InExpo));
            _stoneSequence.Join(playerStoneColor.rectTransform.
                DOScale(Vector3.zero, duration: 1f).SetEase(Ease.InExpo));
            return;
        }

        // Start!! 연출을 건너뛰고 2.2초 이상 경과한 경우
        if (_isStartPrinted is false)
            _audioSource.PlayOneShot(startSound);

        enabled = false;
        _countDownText.enabled = false;
        _stoneSequence.Kill();
        _stoneSequence = null;
        playerStoneColor.gameObject.SetActive(false);
        EventManager.Instance.StartGame();
    }

    private void OnDisable()
    {
        _tween?.Kill();
        _stoneSequence?.Kill();
    }

    private void ResizeStoneObject()
        => playerStoneColor.rectTransform.sizeDelta =
            Vector2.one * (boardScaler.IsWide ? 160f : 100f);
    
    private void DoTextAnim(string text)
    {
        _countDownText.text = text;
        _countDownText.fontSize = _minFontSize;

        _tween = DOTween.To(getter: () => _countDownText.fontSize,
            setter: size => _countDownText.fontSize = size,
            endValue: _originFontSize, duration: 1f).SetEase(Ease.OutSine);
    }

    private void OnGameEnd() => gameObject.SetActive(false);
}
