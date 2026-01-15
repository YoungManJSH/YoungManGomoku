using System;
using TMPro;
using UnityEngine;

public class GiboMessageController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GiboBoardManager boardManager;
    [SerializeField] private string readFailedText;
    [SerializeField] private string deleteFailedText;
    [SerializeField] private string simulationFailedText;

    private Action _requestedAction;
    
    private void Awake()
    {
        boardManager.OnReadFailed += OnReadFailed;
        boardManager.OnSimulationCompleted += OnSimulationCompleted;
        
        gameObject.SetActive(false);
    }


    public void MessageBoxOpen(string message, Action requestedAct = null)
    {
        messageText.text = message;
        _requestedAction = requestedAct;
        gameObject.SetActive(true);
    }

    public void OnCancel() => gameObject.SetActive(false);
    
    public void OnConfirm()
    {
        _requestedAction?.Invoke();
        gameObject.SetActive(false);
    }

    private void OnReadFailed()
        => MessageBoxOpen(readFailedText, TryDeleteFile);

    private void OnSimulationCompleted(bool isSuccess)
    {
        if (isSuccess is false)
            MessageBoxOpen(simulationFailedText);
    }

    private void TryDeleteFile()
    {
        if (GiboFileManager.TryDeleteGiboFile())
            BackToList();
        else
            MessageBoxOpen(deleteFailedText, BackToList);
    }

    private void BackToList()
    {
        LobbyUIController.UIState = LobbyUIController.PanelState.ReplayOpen;
        SceneLoadManager.LoadScene(SceneLoadManager.SceneType.LobbyScene).Cancel();
    }
}
