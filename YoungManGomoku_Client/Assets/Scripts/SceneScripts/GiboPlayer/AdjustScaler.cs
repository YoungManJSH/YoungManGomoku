using UnityEngine;
using UnityEngine.UI;

public class AdjustScaler : MonoBehaviour
{
    [SerializeField] private RectTransform bottomPanel;

    private CanvasScaler _canvasScaler;
    private float _baseRatio;
    private int _lastWidth;
    private int _lastHeight;

    private void Awake()
    {
        _canvasScaler = GetComponent<CanvasScaler>();
        _baseRatio = _canvasScaler.referenceResolution.x / _canvasScaler.referenceResolution.y;
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

        _canvasScaler.matchWidthOrHeight = (float)_lastWidth / _lastHeight > _baseRatio ? 1f : 0f;
    }
}
