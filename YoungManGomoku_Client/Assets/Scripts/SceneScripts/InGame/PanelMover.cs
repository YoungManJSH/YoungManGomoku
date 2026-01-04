using UnityEngine;
using UnityEngine.UI;

public class PanelMover : MonoBehaviour
{
    [SerializeField] private RectTransform topPanel;
    [SerializeField] private RectTransform bottomPanel;
    [SerializeField] private RectTransform messageBoxPanel;
    [SerializeField] private RectTransform resultPanel;
    [SerializeField] private RectTransform startCountDown;
    [SerializeField] private GridLayoutGroup buttonGridLayout;
    [SerializeField] private SpriteRenderer boardRenderer;
    [SerializeField] private IngameBoardScaler ingameBoardScaler;
    [SerializeField] private Camera mainCamera;
    
    private CanvasScaler _canvasScaler;
    private RectTransform _buttonRect;
    private UIPosition _uiPos;

    private void Awake()
    {
        _canvasScaler = GetComponent<CanvasScaler>();
        _buttonRect = buttonGridLayout.GetComponent<RectTransform>();
        _uiPos = ingameBoardScaler.UIPos;
        ingameBoardScaler.OnBoardScaled += async() => await MoveUIPanel();
    }
    
    private async Awaitable MoveUIPanel()
    {
        float nowAspect = mainCamera.aspect;
        
        _canvasScaler.matchWidthOrHeight = nowAspect > _uiPos.TallRatio ? 1f : 0f;
        
        await Awaitable.EndOfFrameAsync();
        
        Vector3 boardMin = mainCamera.WorldToScreenPoint(boardRenderer.bounds.min);
        Vector3 boardMax = mainCamera.WorldToScreenPoint(boardRenderer.bounds.max);

        _uiPos.PanelMovingAndScaling(startCountDown, resultPanel, messageBoxPanel,
            topPanel, bottomPanel, boardMin, boardMax, nowAspect);
        
        // ButtonGrid 조정하기
        buttonGridLayout.cellSize = new Vector2(_buttonRect.rect.width / 2f, _buttonRect.rect.height / 2f);
    }
}