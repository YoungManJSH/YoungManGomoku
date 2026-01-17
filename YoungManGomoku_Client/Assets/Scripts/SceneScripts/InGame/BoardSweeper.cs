using DG.Tweening;
using UnityEngine;

public class BoardSweeper : MonoBehaviour
{
    [Header("세로 모드일 때의 손 이동 속도")]
    [SerializeField] private Vector2 tallMiddleVel;
    [SerializeField] private Vector2 tallEndVel;
    [Header("가로 모드일 때의 손 이동 속도")]
    [SerializeField] private Vector2 wideMiddleVel;
    [SerializeField] private Vector2 wideEndVel;
    [Header("구간별 이동 시간")]
    [SerializeField] private float toMiddleDuration;
    [SerializeField] private float switchingDuration;
    [SerializeField] private float toEndDuration;
    [Header("돌과 부딪힐 때 낼 효과음")]
    [SerializeField] private AudioClip stoneSound;
    [SerializeField] private bool isPlayerHand;
    
    private Rigidbody2D _rb;
    private AudioSource _audioSource;
    private IngameBoardScaler _boardScaler;
    private Sequence _seq;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _audioSource = GetComponent<AudioSource>();
        _boardScaler = GetComponentInParent<IngameBoardScaler>();

        if (isPlayerHand)
            EventManager.Instance.OnPlayerSurrender += Sweeping;
        else
            EventManager.Instance.OnOppositeSurrender += Sweeping;
        
        gameObject.SetActive(false);
    }

    private void OnDestroy() => _seq?.Kill();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Stone"))
        {
            _audioSource.PlayOneShot(stoneSound);
        }
    }
    
    private void Sweeping()
    {
        gameObject.SetActive(true);
        _audioSource.Play();
        _rb.simulated = true;
        
        _seq = DOTween.Sequence();
        
        _seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            endValue: _boardScaler.IsWide ? wideMiddleVel : tallMiddleVel, toMiddleDuration).
            SetEase(Ease.OutQuad));
        
        _seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            endValue: _boardScaler.IsWide ? wideEndVel : tallEndVel, switchingDuration).
            SetEase(Ease.InOutQuad));
        
        _seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            Vector2.zero, toEndDuration).SetEase(Ease.InQuad));

        _seq.OnComplete(() => gameObject.SetActive(false));
    }
}
