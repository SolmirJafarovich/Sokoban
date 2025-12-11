using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Sokoban.App;

namespace Sokoban.App.Screens;

public sealed class LeaderboardScreen : IGameScreen
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont uiFont;
    private readonly Texture2D whiteTexture;
    private readonly GlobalLeaderboardService globalService;

    private List<Page> pages = new();
    private int currentPageIndex;

    public LeaderboardScreen(
        GraphicsDevice graphicsDevice,
        SpriteFont uiFont,
        Texture2D whiteTexture,
        GlobalLeaderboardService globalService)
    {
        this.graphicsDevice = graphicsDevice;
        this.uiFont = uiFont;
        this.whiteTexture = whiteTexture;
        this.globalService = globalService;
    }

    public void SetData(
        IReadOnlyList<LevelInfo> levelInfos,
        IReadOnlyList<PlayerProfile> profiles,
        int selectedLevelIndex)
    {
        var newPages = new List<Page>();

        var globalEntries = globalService.BuildLeaderboard(profiles);
        newPages.Add(Page.CreateGlobal(globalEntries));

        for (var i = 0; i < levelInfos.Count; i++)
        {
            var info = levelInfos[i];
            var levelId = Path.GetFileName(info.FilePath);
            var rows = BuildLevelRows(levelId, profiles);
            newPages.Add(Page.CreateLevel(info.Name, levelId, rows));
        }

        pages = newPages;

        if (selectedLevelIndex >= 0 && selectedLevelIndex < levelInfos.Count)
            currentPageIndex = selectedLevelIndex + 1;
        else
            currentPageIndex = 0;
    }

    public ScreenCommand Update(GameTime gameTime, KeyboardState current, KeyboardState previous)
    {
        if (IsActionPressed(current, previous, Keys.Escape, Keys.Q) ||
            IsActionPressed(current, previous, Keys.Back, Keys.Tab))
        {
            return new ScreenCommand(ScreenCommandType.GoToLevelSelection);
        }

        if (IsLeftPressed(current, previous))
            currentPageIndex--;

        if (IsRightPressed(current, previous))
            currentPageIndex++;

        if (pages.Count == 0)
        {
            currentPageIndex = 0;
        }
        else
        {
            if (currentPageIndex < 0)
                currentPageIndex = 0;
            if (currentPageIndex >= pages.Count)
                currentPageIndex = pages.Count - 1;
        }

        return ScreenCommand.None;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        var height = graphicsDevice.PresentationParameters.BackBufferHeight;

        if (pages.Count == 0)
        {
            const string header = "LEADERBOARD";
            var headerSize = uiFont.MeasureString(header);
            var headerPos = new Vector2(width / 2f - headerSize.X / 2f, 20f);
            spriteBatch.DrawString(uiFont, header, headerPos, Color.White);

            const string text = "NO DATA";
            var textSize = uiFont.MeasureString(text);
            var textPos = new Vector2(width / 2f - textSize.X / 2f, height / 2f - textSize.Y / 2f);
            spriteBatch.DrawString(uiFont, text, textPos, Color.LightGray);

            return;
        }

        var page = pages[currentPageIndex];

        var title = page.IsGlobal ? "GLOBAL LEADERBOARD" : $"LEVEL: {page.Title}";
        var titleSize = uiFont.MeasureString(title);
        var titlePos = new Vector2(width / 2f - titleSize.X / 2f, 20f);
        spriteBatch.DrawString(uiFont, title, titlePos, Color.White);

        var panelRect = new Rectangle(40, 80, width - 80, height - 160);
        DrawPanel(spriteBatch, panelRect, Color.DimGray, Color.DarkSlateGray);

        DrawTable(spriteBatch, panelRect, page);

        var pageLabel = $"{currentPageIndex + 1}/{pages.Count}";
        var pageSize = uiFont.MeasureString(pageLabel);
        var pagePos = new Vector2(width / 2f - pageSize.X / 2f, panelRect.Bottom + 10);
        spriteBatch.DrawString(uiFont, pageLabel, pagePos, Color.LightGray);

        UiTextUtils.DrawHint(
            spriteBatch,
            uiFont,
            "LEFT/RIGHT - page Q - levels",
            width,
            height);
    }

    private static List<LevelRow> BuildLevelRows(string levelId, IReadOnlyList<PlayerProfile> profiles)
    {
        var rows = new List<LevelRow>();

        foreach (var profile in profiles)
        {
            if (profile.TryGetLevelStats(levelId, out var stats))
            {
                rows.Add(new LevelRow(
                    profile.Name,
                    true,
                    stats.BestSteps,
                    stats.BestTimeMs));
            }
            else
            {
                rows.Add(new LevelRow(profile.Name, false, 0, 0));
            }
        }

        rows.Sort(CompareLevelRows);
        return rows;
    }

    private void DrawTable(SpriteBatch spriteBatch, Rectangle panelRect, Page page)
    {
        var headerY = panelRect.Y + 20;
        var rowStartY = headerY + uiFont.LineSpacing + 10;
        var lineHeight = uiFont.LineSpacing;

        var rankX = panelRect.X + 20f;
        var nameX = panelRect.X + 60f;

        if (page.IsGlobal)
        {
            var levelsAnchorX = panelRect.X + panelRect.Width * 0.50f;
            var stepsAnchorX = panelRect.X + panelRect.Width * 0.70f;
            var timeAnchorX = panelRect.X + panelRect.Width * 0.88f;

            var nameMaxWidth = levelsAnchorX - nameX - 25f;

            DrawLeft(spriteBatch, "#", rankX, headerY, Color.Gold);
            DrawLeft(spriteBatch, "PLAYER", nameX, headerY, Color.Gold);
            DrawCentered(spriteBatch, "LEVELS", levelsAnchorX, headerY, Color.Gold);
            DrawCentered(spriteBatch, "STEPS", stepsAnchorX, headerY, Color.Gold);
            DrawCentered(spriteBatch, "TIME", timeAnchorX, headerY, Color.Gold);

            var y = rowStartY;
            var maxRows = (panelRect.Bottom - rowStartY - 20) / lineHeight;

            for (var i = 0; i < page.GlobalEntries.Count && i < maxRows; i++)
            {
                var entry = page.GlobalEntries[i];

                var rankText = (i + 1).ToString();
                var nameText = TruncateText(entry.PlayerName, nameMaxWidth);
                var levelsText = entry.CompletedLevels.ToString();
                var stepsText = entry.TotalSteps.ToString();
                var timeText = FormatTime(entry.TotalTimeMs);

                DrawLeft(spriteBatch, rankText, rankX, y, Color.LightGray);
                DrawLeft(spriteBatch, nameText, nameX, y, Color.White);
                DrawCentered(spriteBatch, levelsText, levelsAnchorX, y, Color.LightGreen);
                DrawCentered(spriteBatch, stepsText, stepsAnchorX, y, Color.LightGreen);
                DrawCentered(spriteBatch, timeText, timeAnchorX, y, Color.LightGreen);

                y += lineHeight;
            }
        }
        else
        {
            var stepsAnchorX = panelRect.X + panelRect.Width * 0.65f;
            var timeAnchorX = panelRect.X + panelRect.Width * 0.83f;

            var nameMaxWidth = stepsAnchorX - nameX - 25f;

            DrawLeft(spriteBatch, "#", rankX, headerY, Color.Gold);
            DrawLeft(spriteBatch, "PLAYER", nameX, headerY, Color.Gold);
            DrawCentered(spriteBatch, "STEPS", stepsAnchorX, headerY, Color.Gold);
            DrawCentered(spriteBatch, "TIME", timeAnchorX, headerY, Color.Gold);

            var y = rowStartY;
            var maxRows = (panelRect.Bottom - rowStartY - 20) / lineHeight;

            for (var i = 0; i < page.LevelRows.Count && i < maxRows; i++)
            {
                var row = page.LevelRows[i];

                var rankText = (i + 1).ToString();
                var nameText = TruncateText(row.PlayerName, nameMaxWidth);

                var nameColor = row.HasResult ? Color.White : Color.DarkGray;
                var valueColor = row.HasResult ? Color.LightGreen : Color.DarkGray;

                DrawLeft(spriteBatch, rankText, rankX, y, Color.LightGray);
                DrawLeft(spriteBatch, nameText, nameX, y, nameColor);

                if (row.HasResult)
                {
                    var stepsText = row.BestSteps.ToString();
                    var timeText = FormatTime(row.BestTimeMs);

                    DrawCentered(spriteBatch, stepsText, stepsAnchorX, y, valueColor);
                    DrawCentered(spriteBatch, timeText, timeAnchorX, y, valueColor);
                }

                y += lineHeight;
            }
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

    private void DrawLeft(SpriteBatch spriteBatch, string text, float x, float y, Color color)
    {
        var position = new Vector2(x, y);
        spriteBatch.DrawString(uiFont, text, position, color);
    }

    private void DrawCentered(SpriteBatch spriteBatch, string text, float centerX, float y, Color color)
    {
        var size = uiFont.MeasureString(text);
        var position = new Vector2(centerX - size.X / 2f, y);
        spriteBatch.DrawString(uiFont, text, position, color);
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

    private string TruncateText(string text, float maxWidth)
    {
        if (maxWidth <= 0f)
            return string.Empty;

        var width = uiFont.MeasureString(text).X;
        if (width <= maxWidth)
            return text;

        const string ellipsis = "...";
        var ellipsisWidth = uiFont.MeasureString(ellipsis).X;
        var allowedWidth = maxWidth - ellipsisWidth;

        if (allowedWidth <= 0f)
            return ellipsis;

        var result = text;

        while (result.Length > 0 && uiFont.MeasureString(result).X > allowedWidth)
            result = result[..^1];

        return result + ellipsis;
    }

    private static int CompareLevelRows(LevelRow x, LevelRow y)
    {
        if (x.HasResult && !y.HasResult)
            return -1;
        if (!x.HasResult && y.HasResult)
            return 1;
        if (!x.HasResult && !y.HasResult)
            return string.Compare(x.PlayerName, y.PlayerName, StringComparison.Ordinal);

        if (x.BestSteps != y.BestSteps)
            return x.BestSteps.CompareTo(y.BestSteps);

        if (x.BestTimeMs != y.BestTimeMs)
            return x.BestTimeMs.CompareTo(y.BestTimeMs);

        return string.Compare(x.PlayerName, y.PlayerName, StringComparison.Ordinal);
    }

    private static bool IsKeyPressed(Keys key, KeyboardState current, KeyboardState previous)
    {
        return current.IsKeyDown(key) && !previous.IsKeyDown(key);
    }

    private static bool IsLeftPressed(KeyboardState current, KeyboardState previous)
    {
        return IsKeyPressed(Keys.Left, current, previous) || IsKeyPressed(Keys.A, current, previous);
    }

    private static bool IsRightPressed(KeyboardState current, KeyboardState previous)
    {
        return IsKeyPressed(Keys.Right, current, previous) || IsKeyPressed(Keys.D, current, previous);
    }

    private static bool IsActionPressed(KeyboardState current, KeyboardState previous, Keys primary, Keys secondary)
    {
        return IsKeyPressed(primary, current, previous) || IsKeyPressed(secondary, current, previous);
    }

    private sealed class Page
    {
        private Page(bool isGlobal, string title, List<GlobalLeaderboardEntry> globalEntries, List<LevelRow> levelRows)
        {
            IsGlobal = isGlobal;
            Title = title;
            GlobalEntries = globalEntries;
            LevelRows = levelRows;
        }

        public bool IsGlobal { get; }

        public string Title { get; }

        public List<GlobalLeaderboardEntry> GlobalEntries { get; }

        public List<LevelRow> LevelRows { get; }

        public static Page CreateGlobal(IReadOnlyList<GlobalLeaderboardEntry> entries)
        {
            return new Page(true, "GLOBAL", new List<GlobalLeaderboardEntry>(entries), new List<LevelRow>());
        }

        public static Page CreateLevel(string levelName, string levelId, IReadOnlyList<LevelRow> rows)
        {
            return new Page(false, levelName, new List<GlobalLeaderboardEntry>(), new List<LevelRow>(rows));
        }
    }

    private readonly struct LevelRow
    {
        public LevelRow(string playerName, bool hasResult, int bestSteps, int bestTimeMs)
        {
            PlayerName = playerName;
            HasResult = hasResult;
            BestSteps = bestSteps;
            BestTimeMs = bestTimeMs;
        }

        public string PlayerName { get; }
        public bool HasResult { get; }
        public int BestSteps { get; }
        public int BestTimeMs { get; }
    }
}
