using System;
using UnityEngine;

public class TimeController
{
    public readonly float initByoyomiSeconds;
    private float _mainTime;
    private int _byoyomiLeft;
    private float _byoyomi;
    private bool _isByoyomi;
    public bool IsByoyomiPurchased { get; private set; }
    
    public float MainTime
    {
        get => _mainTime;
        private set
        {
            if (value <= 0f)
            {
                _mainTime = 0f;
                _isByoyomi = true;
                Byoyomi = initByoyomiSeconds;
                StartByoyomi!.Invoke();
                return;
            }

            _mainTime = value;
        }
    }
    public int ByoyomiLeft
    {
        get => _byoyomiLeft;
        private set
        {
            Debug.Assert(value >= 0);
            if (value > _byoyomiLeft)
            {
                _byoyomiLeft = value;
                return;
            }
            
            _byoyomiLeft = value;
            if (value == 0)
            {
                OnTimeLose?.Invoke();
                return;
            }

            if (value == 1 && IsByoyomiPurchased is false)
            {
                OnByoyomiPurchaseActivate?.Invoke();
            }

            UseByoyomi?.Invoke();
        }
    }
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
    public event Action OnByoyomiPurchaseActivate;

    public TimeController(float initMainTime, int initByoyomiCount, float byoyomiSeconds)
    {
        _isByoyomi = false;
        IsByoyomiPurchased = false;
        
        initByoyomiSeconds = byoyomiSeconds;
        MainTime = initMainTime;
        ByoyomiLeft = initByoyomiCount;
    }

    public void TimeProgress(float deltaTime)
    {
        if (_isByoyomi)
        {
            Byoyomi -= deltaTime;
            return;
        }
        
        MainTime -= deltaTime;
    }

    public void InitByoyomiSecond()
        => Byoyomi = initByoyomiSeconds;

    public void ByoyomiPurchase(int amount)
    {
        if (IsByoyomiPurchased)
        {
            Debug.Break();
            Debug.LogError("초읽기 구매가 재요청되었음");
            return;
        }
        IsByoyomiPurchased = true;
        Byoyomi = initByoyomiSeconds;
        ByoyomiLeft += amount;
    }
}