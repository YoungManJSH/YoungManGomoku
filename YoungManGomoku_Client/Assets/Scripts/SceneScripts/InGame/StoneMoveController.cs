using System.Collections.Generic;
using UnityEngine;

public class StoneMoveController : MonoBehaviour
{
    [SerializeField] private GameObject blackStone;
    [SerializeField] private GameObject whiteStone;
    [SerializeField] private GameObject forbiddenMark;
    [SerializeField] private AudioClip deniedSound;
    [SerializeField] private float previewAlpha;
    [SerializeField] private BoardGenerator boardGenerator;
    
    private Board _boardInform;
    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioSource;
    private Transform _blackParent;
    private Transform _whiteParent;
    private GameObject _blackPreview;
    private GameObject _whitePreview;
    private Transform _forbiddenParent;
    private HashSet<Vector2Int> _forbiddenCoords;
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
        
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;

        _blackParent = new GameObject("Black Parent").transform;
        _whiteParent = new GameObject("White Parent").transform;
        _blackParent.SetParent(transform);
        _whiteParent.SetParent(transform);
        
        _forbiddenParent = new GameObject("Forbidden Parent").transform;
        _forbiddenParent.SetParent(transform);
        _forbiddenCoords = new HashSet<Vector2Int>();
        
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

        if (TryGetBoardCoord(worldPos, out Vector2Int coord)
            && _boardInform[coord.x, coord.y] == Stone.Empty
            && _forbiddenCoords.Contains(coord) is false)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 position = _spriteRenderer.bounds.min +
                    new Vector3(_firstLineWorld + _cellSizeWorld * coord.y, _firstLineWorld + _cellSizeWorld * (Board.MaxCoord - coord.x), 0f);
                
                if (_boardInform.MoveStone(coord.x, coord.y))
                {
                    PlaceStone(position);
                    _audioSource.Play();
                    if (_isBlackTurn) ClearForbiddenMarks();
                    _isBlackTurn = _boardInform.NowTurn % 2 == 0;
                }
                else // 금수로 인한 착수 실패
                {
                    PlaceForbiddenMark(position);
                    _audioSource.PlayOneShot(deniedSound);
                    _forbiddenCoords.Add(coord);
                }
                
                (_isBlackTurn ? _blackPreview : _whitePreview).SetActive(false);
                return;
            }
            
            UpdatePreview(coord);
        }
        else
        {
            (_isBlackTurn ? _blackPreview : _whitePreview).SetActive(false);
        }
    }

    private void CreatePreview()
    {
        Color previewColor = new Color(1f, 1f, 1f, previewAlpha);
        
        _blackPreview = Instantiate(blackStone, _blackParent);
        _blackPreview.GetComponent<SpriteRenderer>().color = previewColor;
        _blackPreview.name = "Black Preview";
        _blackPreview.SetActive(false);
        
        _whitePreview = Instantiate(whiteStone, _whiteParent);
        _whitePreview.GetComponent<SpriteRenderer>().color = previewColor;
        _whitePreview.name = "White Preview";
        _whitePreview.SetActive(false);
    }

    private bool TryGetBoardCoord(Vector3 worldPos, out Vector2Int coord)
    {
        Bounds bounds = _spriteRenderer.bounds;
        
        // 보드 영역에 들어가지 않는 좌표인 경우
        if (bounds.Contains(worldPos) is false)
        {
            coord = new Vector2Int(-1, -1);
            return false;
        }

        Vector3 localPos = worldPos - bounds.min;

        if (localPos.x < _marginWorld || bounds.size.x - _marginWorld < localPos.x ||
            localPos.y < _marginWorld || bounds.size.y - _marginWorld < localPos.y)
        {
            coord = new Vector2Int(-1, -1);
            return false;
        }

        coord = new Vector2Int(Board.MaxCoord - Mathf.FloorToInt((localPos.y - _marginWorld) / _cellSizeWorld),
            Mathf.FloorToInt((localPos.x - _marginWorld) / _cellSizeWorld));
        return true;
    }

    /// <summary> [row, col] 위치에 착수 위치 미리보기 표시 </summary>
    private void UpdatePreview(Vector2Int coord)
    {
        GameObject nowPreview = _isBlackTurn ? _blackPreview :  _whitePreview;

        nowPreview.transform.position = _spriteRenderer.bounds.min +
                                        new Vector3(_firstLineWorld + _cellSizeWorld * coord.y,
                                            _firstLineWorld + _cellSizeWorld * (Board.MaxCoord - coord.x), 0f);
        
        nowPreview.SetActive(true);
    }

    /// <summary> [row, col] 위치에 Stone prefab을 Instantiate </summary>
    private void PlaceStone(Vector3 position)
    {
        if (_isBlackTurn)
        {
            Instantiate(blackStone, position, Quaternion.identity, _blackParent);
        }
        else
        {
            Instantiate(whiteStone, position, Quaternion.identity, _whiteParent);
        }
    }

    /// <summary> [row, col] 위치에 금수 마크 생성 </summary>
    private void PlaceForbiddenMark(Vector3 position) 
        => Instantiate(forbiddenMark, position,  Quaternion.identity, _forbiddenParent);

    private void ClearForbiddenMarks()
    {
        for (int i = _forbiddenParent.childCount - 1; i >= 0; --i)
        {
            Destroy(_forbiddenParent.GetChild(i).gameObject);
        }

        _forbiddenCoords.Clear();
    }
}
