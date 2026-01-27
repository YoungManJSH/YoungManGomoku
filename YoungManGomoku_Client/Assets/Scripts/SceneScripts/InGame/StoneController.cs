using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class StoneController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer circle;
    [SerializeField] private SpriteRenderer xMark;
    
    private Rigidbody2D _rb;
    private CircleCollider2D _collider;
    private TweenerCore<Color, Color, ColorOptions> _fadeTween;
    private TweenerCore<Vector3, Vector3, VectorOptions> _scaleTween;
    private float _originScale;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        _rb.simulated = false;
        _collider.enabled = false;
        _originScale = transform.localScale.x;

        EventManager.Instance.OnPlayerSurrender += OnStartSweeping;
        EventManager.Instance.OnOppositeSurrender += OnStartSweeping;
        
        transform.localScale *= 1.5f;
        _scaleTween = transform.DOScale(endValue: _originScale, duration: 0.4f);
    }

    private void OnDestroy()
    {
        _fadeTween?.Kill();

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnPlayerSurrender -= OnStartSweeping;
            EventManager.Instance.OnOppositeSurrender -= OnStartSweeping;
        }
    }
    
    /// <summary>이 개체가 오목의 구성원일 때 적용할 애니메이션을 시작</summary>
    public void GomokuAction()
    {
        if (_scaleTween.IsActive())
        {
            _scaleTween.OnComplete(ActivatingCircle);
            return;
            
            /* 애니메이션 재생 종료 타이밍의 경계에 있으면 위험한 코드임.
             * 하지만 현재 호출 구조에서는 애니메이션 극초반 타이밍에 들어옴.
             * 게다가 조금 틀어져도 게임 핵심 로직에 영향이 없는 연출임.
             * 그러니 그냥 적당히 편하게 처리하는 코드로 작성하였음. */
        }
        
        transform.DOScale(endValue: _originScale * 1.5f, duration: 0.2f).
            SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine).OnComplete(ActivatingCircle);
    }

    /// <summary>무르기 대상 돌인지를 표시/해제</summary>
    /// <param name="isActivate">표시/해제 여부</param>
    public void XMarking(bool isActivate)
    {
        /* 1. Fake null 상황에서 Early Exit
         * 2. 상태가 변경되지 않는 호출이면 Early Exit */
        if (xMark == null || xMark.gameObject.activeSelf == isActivate)
            return;
        
        xMark.gameObject.SetActive(isActivate);

        if (isActivate)
        {
            _fadeTween = xMark.DOFade(endValue: 0.3f, duration: 0.25f).
                SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
        else
        {
            /* 함수 도입부에서 Early Exit을 통과했으므로 가능한 코드
             * 즉, 여기서 tween은 항상 살아있다고 가정함 */
            _fadeTween.Rewind();
            _fadeTween.Kill();
            _fadeTween = null;
        }
    }

    /// <summary>[무르기] 스케일 애니메이션과 함께 돌을 제거</summary>
    public void TakeBack()
    {
        XMarking(isActivate: false);
        
        gameObject.transform.DOScale(endValue: 0f, duration: 0.4f).
            OnComplete(() => Destroy(gameObject));
    }

    private void ActivatingCircle()
    {
        circle.gameObject.SetActive(true);
        
        _fadeTween = circle.DOFade(endValue: 0.3f, duration: 0.25f).
            SetLoops(10, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }
    
    private void OnStartSweeping()
    {
        _collider.enabled = true;
        _rb.simulated = true;
        Destroy(gameObject, 3f);
    }
}
