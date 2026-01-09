using DG.Tweening;
using UnityEngine;

public class BoardSweeper : MonoBehaviour
{
    [SerializeField] private Vector2 middleVelocity;
    [SerializeField] private Vector2 endVelocity;
    [SerializeField] private float toMiddleDuration;
    [SerializeField] private float switchingDuration;
    [SerializeField] private float toEndDuration;
    [SerializeField] private AudioClip stoneSound;
    [SerializeField] private bool isPlayerHand;
    
    private Rigidbody2D _rb;
    private AudioSource _audioSource;
    private IngameBoardScaler _boardScaler;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _audioSource = GetComponent<AudioSource>();
        _boardScaler = GetComponentInParent<IngameBoardScaler>();
        
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Stone"))
        {
            _audioSource.PlayOneShot(stoneSound);
        }
    }
    
    public void Sweeping()
    {
        EventManager.Instance.StartSweeping();
        
        gameObject.SetActive(true);
        _rb.simulated = true;

        _audioSource.Play();
        Sequence seq = DOTween.Sequence();
        
        seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            endValue: middleVelocity / (_boardScaler.IsWide ? _boardScaler.UIPos.TallRatio : 1f), toMiddleDuration).
            SetEase(Ease.OutQuad));
        
        seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            endValue: endVelocity / (_boardScaler.IsWide ? _boardScaler.UIPos.TallRatio : 1f), switchingDuration).
            SetEase(Ease.InOutQuad));
        
        seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            Vector2.zero, toEndDuration).SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            if (isPlayerHand) EventManager.Instance.RequestSurrender();
            
            gameObject.SetActive(false);
        });
    }
}
