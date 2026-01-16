using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YoungManGomoku_Protocol;

public class LobbyUIController : MonoBehaviour
{
    [Header("MenuPanel")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TextMeshProUGUI playerNicknameInMenu;
    [SerializeField] private TextMeshProUGUI playerStatsInMenu;
    [SerializeField] private Slider playerLevelSliderInMenu;
    [SerializeField] private TextMeshProUGUI playerLevelInMenu;
    
    [Header("ReplayPanel")]
    [SerializeField] private GameObject replayPanel;
    
    [Header("SettingPanel")]
    [SerializeField] private GameObject settingPanel;
    
    [Header("MatchMakingPanel")]
    [SerializeField] private GameObject matchMakingPanel;
    [SerializeField] private TextMeshProUGUI playerNicknameInMatchMaking;
    [SerializeField] private TextMeshProUGUI playerStatsInMatchMaking;

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

        playerNicknameInMatchMaking.text = playerData.Nickname;
        playerStatsInMatchMaking.text = $"{playerData.WinCount}승 {playerData.LoseCount}패";
        
        Debug.Log($"{playerData.ExperiencePoint}, {playerData.MaxExperiencePoint}, {playerData.ExperiencePoint / playerData.MaxExperiencePoint}");
        
        playerLevelSliderInMenu.value = (float)playerData.ExperiencePoint / playerData.MaxExperiencePoint;
        playerLevelInMenu.text = playerData.Level.ToString();
    }

    // 키보드 esc를 누를 때, 메치메이킹 취소 / 리플레이 창 제거 / 메뉴 창 온오프 기능을 넣음.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (matchMakingPanel.activeSelf)
            {
                LobbySceneManager.Instance.CancelMatchMaking();
                return;
            }
            
            if (replayPanel.activeSelf)
            {
                replayPanel.SetActive(false);
                return;
            }
            
            if (settingPanel.activeSelf)
            {
                settingPanel.SetActive(false);
                return;
            }

            bool isActive = menuPanel.activeSelf;
            menuPanel.SetActive(!isActive);
        }
    }

    public void MoveSceneToShop()
        => SceneLoadManager.LoadScene(SceneLoadManager.SceneType.ShopScene).Cancel();

    public void OpenCloseReplayPanel()
    {
        bool isActive = replayPanel.activeSelf;
        replayPanel.SetActive(!isActive);
    }
    
    public void OpenCloseSettingPanel()
    {
        bool isActive = settingPanel.activeSelf;
        settingPanel.SetActive(!isActive);
    }
}