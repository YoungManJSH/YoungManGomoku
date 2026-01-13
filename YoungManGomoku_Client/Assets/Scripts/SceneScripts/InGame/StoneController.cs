using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class StoneController : MonoBehaviour
{
    [SerializeField] private GameObject circle;
    [SerializeField] private SpriteRenderer xMark;
    
    private Rigidbody2D _rb;
    private CircleCollider2D _collider;
    private TweenerCore<Color, Color, ColorOptions> _tween;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        _rb.simulated = false;
        _collider.enabled = false;

        EventManager.Instance.OnPlayerSurrender += OnStartSweeping;
        EventManager.Instance.OnOppositeSurrender += OnStartSweeping;
        
        float originScale = gameObject.transform.localScale.x;
        gameObject.transform.localScale *= 1.5f;
        gameObject.transform.DOScale(endValue: originScale, duration: 0.4f);
    }

    private void OnDestroy()
    {
        _tween?.Kill();

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnPlayerSurrender -= OnStartSweeping;
            EventManager.Instance.OnOppositeSurrender -= OnStartSweeping;
        }
    }
    
    public void GomokuAction()
    {
        float targetScale = transform.localScale.x * 1.5f;

        transform.DOScale(endValue: targetScale, duration: 0.25f).
            SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine).
            OnComplete(() => circle.SetActive(true));
    }

    public void XMarking(bool isActivate)
    {
        // 중복 호출 시 DOTween 관리 꼬임 방지를 위해 early exit
        if (xMark.gameObject.activeSelf == isActivate)
            return;
        
        xMark.gameObject.SetActive(isActivate);

        if (isActivate)
        {
            _tween = xMark.DOFade(endValue: 0.3f, duration: 0.25f).
                SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
        else
        {
            _tween.Rewind();
            _tween.Kill();
            _tween = null;
        }
    }

    public void TakeBack()
    {
        XMarking(isActivate: false);
        
        gameObject.transform.DOScale(endValue: 0f, duration: 0.4f).
            OnComplete(() => Destroy(gameObject));
    }
    
    private void OnStartSweeping()
    {
        _collider.enabled = true;
        _rb.simulated = true;
        Destroy(gameObject, 3f);
    }

}
