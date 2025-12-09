using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public class TimeController
    {
        private float _mainTime;
        public float MainTime
        {
            get => _mainTime;
            private set
            {
                if (value <= 0f)
                {
                    _mainTime = 0f;
                    Byoyomi = initByoyomiSeconds;
                    StartByoyomi!.Invoke();
                    return;
                }
                
                _mainTime = value;
            }
        }

        private int _byoyomiLeft;
        public int ByoyomiLeft
        {
            get => _byoyomiLeft;
            private set
            {
                Debug.Assert(value >= 0);
                _byoyomiLeft = value;
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
        private float _byoyomi;
        public float Byoyomi
        {
            get => _byoyomi;
            private set
            {
                if (value <= 0f)
                {
                    --ByoyomiLeft;
                    _byoyomi = initByoyomiSeconds;
                    return;
                }
                _byoyomi = value;
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
    [SerializeField] private PlayerPanelController playerPanel;
    [SerializeField] private PlayerPanelController oppositePanel;
    [SerializeField] private ProgressText progressText;
    
    public Board BoardInform { get; private set; }
    public bool IsPlayerBlack { get; private set; }
    public TimeController PlayerTime { get; private set; }
    public TimeController OppositeTime { get; private set; }
    
    private TimeController _nowPlayerTime;
    private EventManager _eventManager;

    private void Awake()
    {
        _eventManager = GetComponent<EventManager>();
        
        // Awake 타임에 IsPlayerBlack 정보가 정해져야 함!
        IsPlayerBlack = true; // 테스트용 임시 초기화, 이후 서버에서 받아온 정보로 결정
        
        BoardInform = new Board();
        PlayerTime = new TimeController(mainTimeSeconds, byoyomiCounts, byoyomiSeconds);
        OppositeTime = new TimeController(mainTimeSeconds, byoyomiCounts, byoyomiSeconds);
        
        playerPanel.OnBlackDecided(IsPlayerBlack);
        oppositePanel.OnBlackDecided(!IsPlayerBlack);
        progressText.OnBoardGenerated(BoardInform, IsPlayerBlack, PlayerTime, OppositeTime);

        _eventManager.OnGameStart += () => enabled = true;
        _eventManager.OnGameEnd += () => enabled = false;
        
        stoneMoveController.OnStoneMove += isBlackTurn =>
        {
            _nowPlayerTime = isBlackTurn == IsPlayerBlack ? PlayerTime : OppositeTime;
            (isBlackTurn == IsPlayerBlack ? OppositeTime : PlayerTime).InitByoyomiSecond();
        };
    }

    private void Start()
    {
        enabled = false;
    }

    private void Update()
    {
        _nowPlayerTime.TimeProgress(Time.deltaTime);
    }
}
