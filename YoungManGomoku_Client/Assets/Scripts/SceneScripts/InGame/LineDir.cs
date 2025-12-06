public enum LineDir
{
    None = -1,

    /// <summary>(→) Left to Right</summary>
    Horizontal,

    /// <summary>(↓) Top to Down</summary>
    Vertical,

    /// <summary>(↗) Left-Down to Right-Top</summary>
    DiagonalUp,

    /// <summary>(↘) Left-Top to Right-Down</summary>
    DiagonalDown,
    Max
}