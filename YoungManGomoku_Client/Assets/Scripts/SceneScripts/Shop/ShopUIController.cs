using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ClientToServer;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;

public class ShopUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerMoneyInShop;

    [Header("MenuPanel")] 
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TextMeshProUGUI playerNicknameInMenu;
    [SerializeField] private TextMeshProUGUI playerStatsInMenu;
    [SerializeField] private Image playerProfileInMenu;

    [Header("ShopUI")] 
    [SerializeField] private CanvasScaler scaler;
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private GameObject profileUIPrefab;
    [SerializeField] private GameObject profileUIForAndroidPrefab;
    [SerializeField] private ProfileImages profileImages;

    [Header("ItemBuyPanel")] 
    [SerializeField] private GameObject itemBuyPanel;

    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemCost;
    [SerializeField] private Button itemBuyButton;

    [Header("NotEnoughMoneyPanel")] 
    [SerializeField] private GameObject notEnoughMoneyPanel;
    
    [Header("DebugMode")] 
    [SerializeField] private TextMeshProUGUI debugText;

    /// <summary>
    /// 더블 클릭 감지용 변수들
    /// </summary>
    /// <returns></returns>
    private Button lastClickedButton;
    private float lastClickTime; 
    private float doubleClickIntervalTime;

    /// <summary>
    /// 장착된 아이템 캐시용
    /// </summary>
    private GameObject equippedProfile;

    private void Awake()
    {
        lastClickTime = 0f;
        doubleClickIntervalTime = 0.3f;
        
#if UNITY_STANDALONE || UNITY_EDITOR
        RectTransform rt = grid.GetComponent<RectTransform>(); // CanvasScaler의 기준 해상도
        float referenceWidth = scaler.referenceResolution.x;
        float referenceHeight = scaler.referenceResolution.y; // 현재 RectTransform 크기 기준 비율
        float widthRatio = rt.rect.width / referenceWidth;
        float heightRatio = rt.rect.height / referenceHeight; // 셀 크기 계산
        float cellWidth = 450f * widthRatio;
        float cellHeight = 500f * heightRatio;
        grid.cellSize = new Vector2(cellWidth, cellHeight);
#endif
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
        var playerData = PlayerDataFromWebServer.Instance.PlayerData;

        playerProfileInMenu.sprite = profileImages[playerData.EquipProfile];
        playerNicknameInMenu.text = playerData.Nickname;
        playerStatsInMenu.text = $"{playerData.WinCount}승 {playerData.LoseCount}패";

        playerMoneyInShop.text = playerData.GameMoney.ToString();

        // 상점 UI 설정
        UpdateShopProfileUI();
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
        int count = 0;

        foreach (var profileItem in ShopDataManager.Instance.GetBuyableProfile())
        {

#if UNITY_STANDALONE || UNITY_EDITOR
            GameObject newProfileItem = Instantiate(profileUIPrefab, grid.gameObject.transform, true);
#else

            GameObject newProfileItem = Instantiate(profileUIForAndroidPrefab, grid.gameObject.transform, true);
#endif
            newProfileItem.transform.Find("ItemName").GetComponent<TextMeshProUGUI>().text = profileItem.ItemName;
            newProfileItem.transform.Find("ItemImage").GetComponent<Image>().sprite =
                profileImages[(ProfileImageType)count];
            newProfileItem.transform.Find("Image").Find("Cost").GetComponent<TextMeshProUGUI>().text =
                profileItem.Cost.ToString();
            
            if (ownedProfiles[count] == true)
            {
                newProfileItem.transform.Find("HasItem").gameObject.SetActive(true);
                SetEquipEventToButton(newProfileItem, profileItem, (ProfileImageType)count);
            }
            else if (profileItem.LevelLimit <= PlayerDataFromWebServer.Instance.PlayerData.Level)
            {
                SetBuyEventToButton(newProfileItem, profileItem, (ProfileImageType)count);
            }
            else
            {
                newProfileItem.transform.Find("ButtonOff").gameObject.SetActive(true);
                newProfileItem.transform.Find("ButtonOff").Find("LevelLimit").GetComponent<TextMeshProUGUI>().text =
                    $"레벨제한 {profileItem.LevelLimit}";
            }

            if (count == (int)PlayerDataFromWebServer.Instance.PlayerData.EquipProfile)
            {
                newProfileItem.transform.Find("EquippedItem").gameObject.SetActive(true);
                equippedProfile = newProfileItem;
            }

            count++;
        }
    }

    /// <summary>
    /// 구매 가능한 프로필이라면 클릭시 실행되는 이벤트
    /// 프로필 버튼 클릭시 -> 구매 확정 창이 뜨고, 해당 창의 내부를 채우기.
    /// 구매 확정 버튼 -> 서버로 요청 보내고 긍정이 오면, 구매 이후 데이터 반영 및 UI 변경.
    /// </summary>
    /// <param name="button"></param>
    /// <param name="shopItemData"></param>
    /// <param name="profileType"></param>
    private void SetBuyEventToButton(GameObject button, ShopItemData shopItemData, ProfileImageType profileType)
    {
        button.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (PlayerDataFromWebServer.Instance.PlayerData.CashMoney >= shopItemData.Cost)
            {
                notEnoughMoneyPanel.SetActive(true);
                return;
            }

            // 상점 구매 확정 창 출현 및 내부 UI 채우기
            itemBuyPanel.SetActive(true);
            itemImage.sprite = profileImages[profileType];
            itemName.text = shopItemData.ItemName;
            itemCost.text = shopItemData.Cost.ToString();

            // 구매하기 버튼에 지정할 코드
            itemBuyButton.onClick.RemoveAllListeners();
            itemBuyButton.onClick.AddListener(async () =>
            {
                CS_RequestBuyItemDTO requestBuyItemDTO = new CS_RequestBuyItemDTO();
#if UNITY_STANDALONE || UNITY_EDITOR
                requestBuyItemDTO.IDToken = SystemInfo.deviceUniqueIdentifier;
#else
                requestBuyItemDTO.IDToken = PlayerDataFromWebServer.Instance.IDToken;
                debugText.text = $"아이디 토큰 설정완료";
#endif
                requestBuyItemDTO.BuyItemType = shopItemData.ItemType;
                requestBuyItemDTO.BuyItemID = (uint)profileType;
                requestBuyItemDTO.GameMoney = PlayerDataFromWebServer.Instance.PlayerData.GameMoney;
                
                SC_ResponseStringDTO response = await GetComponent<NetworkManager>().RequestBuySkin(requestBuyItemDTO);

                if (response.IsSuccess == true)
                {
                    // 보유 금액 제거 및 UI 갱신
                    PlayerDataFromWebServer.Instance.PlayerData.GameMoney -= shopItemData.Cost;
                    playerMoneyInShop.text = PlayerDataFromWebServer.Instance.PlayerData.GameMoney.ToString();

                    // 구매한 아이템 상태 변경
                    button.transform.Find("HasItem").gameObject.SetActive(true);

                    // 캐시 된 인벤토리 업데이트
                    ShopDataManager.Instance.ChangePlayerProfileStateAfterBuy(profileType);

                    // 장비 가능 UI로 변경 필요
                    button.GetComponent<Button>().onClick.RemoveAllListeners();
                    SetEquipEventToButton(button, shopItemData, profileType);

                    // 아이템 구매 창 비활성화
                    itemBuyPanel.SetActive(false);
                }
                else
                {
                    // 아이템 구매 창 비활성화만 적용
                    itemBuyPanel.SetActive(false);
                }
            });
        });
    }

    private void SetEquipEventToButton(GameObject button, ShopItemData shopItemData, ProfileImageType profileType)
    {
        button.GetComponent<Button>().onClick.AddListener(async () =>
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;
            
            if (lastClickedButton == null || lastClickedButton != button.GetComponent<Button>())
            {
                lastClickedButton = button.GetComponent<Button>();
                return;
            }
            
            if (timeSinceLastClick > doubleClickIntervalTime)
            {
                return;
            }

            if (profileType == PlayerDataFromWebServer.Instance.PlayerData.EquipProfile)
            {
                return;
            }
            
            // 같은 프로필을 클릭중이고, 더블클릭으로 판정된 해피패스
            
            CS_RequestEquipItemDTO requestEquipItemDTO = new CS_RequestEquipItemDTO();
#if UNITY_STANDALONE || UNITY_EDITOR
            requestEquipItemDTO.IDToken = SystemInfo.deviceUniqueIdentifier;
#else
                requestEquipItemDTO.IDToken = PlayerDataFromWebServer.Instance.IDToken;
#endif
            requestEquipItemDTO.EquipItemType = shopItemData.ItemType;
            requestEquipItemDTO.EquipItemID = (int)profileType;

            SC_ResponseStringDTO response = await GetComponent<NetworkManager>().RequestEquipSkin(requestEquipItemDTO);

            if (response.IsSuccess == true)
            {
                // 장착중인 프로필 데이터 변경
                PlayerDataFromWebServer.Instance.PlayerData.EquipProfile = profileType;
                
                // UI 변경사항 적용 (메뉴창)
                playerProfileInMenu.sprite = profileImages[PlayerDataFromWebServer.Instance.PlayerData.EquipProfile];
                
                // UI 변경사항 적용 (상점창)
                button.transform.Find("EquippedItem").gameObject.SetActive(true);

                if (equippedProfile != null)
                {
                    equippedProfile.transform.Find("EquippedItem").gameObject.SetActive(false);
                }
                
                equippedProfile = button;
            }
        });
    }
}
