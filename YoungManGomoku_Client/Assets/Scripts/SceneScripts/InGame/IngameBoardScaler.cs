using System;
using UnityEngine;

public class IngameBoardScaler : MonoBehaviour
{
    public const float TallRatio = 1080f / 2200f;
    public const float WideRatio = 1.5f;
    
    [SerializeField] private BoardImageData boardData;
    [SerializeField, Tooltip("세로 모드에서 보드의 화면 기준 높이 (0 ~ 1)")]
    private float boardPosY;
    
    public BoardImageData BoardData => boardData;

    private SpriteRenderer _spriteRenderer;
    private Camera _mainCamera;
    private float _worldSize;
    private int _lastWidth;
    private int _lastHeight;
    
    public event Action OnBoardScaled;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sprite = BoardGenerator.GenerateBoard(boardData);
        _worldSize = boardData.TotalPixel / boardData.PixelsPerUnit;
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
        float nowAspect = _mainCamera.aspect;
        
        /* nowAspect < WideRatio : 세로 모드 (보드를 화면 가로 사이즈에 맞춤)
         * nowAspect >= WideRatio : 가로 모드 (보드를 화면 세로 사이즈에 맞춤)
         *
         * (세부 사항)
         * nowAspect < TallRatio : 세로 모드 + 상하 레터박스
         * TallRatio <= nowAspect < WideRatio : 세로 모드 + 좌우 레터박스
         * WideRatio <= nowAspect : 가로 모드 + 좌우 레터박스 */
        float boardSize = _mainCamera.orthographicSize * 2f *
                         (nowAspect < WideRatio ? Mathf.Min(nowAspect, TallRatio) : 1f);
        
        
        float scale = boardSize / _worldSize;
        transform.localScale = new Vector3(scale, scale, 1f);
        
        if (nowAspect < WideRatio) // 세로 모드
        {
            transform.position = _mainCamera.ViewportToWorldPoint(new Vector3(0.5f,
                boardPosY, Mathf.Abs(_mainCamera.transform.position.z)));
        }
        else // 가로 모드
        {
            /* WideRatio 기준으로 보드를 좌측에 맞추게 됨
             * 따라서 nowAspect > WideRatio이면 좌우 레터박스가 생김 */ 
            float posX = -_mainCamera.orthographicSize * WideRatio +
                         _spriteRenderer.bounds.extents.x;
            
            transform.position = new Vector3(posX, 0f, Mathf.Abs(_mainCamera.transform.position.z));
        }
    }
}