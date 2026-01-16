using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GiboMessageController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GiboBoardManager boardManager;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private string readFailedText;
    [SerializeField] private string deleteFailedText;
    [SerializeField] private string simulationFailedText;

    private Action _requestedAction;
    
    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
        
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
    
    private void OnConfirm()
    {
        gameObject.SetActive(false);
        _requestedAction?.Invoke();
    }

    private void OnCancel()
    {
        gameObject.SetActive(false);
        _requestedAction = null;
    }

    private void OnReadFailed()
        => MessageBoxOpen(readFailedText, DeleteFile);

    private void OnSimulationCompleted(bool isSuccess)
    {
        if (isSuccess is false)
            MessageBoxOpen(simulationFailedText);
    }

    private void DeleteFile()
    {
        if (GiboFileManager.TryDeleteGiboFile())
            GiboBoardManager.TurnBackToLobby();
        else
            MessageBoxOpen(deleteFailedText, GiboBoardManager.TurnBackToLobby);
    }
}
