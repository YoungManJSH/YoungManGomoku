using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using YoungManGomoku_Protocol;

public class ShopUIController : MonoBehaviour
{
    [SerializeField] private GameObject characterSkinPanel;
    [SerializeField] private GameObject goBoardSkinPanel;
    [SerializeField] private GameObject warningMessagePanel;
    
    [Header("MenuPanel")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TextMeshProUGUI playerNicknameInMenu;
    [SerializeField] private TextMeshProUGUI playerStatsInMenu;

    private PlayerData playerData;
    
    private void Start()
    {
        if (PlayerDataFromWebServer.Instance == null)
        {
            return;
        }
        
        playerData =  PlayerDataFromWebServer.Instance.PlayerData;
        
        playerNicknameInMenu.text = playerData.Nickname;
        playerStatsInMenu.text = $"{playerData.WinCount}승 {playerData.LoseCount}패";
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isActive = menuPanel.activeSelf;
            menuPanel.SetActive(!isActive);
        }
    }
    
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
#if UNITY_ANDROID && !UNITY_EDITOR
        SceneManager.LoadScene("LobbyScene - Android");
#elif UNITY_STANDALONE || UNITY_EDITOR
        SceneManager.LoadScene("LobbyScene - PC");
#endif
    }
}
