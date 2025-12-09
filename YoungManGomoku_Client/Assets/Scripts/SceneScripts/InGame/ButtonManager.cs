using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private MessageBoxManager messageBox;
    
    [SerializeField] private Button surrenderButton;
    [SerializeField] private Button timePurchaseButton;
    [SerializeField] private Button takeBackButton;
    [SerializeField] private Button exitButton;

    [SerializeField] private string surrenderConfirmMsg;
    [SerializeField] private string timePurchaseConfirmMsg;
    [SerializeField] private string takeBackConfirmMsg;

    private TextMeshProUGUI _surrenderText;
    private TextMeshProUGUI _timePurchaseText;
    private TextMeshProUGUI _takeBackText;
    private TextMeshProUGUI _exitText;
    
    private HashSet<(Button, TextMeshProUGUI)> _activateSet; // 플레이어 턴이 올 때마다 활성화시킬 버튼
    private bool _isPlayerTurn;
    
    private void Awake()
    {
        _surrenderText = surrenderButton.GetComponentInChildren<TextMeshProUGUI>();
        _timePurchaseText = timePurchaseButton.GetComponentInChildren<TextMeshProUGUI>();
        _takeBackText = takeBackButton.GetComponentInChildren<TextMeshProUGUI>();
        _exitText = exitButton.GetComponentInChildren<TextMeshProUGUI>();
        
        ButtonInactivate(surrenderButton, _surrenderText);
        ButtonInactivate(timePurchaseButton, _timePurchaseText);
        ButtonInactivate(takeBackButton, _takeBackText);
        ButtonInactivate(exitButton, _exitText);
        
        _activateSet = new HashSet<(Button button, TextMeshProUGUI text)>();
        _isPlayerTurn = GameManager.Instance.IsPlayerBlack;

        EventManager.Instance.OnGameStart += OnGameStart;
        EventManager.Instance.OnGameEnd += OnGameEnd;
        GameManager.Instance.BoardInform.OnTurnBackActivate += OnTurnBackActivate;
        GameManager.Instance.PlayerTime.OnTimePurchaseActivate += OnTimePurchaseActivate;
        GameManager.Instance.BoardInform.OnTurnChanged += OnTurnChanged;
    }
    
    public void SurrenderInput()
        => messageBox.MessageBoxOpen(surrenderConfirmMsg, EventManager.Instance.PlayerSurrendered);

    public void TimePurchaseInput()
        => messageBox.MessageBoxOpen(timePurchaseConfirmMsg, null);

    public void TakeBackInput()
        => messageBox.MessageBoxOpen(timePurchaseConfirmMsg, null);
    
    public void ExitInput()
        => UnityEditor.EditorApplication.isPlaying = false;

    private void OnGameStart() => ButtonActivate(surrenderButton, _surrenderText);
    
    private void OnGameEnd()
    {
        ButtonInactivate(surrenderButton, _surrenderText);
        ButtonInactivate(timePurchaseButton, _timePurchaseText);
        ButtonInactivate(takeBackButton, _takeBackText);
        ButtonActivate(exitButton, _exitText);
    }

    private void OnTurnBackActivate()
        => _activateSet.Add((takeBackButton, _takeBackText));

    private void OnTimePurchaseActivate()
    {
        ButtonActivate(timePurchaseButton, _timePurchaseText);
        _activateSet.Add((timePurchaseButton, _timePurchaseText));
    }

    private void OnTurnChanged(int turn)
    {
        _isPlayerTurn = (turn % 2 == 0) == GameManager.Instance.IsPlayerBlack;
        
        if (_isPlayerTurn)
        {
            foreach (var buttons in _activateSet)
            {
                ButtonActivate(buttons.Item1, buttons.Item2);
            }
        }
        else
        {
            foreach (var buttons in _activateSet)
            {
                ButtonInactivate(buttons.Item1, buttons.Item2);
            }
        }
    }
    
    private void ButtonInactivate(Button button, TextMeshProUGUI buttonText)
    {
        buttonText.color = new Color(1f, 1f, 1f, 0.5f);
        button.interactable = false;
    }

    private void ButtonActivate(Button button, TextMeshProUGUI buttonText)
    {
        buttonText.color = Color.white;
        button.interactable = true;
    }
}
