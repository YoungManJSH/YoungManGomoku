using System;
using System.Threading;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YoungManGomoku_Protocol.ClientToServer;

public class RespondTakeBack : MonoBehaviour
{
    [SerializeField, Tooltip("응답 가능 시간(초)")]
    private int timeLimit;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button denyButton;
    [SerializeField] private TextMeshProUGUI messageUI;
    [SerializeField] private Color emphasizedColor;
    [SerializeField] private Color translucentColor;
    [SerializeField] private string message;
    
    private long _requestedTime;
    private int _remainSecond;
    private Sequence _colorSequence;
    private CancellationTokenSource _cts;
    private CS_PermitDTO _takeBackPermitDTO;

    private void Awake()
    {
        Image boxImage = GetComponent<Image>();
        
        // DOTween 개체 관리 최적화를 위해 캐싱
        _colorSequence = DOTween.Sequence()
            .Append(boxImage.DOColor(endValue: emphasizedColor, duration: 0.5f))
            .Append(boxImage.DOColor(endValue: translucentColor, duration: 1f))
            .SetAutoKill(false); // SetAutoKill(false)를 해줘야 2번째에도 재생된다.
        _colorSequence.Pause();
        
        acceptButton.onClick.AddListener(Accept);
        denyButton.onClick.AddListener(Deny);
        messageUI.text = $"{message} - {timeLimit:D2}";
        _takeBackPermitDTO = new CS_PermitDTO(GameManager.Instance.IdToken, isPermit: false);
        
        EventManager.Instance.OnTakeBackRequested += async isMyRequest =>
        {
            if (isMyRequest) return;
            
            await WaitRespond();
        };
        
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

        if (Input.GetButtonDown("Submit"))
        {
            Accept();
            return;
        }
        
        if (Input.GetButtonDown("Cancel"))
        {
            Deny();
        }
    }

    private void OnEnable() => _colorSequence.Restart();
    private void OnDisable() => _colorSequence.Pause();
    private void OnDestroy() => _colorSequence.Kill();

    private void Accept()
    {
        _takeBackPermitDTO.IsPermit = true;
        _cts.Cancel();
    }

    private void Deny() => _cts.Cancel();

    private async Awaitable WaitRespond()
    {
        _requestedTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _remainSecond = timeLimit;
        _takeBackPermitDTO.IsPermit = false; // 유저 입력이 없을 경우 적용할 기본값 (거절)
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
            EventManager.Instance.UpdateLastRequestTime();
            NetworkManager.Instance.RequestTakeBackPermit(_takeBackPermitDTO, timeOutSeconds: 5).Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}
