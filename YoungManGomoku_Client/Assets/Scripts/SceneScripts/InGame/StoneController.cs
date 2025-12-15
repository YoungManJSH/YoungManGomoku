using UnityEngine;

public class StoneController : MonoBehaviour
{
    [SerializeField] private AudioClip stoneSound;
    
    private Rigidbody2D _rb;
    private CircleCollider2D _collider;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        _rb.simulated = false;
        _collider.enabled = false;

        EventManager.Instance.OnPlayerSurrender += OnSurrendered;
        EventManager.Instance.OnOppositeSurrender += OnSurrendered;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Stone"))
        {
            GetComponent<AudioSource>().PlayOneShot(stoneSound);
        }
    }

    private void OnSurrendered()
    {
        _collider.enabled = true;
        _rb.simulated = true;
        Destroy(gameObject, 1f);
    }
}
