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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _audioSource = GetComponent<AudioSource>();
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
        /* TODO : 추후 이 부분에서 playerHand일 경우 서버로 기권 의사 전달 */
        EventManager.Instance.StartSweeping();
        
        gameObject.SetActive(true);
        _rb.simulated = true;

        _audioSource.Play();
        Sequence seq = DOTween.Sequence();
        
        seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            middleVelocity, toMiddleDuration).SetEase(Ease.OutQuad));
        
        seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            endVelocity, switchingDuration).SetEase(Ease.InOutQuad));
        
        seq.Append(DOTween.To(getter: () => _rb.linearVelocity, setter: vec => _rb.linearVelocity = vec,
            Vector2.zero, toEndDuration).SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            if (isPlayerHand)
            {
                EventManager.Instance.PlayerSurrendered();
                /* TODO : 추후 이 부분 삭제, 해당 처리는 서버의 응답과 연동 */
            }
            else
            {
                EventManager.Instance.OppositeSurrendered();
                /* TODO : 이 부분은 유지, 서버의 응답을 받고 실행된 영역이므로 */
            }
            gameObject.SetActive(false);
        });
    }
}
