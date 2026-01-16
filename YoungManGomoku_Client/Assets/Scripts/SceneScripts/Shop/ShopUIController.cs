using TMPro;
using UnityEngine;
using YoungManGomoku_Protocol;

public class ShopUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerMoneyInShop;
    
    [Header("MenuPanel")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TextMeshProUGUI playerNicknameInMenu;
    [SerializeField] private TextMeshProUGUI playerStatsInMenu;

    private PlayerData playerData;
    
    /// <summary>
    /// 서버로부터 받아온 플레이어 데이터를 기반으로 UI적인 화면 표시
    /// </summary>
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
    
    /// <summary>
    /// esc 키 입력으로 메뉴 UI on/off
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isActive = menuPanel.activeSelf;
            menuPanel.SetActive(!isActive);
        }
    }

    public void MoveSceneToLobby()
        => SceneLoadManager.LoadScene(SceneLoadManager.SceneType.LobbyScene).Cancel();
}
