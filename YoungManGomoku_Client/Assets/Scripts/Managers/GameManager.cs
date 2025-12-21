using System;
using UnityEngine;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class GameManager : MonoBehaviour
{
    [SerializeField] private StoneMoverSingle stoneMoverSingle;
    [SerializeField] private BoardSweeper oppositeSweeper;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioClip drawSound;
    
    public static GameManager Instance { get; private set; }
    
    public Board BoardInform { get; private set; }
    public bool IsPlayerBlack { get; private set; }
    public UserTimer PlayerTimer { get; private set; }
    public UserTimer OppositeTimer { get; private set; }
    public int ByoyomiPurchaseAmount { get; private set; }
    public bool IsByoyomiPurchased { get; private set; }

    public BasicPlayerData myPlayer;
    public BasicPlayerData oppositePlayer;
    
    private UserTimer _nowPlayerTimer;
    private EventManager _eventManager;
    private AudioSource _audioSource;
    private long _lastTime;

    // 다른 오브젝트들의 Awake가 일어나기 전에 이 Awake가 먼저 실행되어야 함!
    // 프로젝트 세팅 - Script Execution Order에서 이 스크립트를 -2로 설정하였음.
    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
        _eventManager = GetComponent<EventManager>();
        _audioSource = GetComponent<AudioSource>();
        BoardInform = new Board();
        IsByoyomiPurchased = false;
        
        #region 타이머 진행 제어 (스크립트 활성화 여부)
        _eventManager.OnGameStart += () => enabled = true;
        _eventManager.OnGameEnd += () => enabled = false;
        _eventManager.OnStartSweeping += () => enabled = false;
        _eventManager.OnBlackUnmovable += () => enabled = false;
        #endregion
        
        _eventManager.OnGameWin += () => _audioSource.PlayOneShot(winSound);
        _eventManager.OnGameLose += () => _audioSource.PlayOneShot(loseSound);
        _eventManager.OnGameDraw += () => _audioSource.PlayOneShot(drawSound);
        _eventManager.OnPlayerByoyomiPurchase += _ => IsByoyomiPurchased = true;
        
        #region 테스트용 임시 초기화
        IsPlayerBlack = true;
        myPlayer = new BasicPlayerData("흑돌 임시", 10, 5, 10, 15.5f);
        oppositePlayer = new BasicPlayerData("백돌 임시", 10, 5, 19, 2323.4f);
        PlayerTimer = new UserTimer(15f, 2, 15f);
        OppositeTimer = new UserTimer(15f, 2, 15f);
        ByoyomiPurchaseAmount = 2;
        #endregion
        
        #region 서버에서 받아온 매칭 정보로 초기화
        /*
        SC_MatchResultDTO matchResult = PlayerDataFromWebServer.Instance.MatchResultDTO;
        if (matchResult.MatchingSuccess is false ||
            matchResult.MyStoneColorType is StoneColorType.Empty)
        {
            return;
        }
            
        IsPlayerBlack = matchResult.MyStoneColorType is StoneColorType.Black;
        
        PlayerData my = PlayerDataFromWebServer.Instance.PlayerData;
        OpponentPlayerData opponent = matchResult.OpponentPlayer;
        myPlayer = new BasicPlayerData(my.Nickname, my.WinCount, my.DrawCount, my.LoseCount, my.Rating);
        oppositePlayer = new BasicPlayerData(opponent.Nickname, opponent.WinCount, opponent.DrawCount, opponent.LoseCount, opponent.Rating);

        SC_TimerSettingDTO timerInform = matchResult.TimerSettingDTO;
        PlayerTimer = new UserTimer(timerInform.MainTime, timerInform.ByoyomiCount, timerInform.ByoyomiSeconds);
        OppositeTimer = new UserTimer(10f, 2, 15f);
        ByoyomiPurchaseAmount = timerInform.ByoyomiPurchaseAmount;
        _eventManager.OnPlayerByoyomiPurchase += PlayerTimer.ByoyomiPurchased;
        _eventManager.OnOppositeByoyomiPurchase += OppositeTimer.ByoyomiPurchased;
        */
        #endregion
        
        #region 기보 저장
        if (IsPlayerBlack)
        {
            BasicPlayerData blackUser = myPlayer;
            BasicPlayerData whiteUser = oppositePlayer;

            _eventManager.OnPlayerGomoku += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "흑 승리");
            _eventManager.OnOppositeGomoku += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "흑 패배");
            _eventManager.OnBlackUnmovable += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "흑 금수패");
            _eventManager.OnGameDraw += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "무승부");
            _eventManager.OnPlayerSurrender += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "흑 기권패");
            _eventManager.OnOppositeSurrender += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "흑 기권승");
            _eventManager.OnOppositeDisconnectedWin += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "흑 접속끊김승");
            _eventManager.OnPlayerDisconnectedLose += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "흑 접속끊김패");
            _eventManager.OnPlayerTimeOut += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "흑 시간패");
            _eventManager.OnOppositeTimeOut += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "흑 시간승");
        }
        else
        {
            BasicPlayerData blackUser = oppositePlayer;
            BasicPlayerData whiteUser = myPlayer;
            
            _eventManager.OnPlayerGomoku += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "백 승리");
            _eventManager.OnOppositeGomoku += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "백 패배");
            _eventManager.OnBlackUnmovable += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "백 금수승");
            _eventManager.OnGameDraw += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "무승부");
            _eventManager.OnPlayerSurrender += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "백 기권패");
            _eventManager.OnOppositeSurrender += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "백 기권승");
            _eventManager.OnOppositeDisconnectedWin += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "백 접속끊김승");
            _eventManager.OnPlayerDisconnectedLose += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "백 접속끊김패");
            _eventManager.OnPlayerTimeOut += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "백 시간패");
            _eventManager.OnOppositeTimeOut += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "백 시간승");
        }
        #endregion
        
        // Board의 OnTurnChanged는 무르기 때도 실행되는 이벤트
        // 오직 착수만 의미하는 이벤트는 OnStoneMove
        stoneMoverSingle.OnStoneMove += isBlackTurn =>
        {
            _nowPlayerTimer = isBlackTurn == IsPlayerBlack ? PlayerTimer : OppositeTimer;
            _lastTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            /* TODO: 서버에서 받은 타이머 정보로 양쪽 ReviseTimer 실행하기
             내 돌 착수 정보 전송 시점에 OnStoneMove 이벤트를 바로 발생시켜서 내 시간이 안 가도록 해야 함*/
            
            // 임시 코드, 초읽기 시간 복구용
            PlayerTimer.ReviseTimer();
            OppositeTimer.ReviseTimer();
        };
    }
    
    private void OnDestroy() => Instance = null;

    private void Start() => enabled = false;
    
    private void Update()
    {
        long nowTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _nowPlayerTimer.Progress((nowTime - _lastTime) / 1000f);
        _lastTime = nowTime;
    }

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
