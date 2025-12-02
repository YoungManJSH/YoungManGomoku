using UnityEngine;

public class StoneMoveController : MonoBehaviour
{
    [SerializeField] private GameObject blackStone;
    [SerializeField] private GameObject whiteStone;
    [SerializeField] private float previewAlpha;
    [SerializeField] private BoardGenerator boardGenerator;
    
    private Board _boardInform;
    private SpriteRenderer _spriteRenderer;
    private GameObject _blackPreview;
    private GameObject _whitePreview;
    private Camera _mainCamera;
    private float _pixelToWorld; // Board pixel to world ratio
    private float _marginWorld; // Board 가장자리 인식하지 않는 영역 넓이
    private float _firstLineWorld; // 첫 번째 격자 위치
    private float _cellSizeWorld; // World 좌표 단위 격자 간격
    private bool _isBlackTurn;
    
    private void Awake()
    {
        _boardInform = new Board();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        CreatePreview();
        _isBlackTurn = true;
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        _pixelToWorld = _spriteRenderer.bounds.size.x / boardGenerator.TotalPixel;
        _marginWorld = (boardGenerator.MarginSize - boardGenerator.CellSize / 2f) * _pixelToWorld;
        _firstLineWorld = boardGenerator.MarginSize * _pixelToWorld;
        _cellSizeWorld = boardGenerator.CellSize * _pixelToWorld;
    }

    private void Update()
    {
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;

        if (TryGetBoardCoord(worldPos, out int row, out int col)
            && _boardInform[row, col] == Stone.Empty)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (_boardInform.MoveStone(row, col))
                {
                    PlaceStone(row, col);
                    _isBlackTurn = _boardInform.NowTurn % 2 == 0;
                }
                else // 금수로 인한 착수 실패
                {
                    // 이 타이밍에 X자를 띄우는 식으로 연출하면 될 듯? 
                }
                
                (_isBlackTurn ? _blackPreview : _whitePreview).SetActive(false);
                return;
            }
            
            UpdatePreview(row, col);
        }
        else
        {
            (_isBlackTurn ? _blackPreview : _whitePreview).SetActive(false);
        }
    }

    private void CreatePreview()
    {
        Color previewColor = new Color(1f, 1f, 1f, previewAlpha);
        
        _blackPreview = Instantiate(blackStone);
        _blackPreview.GetComponent<SpriteRenderer>().color = previewColor;
        _blackPreview.SetActive(false);
        
        _whitePreview = Instantiate(whiteStone);
        _whitePreview.GetComponent<SpriteRenderer>().color = previewColor;
        _whitePreview.SetActive(false);
    }

    private bool TryGetBoardCoord(Vector3 worldPos, out int row, out int col)
    {
        Bounds bounds = _spriteRenderer.bounds;
        
        // 보드 영역에 들어가지 않는 좌표인 경우
        if (bounds.Contains(worldPos) is false)
        {
            row = col = -1;
            return false;
        }

        Vector3 localPos = worldPos - bounds.min;

        if (localPos.x < _marginWorld || bounds.size.x - _marginWorld < localPos.x ||
            localPos.y < _marginWorld || bounds.size.y - _marginWorld < localPos.y)
        {
            row = col = -1;
            return false;
        }
        
        col = Mathf.FloorToInt((localPos.x - _marginWorld) / _cellSizeWorld);
        row = Board.MaxCoord - Mathf.FloorToInt((localPos.y - _marginWorld) / _cellSizeWorld);
        return true;
    }

    private void UpdatePreview(int row, int col)
    {
        GameObject nowPreview = _isBlackTurn ? _blackPreview :  _whitePreview;

        nowPreview.transform.position = _spriteRenderer.bounds.min +
                                        new Vector3(_firstLineWorld + _cellSizeWorld * col,
                                            _firstLineWorld + _cellSizeWorld * (Board.MaxCoord - row), 0f);
        
        nowPreview.SetActive(true);
    }

    private void PlaceStone(int row, int col)
    {
        Vector3 posOffset = new Vector3(_firstLineWorld + _cellSizeWorld * col,
            _firstLineWorld + _cellSizeWorld * (Board.MaxCoord - row), 0f);
        
        Instantiate(_isBlackTurn ? blackStone : whiteStone,
            _spriteRenderer.bounds.min + posOffset,
            Quaternion.identity);
    }
}
