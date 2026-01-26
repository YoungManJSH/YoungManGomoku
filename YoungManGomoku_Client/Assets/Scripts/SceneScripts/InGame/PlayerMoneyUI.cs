using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;

public class PlayerMoneyUI : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset glowFont;
    [SerializeField] private MessageBoxManager messageBox;
    
    private TextMeshProUGUI _moneyText;
    private int _nowMoney; // 현재 UI에 표시되고 있는 재화
    private TMP_FontAsset _originFont;
    private TweenerCore<int, int, NoOptions> _moneyChangeTween;
    private TweenerCore<float, float, FloatOptions> _glowTween;

    private void Awake()
    {
        _moneyText = GetComponent<TextMeshProUGUI>();
        _nowMoney = GameManager.Instance.PlayerMoney;
        _moneyText.text = _nowMoney.ToString();
        _originFont = _moneyText.font;
        
        GameManager.Instance.OnPlayerMoneyChanged += OnMoneyChanged;
        EventManager.Instance.OnGameEnd += StopGlowEffect;
        messageBox.TurnBackToGame += StopGlowEffect;
    }

    private void OnDestroy()
    {
        _moneyChangeTween?.Kill();
        StopGlowEffect();
    }

    public void StartGlowEffect()
    {
        _moneyText.font = glowFont;

        _glowTween = _moneyText.fontMaterial.DOFloat(endValue: 0.5f, ShaderUtilities.ID_GlowOuter, duration: 0.5f)
            .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    private void StopGlowEffect()
    {
        if (_glowTween == null) return;

        _glowTween.Rewind();
        _glowTween.Kill();
        _glowTween = null;
        _moneyText.font = _originFont;
    }
    
    /// <summary>재화가 갱신될 때 UI 갱신 텍스트 애니메이션</summary>
    /// <param name="money">갱신된 재화의 액수</param>
    private void OnMoneyChanged(int money)
    {
        _moneyChangeTween?.Kill();
        
        _moneyChangeTween = DOTween.To(getter: () => _nowMoney, setter: x =>
        {
            _nowMoney = x;
            _moneyText.text = x.ToString();
        }, money, duration: 0.5f).SetEase(Ease.InOutCubic);
    }
}
