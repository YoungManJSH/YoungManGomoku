using System;
using System.Threading;
using TMPro;
using UnityEngine;

public class ToastBoxController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageUI;
    [SerializeField] private string waitingTakeBackString;
    [SerializeField] private string waitingRematchString;
    [SerializeField] private string rematchFailedString;

    private const int COUNTS = 3;

    private CancellationTokenSource _cts;
    private string[] _takeBackStrings;
    private string[] _rematchStrings;
    private string[] _nowStrings;

    private void Awake()
    {
        _takeBackStrings = new string[COUNTS];
        _rematchStrings = new string[COUNTS];

        for (int i = 0; i < COUNTS; ++i)
        {
            var dots = new string('·', i + 1);
            _takeBackStrings[i] = $"{dots}{waitingTakeBackString}{dots}";
            _rematchStrings[i] = $"{dots}{waitingRematchString}{dots}";
        }
        
        EventManager em = EventManager.Instance;
        em.OnTakeBackRequested += OnTakeBackRequested;
        em.OnTakeBack += _ => CloseToastBox();
        em.OnWaitingRematch += OnWaitingRematch;
        em.OnRematchFailed += OnRematchFailed;

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _cts = new CancellationTokenSource();
        ChangeString().Cancel();
    }

    private void OnDisable()
    {
        if (_cts == null) return;
        
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private void OnTakeBackRequested(bool isMyRequest)
    {
        if (isMyRequest)
        {
            _nowStrings = _takeBackStrings;
            gameObject.SetActive(true);
        }
    }

    private void OnWaitingRematch()
    {
        _nowStrings = _rematchStrings;
        gameObject.SetActive(true);
    }

    private void OnRematchFailed()
    {
        /* case1: OnWaitingRematch 중에 응답받은 경우
         * case2: 재대결 요청 전에 먼저 응답받은 경우 */
        
        /* TODO: 투명도 조절 애니메이션 넣기
         * 3초 정도 메시지 띄웠다가 사라지도록 조정하기 */ 
    }

    private void CloseToastBox() => gameObject.SetActive(false);
    
    private async Awaitable ChangeString()
    {
        try
        {
            while (true)
            {
                for (int i = 0; i < COUNTS; ++i)
                {
                    messageUI.text = _nowStrings[i];
                    await Awaitable.WaitForSecondsAsync(0.5f, _cts.Token);
                }
            }
        }
        catch (OperationCanceledException) { }
    }
}
