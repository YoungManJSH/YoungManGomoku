namespace YoungManGomoku_Protocol.TypeEnum.InGame
{
    // 모든 End Reason Code는 받는 클라이언트 기준
    public enum GameEndCode
    {
        Error = -1,
        None, 
        GomokuWin, 
        GomokuLose, 
        BlackUnmovable, 
        SurrenderWin, 
        SurrenderLose,
        TimeOutWin, 
        TimeOutLose, 
        DisconnectedWin, 
        DisconnectedLose, 
        Draw
    }
}