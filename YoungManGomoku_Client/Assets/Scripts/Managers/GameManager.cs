using System;
using UnityEngine;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ServerToClient;

public class GameManager : MonoBehaviour
{
    [SerializeField] private StoneMoveController stoneMoveController;
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

    public BasicPlayerData player;
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
        
        _eventManager.OnGameStart += () => enabled = true;
        _eventManager.OnGameEnd += () => enabled = false;
        _eventManager.OnStartSweeping += () => enabled = false;
        _eventManager.OnGameWin += () => _audioSource.PlayOneShot(winSound);
        _eventManager.OnGameLose += () => _audioSource.PlayOneShot(loseSound);
        _eventManager.OnGameDraw += () => _audioSource.PlayOneShot(drawSound);
        _eventManager.OnPlayerByoyomiPurchase += _ => IsByoyomiPurchased = true; 
        
        #region 서버에서 받아온 매칭 정보로 초기화
        SC_MatchResultDTO matchResult = PlayerDataFromWebServer.Instance.MatchResultDTO;
        // IsPlayerBlack = matchResult.어쩌고저쩌고;
        
        PlayerData myPlayer = PlayerDataFromWebServer.Instance.PlayerData;
        OpponentPlayerData opponent = matchResult.OpponentPlayer;
        player = new BasicPlayerData(myPlayer.Nickname, myPlayer.WinCount, myPlayer.DrawCount, myPlayer.LoseCount, myPlayer.Rating);
        oppositePlayer = new BasicPlayerData(opponent.Nickname, opponent.WinCount, opponent.DrawCount, opponent.LoseCount, opponent.Rating);

        SC_TimerSettingDTO timerInform = matchResult.TimerSettingDTO;
        PlayerTimer = new UserTimer(timerInform.MainTime, timerInform.ByoyomiCount, timerInform.ByoyomiSeconds);
        OppositeTimer = new UserTimer(10f, 2, 15f);
        ByoyomiPurchaseAmount = timerInform.ByoyomiPurchaseAmount;
        _eventManager.OnPlayerByoyomiPurchase += PlayerTimer.ByoyomiPurchased;
        _eventManager.OnOppositeByoyomiPurchase += OppositeTimer.ByoyomiPurchased;
        
        if (IsPlayerBlack)
        {
            var blackUser = player;
            var whiteUser = oppositePlayer;
            BoardInform.BlackWin += () => BoardInform.SaveRecord(blackUser,  whiteUser, "흑 승리");
            BoardInform.WhiteWin += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 패배");
            _eventManager.OnGameDraw += () => BoardInform.SaveRecord(blackUser, whiteUser, "무승부");
            _eventManager.OnPlayerSurrender += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 기권패");
            _eventManager.OnOppositeSurrender += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 기권승");
            _eventManager.OnOppositeDisconnectedWin += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 접속끊김승");
            _eventManager.OnPlayerDisconnectedLose += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 접속끊김패");
            _eventManager.OnPlayerTimeOut += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 시간패");
            _eventManager.OnOppositeTimeOut += () => BoardInform.SaveRecord(blackUser, whiteUser, "흑 시간승");
        }
        else
        {
            var blackUser = oppositePlayer;
            var whiteUser = player;
            BoardInform.BlackWin += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 패배");
            BoardInform.WhiteWin += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 승리");
            _eventManager.OnGameDraw += () => BoardInform.SaveRecord(blackUser, whiteUser, "무승부");
            _eventManager.OnPlayerSurrender += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 기권패");
            _eventManager.OnOppositeSurrender += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 기권승");
            _eventManager.OnOppositeDisconnectedWin += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 접속끊김승");
            _eventManager.OnPlayerDisconnectedLose += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 접속끊김패");
            _eventManager.OnPlayerTimeOut += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 시간패");
            _eventManager.OnOppositeTimeOut += () => BoardInform.SaveRecord(blackUser, whiteUser, "백 시간승");
        }
        
        // Board의 OnTurnChanged는 무르기 때도 실행되는 이벤트
        // 오직 착수만 의미하는 이벤트는 OnStoneMove
        // 착수 통신은 StoneMoveController에 일임하도록 짜기
        stoneMoveController.OnStoneMove += isBlackTurn =>
        {
            _nowPlayerTimer = isBlackTurn == IsPlayerBlack ? PlayerTimer : OppositeTimer;
            _lastTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            /* TODO: 서버에서 받은 타이머 정보로 양쪽 ReviseTimer 실행하기
             내 돌 착수 정보 전송 시점에 OnStoneMove 이벤트를 바로 발생시켜서 내 시간이 안 가도록 해야 함*/
            // 임시 코드, 초읽기 시간 복구용
            PlayerTimer.ReviseTimer(PlayerTimer.MainTime, PlayerTimer.ByoyomiCount);
            OppositeTimer.ReviseTimer(OppositeTimer.MainTime, Math.Max(OppositeTimer.ByoyomiCount, 1));
        };
        #endregion
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
