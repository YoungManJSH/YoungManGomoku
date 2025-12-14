using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConfirmButtonMover : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private float dragStartTime;
    [SerializeField] private Sprite blackButtonSprite;
    [SerializeField] private StoneMoveController stoneMoveController;

    private RectTransform _myRect;
    private RectTransform _parentRect;
    private Image _buttonImage;
    private Button _button;

    private bool _isHolding;
    private bool _isDragging;
    private float _holdTime;

    private void Awake()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        Destroy(gameObject);
        return;
#endif
        _myRect = GetComponent<RectTransform>();
        _parentRect = _myRect.parent.GetComponent<RectTransform>();
        _buttonImage = GetComponent<Image>();
        _button = GetComponent<Button>();
        _button.interactable = false;
        
        if (GameManager.Instance.IsPlayerBlack)
        {
            _buttonImage.sprite = blackButtonSprite;
            GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
        }
        
        _isHolding = false;
        _isDragging = false;
        _holdTime = 0f;
        
        //stoneMoveController.OnStoneMove += OnStoneMove;
    }

    private void Update()
    {
        if (_isHolding && _isDragging is false)
        {
            _holdTime += Time.unscaledDeltaTime;
            if (_holdTime >= dragStartTime)
            {
                _isDragging = true;
                _buttonImage.color = new Color(1f, 1f, 1f, 0.45f);
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isHolding = true;
        _holdTime = 0f;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging is false) return;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect,
            eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        
        Vector2 clamped = new Vector2(Mathf.Clamp(localPoint.x, 0f, _parentRect.rect.width),
            Mathf.Clamp(localPoint.y, _parentRect.rect.height * -1f, 0f));
        
        _myRect.anchoredPosition = clamped;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isHolding = false;
        _isDragging = false;
        _buttonImage.color = Color.white;
    }

    private void OnStoneMove(bool isBlackTurn)
        => _button.interactable = isBlackTurn == GameManager.Instance.IsPlayerBlack;
}
