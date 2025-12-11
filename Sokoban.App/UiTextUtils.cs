using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Sokoban.App;

public static class UiTextUtils
{
    public static void DrawHint(
        SpriteBatch spriteBatch,
        SpriteFont font,
        string text,
        int screenWidth,
        int screenHeight,
        int bottomMargin = 20,
        Color? color = null)
    {
        var maxWidth = screenWidth - 40f;
        var lines = WrapText(font, text, maxWidth);

        var hintColor = color ?? Color.LightGray;
        var totalHeight = lines.Count * font.LineSpacing;
        var startY = screenHeight - bottomMargin - totalHeight;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var size = font.MeasureString(line);
            var position = new Vector2(
                (screenWidth - size.X) / 2f,
                startY + i * font.LineSpacing);

            spriteBatch.DrawString(font, line, position, hintColor);
        }
    }

    private static List<string> WrapText(SpriteFont font, string text, float maxWidth)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return result;
        }

        var words = text.Split(' ');
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var trimmed = word.Trim();
            if (trimmed.Length == 0)
                continue;

            var candidate = string.IsNullOrEmpty(currentLine)
                ? trimmed
                : currentLine + " " + trimmed;

            if (font.MeasureString(candidate).X <= maxWidth)
            {
                currentLine = candidate;
            }
            else
            {
                if (!string.IsNullOrEmpty(currentLine))
                    result.Add(currentLine);

                currentLine = trimmed;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            result.Add(currentLine);

        return result;
    }
}
