using UnityEngine;
using UnityEngine.UI;

public class PanelMover : MonoBehaviour
{
    [SerializeField] private RectTransform topPanel;
    [SerializeField] private RectTransform bottomPanel;
    [SerializeField] private RectTransform messageBoxPanel;
    [SerializeField] private RectTransform respondBoxPanel;
    [SerializeField] private RectTransform toastPanel;
    [SerializeField] private RectTransform resultPanel;
    [SerializeField] private RectTransform startCountDown;
    [SerializeField] private SpriteRenderer boardRenderer;
    [SerializeField] private IngameBoardScaler ingameBoardScaler;
    [SerializeField] private Camera mainCamera;
    
    private CanvasScaler _canvasScaler;
    private UIPosition _uiPos;

    private void Awake()
    {
        _canvasScaler = GetComponent<CanvasScaler>();
        _uiPos = ingameBoardScaler.UIPos;
        ingameBoardScaler.OnBoardScaled += MoveUIPanel;
    }
    
    private void MoveUIPanel()
    {
        float nowAspect = mainCamera.aspect;
        _canvasScaler.matchWidthOrHeight = nowAspect > _uiPos.TallRatio ? 1f : 0f;
        
        Vector3 boardMin = mainCamera.WorldToScreenPoint(boardRenderer.bounds.min);
        Vector3 boardMax = mainCamera.WorldToScreenPoint(boardRenderer.bounds.max);
        
        _uiPos.PanelMovingAndScaling(startCountDown, resultPanel, messageBoxPanel, respondBoxPanel,
            toastPanel, topPanel, bottomPanel, boardMin, boardMax, nowAspect);
    }
}