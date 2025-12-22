using System.Collections.Generic;
using UnityEngine;
using YoungManGomoku_Protocol.TypeEnum.InGame;

/// <summary> 착수 제어 추상 클래스 </summary>
public abstract class StoneMover : MonoBehaviour
{
    [SerializeField] protected GameObject blackStone;
    [SerializeField] protected GameObject whiteStone;
    [SerializeField] protected GameObject forbiddenMark;
    [SerializeField] protected AudioClip deniedSound;
    [SerializeField] protected AudioClip takeBackSound;
    [SerializeField] protected MessageBoxManager messageBox;
    [SerializeField] protected float previewAlpha;
    [SerializeField] protected Camera mainCamera;
    [SerializeField] private GameObject recentMark;
    
    protected Board boardInform;
    protected SpriteRenderer spriteRenderer;
    protected AudioSource audioSource;
    protected Transform blackParent;
    protected Transform whiteParent;
    private Transform forbiddenParent;
    protected HashSet<(int row, int col)> forbiddenCoords;
    protected Color previewColor;
    protected bool isBlackTurn;
    protected (int row, int col) prevCoord; // 직전에 인식한 오목판 좌표
    
    private float marginWorld; // Board 가장자리 인식하지 않는 영역 넓이
    protected float firstLineWorld; // 첫 번째 격자 위치
    protected float cellSizeWorld; // World 좌표 단위 격자 간격
    
    private IngameBoardManager _ingameBoardManager;
    private (GameObject black, GameObject white) _recentStone;
    private Camera _mainCamera;
    private EventManager _em;

    protected void Awake()
    {
        _ingameBoardManager = GetComponent<IngameBoardManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        blackParent = new GameObject("Black Parent").transform;
        whiteParent = new GameObject("White Parent").transform;
        forbiddenParent = new GameObject("Forbidden Parent").transform;
        blackParent.SetParent(transform);
        whiteParent.SetParent(transform);
        forbiddenParent.SetParent(transform);
        forbiddenCoords = new HashSet<(int row, int col)>();
        
        previewColor = new Color(1f, 1f, 1f, previewAlpha);
        CreatePreview();
        
        boardInform = GameManager.Instance.BoardInform;
        prevCoord = (-1, -1);
        isBlackTurn = true;
        recentMark.SetActive(false);
        
        _ingameBoardManager.OnBoardScaled += async () => await CalcWorldValue();
        boardInform.OnBlackUnmovable += OnBlackUnmovable;
        messageBox.OnOpened += MessageBoxOpened;
        messageBox.TurnBackToGame += MessageBoxClosed;

        _em = EventManager.Instance;
        _em.OnGameStart += OnGameStart;
        _em.OnGameEnd += DisableUpdate;
        _em.OnStartSweeping += DisableUpdate;
        _em.OnTakeBack += TakeBack;
        
        OnAwake(); // 자식 클래스에서 추가적으로 정의한 Awake 로직
    }

    /// <summary> 자식 클래스에서 추가적으로 실행할 Awake </summary>
    protected virtual void OnAwake() {}

    protected void Start() => enabled = false;

    protected void Update() => InputProcessing();

    protected void OnDestroy()
    {
        boardInform.OnBlackUnmovable -= OnBlackUnmovable;
        messageBox.OnOpened -= MessageBoxOpened;
        messageBox.TurnBackToGame -= MessageBoxClosed;
        _em.OnGameStart -= OnGameStart;
        _em.OnGameEnd -= DisableUpdate;
        _em.OnStartSweeping -= DisableUpdate;
        _em.OnTakeBack -= TakeBack;
    }
    
    /// <summary> 모바일용 착수 확인 버튼 동작 함수 </summary>
    public abstract void MoveConfirmed();
    
    private void MessageBoxOpened() => enabled = false;
    protected abstract void MessageBoxClosed();

    /// <summary> PC 및 모바일 착수 입력 프로세스 </summary>
    protected abstract void InputProcessing();
    
    /// <summary>[row, col] 위치에 착수 시도, 금수일 경우 Forbidden mark 생성</summary>
    protected abstract void MoveStone((int row, int col) coord);

    /// <summary> 착수 위치를 미리 표시하는 반투명 preview 생성 </summary>
    protected abstract void CreatePreview();

    /// <summary> [row, col] 위치에 착수 위치 미리보기 표시 </summary>
    protected abstract void UpdatePreview((int row, int col) coord);
    
    /// <summary> position에 Stone prefab을 Instantiate </summary>
    protected void PlaceStone(Vector3 position)
    {
        recentMark.transform.position = position;

        if (isBlackTurn)
            _recentStone.black = Instantiate(blackStone, position, Quaternion.identity, blackParent);
        else
            _recentStone.white = Instantiate(whiteStone, position, Quaternion.identity, whiteParent);
    }
    
    /// <summary> 금수 표시 마크 생성 </summary>
    protected void PlaceForbiddenMark(Vector3 position)
        => Instantiate(forbiddenMark, position, Quaternion.identity, forbiddenParent);
    
    /// <summary> 금수 표시 마크 삭제 </summary>
    protected void ClearForbiddenMarks()
    {
        for (int i = forbiddenParent.childCount - 1; i >= 0; --i)
        {
            Destroy(forbiddenParent.GetChild(i).gameObject);
        }

        forbiddenCoords.Clear();
    }
    
    /// <summary> 월드 좌표를 오목판 좌표 변환 </summary>
    /// <param name="worldPos"> 입력된 월드 좌표 </param>
    /// <param name="coord"> 변환된 오목판 좌표 </param>
    /// <returns>
    /// <para>true: worldPos가 오목판 안에 있음, coord = 오목판 좌표</para>
    /// <para>false: worldPos가 오목판 밖에 있음, coord = (-1, -1)</para>
    /// </returns>
    protected bool TryGetBoardCoord(Vector3 worldPos, out (int row, int col) coord)
    {
        Bounds bounds = spriteRenderer.bounds;

        // 보드 영역에 들어가지 않는 좌표인 경우
        if (bounds.Contains(worldPos) is false)
        {
            coord = (-1, -1);
            return false;
        }

        Vector3 localPos = worldPos - bounds.min;

        if (localPos.x < marginWorld || bounds.size.x - marginWorld < localPos.x ||
            localPos.y < marginWorld || bounds.size.y - marginWorld < localPos.y)
        {
            coord = (-1, -1);
            return false;
        }

        coord = (Board.MaxCoord - Mathf.FloorToInt((localPos.y - marginWorld) / cellSizeWorld),
            Mathf.FloorToInt((localPos.x - marginWorld) / cellSizeWorld));

        return true;
    }
    
    /// <summary> 창 크기가 변할 때 월드좌표 기준값 다시 계산 </summary>
    private async Awaitable CalcWorldValue()
    {
        await Awaitable.EndOfFrameAsync();

        var data = _ingameBoardManager.BoardData;
        float pixelToWorld = spriteRenderer.bounds.size.x / data.TotalPixel;
        marginWorld = (data.MarginSize - data.CellSize / 2f) * pixelToWorld;
        firstLineWorld = data.MarginSize * pixelToWorld;
        cellSizeWorld = data.CellSize * pixelToWorld;
    }
    
    /// <summary> 게임 시작 시 천원점 자동 착수 </summary>
    private void OnGameStart()
    {
        MoveStone((7, 7));
        recentMark.SetActive(true);
    }

    private void DisableUpdate() => enabled = false;
    
    /// <summary> 무르기 적용 - 최근 돌 2개 제거 </summary>
    private void TakeBack()
    {
        Destroy(_recentStone.black);
        Destroy(_recentStone.white);
        audioSource.PlayOneShot(takeBackSound);
        ClearForbiddenMarks();
    }
    
    /// <summary> 흑돌 금수패 상황에서 적용할 연출 </summary>
    private void OnBlackUnmovable()
    {
        enabled = false;
        audioSource.volume = 0f;

        for (int row = 0; row < Board.BoardSize; ++row)
        {
            for (int col = 0; col < Board.BoardSize; ++col)
            {
                if (boardInform[row, col] == StoneColorType.Empty)
                {
                    MoveStone((row, col));
                }
            }
        }

        audioSource.volume = 1f;
    }
}
