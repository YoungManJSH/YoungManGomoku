public readonly struct BasicPlayerData
{
    public readonly string name;
    public readonly uint win;
    public readonly uint draw;
    public readonly uint lose;
    public readonly float rating;

    public BasicPlayerData(string name, uint win, uint draw, uint lose, float rating)
    {
        this.name = name;
        this.win = win;
        this.draw = draw;
        this.lose = lose;
        this.rating = rating;
    }
}