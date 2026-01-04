using UnityEngine;

[CreateAssetMenu(fileName = "UIPosition", menuName = "Scriptable Objects/UIPosition")]
public class UIPosition : ScriptableObject
{
    [SerializeField, Tooltip("세로 모드 기준 종횡비")] private float tallRatio;
    [SerializeField, Tooltip("가로 모드 기준 종횡비")] private float wideRatio;
    [SerializeField, Tooltip("세로 모드 오목판 높이 (0 ~ 1)")] private float boardPosY;
    
    [Header("오목판 Rect를 기준으로 각 UI들의 Anchors")]
    [SerializeField, Tooltip("StartCountDown")] private Rect countAnchors;
    [SerializeField, Tooltip("ResultPanel 세로 모드")] private Rect resultTallAnchors;
    [SerializeField, Tooltip("messageBox 세로 모드")] private Rect messageTallAnchors;
    [SerializeField, Tooltip("topPanel 세로 모드 한계선")] private float topYLimit;
    
    [Header("Wide 버전 Anchors")]
    [SerializeField, Tooltip("ResultPanel 가로 모드")] private Rect resultWideAnchors;
    [SerializeField, Tooltip("messageBox 가로 모드")] private Rect messageWideAnchors;
    [SerializeField, Tooltip("topPanel 가로 모드 높이")] private float topHeight;
    [SerializeField, Tooltip("bottomPanel 가로 모드 높이")] private float bottomHeight;
    
    public float TallRatio => tallRatio;
    public float WideRatio => wideRatio;
    public float BoardPosY => boardPosY;

    public void PanelMovingAndScaling(RectTransform startCount, RectTransform resultPanel, RectTransform messagePanel,
        RectTransform topPanel, RectTransform bottomPanel, Vector3 boardMin, Vector3 boardMax, float nowAspect)
    {
        float minX = boardMin.x / Screen.width;
        float maxX = boardMax.x / Screen.width;
        float minY = boardMin.y / Screen.height;
        float maxY = boardMax.y / Screen.height;
        
        // 시작 count down 텍스트 UI는 가로 모드와 세로 모드가 동일한 연산
        startCount.anchorMin = new Vector2(LerpX(countAnchors.xMin), LerpY(countAnchors.yMin));
        startCount.anchorMax = new Vector2(LerpX(countAnchors.xMax), LerpY(countAnchors.yMax));
        
        // RectTransform의 Pivot에 종속적인 연산이므로 주의! 
        if (nowAspect < wideRatio) // 세로 모드
        {
            // 오목판 위치와 크기에 따라서 정해진 위치와 배율로 배치
            resultPanel.anchorMin = new Vector2(LerpX(resultTallAnchors.xMin), LerpY(resultTallAnchors.yMin));
            resultPanel.anchorMax = new Vector2(LerpX(resultTallAnchors.xMax), LerpY(resultTallAnchors.yMax));
            
            messagePanel.anchorMin = new Vector2(LerpX(messageTallAnchors.xMin), LerpY(messageTallAnchors.yMin));
            messagePanel.anchorMax = new Vector2(LerpX(messageTallAnchors.xMax), LerpY(messageTallAnchors.yMax));

            // top, bottom panel을 board의 위아래로 배치
            topPanel.anchorMin = new Vector2(minX, maxY);
            topPanel.anchorMax = new Vector2(maxX, topYLimit);

            bottomPanel.anchorMin = new Vector2(minX, 0f);
            bottomPanel.anchorMax = new Vector2(maxX, minY);
        }
        else // 가로 모드
        {
            // 오목판 위치와 크기에 따라서 정해진 위치와 배율로 배치
            resultPanel.anchorMin = new Vector2(LerpX(resultWideAnchors.xMin), LerpY(resultWideAnchors.yMin));
            resultPanel.anchorMax = new Vector2(LerpX(resultWideAnchors.xMax), LerpY(resultWideAnchors.yMax));
            
            messagePanel.anchorMin = new Vector2(LerpX(messageWideAnchors.xMin), LerpY(messageWideAnchors.yMin));
            messagePanel.anchorMax = new Vector2(LerpX(messageWideAnchors.xMax), LerpY(messageWideAnchors.yMax));
            
            
            // top, bottom panel을 board의 우측으로 배치
            float rightLimit = 0.5f + wideRatio / nowAspect / 2f; // UI 표시 한계선 (wideRatio만큼만 화면 표시)

            topPanel.anchorMin = new Vector2(maxX, LerpY(bottomHeight)); // bottom panel 위로 배치
            topPanel.anchorMax = new Vector2(rightLimit, LerpY(bottomHeight + topHeight));

            bottomPanel.anchorMin = new Vector2(maxX, 0f);
            bottomPanel.anchorMax = new Vector2(rightLimit, bottomHeight);
        }
        
        // 설정된 Anchor를 꽉 채우도록 조정 여백 제거
        startCount.offsetMin = startCount.offsetMax = Vector2.zero;
        resultPanel.offsetMin = resultPanel.offsetMax = Vector2.zero;
        messagePanel.offsetMin = messagePanel.offsetMax = Vector2.zero;
        topPanel.offsetMin = topPanel.offsetMax = Vector2.zero;
        bottomPanel.offsetMin = bottomPanel.offsetMax = Vector2.zero;
        
        return;
        
        float LerpX(float t) => Mathf.Lerp(minX, maxX, t);
        float LerpY(float t) => Mathf.Lerp(minY, maxY, t);
    }
}
 