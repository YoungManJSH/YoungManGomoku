using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;

public class ShopUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerMoneyInShop;
    
    [Header("MenuPanel")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TextMeshProUGUI playerNicknameInMenu;
    [SerializeField] private TextMeshProUGUI playerStatsInMenu;
    
    [Header("ShopUI")]
    [SerializeField] private CanvasScaler scaler;
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private GameObject profileUIPrefab;
    [SerializeField] private ProfileImages profileImages;

    private PlayerData playerData;

    private void Awake()
    {
        RectTransform rt = grid.GetComponent<RectTransform>(); // CanvasScaler의 기준 해상도
        float referenceWidth = scaler.referenceResolution.x;
        float referenceHeight = scaler.referenceResolution.y; // 현재 RectTransform 크기 기준 비율
        float widthRatio = rt.rect.width / referenceWidth;
        float heightRatio = rt.rect.height / referenceHeight; // 셀 크기 계산
        float cellWidth = 450f * widthRatio;
        float cellHeight = 500f * heightRatio;
        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }

    /// <summary>
    /// 서버로부터 받아온 플레이어 데이터를 기반으로 UI적인 화면 표시
    /// </summary> 
    private void Start()
    {
        if (PlayerDataFromWebServer.Instance == null)
        {
            return;
        }
        
        // 플레이어 정보 UI 설정
        playerData =  PlayerDataFromWebServer.Instance.PlayerData;
        
        playerNicknameInMenu.text = playerData.Nickname;
        playerStatsInMenu.text = $"{playerData.WinCount}승 {playerData.LoseCount}패";

        playerMoneyInShop.text = playerData.GameMoney.ToString();
        
        // 상점 UI 설정
        
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
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        LobbyUIController.UIState = LobbyUIController.PanelState.MenuOpen;
#endif
        SceneLoadManager.LoadScene(SceneLoadManager.SceneType.LobbyScene).Cancel();
    }

    /// <summary>
    /// 서버에 있는 데이터에 맞춰 UI를 동적으로 생성하는 코드
    /// 서버 코드가 for문으로 순차적으로 반환하기에 여기서도 count 변수를 둬서 순차를 이용함.
    /// </summary>
    private void UpdateShopProfileUI()
    {
        bool[] ownedProfiles = ShopDataManager.Instance.GetOwnedPlayerProfile();
        int count = 1;
        
        foreach (var profileItem in ShopDataManager.Instance.GetBuyableProfile())
        {
            GameObject newProfileItem = Instantiate(profileUIPrefab, grid.gameObject.transform, true);
            
            newProfileItem.transform.Find("ItemName").GetComponent<TextMeshProUGUI>().text = profileItem.ItemName;
            newProfileItem.transform.Find("ItemImage").GetComponent<Image>().sprite = profileImages[(ProfileImageType)count];
            newProfileItem.transform.Find("Image").Find("Cost").GetComponent<TextMeshProUGUI>().text = profileItem.Cost.ToString();

            if (ownedProfiles[count] == true)
            {
                newProfileItem.transform.Find("HasItem").gameObject.SetActive(true);
            }

            if (count == (int)PlayerDataFromWebServer.Instance.PlayerData.EquipProfile)
            {
                newProfileItem.transform.Find("EquipedItem").gameObject.SetActive(true);
            }

            if (profileItem.LevelLimit < PlayerDataFromWebServer.Instance.PlayerData.Level)
            {
                newProfileItem.transform.Find("ButtonOff").gameObject.SetActive(true);
                newProfileItem.transform.Find("ButtonOff").Find("LevelLimit").GetComponent<TextMeshProUGUI>().text = $"레벨제한 {profileItem.LevelLimit}";
            }

            count++;
        }
    }
}
