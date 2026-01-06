using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;

public class PlayerPanelController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mainTimer;
    [SerializeField] private TextMeshProUGUI byoyomiTimer;
    [SerializeField] private TextMeshProUGUI byoyomiCount;
    [SerializeField] private Image stoneImage;
    [SerializeField] private Image clockIcon;
    [SerializeField] private Image profile;
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI recordText;
    [SerializeField] private StoneMover stoneMover;
    [SerializeField] private AudioClip useByoyomiSound;
    [SerializeField] private AudioClip byoyomiTickSound;
    [SerializeField] private AudioClip byoyomiWarningSound;
    [SerializeField] private AudioClip byoyomiPurchaseSound;
    [SerializeField] private TMP_FontAsset glowFont;
    [SerializeField, Tooltip("플레이어가 백일 경우 교체할 돌 스프라이트")]
    private Sprite otherColorStone;
    [SerializeField, Tooltip("적용할 프로필 이미지들")]
    private ProfileImages profileImages;
    [SerializeField] private bool isPlayer;

    private const float TOLERANCE = 0.7f;
    private static readonly Color Translucent = new (1f, 1f, 1f, 0.3f);

    public event Action OnLastByoyomi;
    
    private UserTimer _myTimer;
    private bool _isThisBlack;
    private AudioSource _audioSource;
    private TMP_FontAsset _originFont;
    private TweenerCore<float, float, FloatOptions> _tween;
    private int _prevTime;
    private int _prevByoyomiCount;
    private string _initByoyomiSecondText;
    private bool _isByoyomi;
    private bool _isGameEnd;
    private CancellationTokenSource _byoyomiUseCts;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _originFont = byoyomiCount.font;
        _prevTime = -1;
        _isGameEnd = false;
        
        GameManager gm = GameManager.Instance;
        _isThisBlack = isPlayer == gm.IsPlayerBlack;
        if (gm.IsPlayerBlack is false) stoneImage.sprite = otherColorStone;
        gm.BoardInform.OnBlackUnmovable += () =>
        {
            enabled = false;
            _isGameEnd = true;
        };
        
        _myTimer = isPlayer ? gm.PlayerTimer : gm.OppositeTimer;
        _isByoyomi = _myTimer.MainTime == 0f;
        _prevByoyomiCount = _myTimer.ByoyomiCount;
        
        if (_isByoyomi)
        {
            mainTimer.text = "00:00";
            mainTimer.color = Translucent;
            byoyomiTimer.color = Color.white;
            byoyomiCount.color = Color.white;
            clockIcon.color = Color.white;
        }
        else
        {
            int mainTime = Mathf.CeilToInt(_myTimer.MainTime);
            mainTimer.text = $"{mainTime / 60:D2}:{mainTime % 60:D2}";
            mainTimer.color = Color.white;
            byoyomiTimer.color = Translucent;
            byoyomiCount.color = Translucent;
            clockIcon.color = Translucent;
        }
        
        if (_prevByoyomiCount == 1)
        {
            byoyomiCount.text = "1회";
            byoyomiCount.font = glowFont;
        }
        else
        {
            byoyomiCount.text = $"{_prevByoyomiCount}회";
        }
        
        _initByoyomiSecondText = Mathf.CeilToInt(_myTimer.initByoyomiSeconds).ToString("D2");
        byoyomiTimer.text = _initByoyomiSecondText;
        
        BasicPlayerData myUser = isPlayer ? gm.MyPlayer : gm.OppositePlayer;
        InputUserInform(myUser.name, myUser.win, myUser.draw, myUser.lose, myUser.rating, myUser.imageNum);
        
        EventManager em = EventManager.Instance;
        em.OnGameEnd += () =>
        {
            _isGameEnd = true;
            enabled = false;
            CancelByoyomiUse();
        };
            
        if (isPlayer)
        {
            em.OnPlayerTimeOut += OnTimeOut;
            em.OnPlayerByoyomiPurchase += OnByoyomiPurchase;
        }
        else
        {
            em.OnOppositeTimeOut += OnTimeOut;
            em.OnOppositeByoyomiPurchase += OnByoyomiPurchase;
        }
        
        stoneMover.OnStoneMove += OnStoneMove;
        _myTimer.OnTimerSynchro += OnTimerSynchro;
        
        enabled = false;
    }
    
    private void OnDisable() => StopGlowEffect();

    private void Update()
    {
        int remainTime;

        #region 자유시간 사용 중일 때
        if (_isByoyomi is false)
        {
            if (_myTimer.MainTime == 0f)
            {
                StartByoyomi();
                return;
            }
            
            remainTime = Mathf.CeilToInt(_myTimer.MainTime);
            if (remainTime == _prevTime) return;

            mainTimer.text = $"{remainTime / 60:D2}:{remainTime % 60:D2}";
            _prevTime = remainTime;
            return;
        }
        #endregion

        #region 초읽기 중일 때
        remainTime = Mathf.CeilToInt(_myTimer.NowByoyomiSeconds);
        
        if (_myTimer.ByoyomiCount < _prevByoyomiCount)
        {
            CancelByoyomiUse();
            _byoyomiUseCts = new CancellationTokenSource();
            OnUseByoyomi(_myTimer.ByoyomiCount, _byoyomiUseCts.Token).Cancel();
        }
        
        if (remainTime == _prevTime) return;

        byoyomiTimer.text = $"{remainTime:D2}";
        _prevTime = remainTime;

        if (remainTime > 9 || remainTime == 0) return;
        
        #region 9초 이하일 때
        byoyomiTimer.font = glowFont;
        
        if (isPlayer)
        {
            if (_prevByoyomiCount == 1 && remainTime < 5)
            {
                _audioSource.PlayOneShot(byoyomiWarningSound);
                StartGlowEffect(byoyomiTimer.fontMaterial, loops: 1);
            }
            else
            {
                _audioSource.PlayOneShot(byoyomiTickSound);
            }
        }
        #endregion
        #endregion
    }

    private void CancelByoyomiUse()
    {
        if (_byoyomiUseCts != null)
        {
            _byoyomiUseCts.Cancel();
            _byoyomiUseCts.Dispose();
            _byoyomiUseCts = null;
        }
    }

    private void InputUserInform(string nickname, uint win, uint draw, uint lose, float rating, ProfileImageType imageNum)
    {
        nicknameText.text = nickname;
        recordText.text = $"{win}승 {draw}무 {lose}패 ({rating:F1}pt)";
        profile.sprite = profileImages[imageNum];
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

    private void OnStoneMove(bool isBlackTurn)
    {
        if (_isGameEnd) return;
        
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
            stoneImage.color = Translucent;

            if (_isByoyomi)
            {
                byoyomiTimer.font = _originFont;
                byoyomiTimer.text = _initByoyomiSecondText;
                byoyomiTimer.color = Translucent;
                byoyomiCount.color = Translucent;
                clockIcon.color = Translucent;
            }
            else
            {
                mainTimer.color = Translucent;
            }
        }
    }

    private void OnTimerSynchro(float mainTime, int leftCount)
    {
        CancelByoyomiUse();

        int mainTimeToInt = Mathf.CeilToInt(mainTime);
        mainTimer.text = mainTime > 0 ? $"{mainTimeToInt / 60:D2}:{mainTimeToInt % 60:D2}" : "00:00";
        byoyomiCount.text = $"{leftCount}회";
        
        if (leftCount == 1)
        {
            OnLastByoyomi?.Invoke();
            byoyomiCount.font = glowFont;
        }
        else
        {
            byoyomiCount.font = _originFont;
        }
        
        // Player의 타이머는 역행할 일이 없음을 전제로 함.
        if (isPlayer && mainTime == 0f)
        {
            if (leftCount == 1)
            {
                StartGlowEffect(byoyomiCount.fontMaterial, loops: 2);
                _audioSource.PlayOneShot(byoyomiWarningSound);
            }
            else
            {
                _audioSource.PlayOneShot(useByoyomiSound);
            }
        }
    }

    private void StartByoyomi()
    {
        _isByoyomi = true;
        _prevTime = (int)_myTimer.initByoyomiSeconds;
        
        mainTimer.text = "00:00";
        mainTimer.color = Translucent;

        byoyomiTimer.color = Color.white;
        byoyomiCount.color = Color.white;
        clockIcon.color = Color.white;

        if (isPlayer)
        {
            _audioSource.PlayOneShot(_prevByoyomiCount == 1 ? byoyomiWarningSound: useByoyomiSound);
        }
    }

    private async Awaitable OnUseByoyomi(int leftCount, CancellationToken cts)
    {
        StopGlowEffect();
        _prevByoyomiCount = leftCount;
        if (leftCount == 0) return;
        
        try
        {
            byoyomiTimer.font = _originFont;
            
            if (isPlayer is false)
            {
                await Awaitable.WaitForSecondsAsync(TOLERANCE, cts);
            }

            byoyomiCount.text = $"{leftCount}회";

            if (leftCount == 1)
            {
                OnLastByoyomi?.Invoke();
                byoyomiCount.font = glowFont;
                if (isPlayer)
                {
                    StartGlowEffect(byoyomiCount.fontMaterial, loops: 2);
                    _audioSource.PlayOneShot(byoyomiWarningSound);
                }
            }
            else if (isPlayer)
            {
                _audioSource.PlayOneShot(useByoyomiSound);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void OnByoyomiPurchase(int amount)
    {
        StopGlowEffect();
        byoyomiTimer.font = _originFont;
        byoyomiCount.font = _originFont;
        _prevByoyomiCount += amount;
        byoyomiCount.text = $"{_prevByoyomiCount}회";
        _audioSource.PlayOneShot(byoyomiPurchaseSound);
    }

    private void OnTimeOut()
    {
        byoyomiTimer.text = "00";
        byoyomiCount.text = "0회";
    }
}