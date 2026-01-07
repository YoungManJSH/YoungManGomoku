using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RespondTakeBack : MonoBehaviour
{
    [SerializeField, Tooltip("응답 가능 시간(초)")]
    private int timeLimit;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button denyButton;
    [SerializeField] private TextMeshProUGUI messageUI;
    [SerializeField] private string message;
    
    private long _requestedTime;
    private int _remainSecond;
    private bool _respond;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        acceptButton.onClick.AddListener(Accept);
        denyButton.onClick.AddListener(Deny);
        messageUI.text = $"{message} - {timeLimit:D2}";
        gameObject.SetActive(false);
    }
    
    private void Update()
    {
        // Awaitable 대기 로직과의 레이스 컨디션 방어
        if (_cts == null) return;
        
        long elapsedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _requestedTime;
        int nowRemain = timeLimit - Mathf.FloorToInt(elapsedTime / 1000f);

        if (nowRemain == _remainSecond) return;

        if (nowRemain <= 0)
        {
            Deny();
            return;
        }
        
        messageUI.text = $"{message} - {nowRemain:D2}";
        _remainSecond = nowRemain;
    }

    private void Accept()
    {
        _respond = true;
        _cts.Cancel();
    }

    private void Deny() => _cts.Cancel();

    public async Awaitable<bool> Respond()
    {
        _requestedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _remainSecond = timeLimit;
        _respond = false; // 유저 입력이 없을 경우 적용할 기본값 (거절)
        _cts = new CancellationTokenSource();
        
        try
        {
            gameObject.SetActive(true);
            await Awaitable.WaitForSecondsAsync(timeLimit, _cts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            gameObject.SetActive(false);
            _cts.Dispose();
            _cts = null;
        }
        
        return _respond;
    }
}
