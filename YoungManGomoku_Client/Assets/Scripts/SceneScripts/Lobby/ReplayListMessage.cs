using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReplayListMessage : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI message;
    [SerializeField] private LobbyUIController lobbyUIController;
    
    private CancellationTokenSource _cts;
    private bool _respond;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
        gameObject.SetActive(false);
    }
    
    private void Update()
    {
        //TODO: 외부 영역 클릭(터치) 시 취소로 꺼져야 함!!
        
        if (Input.GetButtonDown("Submit"))
        {
            OnConfirm();
            return;
        }

        if (Input.GetButtonDown("Cancel"))
        {
            OnCancel();
        }
    }

    private void OnEnable() => lobbyUIController.enabled = false;
    private void OnDisable() => lobbyUIController.enabled = true;
    
    public async Awaitable<bool> OpenMessageBox(string messageText)
    {
        _cts = new CancellationTokenSource();
        _respond = false;
        
        message.text = messageText;
        gameObject.SetActive(true);

        try { await Awaitable.WaitForSecondsAsync(60f, _cts.Token); }
        catch (OperationCanceledException) { }
        
        gameObject.SetActive(false);
        _cts.Dispose();
        _cts = null;
        
        return _respond;
    }
    
    private void OnConfirm()
    {
        _respond = true;
        _cts.Cancel();
    }

    private void OnCancel() => _cts.Cancel();
}