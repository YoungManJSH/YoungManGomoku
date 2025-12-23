using System;
using System.Collections.Generic;
using UnityEngine;
using YoungManGomoku_Protocol.TypeEnum.InGame;

/// <summary> 착수 제어 추상 클래스 </summary>
public abstract class StoneMover : MonoBehaviour
{
    [SerializeField] protected GameObject blackStone;
    [SerializeField] protected GameObject whiteStone;
    [SerializeField] private GameObject forbiddenMark;
    [SerializeField] private AudioClip deniedSound;
    [SerializeField] private AudioClip takeBackSound;
    [SerializeField] private MessageBoxManager messageBox;
    [SerializeField] private float previewAlpha;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject recentMark;

    protected Transform BlackParent { get; private set; }
    protected Transform WhiteParent { get; private set; }
    protected GameObject NowPreview { get; set; }
    protected Color PreviewColor { get; private set; }
    /// <summary> 직전에 인식한 오목판 좌표 </summary>
    protected (int row, int col) PrevCoord { get; private set; }
    
    private Board _boardInform;
    private SpriteRenderer _spriteRenderer;
    private AudioSource _audioSource;
    private Transform _forbiddenParent;
    private IngameBoardManager _ingameBoardManager;
    private HashSet<(int row, int col)> _forbiddenCoords;
    private (GameObject black, GameObject white) _recentStone;
    private Camera _mainCamera;
    private EventManager _em;
    
    private float _marginWorld; // Board 가장자리 인식하지 않는 영역 넓이
    private float _firstLineWorld; // 첫 번째 격자 위치
    private float _cellSizeWorld; // World 좌표 단위 격자 간격
    private bool _isBlackTurn;
    private bool _muteDeniedSound;
    
    /// <summary> 매개변수: 시작된 턴이 흑돌 턴인지 여부 </summary>
    public event Action<bool> OnStoneMove;
    
    protected void Awake()
    {
        _ingameBoardManager = GetComponent<IngameBoardManager>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;

        BlackParent = new GameObject("Black Parent").transform;
        WhiteParent = new GameObject("White Parent").transform;
        _forbiddenParent = new GameObject("Forbidden Parent").transform;
        BlackParent.SetParent(transform);
        WhiteParent.SetParent(transform);
        _forbiddenParent.SetParent(transform);
        _forbiddenCoords = new HashSet<(int row, int col)>();
        
        PreviewColor = new Color(1f, 1f, 1f, previewAlpha);
        CreatePreview();
        
        _boardInform = GameManager.Instance.BoardInform;
        PrevCoord = (-1, -1);
        _isBlackTurn = true;
        _muteDeniedSound = false;
        recentMark.SetActive(false);
        
        _ingameBoardManager.OnBoardScaled += async () => await CalcWorldValue();
        _boardInform.OnBlackUnmovable += OnBlackUnmovable;
        messageBox.OnOpened += MessageBoxOpened;
        messageBox.TurnBackToGame += MessageBoxClosed;

        _em = EventManager.Instance;
        _em.OnGameStart += OnGameStart;
        _em.OnGameEnd += DisableUpdate;
        _em.OnStartSweeping += DisableUpdate;
        _em.OnTakeBack += TakeBack;
        GameManager.Instance.PlayerTimer.OnTimeOut += DisableUpdate;
        
        OnAwake(); // 자식 클래스에서 추가적으로 정의한 Awake 로직
    }

    /// <summary> 자식 클래스에서 추가적으로 실행할 Awake </summary>
    protected virtual void OnAwake() {}

    protected void Start() => enabled = false;

    protected void Update() => InputProcessing();
    
    /// <summary> 모바일용 착수 확인 버튼 동작 함수 </summary>
    public abstract void MoveConfirmed();
    
    private void MessageBoxOpened() => enabled = false;
    protected abstract void MessageBoxClosed();

    /// <summary> 착수 위치를 미리 표시하는 반투명 preview 생성 </summary>
    protected abstract void CreatePreview();
    
    /// <summary>[row, col] 위치에 착수 시도, 금수일 경우 Forbidden mark 생성</summary>
    protected void MoveStone((int row, int col) coord)
    {
        Vector3 position = _spriteRenderer.bounds.min + new Vector3(_firstLineWorld + _cellSizeWorld * coord.col,
            _firstLineWorld + _cellSizeWorld * (Board.MaxCoord - coord.row), 0f);

        if (_boardInform.TryMoveStone(coord.row, coord.col))
        {
            PlaceStone(position);
            _audioSource.Play();
            if (_isBlackTurn) ClearForbiddenMarks();
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
    
    private void DisableUpdate() => enabled = false;
    
    /// <summary> [row, col] 위치에 착수 위치 미리보기 표시 </summary>
    private void UpdatePreview((int row, int col) coord)
    {
        NowPreview.transform.position = _spriteRenderer.bounds.min +
                                        new Vector3(_firstLineWorld + _cellSizeWorld * coord.col,
                                            _firstLineWorld + _cellSizeWorld * (Board.MaxCoord - coord.row), 0f);
        
        NowPreview.SetActive(true);
    }
    
    /// <summary> PC 및 모바일 착수 입력 프로세스 </summary>
    private void InputProcessing()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;
        
        if (TryGetBoardCoord(worldPos, out var coord) is false ||
            _boardInform[coord.row, coord.col] != StoneColorType.Empty ||
            _forbiddenCoords.Contains(coord))
        {
            NowPreview.SetActive(false);
            PrevCoord = coord;
            return;
        }

        if (Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space))
        {
            NowPreview.SetActive(false);
            PrevCoord = (-1, -1);
            MoveStone(coord);
            return;
        }
            
        if (coord != PrevCoord)
        {
            PrevCoord = coord;
            UpdatePreview(coord);
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
                if (coord == PrevCoord) return;

                PrevCoord = coord;
                
                if (_boardInform[coord.row, coord.col] != StoneColorType.Empty ||
                    _forbiddenCoords.Contains(coord))
                {
                    NowPreview.SetActive(false);
                    return;
                }

                UpdatePreview(coord);
            }
        }
#endif
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
    private async Awaitable CalcWorldValue()
    {
        await Awaitable.EndOfFrameAsync();

        var data = _ingameBoardManager.BoardData;
        float pixelToWorld = _spriteRenderer.bounds.size.x / data.TotalPixel;
        _marginWorld = (data.MarginSize - data.CellSize / 2f) * pixelToWorld;
        _firstLineWorld = data.MarginSize * pixelToWorld;
        _cellSizeWorld = data.CellSize * pixelToWorld;
    }
    
    /// <summary> 게임 시작 시 천원점 자동 착수 </summary>
    private void OnGameStart()
    {
        MoveStone((7, 7));
        recentMark.SetActive(true);
    }
    
    /// <summary> 무르기 적용 - 최근 돌 2개 제거 </summary>
    private void TakeBack()
    {
        Destroy(_recentStone.black);
        Destroy(_recentStone.white);
        _audioSource.PlayOneShot(takeBackSound);
        ClearForbiddenMarks();
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
