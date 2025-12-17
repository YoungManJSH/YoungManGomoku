using System;
using UnityEngine;

public class UserTimer
{
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

    /// <summary> 경과된 시간(deltaTime)에 따라 타이머 갱신 </summary>
    public void Progress(float deltaTime)
    {
        // 서버와 클라 사이에 접속끊김을 판정하는 HeartBeat 간격은 초읽기 시간 이하임을 전제함.
        // 즉, 초읽기 시간을 초과하는 deltaTime은 입력될 수 없음
        Debug.Assert(deltaTime <= initByoyomiSeconds);
        
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

    /// <summary> 착수 시 서버에서 받은 정보로 타이머 수정 </summary>
    public void ReviseTimer(float mainTime, int byoyomiCount)
    {
        MainTime = mainTime;
        ByoyomiCount = byoyomiCount;
        NowByoyomiSeconds = initByoyomiSeconds;
    }

    public void ByoyomiPurchased(int amount)
    {
        // 초읽기 구매는 1개 남은 상황에서만 이루어진다고 전제함
        ByoyomiCount = amount + 1;
        NowByoyomiSeconds = initByoyomiSeconds;
    }
}
