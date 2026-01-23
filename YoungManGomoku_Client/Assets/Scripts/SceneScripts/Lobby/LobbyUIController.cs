using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YoungManGomoku_Protocol;

public class LobbyUIController : MonoBehaviour
{
    public enum PanelState
    {
        Default, MenuOpen, ReplayOpen
    }

    /* 이 부분은 최대한 간단하게 구현
     * 다른 씬 → 로비 씬 이동 시에 해당 씬에서 원하는 UI 상태는
     * 해당 씬에서 알아서 주문한다는 느낌으로... */
    public static PanelState UIState { private get; set; }
    static LobbyUIController() => UIState = PanelState.Default;
    
    [Header("MenuPanel")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private ProfileImages profileImages;
    [SerializeField] private Image profileImage;
    [SerializeField] private TextMeshProUGUI playerNicknameInMenu;
    [SerializeField] private TextMeshProUGUI playerStatsInMenu;
    
    [Header("UserLevelPanel")]
    [SerializeField] private Slider playerLevelSlider;
    [SerializeField] private TextMeshProUGUI playerLevel;
    
    [Header("ReplayPanel")]
    [SerializeField] private GameObject replayPanel;
    
    [Header("SettingPanel")]
    [SerializeField] private GameObject settingPanel;
    
    [Header("MatchMakingPanel")]
    [SerializeField] private GameObject matchMakingPanel;
    [SerializeField] private TextMeshProUGUI playerNicknameInMatchMaking;
    [SerializeField] private TextMeshProUGUI playerStatsInMatchMaking;
    
    private PlayerData playerData;
    
    private void Awake()
    {
        switch (UIState)
        {
            case PanelState.Default:
                break;
            case PanelState.MenuOpen:
                menuPanel.SetActive(true);
                break;
            case PanelState.ReplayOpen:
                menuPanel.SetActive(true);
                replayPanel.SetActive(true);
                break;
        }
        
        // 다른 씬에서 주문한 상태를 적용하고 나면 초기화
        UIState = PanelState.Default;
    }

    private void Start()
    {
        if (PlayerDataFromWebServer.Instance == null)
        {
            return;
        }
        
        playerData =  PlayerDataFromWebServer.Instance.PlayerData;
        
        profileImage.sprite = profileImages[playerData.EquipProfile];
        
        playerNicknameInMenu.text = playerData.Nickname;
        playerStatsInMenu.text = $"{playerData.WinCount}승 {playerData.LoseCount}패";

        playerNicknameInMatchMaking.text = playerData.Nickname;
        playerStatsInMatchMaking.text = $"{playerData.WinCount}승 {playerData.LoseCount}패";

        playerLevelSlider.value = playerData.ExperienceRate;
        playerLevel.text = $"현재 레벨 : {playerData.Level.ToString()}";
    }

    // 키보드 esc를 누를 때, 메치메이킹 취소 / 리플레이 창 제거 / 메뉴 창 온오프 기능을 넣음.
    private void Update()
    {
        if (Input.GetButtonDown("Cancel"))
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
            
            menuPanel.SetActive(!menuPanel.activeSelf);
        }
    }
    
    public void MoveSceneToShop()
        => SceneLoadManager.LoadScene(SceneLoadManager.SceneType.ShopScene).Cancel();

    public void OpenCloseReplayPanel()
        => replayPanel.SetActive(!replayPanel.activeSelf);
    
    public void OpenCloseSettingPanel()
        => settingPanel.SetActive(!settingPanel.activeSelf);
}