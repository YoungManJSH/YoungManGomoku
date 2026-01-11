using System;
using UnityEngine;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class GameManager : MonoBehaviour
{
    [SerializeField] private StoneMover stoneMover;
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
    private CS_RequestTimerSynchroDTO _timerSynchroDTO; 
    private EventManager _eventManager;
    private NetworkManager _networkManager;
    private AudioSource _audioSource;
    private long _lastTime;

    /* 다른 오브젝트들의 Awake가 일어나기 전에 이 Awake가 먼저 실행되어야 함!
     * 프로젝트 세팅 - Script Execution Order에서 이 스크립트를 -2로 설정하였음. */
    private void Awake()
    {
        if (Instance != null) Destroy(Instance);
        Instance = this;
        _eventManager = GetComponent<EventManager>();
        _networkManager = GetComponent<NetworkManager>();
        _audioSource = GetComponent<AudioSource>();
        BoardInform = new Board();
        IsByoyomiPurchased = false;
        
        #region 타이머 진행 제어 (Update 활성화 여부)
        _eventManager.OnGameStart += () => enabled = true;
        _eventManager.OnGameEnd += DisableTimer;
        _eventManager.OnStartSweeping += DisableTimer;
        _eventManager.OnBlackUnmovable += DisableTimer;
        #endregion
        
        _eventManager.OnGameWin += () => _audioSource.PlayOneShot(winSound);
        _eventManager.OnGameLose += () => _audioSource.PlayOneShot(loseSound);
        _eventManager.OnGameDraw += PlayDrawSound;
        _eventManager.OnServerReplyFailed += PlayDrawSound;
        _networkManager.OnRequestFailed += _ => PlayDrawSound();
        _eventManager.OnPlayerByoyomiPurchase += _ => IsByoyomiPurchased = true;
        _eventManager.OnTakeBackRequested += _ => DisableTimer();
        _eventManager.OnTakeBack += OnTakeBack;
        
        /*
        #region 테스트용 임시 초기화
        IsPlayerBlack = true;
        MyPlayer = new BasicPlayerData("흑돌 임시", 10, 5, 10, 15.5f, ProfileImageType.None);
        OppositePlayer = new BasicPlayerData("백돌 임시", 10, 5, 19, 2323.4f, ProfileImageType.StudentGirl);
        PlayerTimer = new UserTimer(15f, 2, 15f);
        OppositeTimer = new UserTimer(15f, 2, 15f);
        ByoyomiPurchaseAmount = 2;
        #endregion
        // */
        
        #region 서버에서 받아온 매칭 정보로 초기화
        IdToken = PlayerDataFromWebServer.Instance.IDToken;
        _timerSynchroDTO = new CS_RequestTimerSynchroDTO(IdToken, 0, default);
        
        PlayerData my = PlayerDataFromWebServer.Instance.PlayerData;
        MyPlayer = new BasicPlayerData(my.Nickname, my.WinCount, my.DrawCount, my.LoseCount, my.Rating, my.EquipProfile);
        
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
        OppositePlayer = new BasicPlayerData(opponent.Nickname, opponent.WinCount, opponent.DrawCount,
            opponent.LoseCount, opponent.Rating, opponent.EquipProfile);

        TimerSettingData timerInform = matchResult.TimerSettingDTO;
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
        
        /* Board.OnTurnChanged<int>는 무르기 때도 실행되는 이벤트
         * 오직 착수만 의미하는 이벤트는 stoneMover.OnStoneMove */
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
    
    /// <summary> MonoBehaviour 비활성화 (타이머 정지) </summary>
    private void DisableTimer() => enabled = false;

    private void PlayDrawSound() => _audioSource.PlayOneShot(drawSound);

    /// <summary> 진행 중인 타이머 교체 및 내 타이머 동기화 요청 </summary>
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

                #region 타이머 동기화
                // 자동 착수되는 첫 수에는 타이머 동기화 요청을 보내지 않음
                if (BoardInform.NowTurn == 1) return;
                
                _timerSynchroDTO.NowTurn = BoardInform.NowTurn;
                _timerSynchroDTO.MyTimer = PlayerTimer.SyncData;
                TimerSyncData serverTimer =
                    await _networkManager.RequestTimerSynchro(_timerSynchroDTO);

                if (serverTimer.IsDefault())
                {
                    /* case 1: OnRequestFailed
                     * case 2: 서버 연산 로직 버그
                     * case 3: 각종 상황에서의 레이스 컨디션
                     * 
                     * _eventManager.ServerReplyFailed()를 하지 않는 이유
                     * 타이머 동기화는 UI/UX 보강을 위한 작업일 뿐임.
                     * 게임의 핵심 로직이 아니므로 속행해도 무방함.
                     * 따라서 유연하게 soft-fail 전략을 선택함 */

                    Debug.LogWarning("타이머 동기화 요청에 default가 응답됨");
                    return;
                }
                
                if (PlayerTimer > serverTimer)
                {
                    PlayerTimer.SynchroTimer(serverTimer);
                    Debug.Log("플레이어의 타이머가 서버로부터 반려됨, 서버 정보로 동기화 완료");
                }
                else Debug.Log("플레이어의 타이머가 승인됨");
                #endregion
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"OnStoneMove Exception, Maybe in TimerSynchro Request : {e}");
        }
    }
    
    private void OnTakeBack(bool isAccepted)
    {
        // 타이머 재개
        _lastTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        enabled = true;
        
        if (isAccepted)
        {
            // 무르기 적용
            if (BoardInform.TryTakeBack() is false)
            {
                Debug.LogError("무르기 로직 에러: NowTurn, record.Count 확인 요망!");
                _eventManager.ServerReplyFailed();
            }
        }
    }
}
