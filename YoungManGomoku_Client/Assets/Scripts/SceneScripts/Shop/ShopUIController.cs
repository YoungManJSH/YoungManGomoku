using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopUIController : MonoBehaviour
{
    [SerializeField] private GameObject characterSkinPanel;
    [SerializeField] private GameObject goBoardSkinPanel;
    [SerializeField] private GameObject warningMessagePanel;

    public void ChangeToGoBoardSkinPanel()
    {
        goBoardSkinPanel.SetActive(true);
        characterSkinPanel.SetActive(false);
    }
    
    public void ChangeToCharacterSkinPanel()
    {
        goBoardSkinPanel.SetActive(false);
        characterSkinPanel.SetActive(true);
    }

    public void OpenDevelopingWarning()
    {
        warningMessagePanel.SetActive(true);
    }

    public void CloseDevelopingWarning()
    {
        warningMessagePanel.SetActive(false);
    }

    public void MoveSceneToLobby()
    {
        SceneManager.LoadScene("LobbyScene");
    }
}
