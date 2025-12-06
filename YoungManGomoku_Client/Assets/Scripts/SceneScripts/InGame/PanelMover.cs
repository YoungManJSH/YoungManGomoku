using UnityEngine;

public class PanelMover : MonoBehaviour
{
    [SerializeField] private RectTransform topPanel;
    [SerializeField] private RectTransform bottomPanel;
    [SerializeField] private RectTransform startCountDown;
    [SerializeField] private SpriteRenderer boardRenderer;
    [SerializeField] private BoardGenerator boardGenerator;
    
    private Camera _mainCamera;

    private void Awake()
    {
        boardGenerator.OnBoardScaled += async() => await MoveUIPanel();
    }
    
    private void Start()
    {
        _mainCamera = Camera.main;
    }
    
    private async Awaitable MoveUIPanel()
    {
        await Awaitable.EndOfFrameAsync();
        
        Vector3 minPoint = _mainCamera.WorldToScreenPoint(boardRenderer.bounds.min);
        Vector3 maxPoint = _mainCamera.WorldToScreenPoint(boardRenderer.bounds.max);
        Rect boardRect = new Rect(minPoint, maxPoint - minPoint);

        topPanel.position = new Vector3(0f, boardRect.yMax, 0f);
        bottomPanel.position = new Vector3(0f, boardRect.yMin, 0f);
        startCountDown.position = new Vector3(boardRect.width / 2f, boardRect.yMin + boardRect.height / 2f, 0f);
    }
}