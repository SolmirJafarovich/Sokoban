using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Sokoban.App.Screens;

public sealed class LeaderboardScreen : IGameScreen
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont uiFont;
    private readonly Texture2D whiteTexture;

    private string levelId = string.Empty;
    private string levelName = string.Empty;
    private List<Row> rows = new();

    public LeaderboardScreen(
        GraphicsDevice graphicsDevice,
        SpriteFont uiFont,
        Texture2D whiteTexture)
    {
        this.graphicsDevice = graphicsDevice;
        this.uiFont = uiFont;
        this.whiteTexture = whiteTexture;
    }

    public void SetLevel(string levelId, string levelName, IReadOnlyList<PlayerProfile> profiles)
    {
        this.levelId = levelId;
        this.levelName = levelName;

        var list = new List<Row>();

        foreach (var profile in profiles)
        {
            if (profile.TryGetLevelStats(levelId, out var stats))
            {
                list.Add(new Row(
                    profile.Name,
                    true,
                    stats.BestTimeMs,
                    stats.BestSteps));
            }
            else
            {
                list.Add(new Row(profile.Name, false, 0, 0));
            }
        }

        list.Sort(CompareRows);
        rows = list;
    }

    public ScreenCommand Update(GameTime gameTime, KeyboardState current, KeyboardState previous)
    {
        if (IsActionPressed(current, previous, Keys.Escape, Keys.Q) ||
            IsActionPressed(current, previous, Keys.Back, Keys.Tab))
        {
            return new ScreenCommand(ScreenCommandType.GoToLevelSelection);
        }

        return ScreenCommand.None;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        var height = graphicsDevice.PresentationParameters.BackBufferHeight;

        var title = $"LEADERBOARD - {levelName}";
        var titleSize = uiFont.MeasureString(title);
        var titlePos = new Vector2(width / 2f - titleSize.X / 2f, 20f);
        spriteBatch.DrawString(uiFont, title, titlePos, Color.White);

        var panelRect = new Rectangle(40, 80, width - 80, height - 160);
        DrawPanel(spriteBatch, panelRect, Color.DimGray, Color.DarkSlateGray);

        if (rows.Count == 0)
        {
            var text = "NO PLAYERS";
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

        var hint = "ESC/Q/BACK - levels";
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

        var nameColumnX = panelRect.X + 40;
        var timeColumnX = panelRect.X + panelRect.Width / 2;
        var stepsColumnX = panelRect.X + panelRect.Width - 160;

        spriteBatch.DrawString(uiFont, "PLAYER", new Vector2(nameColumnX, headerY), Color.Gold);
        spriteBatch.DrawString(uiFont, "TIME", new Vector2(timeColumnX, headerY), Color.Gold);
        spriteBatch.DrawString(uiFont, "STEPS", new Vector2(stepsColumnX, headerY), Color.Gold);

        var y = rowStartY;
        var maxRows = (panelRect.Bottom - rowStartY - 20) / uiFont.LineSpacing;

        for (var i = 0; i < rows.Count && i < maxRows; i++)
        {
            var row = rows[i];
            var nameColor = row.HasResult ? Color.White : Color.DarkGray;
            var valueColor = row.HasResult ? Color.LightGreen : Color.DarkGray;

            var namePos = new Vector2(nameColumnX, y);
            spriteBatch.DrawString(uiFont, row.PlayerName, namePos, nameColor);

            if (row.HasResult)
            {
                var timeText = FormatTime(row.BestTimeMs);
                var stepsText = row.BestSteps.ToString();

                spriteBatch.DrawString(uiFont, timeText, new Vector2(timeColumnX, y), valueColor);
                spriteBatch.DrawString(uiFont, stepsText, new Vector2(stepsColumnX, y), valueColor);
            }

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

    private static int CompareRows(Row x, Row y)
    {
        if (x.HasResult && !y.HasResult)
            return -1;
        if (!x.HasResult && y.HasResult)
            return 1;
        if (!x.HasResult && !y.HasResult)
            return string.Compare(x.PlayerName, y.PlayerName, StringComparison.Ordinal);

        if (x.BestTimeMs != y.BestTimeMs)
            return x.BestTimeMs.CompareTo(y.BestTimeMs);

        if (x.BestSteps != y.BestSteps)
            return x.BestSteps.CompareTo(y.BestSteps);

        return string.Compare(x.PlayerName, y.PlayerName, StringComparison.Ordinal);
    }

    private static string FormatTime(int timeMs)
    {
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

    private readonly struct Row
    {
        public Row(string playerName, bool hasResult, int bestTimeMs, int bestSteps)
        {
            PlayerName = playerName;
            HasResult = hasResult;
            BestTimeMs = bestTimeMs;
            BestSteps = bestSteps;
        }

        public string PlayerName { get; }
        public bool HasResult { get; }
        public int BestTimeMs { get; }
        public int BestSteps { get; }
    }
}
