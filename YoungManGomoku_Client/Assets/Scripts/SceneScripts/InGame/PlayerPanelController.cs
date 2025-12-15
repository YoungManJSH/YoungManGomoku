using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class PlayerPanelController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mainTimer;
    [SerializeField] private TextMeshProUGUI byoyomiTimer;
    [SerializeField] private TextMeshProUGUI byoyomiCount;
    [SerializeField] private Image stoneImage;
    [SerializeField] private Image clockIcon;
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI recordText;
    [SerializeField] private StoneMoveController stoneMoveController;
    [SerializeField] private AudioClip useByoyomiSound;
    [SerializeField] private AudioClip byoyomiTickSound;
    [SerializeField] private AudioClip byoyomiWarningSound;
    [SerializeField] private AudioClip byoyomiPurchaseSound;
    [SerializeField] private TMP_FontAsset glowFont;
    [SerializeField] private Sprite otherColorStone;
    [SerializeField] private bool isPlayer;

    private TimeController _myTimer;
    private bool _isThisBlack;
    private AudioSource _audioSource;
    private TMP_FontAsset _originFont;
    private TweenerCore<float, float, FloatOptions> _tween;
    private int _prevTime;
    private string _initByoyomiSecondText;
    private bool _isByoyomi;
    private bool _isLastByoyomi;
    private Color _translucent;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _originFont = byoyomiCount.font;
        _isByoyomi = false;
        _prevTime = -1;

        _translucent = new Color(1f, 1f, 1f, 0.3f);
        byoyomiTimer.color = _translucent;
        byoyomiCount.color = _translucent;
        clockIcon.color = _translucent;
        
        GameManager gm = GameManager.Instance;
        _isThisBlack = isPlayer == gm.IsPlayerBlack;
        if (gm.IsPlayerBlack is false) stoneImage.sprite = otherColorStone;
        _myTimer = isPlayer ? gm.PlayerTime : gm.OppositeTime;
        _isLastByoyomi = _myTimer.ByoyomiCount == 1;
        _myTimer.StartByoyomi += OnStartByoyomi;
        _myTimer.UseByoyomi += OnUseByoyomi;
        _myTimer.OnTimeLose += OnTimeLose;

        BasicPlayerData myUser = isPlayer ? gm.player : gm.oppositePlayer;
        InputUserInform(myUser.name, myUser.win, myUser.draw, myUser.lose, myUser.rating);
        
        EventManager em = EventManager.Instance;
        em.OnGameEnd += () => enabled = false;
        
        if (isPlayer) em.OnPlayerByoyomiPurchase += OnByoyomiPurchase;
        else em.OnOppositeByoyomiPurchase += OnByoyomiPurchase;
        
        stoneMoveController.OnStoneMove += OnTurnChanged;
    }

    private void Start()
    {
        int mainTime = Mathf.CeilToInt(_myTimer.MainTime);
        mainTimer.text = $"{mainTime / 60:D2}:{mainTime % 60:D2}";
        _initByoyomiSecondText = Mathf.CeilToInt(_myTimer.initByoyomiSeconds).ToString("D2");
        byoyomiTimer.text = _initByoyomiSecondText;
        byoyomiCount.text = $"{_myTimer.ByoyomiCount}회";

        enabled = false;
    }

    private void OnDisable() => StopGlowEffect();
    
    private void Update()
    {
        int remainTime;

        if (_isByoyomi is false)
        {
            remainTime = Mathf.CeilToInt(_myTimer.MainTime);
            if (remainTime == _prevTime) return;

            mainTimer.text = $"{remainTime / 60:D2}:{remainTime % 60:D2}";
            _prevTime = remainTime;
            return;
        }

        remainTime = Mathf.CeilToInt(_myTimer.Byoyomi);
        if (remainTime == _prevTime) return;

        byoyomiTimer.text = $"{remainTime:D2}";
        _prevTime = remainTime;
        
        if (remainTime == 9)
        {
            byoyomiTimer.font = glowFont;
        }

        if (isPlayer)
        {
            if (_isLastByoyomi && remainTime < 5)
            {
                _audioSource.PlayOneShot(byoyomiWarningSound);
                StartGlowEffect(byoyomiTimer.fontMaterial, loops: 1);
            }
            else if (remainTime < 10)
            {
                _audioSource.PlayOneShot(byoyomiTickSound);
            }
        }
    }

    private void InputUserInform(string nickname, uint win, uint draw, uint lose, float rating)
    {
        nicknameText.text = nickname;
        recordText.text = $"{win}승 {draw}무 {lose}패 ({rating:F1}pt)";
    }
    
    private void StartGlowEffect(Material fontMat, int loops)
    {
        StopGlowEffect();
        
        _tween = fontMat.DOFloat(endValue: 0.8f, ShaderUtilities.ID_GlowOuter, duration: 0.5f)
            .SetLoops(loops * 2, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    private void StopGlowEffect()
    {
        if (_tween != null)
        {
            _tween.Rewind();
            _tween.Kill();
            _tween = null;
        }
    }

    private void OnTurnChanged(bool isBlackTurn)
    {
        enabled = isBlackTurn == _isThisBlack;

        if (enabled)
        {
            stoneImage.color = Color.white;

            if (_isByoyomi)
            {
                byoyomiTimer.color = Color.white;
                byoyomiCount.color = Color.white;
                clockIcon.color = Color.white;
            }
            else
            {
                mainTimer.color = Color.white;
            }
        }
        else
        {
            stoneImage.color = _translucent;

            if (_isByoyomi)
            {
                StopGlowEffect();
                byoyomiTimer.font = _originFont;
                byoyomiTimer.text = _initByoyomiSecondText;
                byoyomiTimer.color = _translucent;
                byoyomiCount.color = _translucent;
                clockIcon.color = _translucent;
            }
            else
            {
                mainTimer.color = _translucent;
            }
        }
    }

    private void OnStartByoyomi()
    {
        _isByoyomi = true;
        mainTimer.text = "00:00";
        mainTimer.color = _translucent;

        byoyomiTimer.color = Color.white;
        byoyomiCount.color = Color.white;
        clockIcon.color = Color.white;

        if (isPlayer) _audioSource.PlayOneShot(useByoyomiSound);
    }

    private void OnUseByoyomi()
    {
        StopGlowEffect();
        byoyomiTimer.font = _originFont;
        byoyomiCount.text = $"{_myTimer.ByoyomiCount}회";
        
        if (_myTimer.ByoyomiCount == 1)
        {
            _isLastByoyomi = true;
            byoyomiCount.font = glowFont;
            StartGlowEffect(byoyomiCount.fontMaterial, loops: 2);
            if (isPlayer) _audioSource.PlayOneShot(byoyomiWarningSound);
        }
        else if (isPlayer)
        {
            _audioSource.PlayOneShot(useByoyomiSound);
        }
    }

    private void OnByoyomiPurchase(int amount)
    {
        StopGlowEffect();
        byoyomiTimer.font = _originFont;
        byoyomiCount.font = _originFont;
        byoyomiCount.text = $"{(_myTimer.IsByoyomiPurchased ? _myTimer.ByoyomiCount : _myTimer.ByoyomiCount + amount)}회";
        _audioSource.PlayOneShot(byoyomiPurchaseSound);
        _isLastByoyomi = false;
    }

    private void OnTimeLose()
    {
        byoyomiTimer.text = "00";
        byoyomiCount.text = "0회";
    }
}