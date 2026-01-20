using System;
using UnityEngine;
using YoungManGomoku_Protocol;
using YoungManGomoku_Protocol.ServerToClient;
using YoungManGomoku_Protocol.TypeEnum.PlayerData;

public class ShopDataManager : MonoBehaviour
{
    public static ShopDataManager Instance { get; private set; }

    private SC_FirstEnterShopDTO playerShopData;

    /// <summary>
    /// 해당 스크립트는 서버로부터 상점 데이터를 받아와서 활용합니다.
    /// 서버와의 통신을 줄이기 위해 상점 데이터는 로그인시 한 번만 불러오며, 이후 캐시된 데이터를 사용합니다.
    /// 이후 공개된 메서드를 이용해 상점 상호작용을 진행하고 서버와 통신을 시도합니다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Initialize

    /// <summary>
    /// 타이틀에서 로그인 성공시 실행하는 함수
    /// 상점 데이터를 서버에 요청하고 캐싱해놓는다.
    /// </summary>
    /// <param name="shopDataDTO"></param>
    public void InitializeShopData(SC_FirstEnterShopDTO shopDataDTO)
        => playerShopData = shopDataDTO;

    #endregion

    #region Method

    /// <summary>
    /// 상점에 진입시 실행하는 함수
    /// 프로필 소유 여부를 리턴한다.
    /// </summary>
    public bool[] GetOwnedPlayerProfile()
        => playerShopData.PlayerSkinInventory.ProfileInventrory;

    /// <summary>
    /// 상점에 진입시 실행하는 함수
    /// 구매 가능 아이템을 리턴한다.
    /// </summary>
    public ShopItemData[] GetBuyableProfile()
        => playerShopData.ShopItemData.ProfilenShopDatas;

    /// <summary>
    /// 상점 구매를 완료 후 캐시된 데이터에도 변경사항 적용하기
    /// </summary>
    /// <param name="profileNumber"></param>
    public void ChangePlayerProfileStateAfterBuy(ProfileImageType profileNumber)
        => playerShopData.PlayerSkinInventory.ProfileInventrory[(int)profileNumber] = true;
    #endregion
}