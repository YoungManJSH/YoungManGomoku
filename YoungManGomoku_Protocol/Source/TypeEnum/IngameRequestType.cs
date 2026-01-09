namespace YoungManGomoku_Protocol.TypeEnum.InGame
{
    public enum IngameRequestType
    {
        /// <summary> 상대방 응답으로 아무 일 없었을 때 </summary>
        None,

        /// <summary> 기권(게임 포기) </summary>
        Surrender,
        
        /// <summary> 무르기 신청 구매 </summary>
        TakeBack,
        
        /// <summary> 초읽기 구매 </summary>
        PurchaseByoyomi,

        /// <summary> 무르기 승인 or 거부 허가 확답 </summary>
        TakeBackResult
    }
}