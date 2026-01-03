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

    private void Awake()
    {
        _canvasScaler = GetComponent<CanvasScaler>();
        _buttonRect = buttonGridLayout.GetComponent<RectTransform>();
        ingameBoardScaler.OnBoardScaled += async() => await MoveUIPanel();
    }
    
    private async Awaitable MoveUIPanel()
    {
        float nowAspect = mainCamera.aspect;
        
        _canvasScaler.matchWidthOrHeight = nowAspect > IngameBoardScaler.TallRatio ? 1f : 0f;
        
        await Awaitable.EndOfFrameAsync();
        
        Vector3 minPoint = mainCamera.WorldToScreenPoint(boardRenderer.bounds.min);
        Vector3 maxPoint = mainCamera.WorldToScreenPoint(boardRenderer.bounds.max);
        
        float minX = minPoint.x / Screen.width;
        float minY = minPoint.y / Screen.height;
        float maxX = maxPoint.x / Screen.width;
        float maxY = maxPoint.y / Screen.height;
        
        // 아래 연산들은 모두 Pivot에 종속적이므로 주의할 것!
        
        startCountDown.anchorMin = new Vector2(minX, Mathf.Lerp(minY, maxY, 0.4f));
        startCountDown.anchorMax = new Vector2(maxX, Mathf.Lerp(minY, maxY, 0.6f));
        startCountDown.offsetMin = startCountDown.offsetMax = Vector2.zero;
        
        if (nowAspect < IngameBoardScaler.WideRatio) // 세로 모드, 오목판 위아래로 부착
        {
            resultPanel.anchorMin =
                new Vector2(Mathf.Lerp(minX, maxX, 0.02f), Mathf.Lerp(minY, maxY, 0.3f));
            resultPanel.anchorMax =
                new Vector2(Mathf.Lerp(minX, maxX, 0.98f), Mathf.Lerp(minY, maxY, 0.8f));

            messageBoxPanel.anchorMin =
                new Vector2(Mathf.Lerp(minX, maxX, 0.15f), Mathf.Lerp(minY, maxY, 0.1f));
            messageBoxPanel.anchorMax =
                new Vector2(Mathf.Lerp(minX, maxX, 0.85f), Mathf.Lerp(minY, maxY, 0.35f));

            topPanel.anchorMin = new Vector2(minX, maxY);
            topPanel.anchorMax = new Vector2(maxX, 0.9f);
            
            bottomPanel.anchorMin = new Vector2(minX, 0f);
            bottomPanel.anchorMax = new Vector2(maxX, minY);
            
        }
        else // 가로 모드, 오목판 오른쪽에 부착
        {
            resultPanel.anchorMin =
                new Vector2(Mathf.Lerp(minX, maxX, 0.2f), Mathf.Lerp(minY, maxY, 0.375f));
            resultPanel.anchorMax =
                new Vector2(Mathf.Lerp(minX, maxX, 0.8f), Mathf.Lerp(minY, maxY, 0.625f));
            
            messageBoxPanel.anchorMin =
                new Vector2(Mathf.Lerp(minX, maxX, 0.3f), Mathf.Lerp(minY, maxY, 0.2f));
            messageBoxPanel.anchorMax =
                new Vector2(Mathf.Lerp(minX, maxX, 0.7f), Mathf.Lerp(minY, maxY, 0.35f));
            
            // UI를 표시할 경계선, 여기보다 오른쪽은 레터박스
            float rightLimit = 0.5f + IngameBoardScaler.WideRatio / nowAspect / 2f;
            
            topPanel.anchorMin = new Vector2(maxX, Mathf.Lerp(minY, maxY, 0.35f));
            topPanel.anchorMax = new Vector2(rightLimit, Mathf.Lerp(minY, maxY, 0.47f));
            
            bottomPanel.anchorMin = new Vector2(maxX, 0f);
            bottomPanel.anchorMax = new Vector2(rightLimit, Mathf.Lerp(minY, maxY, 0.35f));
        }
        resultPanel.offsetMin = resultPanel.offsetMax = Vector2.zero;
        messageBoxPanel.offsetMin = messageBoxPanel.offsetMax = Vector2.zero;
        topPanel.offsetMin = topPanel.offsetMax = Vector2.zero;
        bottomPanel.offsetMin = bottomPanel.offsetMax = Vector2.zero;
        
        // ButtonGrid 조정하기
        buttonGridLayout.cellSize = new Vector2(_buttonRect.rect.width / 2f, _buttonRect.rect.height / 2f);
    }
}