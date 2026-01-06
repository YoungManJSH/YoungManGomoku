using DG.Tweening;
using UnityEngine;

public class StoneController : MonoBehaviour
{
    [SerializeField] private GameObject circle;
    
    private Rigidbody2D _rb;
    private CircleCollider2D _collider;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        _rb.simulated = false;
        _collider.enabled = false;

        EventManager.Instance.OnStartSweeping += OnStartSweeping;
    }

    private void OnStartSweeping()
    {
        _collider.enabled = true;
        _rb.simulated = true;
        Destroy(gameObject, 3f);
    }

    public void GomokuAction()
    {
        float targetScale = transform.localScale.x * 1.5f;

        transform.DOScale(targetScale, 0.25f).SetLoops(2, LoopType.Yoyo).
            SetEase(Ease.InOutSine).OnComplete(() => circle.SetActive(true));
    }
}
