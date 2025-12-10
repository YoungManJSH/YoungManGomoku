using System;
using UnityEngine;

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
    
    
    // 다른 오브젝트들의 Awake가 일어나기 전에 이 Awake가 먼저 실행되어야 함!
    // 프로젝트 세팅 - Script Execution Order에서 이 스크립트를 -2로 설정하였음.
    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        Instance = this;
        _eventManager = GetComponent<EventManager>();
        
        IsPlayerBlack = true; // 테스트용 임시 초기화, 이후 서버에서 받아온 정보로 결정
        
        BoardInform = new Board();
        PlayerTime = new TimeController(mainTimeSeconds, byoyomiCounts, byoyomiSeconds);
        OppositeTime = new TimeController(mainTimeSeconds, byoyomiCounts, byoyomiSeconds);

        _eventManager.OnPlayerByoyomiPurchase += PlayerTime.ByoyomiPurchase;
        _eventManager.OnOppositeByoyomiPurchase += OppositeTime.ByoyomiPurchase;
        
        _eventManager.OnGameStart += () => enabled = true;
        _eventManager.OnGameEnd += () => enabled = false;
        
        stoneMoveController.OnStoneMove += isBlackTurn =>
        {
            _nowPlayerTime = isBlackTurn == IsPlayerBlack ? PlayerTime : OppositeTime;
            (isBlackTurn == IsPlayerBlack ? OppositeTime : PlayerTime).InitByoyomiSecond();
        };
    }

    private void Start() => enabled = false;
    private void Update() => _nowPlayerTime.TimeProgress(Time.deltaTime);
}
