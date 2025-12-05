using System;
using UnityEngine;

public class PanelMover : MonoBehaviour
{
    [SerializeField] private RectTransform topPanel;
    [SerializeField] private RectTransform bottomPanel;
    [SerializeField] private GameObject board;

    private SpriteRenderer _boardRenderer;
    private BoardGenerator _boardGenerator;
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        _boardRenderer = board.GetComponent<SpriteRenderer>();
        _boardGenerator = board.GetComponent<BoardGenerator>();
        
#if  UNITY_EDITOR || UNITY_STANDALONE
        _boardGenerator.OnBoardScaled += async() =>
        {
            // 변경된 스케일이 렌더러에 적용 완료될 때까지 대기
            await Awaitable.EndOfFrameAsync();
            MoveUIPanel();
        };
#endif
    }
    
    public void MoveUIPanel()
    {
        Rect boardRect = GetScreenRectOfBoard();

        topPanel.position = new Vector3(0f, boardRect.yMax, 0f);
        bottomPanel.position = new Vector3(0f, boardRect.yMin, 0f);
    }
    
    private Rect GetScreenRectOfBoard()
    {
        Bounds bounds = _boardRenderer.bounds;

        Vector3 minPoint = _mainCamera.WorldToScreenPoint(bounds.min);
        Vector3 maxPoint = _mainCamera.WorldToScreenPoint(bounds.max);

        return new Rect(minPoint, maxPoint - minPoint);
    }
}