using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Sokoban.App.Screens;

public sealed class GlobalLeaderboardScreen : IGameScreen
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont uiFont;
    private readonly Texture2D whiteTexture;

    private List<GlobalLeaderboardEntry> entries = new();

    public GlobalLeaderboardScreen(
        GraphicsDevice graphicsDevice,
        SpriteFont uiFont,
        Texture2D whiteTexture)
    {
        this.graphicsDevice = graphicsDevice;
        this.uiFont = uiFont;
        this.whiteTexture = whiteTexture;
    }

    public void SetEntries(IReadOnlyList<GlobalLeaderboardEntry> newEntries)
    {
        entries = new List<GlobalLeaderboardEntry>(newEntries);
    }

    public ScreenCommand Update(GameTime gameTime, KeyboardState current, KeyboardState previous)
    {
        if (IsActionPressed(current, previous, Keys.Escape, Keys.Q) ||
            IsActionPressed(current, previous, Keys.Back, Keys.Tab))
        {
            return new ScreenCommand(ScreenCommandType.GoToProfileSelection);
        }

        return ScreenCommand.None;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        var height = graphicsDevice.PresentationParameters.BackBufferHeight;

        const string title = "GLOBAL LEADERBOARD";
        var titleSize = uiFont.MeasureString(title);
        var titlePos = new Vector2(width / 2f - titleSize.X / 2f, 20f);
        spriteBatch.DrawString(uiFont, title, titlePos, Color.White);

        var panelRect = new Rectangle(40, 80, width - 80, height - 160);
        DrawPanel(spriteBatch, panelRect, Color.DimGray, Color.DarkSlateGray);

        if (entries.Count == 0)
        {
            var text = "NO DATA";
            var size = uiFont.MeasureString(text);
            var pos = new Vector2(
                panelRect.X + (panelRect.Width - size.X) / 2f,
                panelRect.Y + (panelRect.Height - size.Y) / 2f);
            spriteBatch.DrawString(uiFont, text, pos, Color.LightGray);
        }
        else
        {
            DrawTable(spriteBatch, panelRect);
        }

        var hint = "ESC/Q/BACK - profiles";
        var hintSize = uiFont.MeasureString(hint);
        var hintPos = new Vector2(
            width / 2f - hintSize.X / 2f,
            height - hintSize.Y - 20f);
        spriteBatch.DrawString(uiFont, hint, hintPos, Color.LightGray);
    }

    private void DrawTable(SpriteBatch spriteBatch, Rectangle panelRect)
    {
        var headerY = panelRect.Y + 20;
        var rowStartY = headerY + uiFont.LineSpacing + 10;

        var rankColumnX = panelRect.X + 20;
        var nameColumnX = panelRect.X + 80;
        var levelsColumnX = panelRect.X + panelRect.Width / 2 - 80;
        var stepsColumnX = panelRect.X + panelRect.Width - 230;
        var timeColumnX = panelRect.X + panelRect.Width - 110;

        spriteBatch.DrawString(uiFont, "#", new Vector2(rankColumnX, headerY), Color.Gold);
        spriteBatch.DrawString(uiFont, "PLAYER", new Vector2(nameColumnX, headerY), Color.Gold);
        spriteBatch.DrawString(uiFont, "LEVELS", new Vector2(levelsColumnX, headerY), Color.Gold);
        spriteBatch.DrawString(uiFont, "STEPS", new Vector2(stepsColumnX, headerY), Color.Gold);
        spriteBatch.DrawString(uiFont, "TIME", new Vector2(timeColumnX, headerY), Color.Gold);

        var y = rowStartY;
        var maxRows = (panelRect.Bottom - rowStartY - 20) / uiFont.LineSpacing;

        for (var i = 0; i < entries.Count && i < maxRows; i++)
        {
            var entry = entries[i];
            var rankText = (i + 1).ToString();
            var levelsText = entry.CompletedLevels.ToString();
            var stepsText = entry.TotalSteps.ToString();
            var timeText = FormatTime(entry.TotalTimeMs);

            spriteBatch.DrawString(uiFont, rankText, new Vector2(rankColumnX, y), Color.LightGray);
            spriteBatch.DrawString(uiFont, entry.PlayerName, new Vector2(nameColumnX, y), Color.White);
            spriteBatch.DrawString(uiFont, levelsText, new Vector2(levelsColumnX, y), Color.LightGreen);
            spriteBatch.DrawString(uiFont, stepsText, new Vector2(stepsColumnX, y), Color.LightGreen);
            spriteBatch.DrawString(uiFont, timeText, new Vector2(timeColumnX, y), Color.LightGreen);

            y += uiFont.LineSpacing;
        }
    }

    private void DrawPanel(SpriteBatch spriteBatch, Rectangle rect, Color borderColor, Color fillColor)
    {
        DrawRectangle(spriteBatch, rect, borderColor);
        var innerRect = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6);
        DrawRectangle(spriteBatch, innerRect, fillColor);
    }

    private void DrawRectangle(SpriteBatch spriteBatch, Rectangle rectangle, Color color)
    {
        spriteBatch.Draw(whiteTexture, rectangle, color);
    }

    private static string FormatTime(int timeMs)
    {
        if (timeMs <= 0)
            return "-";

        var ts = TimeSpan.FromMilliseconds(timeMs);
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}:{ts.Seconds:00}.{ts.Milliseconds / 100}";
        return $"{ts.Seconds}.{ts.Milliseconds / 100}s";
    }

    private static bool IsKeyPressed(Keys key, KeyboardState current, KeyboardState previous)
    {
        return current.IsKeyDown(key) && !previous.IsKeyDown(key);
    }

    private static bool IsActionPressed(KeyboardState current, KeyboardState previous, Keys primary, Keys secondary)
    {
        return IsKeyPressed(primary, current, previous) || IsKeyPressed(secondary, current, previous);
    }
}
