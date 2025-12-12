using UnityEngine;

[CreateAssetMenu(fileName = "BoardImageData", menuName = "Scriptable Objects/BoardImageData")]
public class BoardImageData : ScriptableObject
{
    [SerializeField] private Texture2D woodTexture;
    [SerializeField] private int cellSize;
    [SerializeField] private int marginSize;
    [SerializeField] private Color lineColor;
    [SerializeField] private int lineThickness;
    [SerializeField] private int pointRadius;
    [SerializeField] private float pixelsPerUnit;

    public Texture2D WoodTexture => woodTexture;
    public int CellSize => cellSize;
    public int MarginSize => marginSize;
    public Color LineColor => lineColor;
    public int LineThickness => lineThickness;
    public int PointRadius => pointRadius;
    public float PixelsPerUnit => pixelsPerUnit;
    
    public int TotalPixel { get; private set; }

    private void OnEnable()
        => TotalPixel = cellSize * Board.MaxCoord + marginSize * 2;
}
