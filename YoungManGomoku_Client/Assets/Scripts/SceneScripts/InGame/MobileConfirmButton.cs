using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileConfirmButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private float dragStartTime;
    [SerializeField] private Sprite blackButtonSprite;
    [SerializeField] private Sprite whiteButtonSprite;
    [SerializeField] private Image buttonImage;
    [SerializeField] private TextMeshProUGUI buttonText;
    
    private RectTransform _myRect;
    private RectTransform _parentRect;

    private bool _isHolding;
    private bool _isDragging;
    private float _holdTime;
    private (float min, float max) _xLimits;
    private (float min, float max) _yLimits;
    
    private void Awake()
    {
        _myRect = GetComponent<RectTransform>();
        _parentRect = _myRect.parent.GetComponent<RectTransform>();
        
        _isHolding = false;
        _isDragging = false;
        _holdTime = 0f;
    }

    private void Update()
    {
        if (_isHolding && _isDragging is false)
        {
            _holdTime += Time.unscaledDeltaTime;
            
            if (_holdTime >= dragStartTime)
            {
                _isDragging = true;
                buttonImage.color = new Color(1f, 1f, 1f, 0.45f);
                _xLimits = (-_parentRect.rect.width / 2f, _parentRect.rect.width / 2f);
                _yLimits = (-_parentRect.rect.height, -_parentRect.rect.width * 0.06f);
                /* parentRect.height가 종횡비에 따라 가변적임
                 * 따라서 yLimits.max를 width 기준으로 계산한 건 의도된 것 */
            }
        }
    }

#if UNITY_ANDROID
    public void ButtonImageChange(bool isBlack)
    {
        if (isBlack)
        {
            buttonImage.sprite = blackButtonSprite;
            buttonText.color = Color.white;
        }
        else
        {
            buttonImage.sprite = whiteButtonSprite;
            buttonText.color = Color.black;
        }
    }
#endif
    
    #region 드래그 인터페이스 구현부
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
        // localPoint는 _parentRect의 pivot 위치에 따른 상대적인 값임에 유의!
        
        // parentRect pivot = myRect anchors = (x: 0.5, y: 1.0) 기준
        Vector2 clamped = new Vector2(Mathf.Clamp(localPoint.x, _xLimits.min, _xLimits.max),
            Mathf.Clamp(localPoint.y, _yLimits.min, _yLimits.max));
        
        _myRect.anchoredPosition = clamped;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isHolding = false;
        _isDragging = false;
        buttonImage.color = Color.white;
    }
    #endregion
}
