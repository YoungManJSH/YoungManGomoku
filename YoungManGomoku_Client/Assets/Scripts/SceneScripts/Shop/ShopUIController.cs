using TMPro;
using UnityEngine;
using YoungManGomoku_Protocol;

public class ShopUIController : MonoBehaviour
{
    [SerializeField] private GameObject warningMessagePanel;
    [SerializeField] private TextMeshProUGUI playerMoneyInShop;
    
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

        playerMoneyInShop.text = playerData.GameMoney.ToString();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isActive = menuPanel.activeSelf;
            menuPanel.SetActive(!isActive);
        }
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
        => SceneLoadManager.LoadScene(SceneLoadManager.SceneType.LobbyScene).Cancel();
}
