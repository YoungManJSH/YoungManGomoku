using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerPanelController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI mainTimer;
    [SerializeField] private TextMeshProUGUI byoyomiTimer;
    [SerializeField] private TextMeshProUGUI byoyomiCount;
    [SerializeField] private Image stoneImage;
    [SerializeField] private Image clockIcon;
    [SerializeField] private StoneMoveController stoneMoveController;
    [SerializeField] private AudioClip useByoyomiSound;
    [SerializeField] private AudioClip byoyomiTickSound;
    [SerializeField] private bool isPlayer;

    private GameManager _gm;
    private GameManager.TimeController _myTimer;
    [CanBeNull] private AudioSource _audioSource;
    private int _prevTime;
    private string _initByoyomiSecondText;
    private bool _isThisBlack;
    private bool _isByoyomi;
    private Color _translucent;
    
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _isByoyomi = false;
        _prevTime = -1;
        
        _translucent = new Color(1f, 1f, 1f, 0.3f);
        byoyomiTimer.color = _translucent;
        byoyomiCount.color = _translucent;
        clockIcon.color = _translucent;

        stoneMoveController.OnStoneMove += OnTurnChanged;
    }

    private void Start()
    {
        _gm = GameManager.Instance;
        _isThisBlack = isPlayer == _gm.IsPlayerBlack;
        _myTimer = isPlayer ? _gm.PlayerTime : _gm.OppositeTime;
        
        _gm.OnGameEnd += () => enabled = false;
        _myTimer.StartByoyomi += OnStartByoyomi;
        _myTimer.UseByoyomi += OnUseByoyomi;
        _myTimer.OnTimeLose += OnTimeLose;

        int mainTime = Mathf.CeilToInt(_myTimer.MainTime);
        mainTimer.text = $"{mainTime / 60:D2}:{mainTime % 60:D2}";
        _initByoyomiSecondText = Mathf.CeilToInt(_myTimer.initByoyomiSeconds).ToString("D2");
        byoyomiTimer.text = _initByoyomiSecondText;
        byoyomiCount.text = $"{_myTimer.ByoyomiLeft}회";
        
        enabled = false;
    }

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
        if (remainTime < 10)
        {
            byoyomiTimer.color = Color.coral;
            _audioSource?.PlayOneShot(byoyomiTickSound);
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
        
        _audioSource?.PlayOneShot(useByoyomiSound);
    }

    private void OnUseByoyomi()
    {
        byoyomiTimer.color = Color.white;
        byoyomiCount.text = $"{_myTimer.ByoyomiLeft}회";
        _audioSource?.PlayOneShot(useByoyomiSound);
    }

    private void OnTimeLose()
    {
        byoyomiTimer.text = "00";
        byoyomiCount.text = "0회";
    }
}
