using UnityEngine;

[CreateAssetMenu(fileName = "UIPosition", menuName = "Scriptable Objects/UIPosition")]
public class UIPosition : ScriptableObject
{
    [SerializeField, Tooltip("세로 모드 기준 종횡비")] private float tallRatio;
    [SerializeField, Tooltip("가로 모드 기준 종횡비")] private float wideRatio;
    [SerializeField, Tooltip("세로 모드 오목판 높이 (0 ~ 1)")] private float boardPosY;
    
    [Header("오목판 Rect를 기준으로 한 UI 조정값")]
    [SerializeField, Tooltip("StartCountDown")] private Rect countAnchors;
    [SerializeField, Tooltip("ResultPanel 세로 모드")] private Rect resultTallAnchors;
    [SerializeField, Tooltip("messageBox 세로 모드")] private Rect messageTallAnchors;
    [SerializeField, Tooltip("toastBox 세로 모드")] private Rect toastTallAnchors;
    [SerializeField, Tooltip("topPanel 세로 모드 여백 비율")] private float topMarginRatio;
    
    [Header("Wide 버전용 UI 조정값")]
    [SerializeField, Tooltip("ResultPanel 가로 모드")] private Rect resultWideAnchors;
    [SerializeField, Tooltip("messageBox 가로 모드")] private Rect messageWideAnchors;
    [SerializeField, Tooltip("toastBox 가로 모드")] private Rect toastWideAnchors;
    [SerializeField, Tooltip("topPanel 가로 모드 높이")] private float topHeight;
    [SerializeField, Tooltip("bottomPanel 가로 모드 높이")] private float bottomHeight;
    
    public float TallRatio => tallRatio;
    public float WideRatio => wideRatio;
    public float BoardPosY => boardPosY;

    /// <summary>인게임 씬 UI 조절 함수(오목판이 월드 오브젝트)</summary>
    /// <param name="startCount">시작 카운트 다운 텍스트 UI</param>
    /// <param name="resultPanel">결과창 패널</param>
    /// <param name="messagePanel">메시지 박스 패널</param>
    /// <param name="respondPanel">응답용 박스 패널</param>
    /// <param name="toastPanel">토스트 메시지 박스 패널</param>
    /// <param name="topPanel">상단 패널</param>
    /// <param name="bottomPanel">하단 패널</param>
    /// <param name="boardMin">오목판 스크린 좌표 좌하단</param>
    /// <param name="boardMax">오목판 스크린 좌표 우상단</param>
    /// <param name="nowAspect">출력되는 화면의 종횡비</param>
    public void PanelMovingAndScaling(RectTransform startCount, RectTransform resultPanel,
        RectTransform messagePanel, RectTransform respondPanel, RectTransform toastPanel,
        RectTransform topPanel, RectTransform bottomPanel,
        Vector3 boardMin, Vector3 boardMax, float nowAspect)
    {
        float minX = boardMin.x / Screen.width;
        float maxX = boardMax.x / Screen.width;
        float minY = boardMin.y / Screen.height;
        float maxY = boardMax.y / Screen.height;
        
        // 시작 count down 텍스트 UI는 가로 모드와 세로 모드가 동일한 연산
        startCount.anchorMin = new Vector2(LerpX(countAnchors.xMin), LerpY(countAnchors.yMin));
        startCount.anchorMax = new Vector2(LerpX(countAnchors.xMax), LerpY(countAnchors.yMax));
        
        if (nowAspect < wideRatio) // 세로 모드
        {
            // 오목판 위치와 크기에 따라서 정해진 위치와 배율로 배치
            resultPanel.anchorMin = new Vector2(LerpX(resultTallAnchors.xMin), LerpY(resultTallAnchors.yMin));
            resultPanel.anchorMax = new Vector2(LerpX(resultTallAnchors.xMax), LerpY(resultTallAnchors.yMax));
            
            messagePanel.anchorMin = new Vector2(LerpX(messageTallAnchors.xMin), LerpY(messageTallAnchors.yMin));
            messagePanel.anchorMax = new Vector2(LerpX(messageTallAnchors.xMax), LerpY(messageTallAnchors.yMax));

            toastPanel.anchorMin = new Vector2(LerpX(toastTallAnchors.xMin), LerpY(toastTallAnchors.yMin));
            toastPanel.anchorMax = new Vector2(LerpX(toastTallAnchors.xMax), LerpY(toastTallAnchors.yMax));
            
            // topPanel의 상단 앵커 위치
            float topLimit = 0.5f + (0.5f - topMarginRatio) * Mathf.Min(nowAspect / tallRatio, 1f);
            float bottomLimit = 0.5f - 0.5f * Mathf.Min(nowAspect / tallRatio, 1f);
            
            // top, bottom panel을 board의 위아래로 배치
            topPanel.anchorMin = new Vector2(minX, maxY);
            topPanel.anchorMax = new Vector2(maxX, topLimit);

            bottomPanel.anchorMin = new Vector2(minX, bottomLimit);
            bottomPanel.anchorMax = new Vector2(maxX, minY);
        }
        else // 가로 모드
        {
            // 오목판 위치와 크기에 따라서 정해진 위치와 배율로 배치
            resultPanel.anchorMin = new Vector2(LerpX(resultWideAnchors.xMin), LerpY(resultWideAnchors.yMin));
            resultPanel.anchorMax = new Vector2(LerpX(resultWideAnchors.xMax), LerpY(resultWideAnchors.yMax));
            
            messagePanel.anchorMin = new Vector2(LerpX(messageWideAnchors.xMin), LerpY(messageWideAnchors.yMin));
            messagePanel.anchorMax = new Vector2(LerpX(messageWideAnchors.xMax), LerpY(messageWideAnchors.yMax));
            
            toastPanel.anchorMin = new Vector2(LerpX(toastWideAnchors.xMin), LerpY(toastWideAnchors.yMin));
            toastPanel.anchorMax = new Vector2(LerpX(toastWideAnchors.xMax), LerpY(toastWideAnchors.yMax));
            
            // top, bottom panel을 board의 우측으로 배치
            float rightLimit = 0.5f + 0.5f * wideRatio / nowAspect; // UI 표시 한계선 (wideRatio만큼만 화면 표시)

            topPanel.anchorMin = new Vector2(maxX, LerpY(bottomHeight)); // bottom panel 위로 배치
            topPanel.anchorMax = new Vector2(rightLimit, LerpY(bottomHeight + topHeight));

            bottomPanel.anchorMin = new Vector2(maxX, 0f);
            bottomPanel.anchorMax = new Vector2(rightLimit, bottomHeight);
        }
        // 응답용 박스는 메시지 박스와 동일 위치
        respondPanel.anchorMin = messagePanel.anchorMin;
        respondPanel.anchorMax = messagePanel.anchorMax;
        
        // 설정된 Anchors를 꽉 채우도록 여백 제거
        startCount.offsetMin = startCount.offsetMax = Vector2.zero;
        resultPanel.offsetMin = resultPanel.offsetMax = Vector2.zero;
        messagePanel.offsetMin = messagePanel.offsetMax = Vector2.zero;
        respondPanel.offsetMin = respondPanel.offsetMax = Vector2.zero;
        toastPanel.offsetMin = toastPanel.offsetMax = Vector2.zero;
        topPanel.offsetMin = topPanel.offsetMax = Vector2.zero;
        bottomPanel.offsetMin = bottomPanel.offsetMax = Vector2.zero;
        
        return;
        
        float LerpX(float t) => Mathf.Lerp(minX, maxX, t);
        float LerpY(float t) => Mathf.Lerp(minY, maxY, t);
    }

    /// <summary>기보 재생 씬 UI 조절 함수(오목판이 UI 오브젝트)</summary>
    /// <param name="boardPanel">오목판 패널</param>
    /// <param name="messagePanel">메시지 박스 패널</param>
    /// <param name="topPanel">상단 패널</param>
    /// <param name="bottomPanel">하단 패널</param>
    /// <param name="nowAspect">출력되는 화면의 종횡비</param>
    public void PanelMovingAndScaling(RectTransform boardPanel, RectTransform messagePanel,
        RectTransform topPanel, RectTransform bottomPanel, float nowAspect)
    {
        if (nowAspect < wideRatio) // 세로 모드
        {
            float horAspectScale = Mathf.Min(tallRatio / nowAspect, 1f);
            float verAspectScale = Mathf.Min(nowAspect / tallRatio, 1f);

            float leftLimit = 0.5f - 0.5f * horAspectScale;
            float rightLimit = 0.5f + 0.5f * horAspectScale;
            float topLimit = 0.5f + (0.5f - topMarginRatio) * verAspectScale;
            float bottomLimit = 0.5f - 0.5f * verAspectScale;
            
            float boardHeight = (rightLimit - leftLimit) * nowAspect;
            float boardCenterY = 0.5f + (boardPosY - 0.5f) * verAspectScale;
            float boardTopY = boardCenterY + 0.5f * boardHeight;
            float boardBottomY = boardCenterY - 0.5f * boardHeight;
            
            boardPanel.anchorMin = new Vector2(leftLimit, boardBottomY);
            boardPanel.anchorMax = new Vector2(rightLimit, boardTopY);
            
            messagePanel.anchorMin = new Vector2(messageTallAnchors.xMin, messageTallAnchors.yMin);
            messagePanel.anchorMax = new Vector2(messageTallAnchors.xMax, messageTallAnchors.yMax);

            // top, bottom panel을 board의 위아래로 배치
            topPanel.anchorMin = new Vector2(leftLimit, boardTopY);
            topPanel.anchorMax = new Vector2(rightLimit, topLimit);

            bottomPanel.anchorMin = new Vector2(leftLimit, bottomLimit);
            bottomPanel.anchorMax = new Vector2(rightLimit, boardBottomY);
        }
        else // 가로 모드
        {
            float horAspectScale = wideRatio / nowAspect;
            
            float leftLimit = 0.5f - 0.5f * horAspectScale;
            float rightLimit = 0.5f + 0.5f * horAspectScale;

            float boardRight = leftLimit + 1f / nowAspect;

            boardPanel.anchorMin = new Vector2(leftLimit, 0f);
            boardPanel.anchorMax = new Vector2(boardRight, 1f);
            
            messagePanel.anchorMin = new Vector2(messageWideAnchors.xMin, messageWideAnchors.yMin);
            messagePanel.anchorMax = new Vector2(messageWideAnchors.xMax, messageWideAnchors.yMax);

            // top, bottom panel을 board의 우측으로 배치
            topPanel.anchorMin = new Vector2(boardRight, bottomHeight);
            topPanel.anchorMax = new Vector2(rightLimit, bottomHeight + topHeight);

            bottomPanel.anchorMin = new Vector2(boardRight, 0f);
            bottomPanel.anchorMax = new Vector2(rightLimit, bottomHeight);
        }
        
        // 설정된 Anchors를 꽉 채우도록 여백 제거
        boardPanel.offsetMin = boardPanel.offsetMax = Vector2.zero;
        messagePanel.offsetMin = messagePanel.offsetMax = Vector2.zero;
        topPanel.offsetMin = topPanel.offsetMax = Vector2.zero;
        bottomPanel.offsetMin = bottomPanel.offsetMax = Vector2.zero;
    }
}
 