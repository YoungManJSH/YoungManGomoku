using System;
using UnityEngine;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class GameManager : MonoBehaviour
{
    [SerializeField] private StoneMover stoneMover;
    
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
    public IngameItemCost ItemCost => PlayerDataFromWebServer.Instance.MatchResultDTO.ItemCostDTO;
    public int PlayerMoney
    {
        get => PlayerDataFromWebServer.Instance.PlayerData.GameMoney;
        private set
        {
            PlayerDataFromWebServer.Instance.PlayerData.GameMoney = value;
            OnPlayerMoneyChanged?.Invoke(value);
        }
    }
    public bool HasByoyomiCost => PlayerMoney >= ItemCost.ByoyomiPurchaseCost;
    public bool HasTakeBackCost => PlayerMoney >= ItemCost.TakeBackCost;
    
    public event Action<int> OnPlayerMoneyChanged;

    private UserTimer _nowPlayerTimer;
    private CS_RequestTimerSynchroDTO _timerSynchroDTO; 
    private EventManager _eventManager;
    private NetworkManager _networkManager;
    private long _lastTime;

    /* 다른 오브젝트들의 Awake가 일어나기 전에 이 Awake가 먼저 실행되어야 함!
     * 프로젝트 세팅 - Script Execution Order에서 이 스크립트를 -2로 설정하였음. */
    private void Awake()
    {
        if (Instance != null) Destroy(Instance);
        Instance = this;
        _eventManager = GetComponent<EventManager>();
        _networkManager = GetComponent<NetworkManager>();
        BoardInform = new Board();
        IsByoyomiPurchased = false;
        
        _eventManager.OnGameStart += () => enabled = true;
        _eventManager.OnGameEnd += DisableTimer;
        
        _eventManager.OnPlayerByoyomiPurchase += _ => IsByoyomiPurchased = true;
        _eventManager.OnTakeBackRequested += _ => DisableTimer();
        _eventManager.OnTakeBack += OnTakeBack;
        _eventManager.OnPlayerByoyomiPurchase += OnPlayerByoyomiPurchase;
        _eventManager.OnOppositeByoyomiPurchase += OnOppositeByoyomiPurchase;
        
        #region 서버에서 받아온 매칭 정보로 초기화
        IdToken = PlayerDataFromWebServer.Instance.IDToken;
        _timerSynchroDTO = new CS_RequestTimerSynchroDTO(IdToken, nowTurn: 0);
        
        PlayerData my = PlayerDataFromWebServer.Instance.PlayerData;
        MyPlayer = new BasicPlayerData(my.Nickname, my.WinCount, my.DrawCount, my.LoseCount, my.Rating, my.EquipProfile);
        
        SC_MatchResultDTO matchResult = PlayerDataFromWebServer.Instance.MatchResultDTO;
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
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "흑 오목승");
            _eventManager.OnOppositeGomoku += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "흑 오목패");
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
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "백 오목승");
            _eventManager.OnOppositeGomoku += ()
                => GiboFileManager.CreateGiboFile(BoardInform.Record, blackUser, whiteUser, "백 오목패");
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
                TimerSyncData serverTimer =
                    await _networkManager.RequestTimerSynchro(_timerSynchroDTO);

                if (serverTimer.IsInvalid())
                {
                    /* case 1: OnRequestFailed
                     * case 2: 서버 연산 로직 버그
                     * case 3: 각종 상황에서의 레이스 컨디션
                     * 
                     * _eventManager.ServerReplyFailed()를 하지 않는 이유
                     * 타이머 동기화는 UI/UX 보강을 위한 작업일 뿐임.
                     * 게임의 핵심 로직이 아니므로 속행해도 무방함.
                     * 따라서 유연하게 ignore 전략을 선택함
                     * 단, 내 타이머는 상대방 타이머와는 다르게 기본적으로 내 클라이언트의 값을
                     * 사용하는 영역이므로 이 경우 클라이언트 타이머를 그대로 유지함
                     * (서버가 잘못된 값을 보냈으니 서버의 권위를 인정하지 않겠다...?) */

                    Debug.LogWarning($"타이머 동기화 요청에 {serverTimer.MainTime:F1}초," +
                                     $"{serverTimer.ByoyomiCount}회가 응답됨!");
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

            if (BoardInform.NowTurn % 2 == 0 == IsPlayerBlack)
            {
                // 내 턴에서 무르기 성사 → 무르기 비용 차감
                PlayerMoney -= ItemCost.TakeBackCost;
            }
            else
            {
                // 상대방 턴에서 무르기 성사 → 무르기 보상 획득
                PlayerMoney += ItemCost.TakeBackReward;
            }
        }
    }

    private void OnPlayerByoyomiPurchase(int _)
        => PlayerMoney -= ItemCost.ByoyomiPurchaseCost;

    private void OnOppositeByoyomiPurchase(int _)
        => PlayerMoney += ItemCost.ByoyomiPurchaseReward;
}
