using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    [SerializeField] private MessageBoxManager messageBox;
    [SerializeField] private PlayerPanelController playerPanel;
    [SerializeField] private PlayerMoneyUI playerMoneyUI;
    [SerializeField] private StoneMover stoneMover;
    [SerializeField] private ResultPresenter resultPresenter;
    
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
        
        ButtonInactivate(_surrenderSet);
        ButtonInactivate(_byoyomiPurchaseSet);
        ButtonInactivate(_takeBackSet);
        ButtonInactivate(_exitSet);
        _activateSet = new HashSet<ValueTuple<Button, TextMeshProUGUI>>();
        
        EventManager em = EventManager.Instance;
        em.OnGameStart += OnGameStart;
        em.OnGameEnd += OnGameEnd;
        em.OnTakeBack += OnTakeBack;
        em.OnWaitingRematch += () => ButtonInactivate(_exitSet);
        em.OnRematchFailed += () => ButtonActivate(_exitSet);
        
        GameManager gm = GameManager.Instance;
        gm.BoardInform.OnBlackGomoku += DisableIngameButton;
        gm.BoardInform.OnWhiteGomoku += DisableIngameButton;
        gm.BoardInform.OnBlackUnmovable += DisableIngameButton;
        gm.BoardInform.OverMaxTurn += DisableIngameButton;
        gm.BoardInform.OnTurnBackActivate += OnTurnBackActivate;
        gm.OnPlayerMoneyChanged += OnPlayerMoneyChanged;
        playerPanel.OnLastByoyomi += OnLastByoyomi;
        stoneMover.OnStoneMove += OnStoneMove;
        
        _byoyomiPurchaseSet.Item2.text += $" ({gm.ItemCost.ByoyomiPurchaseCost}G)";
        _takeBackSet.Item2.text += $" ({gm.ItemCost.TakeBackCost}G)";
        surrenderConfirmMsg += "\n(판 엎기)";
        byoyomiPurchaseConfirmMsg += $"\n({gm.ByoyomiPurchaseAmount}회, {gm.ItemCost.ByoyomiPurchaseCost}골드)";
        takeBackConfirmMsg += $"\n(성사 시 {gm.ItemCost.TakeBackCost}골드)";
        
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
        => messageBox.MessageBoxOpen(surrenderConfirmMsg, RequestSurrender);

    private void TimePurchaseInput()
    {
        playerMoneyUI.StartGlowEffect();
        messageBox.MessageBoxOpen(byoyomiPurchaseConfirmMsg, RequestTimePurchase);
    }

    private void TakeBackInput()
    {
        stoneMover.MarkTakeBack();
        playerMoneyUI.StartGlowEffect();
        messageBox.MessageBoxOpen(takeBackConfirmMsg, RequestTakeBack);
    }

    private void ExitInput()
    {
        resultPresenter.RejectRematch();
        SceneLoadManager.LoadScene(SceneLoadManager.SceneType.LobbyScene).Cancel();
    }

    private void OnGameStart() => ButtonActivate(_surrenderSet);
    
    private async void OnGameEnd()
    {
        try
        {
            DisableIngameButton();
            
            // 결과창 출력 이전에는 나가기 버튼 비허용
            await Awaitable.WaitForSecondsAsync(0.5f);
            ButtonActivate(_exitSet);
        }
        catch (Exception e)
        {
            Debug.LogError($"게임 종료 상황 버튼 활성화 제어 로직 오류: {e}");
        }
    }

    private void DisableIngameButton()
    {
        _activateSet.Clear();
        ButtonInactivate(_surrenderSet);
        ButtonInactivate(_byoyomiPurchaseSet);
        ButtonInactivate(_takeBackSet);
    }

    private void OnTurnBackActivate()
    {
        if (GameManager.Instance.HasTakeBackCost)
            _activateSet.Add(_takeBackSet);
    }

    private async void OnTakeBack(bool isAccepted)
    {
        try
        {
            if (isAccepted is false) return;

            // 물러진 후로 상태가 변경되기까지 대기
            await Awaitable.NextFrameAsync();

            if (GameManager.Instance.BoardInform.NowTurn < 3)
            {
                _activateSet.Remove(_takeBackSet);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"무르기에 따른 버튼 상태 변경 로직 에러: {e}");
            EventManager.Instance.ServerReplyFailed();
        }
    }

    private void OnPlayerMoneyChanged(int _)
    {
        GameManager gm = GameManager.Instance;
        
        if (gm.HasByoyomiCost is false)
        {
            ButtonInactivate(_byoyomiPurchaseSet);
            _activateSet.Remove(_byoyomiPurchaseSet);
        }
        else if (gm.IsByoyomiPurchased is false &&
                 gm.PlayerTimer.MainTime == 0f &&
                 gm.PlayerTimer.ByoyomiCount == 1)
        {
            // 재화 획득은 상대방 턴일 때만 발생할 수 있으므로 버튼 활성화 생략
            _activateSet.Add(_byoyomiPurchaseSet);
        }

        if (gm.HasTakeBackCost is false)
        {
            ButtonInactivate(_takeBackSet);
            _activateSet.Remove(_takeBackSet);
        }
        else if (gm.BoardInform.NowTurn >= 3)
        {
            // 재화 획득은 상대방 턴일 때만 발생할 수 있으므로 버튼 활성화 생략
            _activateSet.Add(_takeBackSet);
        }
    }
    
    private void OnLastByoyomi()
    {
        GameManager gm = GameManager.Instance;
        
        if (gm.IsByoyomiPurchased || gm.HasByoyomiCost is false)
            return;
        
        // 이전에 초읽기 구매를 한 적이 없고 재화를 충분히 갖고 있는 경우
        ButtonActivate(_byoyomiPurchaseSet);
        _activateSet.Add(_byoyomiPurchaseSet);
    }

    private void RequestSurrender()
    {
        ButtonInactivate(_surrenderSet);
        EventManager.Instance.RequestSurrender();
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
