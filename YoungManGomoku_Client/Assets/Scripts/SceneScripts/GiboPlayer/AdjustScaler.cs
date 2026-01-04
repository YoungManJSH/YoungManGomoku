using UnityEngine;
using UnityEngine.UI;

public class AdjustScaler : MonoBehaviour
{
    [SerializeField] private UIPosition uiPos;
    
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
            AdjustScale();
        }
    }

    private  void AdjustScale()
    {
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;

        _canvasScaler.matchWidthOrHeight = (float)_lastWidth / _lastHeight > uiPos.TallRatio ? 1f : 0f;
    }
}
