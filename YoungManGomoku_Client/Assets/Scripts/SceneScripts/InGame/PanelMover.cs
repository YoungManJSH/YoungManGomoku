using UnityEngine;
using UnityEngine.UI;

public class PanelMover : MonoBehaviour
{
    [SerializeField] private RectTransform topPanel;
    [SerializeField] private RectTransform bottomPanel;
    [SerializeField] private RectTransform messageBoxPanel;
    [SerializeField] private RectTransform resultPanel;
    [SerializeField] private RectTransform startCountDown;
    [SerializeField] private SpriteRenderer boardRenderer;
    [SerializeField] private IngameBoardManager ingameBoardManager;
    
    private Camera _mainCamera;
    private CanvasScaler _canvasScaler;

    private void Awake()
    {
        ingameBoardManager.OnBoardScaled += async() => await MoveUIPanel();
        _canvasScaler = GetComponent<CanvasScaler>();
    }
    
    private void Start()
    {
        _mainCamera = Camera.main;
    }
    
    private async Awaitable MoveUIPanel()
    {
        _canvasScaler.matchWidthOrHeight = ingameBoardManager.IsWide ? 1f : 0f;
        
        await Awaitable.EndOfFrameAsync();
        
        Vector3 minPoint = _mainCamera.WorldToScreenPoint(boardRenderer.bounds.min);
        Vector3 maxPoint = _mainCamera.WorldToScreenPoint(boardRenderer.bounds.max);
        Rect boardRect = new Rect(minPoint, maxPoint - minPoint);
        
        topPanel.position = new Vector3(boardRect.xMin + boardRect.width / 2f, boardRect.yMax, 0f);
        bottomPanel.position = new Vector3(boardRect.xMin + boardRect.width / 2f, boardRect.yMin, 0f);
        messageBoxPanel.position = new Vector3(boardRect.xMin + boardRect.width / 2f, boardRect.yMin + boardRect.height / 15f, 0f);
        resultPanel.position = new Vector3(boardRect.xMin + boardRect.width / 2f, boardRect.yMin + boardRect.height / 2f, 0f);
        startCountDown.position = new Vector3(boardRect.xMin + boardRect.width / 2f, boardRect.yMin + boardRect.height / 2f, 0f);
        
        bottomPanel.sizeDelta = new Vector2(bottomPanel.sizeDelta.x, bottomPanel.anchoredPosition.y);
    }
}