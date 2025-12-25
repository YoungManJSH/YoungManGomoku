using System;
using UnityEngine;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class GameManager : MonoBehaviour
{
    [SerializeField] private StoneMover stoneMover;
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
    public BasicPlayerData MyPlayer { get; private set; }
    public BasicPlayerData OppositePlayer { get; private set; }
    public string IdToken { get; private set; }

    private UserTimer _nowPlayerTimer;
    private EventManager _eventManager;
    private NetworkManager _networkManager;
    private AudioSource _audioSource;
    private long _lastTime;

    // 다른 오브젝트들의 Awake가 일어나기 전에 이 Awake가 먼저 실행되어야 함!
    // 프로젝트 세팅 - Script Execution Order에서 이 스크립트를 -2로 설정하였음.
    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
        _eventManager = GetComponent<EventManager>();
        _networkManager = GetComponent<NetworkManager>();
        _audioSource = GetComponent<AudioSource>();
        BoardInform = new Board();
        IsByoyomiPurchased = false;
        
        #region 타이머 진행 제어 (Update 활성화 여부)
        _eventManager.OnGameStart += () => enabled = true;
        _eventManager.OnGameEnd += () => enabled = false;
        _eventManager.OnStartSweeping += () => enabled = false;
        _eventManager.OnBlackUnmovable += () => enabled = false;
        #endregion
        
        _eventManager.OnGameWin += () => _audioSource.PlayOneShot(winSound);
        _eventManager.OnGameLose += () => _audioSource.PlayOneShot(loseSound);
        _eventManager.OnGameDraw += PlayDrawSound;
        _eventManager.OnServerReplyFailed += PlayDrawSound;
        _networkManager.OnRequestFailed += _ => PlayDrawSound();
        _eventManager.OnPlayerByoyomiPurchase += _ => IsByoyomiPurchased = true;
        
        #region 테스트용 임시 초기화
        IsPlayerBlack = true;
        MyPlayer = new BasicPlayerData("흑돌 임시", 10, 5, 10, 15.5f);
        OppositePlayer = new BasicPlayerData("백돌 임시", 10, 5, 19, 2323.4f);
        PlayerTimer = new UserTimer(15f, 2, 15f);
        OppositeTimer = new UserTimer(15f, 2, 15f);
        ByoyomiPurchaseAmount = 2;
        #endregion
        
        #region 서버에서 받아온 매칭 정보로 초기화
        IdToken = PlayerDataFromWebServer.Instance.IDToken;
        
        PlayerData my = PlayerDataFromWebServer.Instance.PlayerData;
        MyPlayer = new BasicPlayerData(my.Nickname, my.WinCount, my.DrawCount, my.LoseCount, my.Rating);
        
        SC_MatchResultDTO matchResult = PlayerDataFromWebServer.Instance.MatchResultDTO;
        if (matchResult.MatchingSuccess is false ||
            matchResult.MyStoneColorType is StoneColorType.Empty)
        {
            OppositePlayer = new BasicPlayerData(String.Empty, 0, 0, 0, 0f);
            PlayerTimer = new UserTimer(0f, 3, 30f);
            OppositeTimer = new UserTimer(0f, 3, 30f);
            return;
        }
            
        IsPlayerBlack = matchResult.MyStoneColorType is StoneColorType.Black;
        
        OpponentPlayerData opponent = matchResult.OpponentPlayer;
        OppositePlayer = new BasicPlayerData(opponent.Nickname, opponent.WinCount, opponent.DrawCount, opponent.LoseCount, opponent.Rating);

        SC_TimerSettingDTO timerInform = matchResult.TimerSettingDTO;
        PlayerTimer = new UserTimer(timerInform.MainTime, timerInform.ByoyomiCount, timerInform.ByoyomiSeconds);
        OppositeTimer = new UserTimer(timerInform.MainTime, timerInform.ByoyomiCount, timerInform.ByoyomiSeconds);
        ByoyomiPurchaseAmount = timerInform.ByoyomiPurchaseAmount;
        _eventManager.OnPlayerByoyomiPurchase += PlayerTimer.ByoyomiPurchased;
        _eventManager.OnOppositeByoyomiPurchase += OppositeTimer.ByoyomiPurchased;
        #endregion
        
        #region 기보 저장
        if (IsPlayerBlack)
        {
            BasicPlayerData blackUser = MyPlayer;
            BasicPlayerData whiteUser = OppositePlayer;

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
            BasicPlayerData blackUser = OppositePlayer;
            BasicPlayerData whiteUser = MyPlayer;
            
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
        stoneMover.OnStoneMove += OnStoneMove;
    }
    
    private void OnDestroy() => Instance = null;

    private void Start() => enabled = false;
    
    private void Update()
    {
        long nowTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _nowPlayerTimer.Progress((nowTime - _lastTime) / 1000f);
        _lastTime = nowTime;
    }

    private void PlayDrawSound() => _audioSource.PlayOneShot(drawSound);

    private async void OnStoneMove(bool isBlackTurn)
    {
        try
        {
            _lastTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (isBlackTurn == IsPlayerBlack)
            {
                // 플레이어의 턴이 시작한 경우
                _nowPlayerTimer = PlayerTimer;
            }
            else
            {
                // 상대방의 턴이 시작한 경우
                _nowPlayerTimer = OppositeTimer;
                PlayerTimer.ReviseTimer();

                var result = await _networkManager.RequestTimerSynchro(IdToken);

                if (result.IsRejected)
                {
                    PlayerTimer.SynchroTimer(result.ServerTimer);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"OnStoneMove Exception, Maybe in TimerSynchro Request : {e}");
        }
    }
    
    // TODO: 추후 서버에 무르기 요청 구매를 요청하는 코드로 수정하기! 
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
