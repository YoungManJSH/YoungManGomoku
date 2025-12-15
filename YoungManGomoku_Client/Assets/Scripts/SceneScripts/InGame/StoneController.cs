using UnityEngine;

public class StoneController : MonoBehaviour
{
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
}
