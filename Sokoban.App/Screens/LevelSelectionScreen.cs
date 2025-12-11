using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Sokoban.Core;

namespace Sokoban.App.Screens;

public sealed class LevelSelectionScreen : IGameScreen
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont uiFont;
    private readonly Texture2D whiteTexture;
    private readonly IReadOnlyList<LevelInfo> levelInfos;

    private int selectedLevelIndex;
    private PlayerProfile? currentProfile;

    public LevelSelectionScreen(
        GraphicsDevice graphicsDevice,
        SpriteFont uiFont,
        Texture2D whiteTexture,
        IReadOnlyList<LevelInfo> levelInfos)
    {
        this.graphicsDevice = graphicsDevice;
        this.uiFont = uiFont;
        this.whiteTexture = whiteTexture;
        this.levelInfos = levelInfos;
    }

    public int SelectedLevelIndex => selectedLevelIndex;

    public void SetCurrentProfile(PlayerProfile? profile)
    {
        currentProfile = profile;
    }

    public ScreenCommand Update(GameTime gameTime, KeyboardState current, KeyboardState previous)
    {
        if (IsActionPressed(current, previous, Keys.Escape, Keys.Q))
            return new ScreenCommand(ScreenCommandType.GoToProfileSelection);

        if (levelInfos.Count == 0)
            return ScreenCommand.None;

        var columns = GetLevelGridColumns();

        if (IsUpPressed(current, previous))
            selectedLevelIndex -= columns;

        if (IsDownPressed(current, previous))
            selectedLevelIndex += columns;

        if (IsLeftPressed(current, previous))
            selectedLevelIndex--;

        if (IsRightPressed(current, previous))
            selectedLevelIndex++;

        if (selectedLevelIndex < 0)
            selectedLevelIndex = 0;

        if (selectedLevelIndex >= levelInfos.Count)
            selectedLevelIndex = levelInfos.Count - 1;

        if (IsActionPressed(current, previous, Keys.Enter, Keys.Space))
            return new ScreenCommand(ScreenCommandType.GoToPlaying);

        if (IsActionPressed(current, previous, Keys.L, Keys.Tab))
            return new ScreenCommand(ScreenCommandType.GoToLeaderboard);

        return ScreenCommand.None;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        var height = graphicsDevice.PresentationParameters.BackBufferHeight;

        var title = currentProfile != null ? $"LEVELS - {currentProfile.Name}" : "LEVELS";
        var titleSize = uiFont.MeasureString(title);
        var titlePosition = new Vector2(width / 2f - titleSize.X / 2f, 20f);
        spriteBatch.DrawString(uiFont, title, titlePosition, Color.White);

        if (levelInfos.Count == 0)
        {
            var text = "NO LEVELS";
            var size = uiFont.MeasureString(text);
            var pos = new Vector2(width / 2f - size.X / 2f, height / 2f - size.Y / 2f);
            spriteBatch.DrawString(uiFont, text, pos, Color.White);
            return;
        }

        const int cardWidth = 220;
        const int cardHeight = 120;
        const int spacingX = 16;
        const int spacingY = 16;
        const int marginX = 40;
        const int topMargin = 80;
        const int bottomMargin = 80;

        var availableWidth = Math.Max(1, width - marginX * 2);
        var availableHeight = Math.Max(1, height - topMargin - bottomMargin);

        var columns = availableWidth / (cardWidth + spacingX);
        if (columns < 1)
            columns = 1;

        var rows = availableHeight / (cardHeight + spacingY);
        if (rows < 1)
            rows = 1;

        var levelsPerPage = columns * rows;
        var totalPages = (levelInfos.Count + levelsPerPage - 1) / levelsPerPage;

        var currentPage = selectedLevelIndex / levelsPerPage;
        if (currentPage < 0)
            currentPage = 0;
        if (currentPage >= totalPages)
            currentPage = totalPages - 1;

        var pageStartIndex = currentPage * levelsPerPage;
        var pageEndIndex = Math.Min(pageStartIndex + levelsPerPage, levelInfos.Count);

        for (var i = pageStartIndex; i < pageEndIndex; i++)
        {
            var indexOnPage = i - pageStartIndex;
            var row = indexOnPage / columns;
            var col = indexOnPage % columns;

            var x = marginX + col * (cardWidth + spacingX);
            var y = topMargin + row * (cardHeight + spacingY);

            var rect = new Rectangle(x, y, cardWidth, cardHeight);
            var isSelected = i == selectedLevelIndex;

            var levelInfo = levelInfos[i];
            var levelId = GetLevelId(levelInfo);

            var isCompleted = currentProfile != null && currentProfile.HasCompletedLevel(levelId);

            var outerColor = isSelected ? Color.Gold : Color.DimGray;
            var innerColor = isCompleted
                ? new Color(34, 139, 34)
                : (isSelected ? Color.DarkOliveGreen : Color.DarkSlateGray);

            DrawPanel(spriteBatch, rect, outerColor, innerColor);

            var levelName = FilterToAscii(levelInfo.Name);
            var textSize = uiFont.MeasureString(levelName);
            var textPosition = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2f,
                rect.Y + 10f);
            spriteBatch.DrawString(uiFont, levelName, textPosition, Color.White);

            var stars = isCompleted ? "***" : "---";
            var starsSize = uiFont.MeasureString(stars);
            var starsPosition = new Vector2(
                rect.X + (rect.Width - starsSize.X) / 2f,
                rect.Bottom - starsSize.Y - 10f);
            spriteBatch.DrawString(uiFont, stars, starsPosition, Color.LightGray);
        }

        if (totalPages > 1)
        {
            var arrowTextLeft = "<";
            var arrowTextRight = ">";

            if (currentPage > 0)
            {
                var size = uiFont.MeasureString(arrowTextLeft);
                var position = new Vector2(
                    marginX / 2f - size.X / 2f,
                    height / 2f - size.Y / 2f);
                spriteBatch.DrawString(uiFont, arrowTextLeft, position, Color.LightGray);
            }

            if (currentPage < totalPages - 1)
            {
                var size = uiFont.MeasureString(arrowTextRight);
                var position = new Vector2(
                    width - marginX / 2f - size.X / 2f,
                    height / 2f - size.Y / 2f);
                spriteBatch.DrawString(uiFont, arrowTextRight, position, Color.LightGray);
            }

            var pageLabel = $"{currentPage + 1}/{totalPages}";
            var pageSize = uiFont.MeasureString(pageLabel);
            var pagePosition = new Vector2(
                width / 2f - pageSize.X / 2f,
                height - pageSize.Y - 20f);
            spriteBatch.DrawString(uiFont, pageLabel, pagePosition, Color.LightGray);
        }

        var hint = "WASD/ARROWS-move  ENTER-start  L-leaderboard  Q-profiles";
        var hintY = height - uiFont.LineSpacing - 40f;
        DrawCenteredScaledText(spriteBatch, hint, hintY, Color.LightGray);
    }

    private int GetLevelGridColumns()
    {
        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        const int cardWidth = 220;
        const int spacingX = 16;
        const int marginX = 40;

        var availableWidth = Math.Max(1, width - marginX * 2);
        var columns = availableWidth / (cardWidth + spacingX);
        if (columns < 1)
            columns = 1;

        return columns;
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

    private void DrawCenteredScaledText(SpriteBatch spriteBatch, string text, float y, Color color)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        var textSize = uiFont.MeasureString(text);
        var maxWidth = width - 40f;
        var scale = 1f;

        if (textSize.X > maxWidth && textSize.X > 0f)
            scale = maxWidth / textSize.X;

        var position = new Vector2(
            width / 2f - textSize.X * scale / 2f,
            y);

        spriteBatch.DrawString(uiFont, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private static string GetLevelId(LevelInfo info)
    {
        return Path.GetFileName(info.FilePath);
    }

    private static string FilterToAscii(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var buffer = new char[text.Length];
        var index = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch >= ' ' && ch <= '~')
            {
                buffer[index] = ch;
                index++;
            }
        }

        return new string(buffer, 0, index);
    }

    private static bool IsKeyPressed(Keys key, KeyboardState current, KeyboardState previous)
    {
        return current.IsKeyDown(key) && !previous.IsKeyDown(key);
    }

    private static bool IsUpPressed(KeyboardState current, KeyboardState previous)
    {
        return IsKeyPressed(Keys.Up, current, previous) || IsKeyPressed(Keys.W, current, previous);
    }

    private static bool IsDownPressed(KeyboardState current, KeyboardState previous)
    {
        return IsKeyPressed(Keys.Down, current, previous) || IsKeyPressed(Keys.S, current, previous);
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
}
