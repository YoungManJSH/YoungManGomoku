using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private MessageBoxManager messageBox;
    [SerializeField] private BoardSweeper playerSweeper;
    [SerializeField] private PlayerPanelController playerPanel;
    [SerializeField] private StoneMover stoneMover;
    
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
    
    private void Awake()
    {
        _surrenderSet = (surrenderButton, surrenderButton.GetComponentInChildren<TextMeshProUGUI>());
        _byoyomiPurchaseSet = (byoyomiPurchaseButton, byoyomiPurchaseButton.GetComponentInChildren<TextMeshProUGUI>());
        _takeBackSet = (takeBackButton, takeBackButton.GetComponentInChildren<TextMeshProUGUI>());
        _exitSet = (exitButton, exitButton.GetComponentInChildren<TextMeshProUGUI>());

        _byoyomiPurchaseSet.Item2.text = $"{_byoyomiPurchaseSet.Item2.text} ({GameManager.Instance.ByoyomiPurchaseAmount}회)";
        byoyomiPurchaseConfirmMsg = $"{byoyomiPurchaseConfirmMsg}\n({GameManager.Instance.ByoyomiPurchaseAmount}회)";
        
        ButtonInactivate(_surrenderSet);
        ButtonInactivate(_byoyomiPurchaseSet);
        ButtonInactivate(_takeBackSet);
        ButtonInactivate(_exitSet);
        _activateSet = new HashSet<ValueTuple<Button, TextMeshProUGUI>>();
        
        EventManager em = EventManager.Instance;
        GameManager gm = GameManager.Instance;
        em.OnGameStart += OnGameStart;
        em.OnGameEnd += OnGameEnd;
        em.OnStartSweeping += DisableIngameButton;
        gm.BoardInform.OnBlackUnmovable += DisableIngameButton;
        gm.BoardInform.OnTurnBackActivate += OnTurnBackActivate;
        playerPanel.OnLastByoyomi += OnLastByoyomi;
        stoneMover.OnStoneMove += OnStoneMove;
        
        surrenderButton.onClick.AddListener(SurrenderInput);
        byoyomiPurchaseButton.onClick.AddListener(TimePurchaseInput);
        takeBackButton.onClick.AddListener(TakeBackInput);
        exitButton.onClick.AddListener(ExitInput);

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
            // 플레이어 턴일 때만 activateSet 활성화
            if (gm.BoardInform.NowTurn % 2 == 0 == gm.IsPlayerBlack)
            {
                foreach (var buttonSet in _activateSet)
                {
                    ButtonActivate(buttonSet);
                }
            }
        };
#endif
    }
    
    private void SurrenderInput()
        => messageBox.MessageBoxOpen(surrenderConfirmMsg, playerSweeper.Sweeping);

    private void TimePurchaseInput()
        => messageBox.MessageBoxOpen(byoyomiPurchaseConfirmMsg, RequestTimePurchase);

    private void TakeBackInput()
    {
        stoneMover.MarkTakeBack();
        messageBox.MessageBoxOpen(takeBackConfirmMsg, RequestTakeBack);
    }

    private void ExitInput()
        => SceneLoadManager.LoadScene(SceneLoadManager.SceneType.LobbyScene);
    
    private void OnGameStart() => ButtonActivate(_surrenderSet);
    
    private void OnGameEnd()
    {
        DisableIngameButton();
        ButtonActivate(_exitSet);
    }

    private void DisableIngameButton()
    {
        _activateSet.Clear();
        ButtonInactivate(_surrenderSet);
        ButtonInactivate(_byoyomiPurchaseSet);
        ButtonInactivate(_takeBackSet);
    }

    private void OnTurnBackActivate()
        => _activateSet.Add(_takeBackSet);

    private void OnLastByoyomi()
    {
        if (GameManager.Instance.IsByoyomiPurchased)
            return;
        
        ButtonActivate(_byoyomiPurchaseSet);
        _activateSet.Add(_byoyomiPurchaseSet);
    }

    private void RequestTimePurchase()
    {
        ButtonInactivate(_byoyomiPurchaseSet);
        _activateSet.Remove(_byoyomiPurchaseSet); // 초읽기 구매는 판당 1번만 가능한 설정
        EventManager.Instance.RequestPurchaseByoyomi();
    }

    private void RequestTakeBack()
    {
        ButtonInactivate(_takeBackSet); // 무르기 신청은 한 턴에 한 번만 가능
        EventManager.Instance.RequestTakeBack();
    }

    private void OnStoneMove(bool isBlackTurn)
    {
        if (isBlackTurn == GameManager.Instance.IsPlayerBlack)
        {
#if UNITY_ANDROID
            if (messageBox.gameObject.activeSelf) return;
#endif
            foreach (var buttonSet in _activateSet)
            {
                ButtonActivate(buttonSet);
            }
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
