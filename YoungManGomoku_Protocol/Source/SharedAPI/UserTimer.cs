using System;

public class UserTimer : IComparable<UserTimer>
{
    private const int TOLERANCE = 400; // ms 단위
    
    public readonly float initByoyomiSeconds; 
    
    public float MainTime { get; private set; } // 처음에 누적하여 소모되는 자유시간
    public int ByoyomiCount { get; private set; } // 남아있는 초읽기 개수
    public float NowByoyomiSeconds { get; private set; }
    
    public event Action OnTimeOut;
    private bool _isTimeOut;

    public UserTimer(float initMainTime, int initByoyomiCount, float byoyomiSeconds)
    {
        MainTime = initMainTime;
        ByoyomiCount = initByoyomiCount;
        initByoyomiSeconds = byoyomiSeconds;
        NowByoyomiSeconds = byoyomiSeconds;
        _isTimeOut = false;
    }

    /// <summary> [서버용] startTime 기준으로 시간패 판정을 할 시각 계산 </summary>
    /// <param name="startTime"> 턴을 시작한 시각 </param>
    /// <return> 시간패가 되는 시각 (허용 오차 합산된 값) </return>
    public long DeadLine(long startTime)
        => startTime + TOLERANCE +
           (long)((MainTime + ByoyomiCount * initByoyomiSeconds) * 1000f);
    
    /// <summary> [서버용] 허용 오차(400ms)를 제외하고 타이머 갱신 </summary>
    /// <param name="startTime"> 턴을 시작했던 시각 </param>
    /// <param name="nowTime"> 착수 정보를 받은 시각 </param>
    public void ProgressExcludingTol(long startTime, long nowTime)
    {
        float interval = Math.Max(nowTime - startTime - TOLERANCE, 0f) / 1000f;
        
        if (MainTime > interval)
        {
            MainTime -= interval;
            return;
        }

        // 남아있는 MainTime을 제외한 값으로 초읽기 계산 
        interval -= MainTime;
        MainTime = 0f;

        ByoyomiCount -= (int)Math.Floor(interval / initByoyomiSeconds);
    }

    /// <summary> 경과된 시간(deltaTime)에 따라 타이머 갱신 </summary>
    public void Progress(float deltaTime)
    {
        // 서버와 클라 사이에 접속끊김을 판정하는 HeartBeat 간격은 초읽기 시간 이하임을 전제함.
        // 즉, 초읽기 시간을 초과하는 deltaTime은 입력될 수 없음
        
        // 라이브 버전에서 예외처리는 생략함
        // deltaTime이 너무 커서 타이머가 꼬이더라도 접속끊김패 처리가 될 것이기 때문

        if (_isTimeOut) return;
        
        if (MainTime > deltaTime)
        {
            MainTime -= deltaTime;
            return;
        }

        NowByoyomiSeconds -= deltaTime - MainTime;
        MainTime = 0f;

        if (NowByoyomiSeconds <= 0f)
        {
            --ByoyomiCount;
            if (ByoyomiCount == 0)
            {
                OnTimeOut?.Invoke();
                _isTimeOut = true;
                NowByoyomiSeconds = 0f;
                return;
            }
            
            NowByoyomiSeconds += initByoyomiSeconds;
        }
    }

    /// <summary> 착수 후 초읽기 복구 함수 </summary>
    public void ReviseTimer()
    {
        if (ByoyomiCount == 0) ByoyomiCount = 1;
        NowByoyomiSeconds = initByoyomiSeconds;
        _isTimeOut = false;
    }
    
    /// <summary> [서버,클라이언트] 타이머 동기화 함수 </summary>
    public void ReviseTimer(float mainTime, int byoyomiCount)
    {
        MainTime = mainTime;
        ByoyomiCount = byoyomiCount;
        NowByoyomiSeconds = initByoyomiSeconds;
        _isTimeOut = false;
    }

    public void ByoyomiPurchased(int amount)
    {
        // 초읽기 구매는 1개 남은 상황에서만 이루어진다고 전제함
        ByoyomiCount = amount + 1;
        NowByoyomiSeconds = initByoyomiSeconds;
    }

    public int CompareTo(UserTimer other)
    {
        if (ReferenceEquals(other, null)) return 1;

        // 내 자유시간 남아있으면 자유시간으로 비교
        if (MainTime > 0f) return MainTime.CompareTo(other.MainTime);
        
        // other만 자유시간이 있는 경우 내가 더 적음
        if (other.MainTime > 0f) return -1;

        // 둘 다 자유시간 없으면 남은 초읽기 개수로 비교
        return ByoyomiCount.CompareTo(other.ByoyomiCount);
    }

    // Comparer<UserTimer>.Default 대신 이쪽으로 구현
    // Why? 우리 프로그램에서 null과 비교하는 상황이 나오면 명백한 코딩 실수
    // 따라서 터뜨리는 게 바람직하므로 CompareTo로 직접 구현
    public static bool operator <(UserTimer a, UserTimer b)
        => a.CompareTo(b) < 0;
    
    public static bool operator >(UserTimer a, UserTimer b)
        => a.CompareTo(b) > 0;
    
    public static bool operator <=(UserTimer a, UserTimer b)
        => a.CompareTo(b) <= 0;

    public static bool operator >=(UserTimer a, UserTimer b)
        => a.CompareTo(b) >= 0;
}