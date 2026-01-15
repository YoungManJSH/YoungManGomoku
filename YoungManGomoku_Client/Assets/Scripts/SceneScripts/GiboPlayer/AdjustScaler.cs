using System;
using UnityEngine;
using UnityEngine.UI;

public class AdjustScaler : MonoBehaviour
{
    [SerializeField] private UIPosition uiPos;
    [SerializeField] private RectTransform boardPanel;
    [SerializeField] private RectTransform messagePanel;
    [SerializeField] private RectTransform topPanel;
    [SerializeField] private RectTransform bottomPanel;

    /// <summary>
    /// <para>가로/세로 모드 전환 시 호출되는 이벤트</para>
    /// <para>true: 가로(wide) 모드</para>
    /// <para>false: 세로(tall) 모드</para>
    /// </summary>
    public event Action<bool> OnChangeAspect;
    public bool IsWide { get; private set; }
    public UIPosition UIPos => uiPos;
    
    private CanvasScaler _canvasScaler;
    
#if UNITY_STANDALONE || UNITY_EDITOR
    private int _lastWidth;
    private int _lastHeight;
#endif

    private void Awake()
    {
        _canvasScaler = GetComponent<CanvasScaler>();
        AdjustScale();
    }

#if UNITY_STANDALONE || UNITY_EDITOR // 모바일은 런타임 해상도 변경에 대응하지 않음
    private void Update()
    {
        if (Screen.width != _lastWidth || Screen.height != _lastHeight)
        {
            AdjustScale();
        }
    }
#endif

    private void AdjustScale()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
#endif
        
        float nowAspect = (float)Screen.width / Screen.height;
        bool isWide = nowAspect >= uiPos.WideRatio;
        
        _canvasScaler.matchWidthOrHeight = nowAspect > uiPos.TallRatio ? 1f : 0f;
        uiPos.PanelMovingAndScaling(boardPanel, messagePanel, topPanel, bottomPanel, nowAspect);
        
        if (isWide != IsWide)
        {
            IsWide = isWide;
            OnChangeAspect?.Invoke(isWide);
        }
    }
}
