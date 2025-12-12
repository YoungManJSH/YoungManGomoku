using System;
using UnityEngine;

public class IngameBoardManager : MonoBehaviour
{
    [SerializeField] private BoardImageData boardData;
    public BoardImageData BoardData => boardData;
    
    private SpriteRenderer _spriteRenderer;
    private Camera _mainCamera;
    
#if UNITY_EDITOR || UNITY_STANDALONE
    private int _lastWidth;
    private int _lastHeight;
#endif
    
    public event Action OnBoardScaled;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sprite = BoardGenerator.GenerateBoard(boardData);
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        // 자식 오브젝트 생성 후에 통째로 크기를 조절하기 위해 Start에서 실행 
        AdjustBoardScale();
        OnBoardScaled!.Invoke(); // 이벤트 구조로 순서 보장
        
#if UNITY_EDITOR || UNITY_STANDALONE
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
#endif
    }
    
#if  UNITY_EDITOR || UNITY_STANDALONE
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
#endif

    private void AdjustBoardScale()
    {
        float worldSize = boardData.TotalPixel / boardData.PixelsPerUnit;
        float screenWidth = _mainCamera.orthographicSize * 2f * _mainCamera.aspect;
        float scale = screenWidth / worldSize;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}