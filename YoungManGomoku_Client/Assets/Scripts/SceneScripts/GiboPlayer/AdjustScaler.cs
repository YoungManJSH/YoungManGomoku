using UnityEngine;
using UnityEngine.UI;

public class AdjustScaler : MonoBehaviour
{
    [SerializeField] private RectTransform bottomPanel;

    private const float BASE_RATIO = 1080f / 2200f;
    private const float WIDE_RATIO = 1.5f;
    
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

        _canvasScaler.matchWidthOrHeight = (float)_lastWidth / _lastHeight > BASE_RATIO ? 1f : 0f;
    }
}
