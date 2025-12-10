using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Sokoban.Core;

namespace Sokoban.App;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch = null!;
    private Texture2D whiteTexture = null!;
    private Level level = null!;
    private KeyboardState previousKeyboardState;
    private int tileSize = 48;

    private GameMode mode = GameMode.Menu;
    private IReadOnlyList<LevelInfo> levelInfos = Array.Empty<LevelInfo>();
    private int selectedLevelIndex;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        graphics.PreferredBackBufferWidth = 800;
        graphics.PreferredBackBufferHeight = 600;
    }

    protected override void Initialize()
    {
        levelInfos = LevelDirectory.LoadLevels(AppContext.BaseDirectory);
        selectedLevelIndex = 0;
        mode = GameMode.Menu;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        whiteTexture = new Texture2D(GraphicsDevice, 1, 1);
        var data = new[] { Color.White };
        whiteTexture.SetData(data);

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();

        if (keyboardState.IsKeyDown(Keys.Escape))
        {
            if (mode == GameMode.Playing)
                mode = GameMode.Menu;
            else
                Exit();
        }

        if (mode == GameMode.Menu)
            HandleMenuInput(keyboardState);
        else if (mode == GameMode.Playing)
            HandleGameInput(keyboardState);

        previousKeyboardState = keyboardState;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (mode == GameMode.Menu)
        {
            GraphicsDevice.Clear(Color.DarkSlateBlue);
            spriteBatch.Begin();
            DrawMenu();
            spriteBatch.End();
        }
        else
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin();
            DrawLevel();
            spriteBatch.End();
        }

        base.Draw(gameTime);
    }

    private void HandleMenuInput(KeyboardState keyboardState)
    {
        if (IsKeyPressed(Keys.Up, keyboardState) || IsKeyPressed(Keys.Left, keyboardState))
            ChangeSelectedLevel(-1);

        if (IsKeyPressed(Keys.Down, keyboardState) || IsKeyPressed(Keys.Right, keyboardState))
            ChangeSelectedLevel(1);

        if (IsKeyPressed(Keys.Enter, keyboardState))
            StartGameWithSelectedLevel();
    }

    private void HandleGameInput(KeyboardState keyboardState)
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
            ChangeSelectedLevel(1);
            mode = GameMode.Menu;
        }
    }

    private void StartGameWithSelectedLevel()
    {
        if (levelInfos.Count == 0)
            throw new InvalidOperationException("No levels loaded.");

        var info = levelInfos[selectedLevelIndex];
        var lines = File.ReadAllLines(info.FilePath);

        level = LevelLoader.LoadFromLines(lines);

        graphics.PreferredBackBufferWidth = level.Width * tileSize;
        graphics.PreferredBackBufferHeight = level.Height * tileSize;
        graphics.ApplyChanges();

        mode = GameMode.Playing;
    }

    private void ChangeSelectedLevel(int delta)
    {
        if (levelInfos.Count == 0)
            return;

        selectedLevelIndex += delta;

        if (selectedLevelIndex < 0)
            selectedLevelIndex = levelInfos.Count - 1;
        else if (selectedLevelIndex >= levelInfos.Count)
            selectedLevelIndex = 0;
    }

    private void DrawMenu()
    {
        var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
        var height = GraphicsDevice.PresentationParameters.BackBufferHeight;

        var itemWidth = 300;
        var itemHeight = 50;
        var spacing = 10;

        var totalHeight = levelInfos.Count * itemHeight + (levelInfos.Count - 1) * spacing;
        var startY = (height - totalHeight) / 2;
        var centerX = width / 2;

        for (var i = 0; i < levelInfos.Count; i++)
        {
            var x = centerX - itemWidth / 2;
            var y = startY + i * (itemHeight + spacing);
            var rect = new Rectangle(x, y, itemWidth, itemHeight);

            var isSelected = i == selectedLevelIndex;
            var color = isSelected ? Color.Gold : Color.DimGray;

            DrawTile(rect, color);

            var innerRect = new Rectangle(x + 4, y + 4, itemWidth - 8, itemHeight - 8);
            var innerColor = isSelected ? Color.DarkOliveGreen : Color.DarkSlateGray;
            DrawTile(innerRect, innerColor);
        }
    }

    private void DrawLevel()
    {
        for (var y = 0; y < level.Height; y++)
        {
            for (var x = 0; x < level.Width; x++)
            {
                var position = new Position(x, y);
                var cell = level.GetCell(position);
                var rectangle = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);

                var baseColor = GetBaseColor(cell.Type);
                DrawTile(rectangle, baseColor);

                if (level.HasBoxOnTarget(position))
                    DrawTile(rectangle, Color.Gold);
                else if (level.HasBox(position))
                    DrawTile(rectangle, Color.SaddleBrown);

                if (position.X == level.PlayerPosition.X && position.Y == level.PlayerPosition.Y)
                    DrawTile(rectangle, Color.CornflowerBlue);
            }
        }
    }

    private bool IsKeyPressed(Keys key, KeyboardState currentKeyboardState)
    {
        return currentKeyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
    }

    private void DrawTile(Rectangle rectangle, Color color)
    {
        spriteBatch.Draw(whiteTexture, rectangle, color);
    }

    private Color GetBaseColor(CellType cellType)
    {
        if (cellType == CellType.Wall)
            return Color.DarkSlateGray;

        if (cellType == CellType.Target)
            return Color.DarkOliveGreen;

        return Color.DimGray;
    }
}
