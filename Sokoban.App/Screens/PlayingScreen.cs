using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Sokoban.Core;

namespace Sokoban.App.Screens;

public sealed class PlayingScreen : IGameScreen
{
    private const int TileSize = 64;

    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont uiFont;
    private readonly Texture2D floorTexture;
    private readonly Texture2D wallTexture;
    private readonly Texture2D targetTexture;
    private readonly Texture2D crateTexture;
    private readonly Texture2D playerTexture;

    private Level? level;
    private string? currentLevelId;
    private int elapsedMilliseconds;
    private int steps;

    public PlayingScreen(
        GraphicsDevice graphicsDevice,
        SpriteFont uiFont,
        Texture2D floorTexture,
        Texture2D wallTexture,
        Texture2D targetTexture,
        Texture2D crateTexture,
        Texture2D playerTexture)
    {
        this.graphicsDevice = graphicsDevice;
        this.uiFont = uiFont;
        this.floorTexture = floorTexture;
        this.wallTexture = wallTexture;
        this.targetTexture = targetTexture;
        this.crateTexture = crateTexture;
        this.playerTexture = playerTexture;
    }

    public void SetLevel(Level newLevel, string levelId)
    {
        level = newLevel;
        currentLevelId = levelId;
        elapsedMilliseconds = 0;
        steps = 0;
    }

    public ScreenCommand Update(GameTime gameTime, KeyboardState current, KeyboardState previous)
    {
        if (level == null)
            return new ScreenCommand(ScreenCommandType.GoToLevelSelection);

        elapsedMilliseconds += (int)gameTime.ElapsedGameTime.TotalMilliseconds;

        if (IsActionPressed(current, previous, Keys.Escape, Keys.Q))
            return new ScreenCommand(ScreenCommandType.GoToLevelSelection);

        if (IsUpPressed(current, previous))
        {
            var result = level.TryMove(Direction.Up);
            if (result != MoveResult.None)
                steps++;
        }

        if (IsDownPressed(current, previous))
        {
            var result = level.TryMove(Direction.Down);
            if (result != MoveResult.None)
                steps++;
        }

        if (IsLeftPressed(current, previous))
        {
            var result = level.TryMove(Direction.Left);
            if (result != MoveResult.None)
                steps++;
        }

        if (IsRightPressed(current, previous))
        {
            var result = level.TryMove(Direction.Right);
            if (result != MoveResult.None)
                steps++;
        }

        if (level.IsCompleted())
        {
            var id = currentLevelId ?? string.Empty;
            var safeTime = elapsedMilliseconds > 0 ? elapsedMilliseconds : 1;
            var safeSteps = steps > 0 ? steps : 1;
            var result = new LevelResult(id, safeTime, safeSteps);
            return new ScreenCommand(ScreenCommandType.GoToLevelSelection, result);
        }

        return ScreenCommand.None;
    }



    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (level == null)
        {
            var width = graphicsDevice.PresentationParameters.BackBufferWidth;
            var height = graphicsDevice.PresentationParameters.BackBufferHeight;
            var text = "NO LEVEL";
            var size = uiFont.MeasureString(text);
            var pos = new Vector2(width / 2f - size.X / 2f, height / 2f - size.Y / 2f);
            spriteBatch.DrawString(uiFont, text, pos, Color.White);
            return;
        }

        var screenWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
        var screenHeight = graphicsDevice.PresentationParameters.BackBufferHeight;

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

        var seconds = elapsedMilliseconds / 1000.0;
        var timeText = $"TIME: {seconds:0.0}s";
        var stepsText = $"STEPS: {steps}";

        var timeSize = uiFont.MeasureString(timeText);
        var stepsSize = uiFont.MeasureString(stepsText);

        var timePos = new Vector2(20, 20);
        var stepsPos = new Vector2(20, 20 + timeSize.Y + 4);

        spriteBatch.DrawString(uiFont, timeText, timePos, Color.LightGray);
        spriteBatch.DrawString(uiFont, stepsText, stepsPos, Color.LightGray);

        var hint = "WASD/ARROWS-move  ESC/Q-levels";
        var hintSize = uiFont.MeasureString(hint);
        var hintPosition = new Vector2(
            screenWidth / 2f - hintSize.X / 2f,
            screenHeight - hintSize.Y - 20f);
        spriteBatch.DrawString(uiFont, hint, hintPosition, Color.LightGray);
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
