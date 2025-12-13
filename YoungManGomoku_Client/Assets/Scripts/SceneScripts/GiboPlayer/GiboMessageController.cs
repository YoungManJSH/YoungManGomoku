using TMPro;
using UnityEngine;

public class GiboMessageController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GiboBoardManager boardManager;

    private void Awake()
    {
        boardManager.OnReadFailed += () => MessageBoxOpen("선택한 파일이 없거나\n올바른 양식이 아닙니다.");
        gameObject.SetActive(false);
    }


    private void MessageBoxOpen(string message)
    {
        messageText.text = message;
        gameObject.SetActive(true);
    }
    
    public void OnConfirm() => gameObject.SetActive(false);
}
