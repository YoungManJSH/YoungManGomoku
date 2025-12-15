using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GiboButtonManager : MonoBehaviour
{
    public enum AutoPlaySpeed
    {
        Half, Origin, Double, Max
    }
    
    [System.Serializable]
    public struct AutoPlayDelay
    {
        public float halfSpeedDelay;
        public float originSpeedDelay;
        public float doubleSpeedDelay;

        public float this[AutoPlaySpeed speed]
            => speed switch
            {
                AutoPlaySpeed.Half => halfSpeedDelay,
                AutoPlaySpeed.Double => doubleSpeedDelay,
                _ => originSpeedDelay
            };
    }

    [SerializeField] private GiboBoardManager boardManager;
    [SerializeField] private GiboMessageController messageBox;
    [SerializeField] private Button zeroTurnButton;
    [SerializeField] private Button prev10Button;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button next10Button;
    [SerializeField] private Button lastTurnButton;
    [SerializeField] private Button autoPlayButton;
    [SerializeField] private Button autoPlayHalfSpeed;
    [SerializeField] private Button autoPlayOriginSpeed;
    [SerializeField] private Button autoPlayDoubleSpeed;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_FontAsset glowFont;
    [SerializeField] private AutoPlayDelay autoPlayDelay;
    [SerializeField] private string exitMessage;
    
    private (Button button, TextMeshProUGUI buttonText) _zeroTurnSet;
    private (Button button, TextMeshProUGUI buttonText) _prev10Set;
    private (Button button, TextMeshProUGUI buttonText) _prevSet;
    private (Button button, TextMeshProUGUI buttonText) _nextSet;
    private (Button button, TextMeshProUGUI buttonText) _next10Set;
    private (Button button, TextMeshProUGUI buttonText) _lastTurnSet;
    private (Button button, TextMeshProUGUI buttonText) _autoPlaySet;
    private (Button button, TextMeshProUGUI buttonText)[] _speedSets;

    private CancellationTokenSource _autoPlayCancelToken;
    private TMP_FontAsset _defaultFont;
    private bool _isAutoPlaying;
    private AutoPlaySpeed _speed;
    
    private void Awake()
    {
        _zeroTurnSet = (zeroTurnButton, zeroTurnButton.GetComponentInChildren<TextMeshProUGUI>());
        _prev10Set = (prev10Button, prev10Button.GetComponentInChildren<TextMeshProUGUI>());
        _prevSet = (prevButton, prevButton.GetComponentInChildren<TextMeshProUGUI>());
        _nextSet = (nextButton, nextButton.GetComponentInChildren<TextMeshProUGUI>());
        _next10Set = (next10Button, next10Button.GetComponentInChildren<TextMeshProUGUI>());
        _lastTurnSet = (lastTurnButton, lastTurnButton.GetComponentInChildren<TextMeshProUGUI>());
        _autoPlaySet = (autoPlayButton, autoPlayButton.GetComponentInChildren<TextMeshProUGUI>());
        _speedSets = new (Button button, TextMeshProUGUI buttonText)[(int)AutoPlaySpeed.Max];
        
        _speedSets[(int)AutoPlaySpeed.Half] = (autoPlayHalfSpeed, autoPlayHalfSpeed.GetComponentInChildren<TextMeshProUGUI>());
        _speedSets[(int)AutoPlaySpeed.Origin] = (autoPlayOriginSpeed, autoPlayOriginSpeed.GetComponentInChildren<TextMeshProUGUI>());
        _speedSets[(int)AutoPlaySpeed.Double] = (autoPlayDoubleSpeed, autoPlayDoubleSpeed.GetComponentInChildren<TextMeshProUGUI>());

        ButtonInactivate(_zeroTurnSet);
        ButtonInactivate(_prev10Set);
        ButtonInactivate(_prevSet);
        ButtonInactivate(_nextSet);
        ButtonInactivate(_next10Set);
        ButtonInactivate(_lastTurnSet);
        ButtonInactivate(_autoPlaySet);
        foreach (var speedButton in _speedSets)
        {
            ButtonInactivate(speedButton);
        }

        _defaultFont = _speedSets[0].buttonText.font;
        _isAutoPlaying = false;
        SetSpeedOrigin();

        boardManager.OnReadSucceed += OnReadSucceed;
        boardManager.OnTurnChanged += OnTurnChanged;
    }

    private void OnDestroy() => CancelAutoPlay();

    public void AutoPlay()
    {
        if (_isAutoPlaying)
        {
            // 자동 재생 중일 경우 진행 중단
            CancelAutoPlay();
        }
        else
        {
            // 자동 재생 중이 아닐 경우 진행 시작
            CancelAutoPlay();
            _autoPlayCancelToken = new CancellationTokenSource();
            boardManager.MoveTurnWithDelay(_autoPlayCancelToken.Token, autoPlayDelay[_speed]).Cancel(); // 취소 아님, Fire-and-Forget
            _isAutoPlaying = true;
            _autoPlaySet.buttonText.text = "자동 재생 취소";

            foreach (var speedButton in _speedSets)
            {
                ButtonActivate(speedButton);
            }
        }
    }

    public void MoveNextTurn()
    {
        CancelAutoPlay();
        boardManager.MoveNextTurn();
    }

    public void MovePrevTurn()
    {
        CancelAutoPlay();
        boardManager.MovePrevTurn();
    }

    public void MoveNext10Turn()
    {
        CancelAutoPlay();
        _autoPlayCancelToken = new CancellationTokenSource();
        boardManager.MoveTurnWithDelay(_autoPlayCancelToken.Token, delay: 0.05f, moveCount: 10, isNext: true).Cancel();
    }

    public void MovePrev10Turn()
    {
        CancelAutoPlay();
        _autoPlayCancelToken = new CancellationTokenSource();
        boardManager.MoveTurnWithDelay(_autoPlayCancelToken.Token, delay: 0.05f, moveCount: 10, isNext: false).Cancel();
    }

    public void MoveLastTurn()
    {
        CancelAutoPlay();
        _autoPlayCancelToken = new CancellationTokenSource();
        boardManager.MoveTurnWithDelay(_autoPlayCancelToken.Token, delay: 0.05f, isNext: true).Cancel();
    }

    public void MoveZeroTurn()
    {
        CancelAutoPlay();
        _autoPlayCancelToken = new CancellationTokenSource();
        boardManager.MoveTurnWithDelay(_autoPlayCancelToken.Token, delay: 0.05f, isNext: false).Cancel();
    }

    public void SetSpeedHalf() => SetSpeed(AutoPlaySpeed.Half);
    public void SetSpeedOrigin() => SetSpeed(AutoPlaySpeed.Origin);
    public void SetSpeedDouble() => SetSpeed(AutoPlaySpeed.Double);

    public void Exit()
    {
        CancelAutoPlay();
        messageBox.MessageBoxOpen(exitMessage, LoadLobby);
    }
    
    private void SetSpeed(AutoPlaySpeed speed)
    {
        _speedSets[(int)_speed].buttonText.font = _defaultFont;
        _speed = speed;
        _speedSets[(int)speed].buttonText.font = glowFont;

        if (_isAutoPlaying)
        {
            // Restart Auto Play with new speed
            _autoPlayCancelToken!.Cancel();
            _autoPlayCancelToken.Dispose();
            _autoPlayCancelToken = new CancellationTokenSource();
            boardManager.MoveTurnWithDelay(_autoPlayCancelToken.Token, autoPlayDelay[_speed]).Cancel();
        }
    }

    private void CancelAutoPlay()
    {
        if (_autoPlayCancelToken != null)
        {
            _autoPlayCancelToken.Cancel();
            _autoPlayCancelToken.Dispose();
            _autoPlayCancelToken = null;
            _isAutoPlaying = false;
            _autoPlaySet.buttonText.text = "자동 재생";

            foreach (var speedButton in _speedSets)
            {
                ButtonInactivate(speedButton);
            }
        }
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
            CancelAutoPlay();
        }
    }
    
    private void LoadLobby()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        SceneManager.LoadScene("Scenes/2.Lobby/LobbyScene - PC");
#elif UNITY_ANDROID
        SceneManager.LoadScene("Scenes/2.Lobby/LobbyScene - Android");
#endif
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