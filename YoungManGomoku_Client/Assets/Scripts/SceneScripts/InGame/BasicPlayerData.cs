using YoungManGomoku_Protocol.TypeEnum.PlayerData;

/// <summary> 인게임에서 쓰이는 플레이어 데이터 struct </summary>
public readonly struct BasicPlayerData
{
    public readonly string name;
    public readonly uint win;
    public readonly uint draw;
    public readonly uint lose;
    public readonly float rating;
    public readonly ProfileImageType imageNum;

    public BasicPlayerData(string name, uint win, uint draw, uint lose, float rating,
        ProfileImageType imageNum = ProfileImageType.None)
    {
        this.name = name;
        this.win = win;
        this.draw = draw;
        this.lose = lose;
        this.rating = rating;
        this.imageNum = imageNum;
    }
}