using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [SerializeField] private Texture2D woodTexture;
    [SerializeField] private int cellSize;
    [SerializeField] private int marginSize;
    [SerializeField] private int lineThickness;
    [SerializeField] private Color lineColor;

    private Renderer _renderer;

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        _renderer.material.mainTexture = GenerateBoard();
    }

    private Texture2D GenerateBoard()
    {
        int totalPixel = cellSize * Board.MaxCoord + marginSize * 2;
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
        for (int i = 0; i < Board.BoardSize; ++i)
        {
            int pos = i * cellSize + marginSize;
            DrawLineHor(boardTexture, 0, totalPixel, pos, lineThickness, lineColor);
            DrawLineVer(boardTexture, pos, 0, totalPixel, lineThickness, lineColor);
        }
        
        // 화점 적용
        int center = marginSize + Board.MaxCoord / 2 * cellSize;
        int point1 = marginSize + 3 * cellSize;
        int point2 = totalPixel - point1;
        DrawCircle(boardTexture, center, center, 6, lineColor);
        DrawCircle(boardTexture, point1, point1, 6, lineColor);
        DrawCircle(boardTexture, point1, point2, 6, lineColor);
        DrawCircle(boardTexture, point2, point1, 6, lineColor);
        DrawCircle(boardTexture, point2, point2, 6, lineColor);
        
        return boardTexture;
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