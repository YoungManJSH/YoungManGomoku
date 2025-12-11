using UnityEngine;
using YoungManGomoku_Protocol;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int mainTimeSeconds;
    [SerializeField] private int byoyomiCounts;
    [SerializeField] private int byoyomiSeconds;
    [SerializeField] private StoneMoveController stoneMoveController;
    [SerializeField] private int byoyomiPurchaseAmount;
    public int ByoyomiPurchaseAmount => byoyomiPurchaseAmount;
    
    public static GameManager Instance { get; private set; }
    
    public Board BoardInform { get; private set; }
    public bool IsPlayerBlack { get; private set; }
    public TimeController PlayerTime { get; private set; }
    public TimeController OppositeTime { get; private set; }
    
    private TimeController _nowPlayerTime;
    private EventManager _eventManager;

    private PlayerData _player;
    private PlayerData _oppositePlayer;
    
    // 다른 오브젝트들의 Awake가 일어나기 전에 이 Awake가 먼저 실행되어야 함!
    // 프로젝트 세팅 - Script Execution Order에서 이 스크립트를 -2로 설정하였음.
    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
        _eventManager = GetComponent<EventManager>();
        
        BoardInform = new Board();
        PlayerTime = new TimeController(mainTimeSeconds, byoyomiCounts, byoyomiSeconds);
        OppositeTime = new TimeController(mainTimeSeconds, byoyomiCounts, byoyomiSeconds);
        
        _eventManager.OnPlayerByoyomiPurchase += PlayerTime.ByoyomiPurchase;
        _eventManager.OnOppositeByoyomiPurchase += OppositeTime.ByoyomiPurchase;
        
        _eventManager.OnGameStart += () => enabled = true;
        _eventManager.OnGameEnd += () => enabled = false;
        
        #region 테스트용 임시 초기화 영역, 이후 서버에서 받아온 정보로 수정
        IsPlayerBlack = true;
        _player = new PlayerData()
        {
            Nickname = "황금뇽재환띠",
            WinCount = 80,
            DrawCount = 1,
            LoseCount = 75
        };
        _oppositePlayer = new PlayerData()
        {
            Nickname = "은색뇽재환띠",
            WinCount = 55,
            DrawCount = 3,
            LoseCount = 43,
        };

        if (IsPlayerBlack)
        {
            var blackUser = _player;
            var whiteUser = _oppositePlayer;
            BoardInform.BlackWin += () => BoardInform.SaveRecord(blackUser,  whiteUser, "흑 승리");
            BoardInform.WhiteWin += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 패배");
            _eventManager.OnGameDraw += () => BoardInform.SaveRecord(blackUser, whiteUser, "무승부");
            _eventManager.OnPlayerSurrender += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 기권패");
            _eventManager.OnOppositeSurrender += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 기권승");
            _eventManager.OnOppositeDisconnectedWin += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 접속끊김승");
            PlayerTime.OnTimeLose += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 시간패");
            OppositeTime.OnTimeLose += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 시간승");
        }
        else
        {
            var blackUser = _oppositePlayer;
            var whiteUser = _player;
            BoardInform.BlackWin += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 패배");
            BoardInform.WhiteWin += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 승리");
            _eventManager.OnGameDraw += () => BoardInform.SaveRecord(blackUser, whiteUser, "무승부");
            _eventManager.OnPlayerSurrender += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 기권패");
            _eventManager.OnOppositeSurrender += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 기권승");
            _eventManager.OnOppositeDisconnectedWin += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 접속끊김승");
            PlayerTime.OnTimeLose += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 시간패");
            OppositeTime.OnTimeLose += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 시간승");
        }
        
        stoneMoveController.OnStoneMove += isBlackTurn =>
        {
            _nowPlayerTime = isBlackTurn == IsPlayerBlack ? PlayerTime : OppositeTime;
            (isBlackTurn == IsPlayerBlack ? OppositeTime : PlayerTime).InitByoyomiSecond();
        };
        #endregion
    }

    private void Start() => enabled = false;
    private void Update() => _nowPlayerTime.TimeProgress(Time.deltaTime);

    // 추후 서버에 무르기 요청 구매를 요청하는 코드로 수정하기! 
    public void SendTakeBackRequest()
    {
        if (BoardInform.TryTakeBack() is false)
        {
            Debug.LogError("무르기 로직 에러, NowTurn, record.Count 확인 요망!");
            return;
        }

        _eventManager.TakeBack();
    }
}
