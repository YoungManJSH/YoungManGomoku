using System;
using System.Collections.Generic;
using System.Text;

namespace YoungManGomoku_Protocol.Source.TypeEnum
{
    public enum CS_PacketType
    {
        None,
        // 하트비트 제대로 동작 안 할것같은데...?
        HeartBeat, // 심장박동, 이게 끊기면 클라 접속 끊긴거임
                   // 대상 A가 심장박동을 보냈을 때 대상 B의 심장박동이 1분째 끊겼다? 접속끊김

        Login,  // 로그인했어
                // ReLogin, // 팅겨서 재로그인했으니 저장된 보드정보를 넘겨줘
        MatchMaking, // 매칭시켜줘
        MatchingCancle, // 매칭취소
        ShopData,   // 상점정보 내놔
        BuyItem_Shop,   // 상점템 이거 살게
        BuyItem_Ingame, // 인게임 중 구매
        TakeBack,   // 무르기
        SetStone,   // 내돌 뒀다
        TimeOver,   // 내 시간 다 끝남
        Surrender,  // 항복


        MAXCount
    }



    // 서버가 클라로 보내는건데... 서버가 수동적이라 '클라의 요청'에만 반응해야함
    public enum SC_PacketType
    {
        GameResult, // 게임결과를 클라로 보냄
        Announce // 공지
    }

    // 매치메이킹 시 매칭된 상대 정보
    public class SC_MatchMaking_EnemyData // Packet
    {
        public string Nickname { get; set; }
        // 승률
        // 승리횟수
        // 프로필사진

        // 돌 타입을 서버에서 랜덤하게 정하기?
    }

    //상점

    public class SC_ShopData
    {
        // 상점 판매 카테고리
        // 프로필타입
        // 돌 스킨
        // 판 스킨
    }


    // 어드레서블 -> 웹서버 안쓰고 AWS로 바로?
    public class AddressableData
    {

    }





    // 인게임
    public class CS_SetStone
    {
        // 내가 둔 돌 위치를 서버에게 전달
        // row col
        // 내가 둔 시간을 서버에게 전달
        // 서버는 지금 서버시간초와 내가 둔 시간초를 비교할 것
    }

    public class CS_Surrender
    {
        // 클라가 항복을 눌렀음
    }

    public class CS_TakeBack
    {

    }


    // ItemID
    public enum ItemIDType
    {
        // 1001 스킨A
        // 1002 스킨B
        // 2001 돌A
        // 2002 돌B
        // 1 강제무르기 (rate-1)
        // 2 시간연장
        // 
    }


    public class CS_BuyItem
    {
        public CS_PacketType type { get; set; }
        public ItemIDType ItemID { get; set; }
    }


    public class SC_pos // (2명 다 줘야됨)
    {
        // 상대가 둔 돌 위치 row col
        // bool 돌 시간차 계산해서 실격이면 false
    }

}
