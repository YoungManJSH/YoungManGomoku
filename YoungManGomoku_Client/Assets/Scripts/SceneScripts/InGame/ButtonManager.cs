using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Button = UnityEngine.UI.Button;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private MessageBoxManager messageBox;
    [SerializeField] private BoardSweeper playerSweeper;
    [SerializeField] private PlayerPanelController playerPanel;
    
    [SerializeField] private Button surrenderButton;
    [SerializeField] private Button byoyomiPurchaseButton;
    [SerializeField] private Button takeBackButton;
    [SerializeField] private Button exitButton;

    [SerializeField] private string surrenderConfirmMsg;
    [SerializeField] private string byoyomiPurchaseConfirmMsg;
    [SerializeField] private string takeBackConfirmMsg;

    private ValueTuple<Button, TextMeshProUGUI> _surrenderSet;
    private ValueTuple<Button, TextMeshProUGUI> _byoyomiPurchaseSet;
    private ValueTuple<Button, TextMeshProUGUI> _takeBackSet;
    private ValueTuple<Button, TextMeshProUGUI> _exitSet;
    
    private HashSet<ValueTuple<Button, TextMeshProUGUI>> _activateSet; // 플레이어 턴이 올 때마다 활성화시킬 버튼 모음
    private bool _isPlayerTurn;
    private bool _isTakeBackedTurn;
    
    private void Awake()
    {
        _surrenderSet = (surrenderButton, surrenderButton.GetComponentInChildren<TextMeshProUGUI>());
        _byoyomiPurchaseSet = (byoyomiPurchaseButton, byoyomiPurchaseButton.GetComponentInChildren<TextMeshProUGUI>());
        _takeBackSet = (takeBackButton, takeBackButton.GetComponentInChildren<TextMeshProUGUI>());
        _exitSet = (exitButton, exitButton.GetComponentInChildren<TextMeshProUGUI>());

        _byoyomiPurchaseSet.Item2.text = $"{_byoyomiPurchaseSet.Item2.text} ({GameManager.Instance.ByoyomiPurchaseAmount}회)";
        byoyomiPurchaseConfirmMsg = $"{byoyomiPurchaseConfirmMsg} ({GameManager.Instance.ByoyomiPurchaseAmount}회)";
        
        ButtonInactivate(_surrenderSet);
        ButtonInactivate(_byoyomiPurchaseSet);
        ButtonInactivate(_takeBackSet);
        ButtonInactivate(_exitSet);
        
        _activateSet = new HashSet<ValueTuple<Button, TextMeshProUGUI>>();
        _isPlayerTurn = GameManager.Instance.IsPlayerBlack;
        _isTakeBackedTurn = false;

        EventManager em = EventManager.Instance;
        GameManager gm = GameManager.Instance;
        
        em.OnGameStart += OnGameStart;
        em.OnGameEnd += OnGameEnd;
        em.OnPlayerByoyomiPurchase += OnByoyomiPurchase;
        em.OnStartSweeping += DisableIngameButton;
        gm.BoardInform.OnBlackUnmovable += DisableIngameButton;
        gm.BoardInform.OnTurnBackActivate += OnTurnBackActivate;
        gm.BoardInform.OnTurnChanged += OnTurnChanged;
        playerPanel.OnLastByoyomi += OnLastByoyomi;

#if UNITY_ANDROID
        messageBox.OnOpened += () =>
        { 
            ButtonInactivate(_surrenderSet);
            foreach (var buttonSet in _activateSet)
            {
                ButtonInactivate(buttonSet);
            }
        };

        messageBox.TurnBackToGame += () =>
        {
            ButtonActivate(_surrenderSet);
            if (_isPlayerTurn)
            {
                foreach (var buttonSet in _activateSet)
                {
                    ButtonActivate(buttonSet);
                }
            }
        };
#endif
    }
    
    public void SurrenderInput()
        => messageBox.MessageBoxOpen(surrenderConfirmMsg, Surrender);

    public void TimePurchaseInput()
        => messageBox.MessageBoxOpen(byoyomiPurchaseConfirmMsg, EventManager.Instance.PlayerByoyomiPurchase);

    public void TakeBackInput()
        => messageBox.MessageBoxOpen(takeBackConfirmMsg, TakeBack);

    public void ExitInput()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        SceneManager.LoadScene("Scenes/2.Lobby/LobbyScene - PC");
#elif UNITY_ANDROID
        SceneManager.LoadScene("Scenes/2.Lobby/LobbyScene - Android");
#endif
    }
    
    private void OnGameStart() => ButtonActivate(_surrenderSet);
    
    private void OnGameEnd()
    {
        ButtonInactivate(_surrenderSet);
        ButtonInactivate(_byoyomiPurchaseSet);
        ButtonInactivate(_takeBackSet);
        ButtonActivate(_exitSet);
    }

    private void DisableIngameButton()
    {
        ButtonInactivate(_surrenderSet);
        ButtonInactivate(_byoyomiPurchaseSet);
        ButtonInactivate(_takeBackSet);
    }

    private void OnTurnBackActivate()
        => _activateSet.Add(_takeBackSet);

    private void OnLastByoyomi()
    {
        if (GameManager.Instance.IsByoyomiPurchased)
        {
            return;
        }
        
        ButtonActivate(_byoyomiPurchaseSet);
        _activateSet.Add(_byoyomiPurchaseSet);
    }

    private void OnByoyomiPurchase(int amount)
    {
        ButtonInactivate(_byoyomiPurchaseSet);
        _activateSet.Remove(_byoyomiPurchaseSet);
    }

    private void Surrender()
    {
        ButtonInactivate(_surrenderSet);
        playerSweeper.Sweeping();
    }

    private void TakeBack()
    {
        _isTakeBackedTurn = true;
        ButtonInactivate(_takeBackSet);
        GameManager.Instance.SendTakeBackRequest();
    }

    private void OnTurnChanged(int turn)
    {
        if (_isTakeBackedTurn)
        {
            _isTakeBackedTurn = false;
            return;
        }
        
        _isPlayerTurn = (turn % 2 == 0) == GameManager.Instance.IsPlayerBlack;
        
        if (_isPlayerTurn)
        {
#if UNITY_ANDROID
            if (messageBox.gameObject.activeSelf is false)
            {
                foreach (var buttonSet in _activateSet)
                {
                    ButtonActivate(buttonSet);
                }
            }
#else
            foreach (var buttonSet in _activateSet)
            {
                ButtonActivate(buttonSet);
            }
#endif
        }
        else
        {
            foreach (var buttonSet in _activateSet)
            {
                ButtonInactivate(buttonSet);
            }
        }
    }
    
    private void ButtonInactivate((Button Button, TextMeshProUGUI Text) buttonSet)
    {
        buttonSet.Button.interactable = false;
        buttonSet.Text.color = new Color(1f, 1f, 1f, 0.5f);
    }

    private void ButtonActivate((Button Button, TextMeshProUGUI Text) buttonSet)
    {
        buttonSet.Button.interactable = true;
        buttonSet.Text.color = Color.white;
    }
}
