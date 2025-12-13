using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GiboButtonManager : MonoBehaviour
{
    [SerializeField] private GiboBoardManager boardManager;
    
    [SerializeField] private Button zeroTurnButton;
    [SerializeField] private Button prev10Button;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button next10Button;
    [SerializeField] private Button lastTurnButton;
    [SerializeField] private Button autoPlayButton;
    [SerializeField] private Button exitButton;
    
    private (Button button, TextMeshProUGUI buttonText) _zeroTurnSet;
    private (Button button, TextMeshProUGUI buttonText) _prev10Set;
    private (Button button, TextMeshProUGUI buttonText) _prevSet;
    private (Button button, TextMeshProUGUI buttonText) _nextSet;
    private (Button button, TextMeshProUGUI buttonText) _next10Set;
    private (Button button, TextMeshProUGUI buttonText) _lastTurnSet;
    private (Button button, TextMeshProUGUI buttonText) _autoPlaySet;

    private CancellationTokenSource _autoPlayCancelToken;
    private bool _isAutoPlaying;
    
    private void Awake()
    {
        _zeroTurnSet = (zeroTurnButton, zeroTurnButton.GetComponentInChildren<TextMeshProUGUI>());
        _prev10Set = (prev10Button, prev10Button.GetComponentInChildren<TextMeshProUGUI>());
        _prevSet = (prevButton, prevButton.GetComponentInChildren<TextMeshProUGUI>());
        _nextSet = (nextButton, nextButton.GetComponentInChildren<TextMeshProUGUI>());
        _next10Set = (next10Button, next10Button.GetComponentInChildren<TextMeshProUGUI>());
        _lastTurnSet = (lastTurnButton, lastTurnButton.GetComponentInChildren<TextMeshProUGUI>());
        _autoPlaySet = (autoPlayButton, autoPlayButton.GetComponentInChildren<TextMeshProUGUI>());

        ButtonInactivate(_zeroTurnSet);
        ButtonInactivate(_prev10Set);
        ButtonInactivate(_prevSet);
        ButtonInactivate(_nextSet);
        ButtonInactivate(_next10Set);
        ButtonInactivate(_lastTurnSet);
        ButtonInactivate(_autoPlaySet);
        
        _isAutoPlaying = false;

        boardManager.OnReadSucceed += OnReadSucceed;
        boardManager.OnTurnChanged += OnTurnChanged;
    }

    private void OnDestroy()
    {
        if (_autoPlayCancelToken != null)
        {
            // 자동 재생이 마지막까지 진행된 경우 남아있게 됨
            _autoPlayCancelToken.Cancel();
            _autoPlayCancelToken.Dispose();
        }
    }

    public void AutoPlay()
    {
        if (_isAutoPlaying)
        {
            // 자동 재생 중일 경우 진행 중단
            _autoPlayCancelToken.Cancel();
            _autoPlayCancelToken.Dispose();
            _autoPlayCancelToken = null;
            _autoPlaySet.buttonText.text = "자동 재생";
        }
        else
        {
            // 자동 재생 중이 아닐 경우 진행 시작
            _autoPlayCancelToken?.Dispose();
            _autoPlayCancelToken = new CancellationTokenSource();
            boardManager.AutoPlay(_autoPlayCancelToken.Token).Cancel(); // 취소 아님, Fire-and-Forget
            _autoPlaySet.buttonText.text = "자동 재생 취소";
        }
        
        _isAutoPlaying = !_isAutoPlaying;
    }
    
    private void OnReadSucceed()
    {
        ButtonActivate(_nextSet);
        ButtonActivate(_next10Set);
        ButtonActivate(_lastTurnSet);
        ButtonActivate(_autoPlaySet);
    }
    
    private void OnTurnChanged(int turn)
    {
        if (turn == 0)
        {
            ButtonInactivate(_zeroTurnSet);
            ButtonInactivate(_prev10Set);
            ButtonInactivate(_prevSet);
        }
        else if (turn == 1)
        {
            ButtonActivate(_zeroTurnSet);
            ButtonActivate(_prev10Set);
            ButtonActivate(_prevSet);
        }
        else if (turn == boardManager.LastTurn - 1)
        {
            ButtonActivate(_nextSet);
            ButtonActivate(_next10Set);
            ButtonActivate(_lastTurnSet);
            ButtonActivate(_autoPlaySet);
        }
        else if (turn == boardManager.LastTurn)
        {
            ButtonInactivate(_nextSet);
            ButtonInactivate(_next10Set);
            ButtonInactivate(_lastTurnSet);
            ButtonInactivate(_autoPlaySet);
            _autoPlaySet.buttonText.text = "자동 재생";
            _isAutoPlaying = false;
        }
    }
    
    private void ButtonInactivate((Button button, TextMeshProUGUI buttonText) buttonSet)
    {
        buttonSet.button.interactable = false;
        buttonSet.buttonText.color = new Color(1f, 1f, 1f, 0.5f);
    }

    private void ButtonActivate((Button button, TextMeshProUGUI buttonText) buttonSet)
    {
        buttonSet.button.interactable = true;
        buttonSet.buttonText.color = Color.white;
    }
}
