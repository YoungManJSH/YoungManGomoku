using System.Threading;
using TMPro;
using UnityEngine;
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
    [SerializeField] private Button markForbiddenButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_FontAsset glowFont;
    [SerializeField] private AutoPlayDelay autoPlayDelay;
    [SerializeField, Tooltip("10턴 이동, 마지막 턴 이동 시 적용할 딜레이")]
    private float moveJumpingDelay;
    [SerializeField] private string exitMessage;
    
    private (Button button, TextMeshProUGUI buttonText) _zeroTurnSet;
    private (Button button, TextMeshProUGUI buttonText) _prev10Set;
    private (Button button, TextMeshProUGUI buttonText) _prevSet;
    private (Button button, TextMeshProUGUI buttonText) _nextSet;
    private (Button button, TextMeshProUGUI buttonText) _next10Set;
    private (Button button, TextMeshProUGUI buttonText) _lastTurnSet;
    private (Button button, TextMeshProUGUI buttonText) _autoPlaySet;
    private (Button button, TextMeshProUGUI buttonText)[] _speedSets;
    private (Button button, TextMeshProUGUI buttonText) _markForbiddenSets;

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
        _markForbiddenSets = (markForbiddenButton, markForbiddenButton.GetComponentInChildren<TextMeshProUGUI>());
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
        ButtonInactivate(_markForbiddenSets);
        foreach (var speedButton in _speedSets)
        {
            ButtonInactivate(speedButton);
        }

        _defaultFont = _speedSets[0].buttonText.font;
        _isAutoPlaying = false;
        SetSpeedOrigin();

        boardManager.OnReadSucceed += OnReadSucceed;
        boardManager.OnTurnChanged += OnTurnChanged;
        boardManager.OnSimulationCompleted += OnSimulationCompleted;
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
        boardManager.MoveTurnWithDelay(_autoPlayCancelToken.Token, moveJumpingDelay,
            moveCount: 10, isNext: true).Cancel();
    }

    public void MovePrev10Turn()
    {
        CancelAutoPlay();
        _autoPlayCancelToken = new CancellationTokenSource();
        boardManager.MoveTurnWithDelay(_autoPlayCancelToken.Token, moveJumpingDelay,
            moveCount: 10, isNext: false).Cancel();
    }

    public void MoveLastTurn()
    {
        CancelAutoPlay();
        _autoPlayCancelToken = new CancellationTokenSource();
        boardManager.MoveTurnWithDelay(_autoPlayCancelToken.Token, moveJumpingDelay, isNext: true).Cancel();
    }

    public void MoveZeroTurn()
    {
        CancelAutoPlay();
        _autoPlayCancelToken = new CancellationTokenSource();
        boardManager.MoveTurnWithDelay(_autoPlayCancelToken.Token, moveJumpingDelay, isNext: false).Cancel();
    }

    public void ToggleMarkForbidden()
    {
        boardManager.ToggleMarkForbidden();

        if (boardManager.IsMarkForbidden)
        {
            _markForbiddenSets.buttonText.color = Color.white;
            _markForbiddenSets.buttonText.text = "흑돌 금수 표시 중";
        }
        else
        {
            _markForbiddenSets.buttonText.color = Color.aquamarine;
            _markForbiddenSets.buttonText.text = "흑돌 금수 표시하기";
        }
    }
    
    public void SetSpeedHalf() => SetSpeed(AutoPlaySpeed.Half);
    public void SetSpeedOrigin() => SetSpeed(AutoPlaySpeed.Origin);
    public void SetSpeedDouble() => SetSpeed(AutoPlaySpeed.Double);

    public void Exit()
    {
        CancelAutoPlay();
        messageBox.MessageBoxOpen(exitMessage, LoadLobbyScene);
    }

    // Todo: 기보 리스트 열려있도록 수정
    private void LoadLobbyScene()
        => SceneLoadManager.LoadScene(SceneLoadManager.SceneType.LobbyScene);
    
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
        }

        _isAutoPlaying = false;
        _autoPlaySet.buttonText.text = "자동 재생";
        foreach (var speedButton in _speedSets)
        {
            ButtonInactivate(speedButton);
        }
    }

    private void OnReadSucceed()
    {
        ButtonActivate(_nextSet);
        ButtonActivate(_next10Set);
        ButtonActivate(_lastTurnSet);
        ButtonActivate(_autoPlaySet);
    }

    private void OnSimulationCompleted(bool isSuccess)
    {
        if (isSuccess)
        {
            ButtonActivate(_markForbiddenSets);
            ToggleMarkForbidden();
        }
    }
    
    private void OnTurnChanged(int turn)
    {
        if (turn == 0)
        {
            ButtonInactivate(_zeroTurnSet);
            ButtonInactivate(_prev10Set);
            ButtonInactivate(_prevSet);
        }
        else if (turn == boardManager.LastTurn)
        {
            ButtonInactivate(_nextSet);
            ButtonInactivate(_next10Set);
            ButtonInactivate(_lastTurnSet);
            ButtonInactivate(_autoPlaySet);
            CancelAutoPlay();
        }
        
        // 1~3수 사이에 끝난 경우를 고려해야 함. else if 처리하면 안 됨
        if (turn == 1)
        {
            ButtonActivate(_zeroTurnSet);
            ButtonActivate(_prev10Set);
            ButtonActivate(_prevSet);
        }
        
        if (turn == boardManager.LastTurn - 1)
        {
            ButtonActivate(_nextSet);
            ButtonActivate(_next10Set);
            ButtonActivate(_lastTurnSet);
            ButtonActivate(_autoPlaySet);
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