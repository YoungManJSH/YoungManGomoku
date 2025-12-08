using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public class TimeController
    {
        private float mainTime;
        public float MainTime
        {
            get => mainTime;
            private set
            {
                if (value <= 0f)
                {
                    mainTime = 0f;
                    Byoyomi = initByoyomiSeconds;
                    StartByoyomi!.Invoke();
                    return;
                }
                
                mainTime = value;
            }
        }

        private int byoyomiLeft;
        public int ByoyomiLeft
        {
            get => byoyomiLeft;
            private set
            {
                Debug.Assert(value >= 0);
                byoyomiLeft = value;
                if (value == 0f)
                {
                    OnTimeLose?.Invoke();
                }
                else
                {
                    UseByoyomi?.Invoke();
                }
            }
        }

        public readonly float initByoyomiSeconds;
        private float byoyomi;
        public float Byoyomi
        {
            get => byoyomi;
            private set
            {
                if (value <= 0f)
                {
                    --ByoyomiLeft;
                    byoyomi = initByoyomiSeconds;
                    return;
                }
                byoyomi = value;
            }
        }
        
        public event Action OnTimeLose;
        public event Action StartByoyomi;
        public event Action UseByoyomi;

        public TimeController(float initMainTime, int initByoyomiCount, float byoyomiSeconds)
        {
            MainTime = initMainTime;
            ByoyomiLeft = initByoyomiCount;
            initByoyomiSeconds = byoyomiSeconds;
        }

        public void TimeProgress(float deltaTime)
        {
            if (MainTime > 0f)
            {
                MainTime -= deltaTime;
                return;
            }

            Byoyomi -= deltaTime;
        }

        public void InitByoyomiSecond() 
            => Byoyomi = initByoyomiSeconds;
    }

    [SerializeField] private int mainTimeSeconds;
    [SerializeField] private int byoyomiCounts;
    [SerializeField] private int byoyomiSeconds;
    [SerializeField] private StoneMoveController stoneMoveController;
    
    public static GameManager Instance { get; private set; }

    public event Action OnGameStart;
    public event Action OnGameEnd;
    
    public event Action OnGameWin;
    public event Action OnGameLose;
    
    public event Action OnPlayerSurrender;
    public event Action OnOppositeSurrender;

    public event Action OnOppositeDisconnected;
    public event Action OnOppositeDisconnectedWin;
    
    public Board BoardInform { get; private set; }
    public bool IsPlayerBlack { get; private set; }
    public TimeController PlayerTime { get; private set; }
    public TimeController OppositeTime { get; private set; }
    
    private TimeController _nowPlayerTime;

    private void Awake()
    {
        Instance = this;
        BoardInform = new Board();
        PlayerTime = new TimeController(mainTimeSeconds, byoyomiCounts, byoyomiSeconds);
        OppositeTime = new TimeController(mainTimeSeconds, byoyomiCounts, byoyomiSeconds);

        OnGameEnd += () => enabled = false;
        
        OnGameWin += () => OnGameEnd!.Invoke();
        OnGameLose += () => OnGameEnd!.Invoke();

        OnPlayerSurrender += () => OnGameLose!.Invoke();
        OnOppositeSurrender += () => OnGameWin!.Invoke();

        PlayerTime.OnTimeLose += () => OnGameLose!.Invoke();
        OppositeTime.OnTimeLose += () => OnGameWin!.Invoke(); // 추후 수정, 상대방 시간패 처리는 서버에서 받아야 함

        OnOppositeDisconnectedWin += () => OnGameWin!.Invoke();
        
        IsPlayerBlack = true; // 테스트용 임시 초기화, 이후 서버에서 받아온 정보로 결정
        stoneMoveController.OnStoneMove += isBlackTurn =>
        {
            _nowPlayerTime = isBlackTurn == IsPlayerBlack ? PlayerTime : OppositeTime;
            (isBlackTurn == IsPlayerBlack ? OppositeTime : PlayerTime).InitByoyomiSecond();
        };
    }

    private void Start()
    {
        BoardInform.BlackWin += () => (IsPlayerBlack ? OnGameWin : OnGameLose)!.Invoke();
        BoardInform.WhiteWin += () => (IsPlayerBlack ? OnGameLose : OnGameWin)!.Invoke();
        enabled = false;
    }

    private void Update()
    {
        _nowPlayerTime.TimeProgress(Time.deltaTime);
    }

    public void StartGame()
    {
        enabled = true;
        OnGameStart!.Invoke();
    }
}
