using System;
using TMPro;
using UnityEngine;

public class GiboMessageController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GiboBoardManager boardManager;

    private Action _requestedAction;
    
    private void Awake()
    {
        boardManager.OnReadFailed += () =>
            MessageBoxOpen("선택한 파일이 없거나\n올바른 양식이 아닙니다.\n로비로 이동할까요?", LoadLobbyScene);
        
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

    private void LoadLobbyScene()
        => SceneLoadManager.LoadScene(SceneLoadManager.SceneType.LobbyScene);
}
