using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Sokoban.Core;

namespace Sokoban.App;

public sealed class Game1 : Game
{
    private const int TileSize = 64;

    private readonly GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch = null!;
    private Texture2D whiteTexture = null!;

    private Texture2D floorTexture = null!;
    private Texture2D wallTexture = null!;
    private Texture2D targetTexture = null!;
    private Texture2D crateTexture = null!;
    private Texture2D playerTexture = null!;
    private SpriteFont uiFont = null!;

    private Level level = null!;
    private KeyboardState previousKeyboardState;

    private GameScreen screen = GameScreen.ProfileSelection;
    private IReadOnlyList<LevelInfo> levelInfos = Array.Empty<LevelInfo>();
    private int selectedLevelIndex;
    private List<PlayerProfile> profiles = new();
    private int selectedProfileIndex;
    private PlayerProfile? currentProfile;
    private GameSettings settings = new();

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        settings = new GameSettings();

        profiles = new List<PlayerProfile>
        {
            new PlayerProfile("Player 1"),
            new PlayerProfile("Player 2")
        };
        selectedProfileIndex = 0;
        currentProfile = profiles[0];

        levelInfos = LevelDirectory.LoadLevels(AppContext.BaseDirectory);
        selectedLevelIndex = 0;

        ApplyDisplayMode();

        screen = GameScreen.ProfileSelection;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        whiteTexture = new Texture2D(GraphicsDevice, 1, 1);
        var data = new[] { Color.White };
        whiteTexture.SetData(data);

        floorTexture = Content.Load<Texture2D>("Tiles/ground");
        wallTexture = Content.Load<Texture2D>("Tiles/wall");
        targetTexture = Content.Load<Texture2D>("Tiles/target");
        crateTexture = Content.Load<Texture2D>("Tiles/crate");
        playerTexture = Content.Load<Texture2D>("Tiles/player");
        uiFont = Content.Load<SpriteFont>("Fonts/UiFont");

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();

        if (IsKeyPressed(Keys.F11, keyboardState))
        {
            settings.ToggleFullScreen();
            ApplyDisplayMode();
        }

        if (keyboardState.IsKeyDown(Keys.Escape) && screen == GameScreen.Playing)
        {
            screen = GameScreen.LevelSelection;
        }
        else if (keyboardState.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (screen == GameScreen.ProfileSelection)
            UpdateProfileSelection(keyboardState);
        else if (screen == GameScreen.LevelSelection)
            UpdateLevelSelection(keyboardState);
        else if (screen == GameScreen.Playing)
            UpdateGamePlaying(keyboardState);

        previousKeyboardState = keyboardState;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        spriteBatch.Begin();

        if (screen == GameScreen.ProfileSelection)
            DrawProfileSelection();
        else if (screen == GameScreen.LevelSelection)
            DrawLevelSelection();
        else if (screen == GameScreen.Playing)
            DrawLevel();

        spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdateProfileSelection(KeyboardState keyboardState)
    {
        if (profiles.Count == 0)
            return;

        if (IsKeyPressed(Keys.Up, keyboardState))
            ChangeSelectedProfile(-1);

        if (IsKeyPressed(Keys.Down, keyboardState))
            ChangeSelectedProfile(1);

        if (IsKeyPressed(Keys.N, keyboardState))
            CreateNewProfile();

        if (IsKeyPressed(Keys.Enter, keyboardState))
        {
            if (selectedProfileIndex >= 0 && selectedProfileIndex < profiles.Count)
                currentProfile = profiles[selectedProfileIndex];

            screen = GameScreen.LevelSelection;
        }

        if (IsKeyPressed(Keys.A, keyboardState))
            settings.ChangeMusicVolume(-1);

        if (IsKeyPressed(Keys.D, keyboardState))
            settings.ChangeMusicVolume(1);

        if (IsKeyPressed(Keys.J, keyboardState))
            settings.ChangeEffectsVolume(-1);

        if (IsKeyPressed(Keys.L, keyboardState))
            settings.ChangeEffectsVolume(1);

        if (IsKeyPressed(Keys.F, keyboardState))
        {
            settings.ToggleFullScreen();
            ApplyDisplayMode();
        }
    }

    private void UpdateLevelSelection(KeyboardState keyboardState)
    {
        if (levelInfos.Count == 0)
            return;

        var columns = GetLevelGridColumns();

        if (IsKeyPressed(Keys.Left, keyboardState))
            selectedLevelIndex--;

        if (IsKeyPressed(Keys.Right, keyboardState))
            selectedLevelIndex++;

        if (IsKeyPressed(Keys.Up, keyboardState))
            selectedLevelIndex -= columns;

        if (IsKeyPressed(Keys.Down, keyboardState))
            selectedLevelIndex += columns;

        if (selectedLevelIndex < 0)
            selectedLevelIndex = 0;

        if (selectedLevelIndex >= levelInfos.Count)
            selectedLevelIndex = levelInfos.Count - 1;

        if (IsKeyPressed(Keys.Enter, keyboardState))
            StartGameWithSelectedLevel();

        if (IsKeyPressed(Keys.Back, keyboardState))
            screen = GameScreen.ProfileSelection;
    }

    private void UpdateGamePlaying(KeyboardState keyboardState)
    {
        if (IsKeyPressed(Keys.Up, keyboardState))
            level.TryMove(Direction.Up);

        if (IsKeyPressed(Keys.Down, keyboardState))
            level.TryMove(Direction.Down);

        if (IsKeyPressed(Keys.Left, keyboardState))
            level.TryMove(Direction.Left);

        if (IsKeyPressed(Keys.Right, keyboardState))
            level.TryMove(Direction.Right);

        if (IsKeyPressed(Keys.R, keyboardState))
            StartGameWithSelectedLevel();

        if (level.IsCompleted())
        {
            if (selectedLevelIndex < levelInfos.Count - 1)
                selectedLevelIndex++;

            screen = GameScreen.LevelSelection;
        }
    }

    private void StartGameWithSelectedLevel()
    {
        if (levelInfos.Count == 0)
            throw new InvalidOperationException("No levels loaded.");

        var info = levelInfos[selectedLevelIndex];
        var lines = File.ReadAllLines(info.FilePath);

        level = LevelLoader.LoadFromLines(lines);

        screen = GameScreen.Playing;
    }

    private void ChangeSelectedProfile(int delta)
    {
        var index = selectedProfileIndex + delta;

        if (index < 0)
            index = 0;

        if (index >= profiles.Count)
            index = profiles.Count - 1;

        selectedProfileIndex = index;
    }

    private void CreateNewProfile()
    {
        var name = $"Player {profiles.Count + 1}";
        var profile = new PlayerProfile(name);
        profiles.Add(profile);
        selectedProfileIndex = profiles.Count - 1;
    }

    private void ApplyDisplayMode()
    {
        graphics.IsFullScreen = settings.IsFullScreen;
        graphics.ApplyChanges();
    }

    private void DrawProfileSelection()
    {
        var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
        var height = GraphicsDevice.PresentationParameters.BackBufferHeight;

        var title = "SELECT PROFILE";
        var titleSize = uiFont.MeasureString(title);
        var titlePosition = new Vector2(width / 2f - titleSize.X / 2f, 40f);
        spriteBatch.DrawString(uiFont, title, titlePosition, Color.White);

        var panelWidth = width / 2 - 60;
        var panelHeight = height - 160;
        var leftPanelRect = new Rectangle(40, 100, panelWidth, panelHeight);
        var rightPanelRect = new Rectangle(width - panelWidth - 40, 100, panelWidth, panelHeight);

        DrawPanel(leftPanelRect, Color.DimGray, Color.DarkSlateGray);
        DrawPanel(rightPanelRect, Color.DimGray, Color.DarkSlateGray);

        DrawProfilesList(leftPanelRect);
        DrawSettingsPanel(rightPanelRect);

        var hint = "ENTER - select   N - new profile   ESC - exit";
        var hintSize = uiFont.MeasureString(hint);
        var hintPosition = new Vector2(width / 2f - hintSize.X / 2f, height - hintSize.Y - 20f);
        spriteBatch.DrawString(uiFont, hint, hintPosition, Color.LightGray);
    }

    private void DrawProfilesList(Rectangle panelRect)
    {
        var itemHeight = 48;
        var spacing = 8;
        var maxVisible = (panelRect.Height - 40) / (itemHeight + spacing);

        var startY = panelRect.Y + 30;
        var startX = panelRect.X + 20;

        for (var i = 0; i < profiles.Count && i < maxVisible; i++)
        {
            var profile = profiles[i];
            var y = startY + i * (itemHeight + spacing);
            var rect = new Rectangle(startX, y, panelRect.Width - 40, itemHeight);

            var isSelected = i == selectedProfileIndex;
            var outerColor = isSelected ? Color.Gold : Color.DimGray;
            var innerColor = isSelected ? Color.DarkOliveGreen : Color.DarkSlateGray;

            DrawPanel(rect, outerColor, innerColor);

            var text = profile.Name;
            var textSize = uiFont.MeasureString(text);
            var textPosition = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2f,
                rect.Y + (rect.Height - textSize.Y) / 2f);

            var textColor = isSelected ? Color.White : Color.LightGray;
            spriteBatch.DrawString(uiFont, text, textPosition, textColor);
        }
    }

    private void DrawSettingsPanel(Rectangle panelRect)
    {
        var x = panelRect.X + 20;
        var y = panelRect.Y + 40;
        var lineHeight = 60;

        DrawSlider(x, y, panelRect.Width - 40, "MUSIC", settings.MusicVolume);
        DrawSlider(x, y + lineHeight, panelRect.Width - 40, "SFX", settings.EffectsVolume);

        var fullScreenText = settings.IsFullScreen ? "ON" : "OFF";
        var label = $"FULLSCREEN: {fullScreenText}";
        var size = uiFont.MeasureString(label);
        var position = new Vector2(x, y + 2 * lineHeight);
        spriteBatch.DrawString(uiFont, label, position, Color.White);

        var hint = "A/D - music   J/L - sfx   F/F11 - fullscreen";
        var hintSize = uiFont.MeasureString(hint);
        var hintPosition = new Vector2(
            panelRect.X + (panelRect.Width - hintSize.X) / 2f,
            panelRect.Bottom - hintSize.Y - 20f);

        spriteBatch.DrawString(uiFont, hint, hintPosition, Color.LightGray);
    }

    private void DrawSlider(int x, int y, int width, string label, int value)
    {
        const int maxValue = 10;
        var barHeight = 20;
        var labelSize = uiFont.MeasureString(label);

        spriteBatch.DrawString(uiFont, label, new Vector2(x, y - labelSize.Y - 4), Color.White);

        var barBackground = new Rectangle(x, y, width, barHeight);
        DrawRectangle(barBackground, Color.DimGray);

        var filledWidth = width * value / maxValue;
        var barFilled = new Rectangle(x, y, filledWidth, barHeight);
        DrawRectangle(barFilled, Color.DarkOliveGreen);

        var valueText = $"{value}/{maxValue}";
        var valueSize = uiFont.MeasureString(valueText);
        var valuePosition = new Vector2(
            x + width - valueSize.X,
            y - valueSize.Y - 4);

        spriteBatch.DrawString(uiFont, valueText, valuePosition, Color.LightGray);
    }

    private void DrawLevelSelection()
    {
        var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
        var height = GraphicsDevice.PresentationParameters.BackBufferHeight;

        var title = currentProfile != null ? $"LEVELS - {currentProfile.Name}" : "LEVELS";
        var titleSize = uiFont.MeasureString(title);
        var titlePosition = new Vector2(width / 2f - titleSize.X / 2f, 20f);
        spriteBatch.DrawString(uiFont, title, titlePosition, Color.White);

        if (levelInfos.Count == 0)
            return;

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

            var outerColor = isSelected ? Color.Gold : Color.DimGray;
            var innerColor = isSelected ? Color.DarkOliveGreen : Color.DarkSlateGray;

            DrawPanel(rect, outerColor, innerColor);

            var levelName = levelInfos[i].Name;
            var filteredName = FilterToAscii(levelName);
            var textSize = uiFont.MeasureString(filteredName);
            var textPosition = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2f,
                rect.Y + 10f);
            spriteBatch.DrawString(uiFont, filteredName, textPosition, Color.White);

            var placeholderStars = "***";
            var starsSize = uiFont.MeasureString(placeholderStars);
            var starsPosition = new Vector2(
                rect.X + (rect.Width - starsSize.X) / 2f,
                rect.Bottom - starsSize.Y - 10f);
            spriteBatch.DrawString(uiFont, placeholderStars, starsPosition, Color.LightGray);
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

        var hint = "ARROWS - move   ENTER - start   BACK - profiles";
        var hintSize = uiFont.MeasureString(hint);
        var hintPosition = new Vector2(
            width / 2f - hintSize.X / 2f,
            height - hintSize.Y - 40f);
        spriteBatch.DrawString(uiFont, hint, hintPosition, Color.LightGray);
    }

    private void DrawLevel()
    {
        var screenWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        var screenHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;

        var levelWidth = level.Width * TileSize;
        var levelHeight = level.Height * TileSize;

        var offsetX = (screenWidth - levelWidth) / 2;
        var offsetY = (screenHeight - levelHeight) / 2;

        if (offsetX < 0)
            offsetX = 0;
        if (offsetY < 0)
            offsetY = 0;

        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var position = new Position(x, y);
                var rectangle = new Rectangle(
                    offsetX + x * TileSize,
                    offsetY + y * TileSize,
                    TileSize,
                    TileSize);

                spriteBatch.Draw(floorTexture, rectangle, Color.White);

                var cell = level.GetCell(position);

                if (cell.Type == CellType.Wall)
                {
                    spriteBatch.Draw(wallTexture, rectangle, Color.White);
                }
                else if (level.IsTarget(position))
                {
                    spriteBatch.Draw(targetTexture, rectangle, Color.White);
                }

                if (level.HasBoxOnTarget(position))
                {
                    spriteBatch.Draw(crateTexture, rectangle, Color.White);
                }
                else if (level.HasBox(position))
                {
                    spriteBatch.Draw(crateTexture, rectangle, Color.White);
                }

                if (position.X == level.PlayerPosition.X && position.Y == level.PlayerPosition.Y)
                {
                    spriteBatch.Draw(playerTexture, rectangle, Color.White);
                }
            }
        }
    }

    private int GetLevelGridColumns()
    {
        var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
        const int cardWidth = 220;
        const int spacingX = 16;
        const int marginX = 40;

        var availableWidth = Math.Max(1, width - marginX * 2);
        var columns = availableWidth / (cardWidth + spacingX);
        if (columns < 1)
            columns = 1;

        return columns;
    }

    private bool IsKeyPressed(Keys key, KeyboardState currentKeyboardState)
    {
        return currentKeyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
    }

    private void DrawRectangle(Rectangle rectangle, Color color)
    {
        spriteBatch.Draw(whiteTexture, rectangle, color);
    }

    private void DrawPanel(Rectangle rect, Color borderColor, Color fillColor)
    {
        DrawRectangle(rect, borderColor);
        var innerRect = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6);
        DrawRectangle(innerRect, fillColor);
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
}