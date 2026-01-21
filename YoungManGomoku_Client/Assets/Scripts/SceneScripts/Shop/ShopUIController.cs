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
    
    [Header("ShopUI")]
    [SerializeField] private CanvasScaler scaler;
    [SerializeField] private GridLayoutGroup grid;
    [SerializeField] private GameObject profileUIPrefab;
    [SerializeField] private ProfileImages profileImages;
    
    [Header("ItemBuyPanel")]
    [SerializeField] private GameObject itemBuyPanel;
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemCost;
    [SerializeField] private Button itemBuyButton;
    
    [Header("NotEnoughMoneyPanel")]
    [SerializeField] private GameObject notEnoughMoneyPanel;

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
            if (count == 0)
            {
                count++;
                continue;
            }
            
            GameObject newProfileItem = Instantiate(profileUIPrefab, grid.gameObject.transform, true);
            
            newProfileItem.transform.Find("ItemName").GetComponent<TextMeshProUGUI>().text = profileItem.ItemName;
            newProfileItem.transform.Find("ItemImage").GetComponent<Image>().sprite = profileImages[(ProfileImageType)count];
            newProfileItem.transform.Find("Image").Find("Cost").GetComponent<TextMeshProUGUI>().text = profileItem.Cost.ToString();

            if (ownedProfiles[count] == true)
            {
                newProfileItem.transform.Find("HasItem").gameObject.SetActive(true);
            }
            else
            {
                SetBuyEventToButton(newProfileItem,profileItem,(ProfileImageType)count);
            }

            if (count == (int)playerData.EquipProfile)
            {
                newProfileItem.transform.Find("EquipedItem").gameObject.SetActive(true);
            }

            if (profileItem.LevelLimit > playerData.Level)
            {
                newProfileItem.transform.Find("ButtonOff").gameObject.SetActive(true);
                newProfileItem.transform.Find("ButtonOff").Find("LevelLimit").GetComponent<TextMeshProUGUI>().text = $"레벨제한 {profileItem.LevelLimit}";
            }

            count++;
        }
    }

    private void SetBuyEventToButton(GameObject button, ShopItemData shopItemData, ProfileImageType imageType)
    {
        button.GetComponent<Button>().onClick.AddListener(()=>
        {
            if (playerData.CashMoney >= shopItemData.Cost)
            {
                notEnoughMoneyPanel.SetActive(true);
                return;
            }
            
            // 상점 구매 확정 창 출현 및 내부 UI 채우기
            itemBuyPanel.SetActive(true);
            itemImage.sprite = profileImages[imageType];
            itemName.text = shopItemData.ItemName;
            itemCost.text =  shopItemData.Cost.ToString();
            
            // 구매하기 버튼에 지정할 코드
            itemBuyButton.onClick.RemoveAllListeners();
            itemBuyButton.onClick.AddListener(async () =>
            {
                CS_RequestBuyItemDTO requestBuyItemDTO = new CS_RequestBuyItemDTO();
                requestBuyItemDTO.IDToken = SystemInfo.deviceUniqueIdentifier;
                requestBuyItemDTO.BuyItemType = shopItemData.ItemType;
                requestBuyItemDTO.BuyItemID = (int)imageType;
                requestBuyItemDTO.GameMoney = playerData.GameMoney;

                SC_ResponseStringDTO response = await GetComponent<NetworkManager>().RequestBuySkin(requestBuyItemDTO);

                if (response.IsSuccess==true)
                {
                    // 보유 금액 제거 및 UI 갱신
                    playerData.GameMoney -= shopItemData.Cost;
                    playerMoneyInShop.text = playerData.GameMoney.ToString();
                    
                    // 구매한 아이템 상태 변경
                    button.transform.Find("HasItem").gameObject.SetActive(true);

                    // 캐시 된 인벤토리 업데이트
                    ShopDataManager.Instance.GetOwnedPlayerProfile()[(int)imageType] = true;
                    
                    // 장비 가능 UI로 변경 필요
                    
                    // 아이템 구매 창 비활성화
                    itemBuyPanel.SetActive(false);
                }
            });
        });
    }
}
