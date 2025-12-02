using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    private const float PPU = 100f; //Pixels per unit

    [SerializeField] private Texture2D woodTexture;
    [SerializeField] private int cellSize;
    [SerializeField] private int marginSize;
    [SerializeField] private int lineThickness;
    [SerializeField] private int pointRadius;
    [SerializeField] private Color lineColor;

    private SpriteRenderer _spriteRenderer;

    private void Start()
    {
        int totalPixel = cellSize * Board.MaxCoord + marginSize * 2;

        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.sprite = GenerateBoard(totalPixel);
        
        AdjustBoardScale(totalPixel);
    }

    private void AdjustBoardScale(int totalPixel)
    {
        float worldSize = totalPixel / PPU;
        float screenWidth = Camera.main!.orthographicSize * 2f * Camera.main.aspect;
        float scale = screenWidth / worldSize;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private Sprite GenerateBoard(int totalPixel)
    {
        int endGridPos = totalPixel - marginSize;
        Texture2D boardTexture = new Texture2D(totalPixel, totalPixel, TextureFormat.RGBA32, false);

        // woodTexture(배경) 적용
        for (int y = 0; y < totalPixel; ++y)
        {
            for (int x = 0; x < totalPixel; ++x)
            {
                boardTexture.SetPixel(x, y, woodTexture.GetPixel(x % woodTexture.width, y % woodTexture.height));
            }
        }

        // 격자선 적용
        DrawLineHor(boardTexture, marginSize, endGridPos, marginSize, lineThickness * 2, lineColor);
        DrawLineHor(boardTexture, marginSize, endGridPos, endGridPos, lineThickness * 2, lineColor);
        DrawLineVer(boardTexture, marginSize, marginSize, endGridPos, lineThickness * 2, lineColor);
        DrawLineVer(boardTexture, endGridPos, marginSize, endGridPos, lineThickness * 2, lineColor);
        for (int i = 1; i < Board.MaxCoord; ++i)
        {
            int pos = marginSize + cellSize * i;
            DrawLineHor(boardTexture, marginSize, endGridPos, pos, lineThickness, lineColor);
            DrawLineVer(boardTexture, pos, marginSize, endGridPos, lineThickness, lineColor);
        }

        // 화점 적용
        int center = marginSize + Board.MaxCoord / 2 * cellSize;
        int point1 = marginSize + 3 * cellSize;
        int point2 = totalPixel - point1;
        DrawCircle(boardTexture, center, center, pointRadius, lineColor);
        DrawCircle(boardTexture, point1, point1, pointRadius, lineColor);
        DrawCircle(boardTexture, point1, point2, pointRadius, lineColor);
        DrawCircle(boardTexture, point2, point1, pointRadius, lineColor);
        DrawCircle(boardTexture, point2, point2, pointRadius, lineColor);

        boardTexture.Apply();
        boardTexture.filterMode = FilterMode.Point;
        
        return Sprite.Create(boardTexture, new Rect(0, 0, totalPixel, totalPixel),
            new Vector2(0.5f, 0.5f), PPU);
    }

    private void DrawLineHor(Texture2D tex, int x0, int x1, int y, int thickness, Color color)
    {
        Debug.Assert(x0 <= x1);

        for (int x = x0; x <= x1; ++x)
        {
            for (int ty = -thickness; ty <= thickness; ++ty)
            {
                tex.SetPixel(x, y + ty, color);
            }
        }
    }

    private void DrawLineVer(Texture2D tex, int x, int y0, int y1, int thickness, Color color)
    {
        Debug.Assert(y0 <= y1);

        for (int y = y0; y <= y1; ++y)
        {
            for (int tx = -thickness; tx <= thickness; ++tx)
            {
                tex.SetPixel(x + tx, y, color);
            }
        }
    }

    private void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
    {
        for (int y = -radius; y <= radius; ++y)
        {
            for (int x = -radius; x <= radius; ++x)
            {
                if (x * x + y * y <= radius * radius)
                {
                    tex.SetPixel(cx + x, cy + y, color);
                }
            }
        }
    }
}