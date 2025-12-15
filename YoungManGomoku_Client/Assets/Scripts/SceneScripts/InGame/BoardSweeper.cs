using DG.Tweening;
using UnityEngine;

public class BoardSweeper : MonoBehaviour
{
    [SerializeField] private Vector2 middleVelocity;
    [SerializeField] private Vector2 endVelocity;
    [SerializeField] private float toMiddleDuration;
    [SerializeField] private float switchingDuration;
    [SerializeField] private float toEndDuration;
    
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        EventManager.Instance.OnPlayerSurrender += Sweeping;
        EventManager.Instance.OnOppositeSurrender += Sweeping;
        gameObject.SetActive(false);
    }

    private void Sweeping()
    {
        gameObject.SetActive(true);
        _rb.simulated = true;

        GetComponent<AudioSource>().Play();
        Sequence seq = DOTween.Sequence();
        
        seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            middleVelocity, toMiddleDuration).SetEase(Ease.OutQuad));
        
        seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            endVelocity, switchingDuration).SetEase(Ease.InOutQuad));
        
        seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            Vector2.zero, toEndDuration).SetEase(Ease.InQuad));

        seq.OnComplete(() => gameObject.SetActive(false));
    }
}
