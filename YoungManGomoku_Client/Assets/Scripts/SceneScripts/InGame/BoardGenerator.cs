using UnityEngine;

public static class BoardGenerator
{ 
    public static Sprite GenerateBoard(BoardImageData data)
    {
        int endGridPos = data.TotalPixel - data.MarginSize;
        
        Texture2D boardTexture = new Texture2D(data.TotalPixel, data.TotalPixel, TextureFormat.RGBA32, false);

        // woodTexture(배경) 적용
        for (int y = 0; y < data.TotalPixel; ++y)
        {
            for (int x = 0; x < data.TotalPixel; ++x)
            {
                boardTexture.SetPixel(x, y, data.WoodTexture.GetPixel(x % data.WoodTexture.width, y % data.WoodTexture.height));
            }
        }

        // 격자선 적용
        DrawLineHor(boardTexture, data.MarginSize, endGridPos, data.MarginSize, data.LineThickness * 2, data.LineColor);
        DrawLineHor(boardTexture, data.MarginSize, endGridPos, endGridPos, data.LineThickness * 2, data.LineColor);
        DrawLineVer(boardTexture, data.MarginSize, data.MarginSize, endGridPos, data.LineThickness * 2, data.LineColor);
        DrawLineVer(boardTexture, endGridPos, data.MarginSize, endGridPos, data.LineThickness * 2, data.LineColor);
        for (int i = 1; i < Board.MaxCoord; ++i)
        {
            int pos = data.MarginSize + data.CellSize * i;
            DrawLineHor(boardTexture, data.MarginSize, endGridPos, pos, data.LineThickness, data.LineColor);
            DrawLineVer(boardTexture, pos, data.MarginSize, endGridPos, data.LineThickness, data.LineColor);
        }

        // 화점 적용
        int center = data.MarginSize + Board.MaxCoord / 2 * data.CellSize;
        int point1 = data.MarginSize + 3 * data.CellSize;
        int point2 = data.TotalPixel - point1;
        DrawCircle(boardTexture, center, center, data.PointRadius, data.LineColor);
        DrawCircle(boardTexture, point1, point1, data.PointRadius, data.LineColor);
        DrawCircle(boardTexture, point1, point2, data.PointRadius, data.LineColor);
        DrawCircle(boardTexture, point2, point1, data.PointRadius, data.LineColor);
        DrawCircle(boardTexture, point2, point2, data.PointRadius, data.LineColor);

        boardTexture.Apply();
        boardTexture.filterMode = FilterMode.Point;
        
        return Sprite.Create(boardTexture, new Rect(0, 0, data.TotalPixel, data.TotalPixel),
            new Vector2(0.5f, 0.5f), data.PixelsPerUnit);
    }

    private static void DrawLineHor(Texture2D tex, int x0, int x1, int y, int thickness, Color color)
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

    private static void DrawLineVer(Texture2D tex, int x, int y0, int y1, int thickness, Color color)
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

    private static void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
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
