using UnityEngine;
using UnityEngine.UI;

public class AdjustScaler : MonoBehaviour
{
    [SerializeField] private Vector2Int baseResolution;
    [SerializeField] private RectTransform bottomPanel;

    private float _baseRatio;
    private CanvasScaler _canvasScaler;
    private int _lastWidth;
    private int _lastHeight;

    private void Awake()
    {
        _baseRatio = (float)baseResolution.x / baseResolution.y;
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

        _canvasScaler.matchWidthOrHeight = (float)_lastWidth / _lastHeight > _baseRatio ? 1f : 0f;
    }
}
