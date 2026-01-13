using System;
using System.Collections.Generic;
using UnityEngine;
using YoungManGomoku_Protocol.TypeEnum.InGame;

/// <summary> 착수 제어 추상 클래스 </summary>
public abstract class StoneMover : MonoBehaviour
{
    [SerializeField] protected StoneController blackStone;
    [SerializeField] protected StoneController whiteStone;
    [SerializeField] private GameObject forbiddenMark;
    [SerializeField] private AudioClip deniedSound;
    [SerializeField] private AudioClip takeBackSound;
    [SerializeField] private MessageBoxManager messageBox;
    [SerializeField] private float previewAlpha;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject recentMark;

    [Header("키보드 입력 세팅값, 초 단위"),
     SerializeField, Tooltip("연속 이동이 작동할 때까지의 시간")]
    private float delay;
    [SerializeField, Tooltip("다음 이동까지의 시간 간격")]
    private float interval;
    
    
    protected Transform BlackParent { get; private set; }
    protected Transform WhiteParent { get; private set; }
    protected GameObject NowPreview { get; set; }
    protected Color PreviewColor { get; private set; }
    /// <summary> 직전에 인식한 오목판 좌표 </summary>
    protected (int row, int col) NowCoord { get; private set; }
    
    private Board _boardInform;
    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioSource;
    private Transform _forbiddenParent;
    private IngameBoardScaler _ingameBoardScaler;
    private BoardImageData _boardImageData;
    private HashSet<(int row, int col)> _forbiddenCoords;
    private (StoneController black, StoneController white) _recentStone;
    private StoneController[,] _stoneObjects;
    private Camera _mainCamera;
    private EventManager _em;

    private Vector3 _prevMousePos;
    private float _marginWorld; // Board 가장자리 인식하지 않는 영역 넓이
    private float _firstLineWorld; // 첫 번째 격자 위치
    private float _cellSizeWorld; // World 좌표 단위 격자 간격

    private float _holdTime;
    private bool _isMoving;
    private bool _isBlackTurn;
    private bool _muteDeniedSound;
    
    /// <summary> 매개변수: 시작된 턴이 흑돌 턴인지 여부 </summary>
    public event Action<bool> OnStoneMove;
    
    protected void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _audioSource = GetComponent<AudioSource>();
        _ingameBoardScaler = GetComponent<IngameBoardScaler>();
        _boardImageData = _ingameBoardScaler.BoardData;
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;

        BlackParent = new GameObject("Black Parent").transform;
        WhiteParent = new GameObject("White Parent").transform;
        _forbiddenParent = new GameObject("Forbidden Parent").transform;
        BlackParent.SetParent(transform);
        WhiteParent.SetParent(transform);
        _forbiddenParent.SetParent(transform);
        _forbiddenCoords = new HashSet<(int row, int col)>();
        _stoneObjects = new StoneController[Board.BoardSize, Board.BoardSize];
        
        _boardInform = GameManager.Instance.BoardInform;
        NowCoord = (-1, -1);
        _isBlackTurn = true;
        _muteDeniedSound = false;
        recentMark.SetActive(false);
        
        _ingameBoardScaler.OnBoardScaled += CalcWorldValue;
        _boardInform.OnBlackGomoku += async() => await OnGomoku(StoneColorType.Black);
        _boardInform.OnWhiteGomoku += async() => await OnGomoku(StoneColorType.White);
        _boardInform.OnBlackUnmovable += OnBlackUnmovable;
        messageBox.OnOpened += MessageBoxOpened;
        messageBox.TurnBackToGame += MessageBoxClosed;
        messageBox.TurnBackToGame += UnmarkTakeBack;

        _em = EventManager.Instance;
        _em.OnGameStart += OnGameStart;
        _em.OnGameEnd += DisableUpdate;
        _em.OnGameEnd += UnmarkTakeBack;
        _em.OnTakeBackRequested += _ => MarkTakeBack();
        _em.OnTakeBack += OnTakeBack;
        GameManager.Instance.PlayerTimer.OnTimeOut += DisableUpdate;
        
        PreviewColor = new Color(1f, 1f, 1f, previewAlpha);
        OnAwake(); // 자식 클래스에서 추가적으로 정의한 Awake 로직
        CreatePreview(); // 추상 함수는 OnAwake 다음으로 순서 보장
    }

    /// <summary> 자식 클래스에서 추가적으로 실행할 Awake </summary>
    protected virtual void OnAwake() {}

    protected void Start() => enabled = false;

    protected void Update() => InputProcessing();
    
    /// <summary> 모바일용 착수 확인 버튼 동작 함수 </summary>
    public abstract void MoveConfirmed();
    
    /// <summary> 무르기가 적용될 돌을 표시 </summary>
    public void MarkTakeBack()
    {
        if (_recentStone.black == null || _recentStone.white == null)
        {
            Debug.LogError("무르기를 할 수 없는 상항에서의 무르기 요청!");
            EventManager.Instance.ServerReplyFailed();
            return;
        }
        
        _recentStone.black.XMarking(isActivate: true);
        _recentStone.white.XMarking(isActivate: true);
    }
    
    /// <summary> 무르기가 적용될 돌의 표시를 해제 </summary>
    private void UnmarkTakeBack()
    {
        _recentStone.black?.XMarking(isActivate: false);
        _recentStone.white?.XMarking(isActivate: false);
    }
    
    private void MessageBoxOpened() => enabled = false;
    protected abstract void MessageBoxClosed();

    /// <summary> 착수 위치를 미리 표시하는 반투명 preview 생성 </summary>
    protected abstract void CreatePreview();
    
    /// <summary>[row, col] 위치에 착수 시도, 금수일 경우 Forbidden mark 생성</summary>
    protected void MoveStone((int row, int col) coord)
    {
        NowCoord = coord;
        
        Vector3 position = _spriteRenderer.bounds.min + new Vector3(_firstLineWorld + _cellSizeWorld * coord.col,
            _firstLineWorld + _cellSizeWorld * (Board.MaxCoord - coord.row), 0f);

        if (_boardInform.TryMoveStone(coord.row, coord.col))
        {
            PlaceStone(position);
            _audioSource.Play();
            
            if (_isBlackTurn)
            {
                ClearForbiddenMarks();
                _stoneObjects[coord.row, coord.col] = _recentStone.black;
            }
            else
            {
                _stoneObjects[coord.row, coord.col] = _recentStone.white;
            }
            
            _isBlackTurn = _boardInform.NowTurn % 2 == 0;
            OnStoneMove?.Invoke(_isBlackTurn);
            return;
        }

        // 금수로 인한 착수 실패
        PlaceForbiddenMark(position);
        _forbiddenCoords.Add(coord);
        if (_muteDeniedSound is false)
        {
            _audioSource.PlayOneShot(deniedSound);
        }
    }
    
    protected void DisableUpdate() => enabled = false;
    
    /// <summary> [row, col] 위치에 착수 위치 미리보기 표시 </summary>
    private void UpdatePreview((int row, int col) coord)
    {
        NowCoord = coord;
        
        NowPreview.transform.position = _spriteRenderer.bounds.min +
                                        new Vector3(_firstLineWorld + _cellSizeWorld * coord.col,
                                            _firstLineWorld + _cellSizeWorld * (Board.MaxCoord - coord.row), 0f);
        
        NowPreview.SetActive(true);
    }
    
    /// <summary> PC 및 모바일 착수 입력 프로세스 </summary>
    private void InputProcessing()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        if (mousePos != _prevMousePos)
        {
            _prevMousePos = mousePos;

            if (TryGetBoardCoord(mousePos, out var coord) &&
                coord != NowCoord &&
                _boardInform[coord.row, coord.col] == StoneColorType.Empty &&
                _forbiddenCoords.Contains(coord) is false)
            {
                UpdatePreview(coord);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            NowPreview.SetActive(false);
            
            if (TryGetBoardCoord(mousePos, out var coord) &&
                _boardInform[coord.row, coord.col] == StoneColorType.Empty &&
                _forbiddenCoords.Contains(coord) is false)
            {
                MoveStone(coord);
            }

            return;
        }

        if (Input.GetButtonDown("Cancel"))
        {
            NowPreview.SetActive(false);
            return;
        }

        if (Input.GetButtonDown("Submit"))
        {
            if (NowPreview.activeSelf)
            {
                NowPreview.SetActive(false);
                MoveStone(NowCoord);
            }

            return;
        }
        
        if (Input.GetButtonDown("Horizontal") || Input.GetButtonDown("Vertical"))
        {
            int hor = (int)Input.GetAxisRaw("Horizontal");
            int ver = (int)Input.GetAxisRaw("Vertical");

            if (TryChangeCoord(out var coord, hor, ver))
            {
                UpdatePreview(coord);
            }

            _holdTime = 0f;
            _isMoving = false;
            return;
        }

        if (Input.GetButton("Horizontal") || Input.GetButton("Vertical"))
        {
            _holdTime += Time.deltaTime;

            if (_isMoving)
            {
                if (_holdTime < interval) return;
                
                int hor = (int)Input.GetAxisRaw("Horizontal");
                int ver = (int)Input.GetAxisRaw("Vertical");

                if (TryChangeCoord(out var coord, hor, ver))
                {
                    UpdatePreview(coord);
                }

                _holdTime = 0f;
                return;
            }
            
            if (_holdTime >= delay)
            {
                _isMoving = true;
                _holdTime = interval; // 다음 Update 때 바로 이동할 수 있도록
            }

            return;
        }

        if (Input.GetButtonUp("Horizontal") || Input.GetButtonUp("Vertical"))
        {
            _holdTime = 0f;
            _isMoving = false;
        }
        
#elif UNITY_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            NowPreview.SetActive(false);
            return;
        }
        
        if (Input.touchCount == 0) return;
        
        Touch touch = Input.GetTouch(0);
        
        if (touch.phase is TouchPhase.Began or TouchPhase.Moved)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(touch.position);
            worldPos.z = 0;

            if (TryGetBoardCoord(worldPos, out var coord))
            {
                if (coord == NowCoord) return;
                
                if (_boardInform[coord.row, coord.col] != StoneColorType.Empty ||
                    _forbiddenCoords.Contains(coord))
                {
                    NowCoord = coord;
                    NowPreview.SetActive(false);
                    return;
                }

                UpdatePreview(coord);
            }
        }
#endif
    }

    /// <summary>NowCoord에서 입력 방향으로 좌표 이동을 시도</summary>
    /// <param name="coord">Circular navigation 방식으로 이동된 좌표</param>
    /// <param name="hor">Horizontal 이동값</param>
    /// <param name="ver">Vertical 이동값</param>
    /// <returns>
    /// <para>true: 비어있는 좌표로 이동하였음</para>
    /// <para>false: 한 바퀴 순환하여도 비어있는 좌표가 없음</para>
    /// </returns>
    private bool TryChangeCoord(out (int row, int col) coord, int hor, int ver)
    {
        int row = NowCoord.row;
        int col = NowCoord.col;
            
        for (int i = 0; i < Board.BoardSize; ++i)
        {
            row -= ver;
            col += hor;

            if (row < 0) row = Board.MaxCoord;
            else if (Board.MaxCoord < row) row = 0;

            if (col < 0) col = Board.MaxCoord;
            else if (Board.MaxCoord < col) col = 0;
                
            if (_boardInform[row, col] == StoneColorType.Empty &&
                _forbiddenCoords.Contains((row, col)) is false)
            {
                coord = (row, col);
                return true;
            }
        }

        coord = NowCoord;
        return false;
    }
    
    /// <summary> position에 Stone prefab을 Instantiate </summary>
    private void PlaceStone(Vector3 position)
    {
        recentMark.transform.position = position;

        if (_isBlackTurn)
            _recentStone.black = Instantiate(blackStone, position, Quaternion.identity, BlackParent);
        else
            _recentStone.white = Instantiate(whiteStone, position, Quaternion.identity, WhiteParent);
    }
    
    /// <summary> 금수 표시 마크 생성 </summary>
    private void PlaceForbiddenMark(Vector3 position)
        => Instantiate(forbiddenMark, position, Quaternion.identity, _forbiddenParent);
    
    /// <summary> 금수 표시 마크 삭제 </summary>
    private void ClearForbiddenMarks()
    {
        for (int i = _forbiddenParent.childCount - 1; i >= 0; --i)
        {
            Destroy(_forbiddenParent.GetChild(i).gameObject);
        }

        _forbiddenCoords.Clear();
    }
    
    /// <summary> 월드 좌표를 오목판 좌표 변환 </summary>
    /// <param name="worldPos"> 입력된 월드 좌표 </param>
    /// <param name="coord"> 변환된 오목판 좌표 </param>
    /// <returns>
    /// <para>true: worldPos가 오목판 안에 있음, coord = 오목판 좌표</para>
    /// <para>false: worldPos가 오목판 밖에 있음, coord = (-1, -1)</para>
    /// </returns>
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
    
    /// <summary> 창 크기가 변할 때 월드좌표 기준값 다시 계산 </summary>
    private void CalcWorldValue()
    {
        float pixelToWorld = _spriteRenderer.bounds.size.x / _boardImageData.TotalPixel;
        _marginWorld = (_boardImageData.MarginSize - _boardImageData.CellSize / 2f) * pixelToWorld;
        _firstLineWorld = _boardImageData.MarginSize * pixelToWorld;
        _cellSizeWorld = _boardImageData.CellSize * pixelToWorld;
    }
    
    /// <summary> 게임 시작 시 천원점 자동 착수 </summary>
    private void OnGameStart()
    {
        MoveStone((7, 7));
        recentMark.SetActive(true);
    }
    
    /// <summary> 무르기 적용 - 최근 돌 2개 제거 </summary>
    private void OnTakeBack(bool isAccepted)
    {
        if (isAccepted is false)
        {
            UnmarkTakeBack();
            return;
        }
        
        if (_recentStone.black == null || _recentStone.white == null)
        {
            Debug.LogError("무르기를 할 수 없는 상항에서의 무르기 실행!");
            EventManager.Instance.ServerReplyFailed();
            return;
        }
        
        NowCoord = (7, 7); // NowCoord가 물러진 위치에 있지 않도록 변경 
        recentMark.transform.position =
            (_isBlackTurn ? _recentStone.black : _recentStone.white).transform.position;
        _recentStone.black.TakeBack();
        _recentStone.white.TakeBack();
        _audioSource.PlayOneShot(takeBackSound);
        ClearForbiddenMarks();
    }

    /// <summary> 오목 상황에서 적용할 연출 </summary>
    private async Awaitable OnGomoku(StoneColorType stoneColor)
    {
        // 월드에서 착수 처리가 완료되고 다음 프레임에 실행 
        await Awaitable.NextFrameAsync();
        
        Dictionary<LineDirection, List<(int row, int col)>> informs =
            JudgeMove.OmokLineInforms(_boardInform, NowCoord, stoneColor);
        
        foreach (var coordList in informs.Values)
        {
            foreach (var coord in coordList)
            {
                _stoneObjects[coord.row, coord.col].GomokuAction();
            }
        }
    }
    
    /// <summary> 흑돌 금수패 상황에서 적용할 연출 </summary>
    private void OnBlackUnmovable()
    {
        enabled = false;
        _muteDeniedSound = true;

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
    }
}
