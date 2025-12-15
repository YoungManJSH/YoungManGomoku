using System;
using UnityEngine;

public class IngameBoardManager : MonoBehaviour
{
    [SerializeField] private BoardImageData boardData;

    [SerializeField, Tooltip("보드의 화면 기준 높이 (0 ~ 1)")]
    private float boardPosRatio;

    [SerializeField, Tooltip("작업 기준 해상도 (width, height)")]
    private Vector2Int baseResolution;

    public BoardImageData BoardData => boardData;
    public bool IsWide => _mainCamera.aspect > _baseRatio;

    private SpriteRenderer _spriteRenderer;
    private Camera _mainCamera;
    private float _baseRatio;
    private int _lastWidth;
    private int _lastHeight;
    
    public event Action OnBoardScaled;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sprite = BoardGenerator.GenerateBoard(boardData);
        _baseRatio = (float)baseResolution.x / baseResolution.y;
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        // 자식 오브젝트 생성 후에 통째로 크기를 조절하기 위해 Start에서 실행
        AdjustBoardScale();
        OnBoardScaled!.Invoke(); // 이벤트 구조로 순서 보장
        
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
    }

    private void Update()
    {
        if (Screen.width != _lastWidth || Screen.height != _lastHeight)
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            AdjustBoardScale();
            OnBoardScaled!.Invoke();
        }
    }

    private void AdjustBoardScale()
    {
        float worldSize = boardData.TotalPixel / boardData.PixelsPerUnit;
        float boardWidth;

        if (IsWide)
        {
            // 화면비가 기준 해상도 비율보다 가로로 더 길 때 높이에 맞춤
            boardWidth = _mainCamera.orthographicSize * 2f * _baseRatio;
        }
        else
        {
            // 화면비가 기준 해상도 비율보다 세로로 더 길 때 너비에 맞춤
            boardWidth = _mainCamera.orthographicSize * 2f * _mainCamera.aspect;
        }

        float scale = boardWidth / worldSize;
        transform.localScale = new Vector3(scale, scale, 1f);
        transform.position = _mainCamera.ViewportToWorldPoint(new Vector3(0.5f,
            boardPosRatio, Mathf.Abs(_mainCamera.transform.position.z)));
    }
}