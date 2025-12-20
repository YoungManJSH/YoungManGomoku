using System;
using System.Collections.Generic;
using UnityEngine;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class StoneMoveController : MonoBehaviour
{
    [SerializeField] private GameObject blackStone;
    [SerializeField] private GameObject whiteStone;
    [SerializeField] private GameObject forbiddenMark;
    [SerializeField] private GameObject recentMark;
    [SerializeField] private AudioClip deniedSound;
    [SerializeField] private AudioClip takeBackSound;
    [SerializeField] private IngameBoardManager ingameBoardManager;
    [SerializeField] private MessageBoxManager messageBox;
    [SerializeField] private float previewAlpha;

    // 추후 보정되는 시간 값도 매개변수로 담아서 전달하기
    public event Action<bool> OnStoneMove;

    private Board _boardInform;
    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioSource;
    private Transform _blackParent;
    private Transform _whiteParent;
    private GameObject _blackPreview;
    private GameObject _whitePreview;
    private Transform _forbiddenParent;
    private HashSet<(int row, int col)> _forbiddenCoords;
    private (GameObject black, GameObject white) _recentStone;
    private Camera _mainCamera;

    private float _marginWorld; // Board 가장자리 인식하지 않는 영역 넓이
    private float _firstLineWorld; // 첫 번째 격자 위치
    private float _cellSizeWorld; // World 좌표 단위 격자 간격

    private (int row, int col) _prevCoord;
    private bool _isBlackTurn;

    private void Awake()
    {
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
        _forbiddenCoords = new HashSet<(int row, int col)>();

        CreatePreview();
        recentMark.SetActive(false);
        _prevCoord = (-1, -1);
        _isBlackTurn = true;
        _boardInform = GameManager.Instance.BoardInform;
        _boardInform.OnBlackUnmovable += OnBlackUnmovable;

        ingameBoardManager.OnBoardScaled += async () => await CalcWorldValue();

        messageBox.OnOpened += () => enabled = false;
        messageBox.TurnBackToGame += () => enabled = true;
        
        EventManager em = EventManager.Instance;
        em.OnGameStart += OnGameStart;
        em.OnGameEnd += () => enabled = false;
        em.OnStartSweeping += () => enabled = false;
        em.OnTakeBack += TakeBack;
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        enabled = false;
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;

        if (TryGetBoardCoord(worldPos, out (int row, int col) coord))
        {
            if (_boardInform[coord.row, coord.col] != StoneColorType.Empty || _forbiddenCoords.Contains(coord))
            {
                (_isBlackTurn ? _blackPreview : _whitePreview).SetActive(false);
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                (_isBlackTurn ? _blackPreview : _whitePreview).SetActive(false);
                MoveStone(coord);
                return;
            }

            if (coord != _prevCoord)
            {
                _prevCoord = coord;
                UpdatePreview(coord);
            }

            return;
        }

        (_isBlackTurn ? _blackPreview : _whitePreview).SetActive(false);

#elif UNITY_ANDROID
        if (Input.touchCount == 0) return;
        
        Touch touch = Input.GetTouch(0);
        
        if (touch.phase is TouchPhase.Began or TouchPhase.Moved)
        {
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(touch.position);
            worldPos.z = 0;

            if (TryGetBoardCoord(worldPos, out (int row, int col) coord))
            {
                if (_boardInform[coord.row, coord.col] != Stone.Empty || _forbiddenCoords.Contains(coord))
                {
                    (_isBlackTurn ? _blackPreview : _whitePreview).SetActive(false);
                    return;
                }

                if (coord != _prevCoord)
                {
                    _prevCoord = coord;
                    UpdatePreview(coord);
                }
            }
        }
#endif
    }

    public void MoveConfirmed()
    {
#if UNITY_ANDROID
        if (_isBlackTurn && _blackPreview.activeSelf)
        {
            _blackPreview.SetActive(false);
            MoveStone(_prevCoord);
        }
        else if (_isBlackTurn is false && _whitePreview.activeSelf)
        {
            _whitePreview.SetActive(false);
            MoveStone(_prevCoord);
        }
#endif
    }

    private void OnDisable()
    {
        _blackPreview.SetActive(false);
        _whitePreview.SetActive(false);
    }

    private void OnGameStart()
    {
        MoveStone((7, 7));
        recentMark.SetActive(true);
        enabled = true;
    }

    private async Awaitable CalcWorldValue()
    {
        await Awaitable.EndOfFrameAsync();

        var data = ingameBoardManager.BoardData;
        float pixelToWorld = _spriteRenderer.bounds.size.x / data.TotalPixel;
        _marginWorld = (data.MarginSize - data.CellSize / 2f) * pixelToWorld;
        _firstLineWorld = data.MarginSize * pixelToWorld;
        _cellSizeWorld = data.CellSize * pixelToWorld;
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

    private bool TryGetBoardCoord(Vector3 worldPos, out (int row, int col) coord)
    {
        Bounds bounds = _spriteRenderer.bounds;

        // 보드 영역에 들어가지 않는 좌표인 경우
        if (bounds.Contains(worldPos) is false)
        {
            coord = (-1, -1);
            return false;
        }

        Vector3 localPos = worldPos - bounds.min;

        if (localPos.x < _marginWorld || bounds.size.x - _marginWorld < localPos.x ||
            localPos.y < _marginWorld || bounds.size.y - _marginWorld < localPos.y)
        {
            coord = (-1, -1);
            return false;
        }

        coord = (Board.MaxCoord - Mathf.FloorToInt((localPos.y - _marginWorld) / _cellSizeWorld),
            Mathf.FloorToInt((localPos.x - _marginWorld) / _cellSizeWorld));

        return true;
    }

    /// <summary>[row, col] 위치에 착수 시도, 금수일 경우 Forbidden mark 생성</summary>
    private void MoveStone((int row, int col) coord)
    {
        Vector3 position = _spriteRenderer.bounds.min + new Vector3(_firstLineWorld + _cellSizeWorld * coord.col,
            _firstLineWorld + _cellSizeWorld * (Board.MaxCoord - coord.row), 0f);

        if (_boardInform.TryMoveStone(coord.row, coord.col))
        {
            PlaceStone(position);
            _audioSource.Play();
            if (_isBlackTurn) ClearForbiddenMarks();
            _isBlackTurn = _boardInform.NowTurn % 2 == 0;
            OnStoneMove!.Invoke(_isBlackTurn);
            return;
        }

        // 금수로 인한 착수 실패
        PlaceForbiddenMark(position);
        _audioSource.PlayOneShot(deniedSound);
        _forbiddenCoords.Add(coord);
    }

    private void OnBlackUnmovable()
    {
        enabled = false;
        _audioSource.volume = 0f;

        for (int row = 0; row < Board.BoardSize; ++row)
        {
            for (int col = 0; col < Board.BoardSize; ++col)
            {
                if (_boardInform[row, col] == StoneColorType.Empty)
                {
                    MoveStone((row, col));
                }
            }
        }

        _audioSource.volume = 1f;
    }

    /// <summary> [row, col] 위치에 착수 위치 미리보기 표시 </summary>
    private void UpdatePreview((int row, int col) coord)
    {
        GameObject nowPreview = _isBlackTurn ? _blackPreview : _whitePreview;

        nowPreview.transform.position = _spriteRenderer.bounds.min +
                                        new Vector3(_firstLineWorld + _cellSizeWorld * coord.col,
                                            _firstLineWorld + _cellSizeWorld * (Board.MaxCoord - coord.row), 0f);

        nowPreview.SetActive(true);
    }

    /// <summary> position에 Stone prefab을 Instantiate </summary>
    private void PlaceStone(Vector3 position)
    {
        recentMark.transform.position = position;

        if (_isBlackTurn)
        {
            _recentStone.black = Instantiate(blackStone, position, Quaternion.identity, _blackParent);
        }
        else
        {
            _recentStone.white = Instantiate(whiteStone, position, Quaternion.identity, _whiteParent);
        }
    }

    private void TakeBack()
    {
        Destroy(_recentStone.black);
        Destroy(_recentStone.white);
        _audioSource.PlayOneShot(takeBackSound);
        ClearForbiddenMarks();
    }

    /// <summary> position에 금수 마크 생성 </summary>
    private void PlaceForbiddenMark(Vector3 position)
        => Instantiate(forbiddenMark, position, Quaternion.identity, _forbiddenParent);

    private void ClearForbiddenMarks()
    {
        for (int i = _forbiddenParent.childCount - 1; i >= 0; --i)
        {
            Destroy(_forbiddenParent.GetChild(i).gameObject);
        }

        _forbiddenCoords.Clear();
    }
}