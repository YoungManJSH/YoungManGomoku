using UnityEngine;
using UnityEngine.UI;

public class AdjustScaler : MonoBehaviour
{
    [SerializeField] private UIPosition uiPos;
    [SerializeField] private RectTransform boardPanel;
    [SerializeField] private RectTransform messagePanel;
    [SerializeField] private RectTransform topPanel;
    [SerializeField] private RectTransform bottomPanel;
    
    private CanvasScaler _canvasScaler;
    private int _lastWidth;
    private int _lastHeight;

    private void Awake()
    {
        _canvasScaler = GetComponent<CanvasScaler>();
        AdjustScale();
    }

    private void Update()
    {
        if (Screen.width != _lastWidth || Screen.height != _lastHeight)
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            AdjustScale();
        }
    }

    private  void AdjustScale()
    {
        float nowAspect = (float)Screen.width / Screen.height;
        _canvasScaler.matchWidthOrHeight = nowAspect > uiPos.TallRatio ? 1f : 0f;
        
        uiPos.PanelMovingAndScaling(boardPanel, messagePanel, topPanel, bottomPanel, nowAspect);
    }
}
