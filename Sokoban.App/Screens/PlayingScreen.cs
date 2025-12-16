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

        if (IsKeyPressed(Keys.Escape, current, previous))
            return new ScreenCommand(ScreenCommandType.GoToLevelSelection);

        if (IsKeyPressed(Keys.R, current, previous))
            return new ScreenCommand(ScreenCommandType.RestartLevel);

        var direction = GetPressedDirection(current, previous);
        if (direction.HasValue)
            TryMovement(direction.Value);

        if (level.IsCompleted())
        {
            var id = currentLevelId ?? string.Empty;
            return new ScreenCommand(
                ScreenCommandType.GoToLevelSelection,
                new LevelResult(id, elapsedMilliseconds, steps));
        }

        return ScreenCommand.None;
    }

    private void TryMovement(Direction direction)
    {
        if (level == null)
            return;

        var result = level.TryMove(direction);
        if (result != MoveResult.None)
            steps++;
    }

    private static Direction? GetPressedDirection(KeyboardState current, KeyboardState previous)
    {
        if (IsUpPressed(current, previous))
            return Direction.Up;
        if (IsDownPressed(current, previous))
            return Direction.Down;
        if (IsLeftPressed(current, previous))
            return Direction.Left;
        if (IsRightPressed(current, previous))
            return Direction.Right;

        return null;
    }
    
    private Vector2 ComputeCameraOffset(int screenWidth, int screenHeight)
    {
        if (level == null)
            return Vector2.Zero;

        var worldWidth = level.Width * TileSize;
        var worldHeight = level.Height * TileSize;

        var player = level.PlayerPosition;
        var playerX = player.X * TileSize + TileSize / 2f;
        var playerY = player.Y * TileSize + TileSize / 2f;

        float halfScreenX = screenWidth / 2f;
        float halfScreenY = screenHeight / 2f;

        float camX;
        float camY;

        if (worldWidth <= screenWidth)
            camX = worldWidth / 2f;
        else
            camX = MathHelper.Clamp(playerX, halfScreenX, worldWidth - halfScreenX);

        if (worldHeight <= screenHeight)
            camY = worldHeight / 2f;
        else
            camY = MathHelper.Clamp(playerY, halfScreenY, worldHeight - halfScreenY);

        return new Vector2(halfScreenX - camX, halfScreenY - camY);
    }


    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (level == null)
            return;

        var screenWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
        var screenHeight = graphicsDevice.PresentationParameters.BackBufferHeight;

        var cameraOffset = ComputeCameraOffset(screenWidth, screenHeight);

        DrawLevel(spriteBatch, cameraOffset);
        DrawHud(spriteBatch, screenWidth, screenHeight);
    }

    private void DrawLevel(SpriteBatch spriteBatch, Vector2 camera)
    {
        for (int y = 0; y < level!.Height; y++)
        {
            for (int x = 0; x < level.Width; x++)
            {
                var pos = new Position(x, y);
                var rect = new Rectangle(
                    (int)(x * TileSize + camera.X),
                    (int)(y * TileSize + camera.Y),
                    TileSize,
                    TileSize);

                spriteBatch.Draw(floorTexture, rect, Color.White);

                var cell = level.GetCell(pos);

                if (cell.Type == CellType.Wall)
                    spriteBatch.Draw(wallTexture, rect, Color.White);

                if (level.IsTarget(pos))
                    spriteBatch.Draw(targetTexture, rect, Color.White);

                if (level.HasBox(pos))
                    spriteBatch.Draw(crateTexture, rect, Color.White);

                if (pos.X == level.PlayerPosition.X && pos.Y == level.PlayerPosition.Y)
                    spriteBatch.Draw(playerTexture, rect, Color.White);
            }
        }
    }

    private void DrawHud(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
    {
        double seconds = elapsedMilliseconds / 1000.0;
        var timeText = $"TIME: {seconds:0.0}s";
        var stepsText = $"STEPS: {steps}";

        spriteBatch.DrawString(uiFont, timeText, new Vector2(20, 20), Color.LightGray);
        spriteBatch.DrawString(uiFont, stepsText, new Vector2(20, 60), Color.LightGray);

        UiTextUtils.DrawHint(
            spriteBatch,
            uiFont,
            "WASD/ARROWS - move   R - restart   ESC - levels",
            screenWidth,
            screenHeight);
    }

    private static bool IsKeyPressed(Keys key, KeyboardState current, KeyboardState previous)
        => current.IsKeyDown(key) && !previous.IsKeyDown(key);

    private static bool IsUpPressed(KeyboardState c, KeyboardState p)
        => IsKeyPressed(Keys.Up, c, p) || IsKeyPressed(Keys.W, c, p);

    private static bool IsDownPressed(KeyboardState c, KeyboardState p)
        => IsKeyPressed(Keys.Down, c, p) || IsKeyPressed(Keys.S, c, p);

    private static bool IsLeftPressed(KeyboardState c, KeyboardState p)
        => IsKeyPressed(Keys.Left, c, p) || IsKeyPressed(Keys.A, c, p);

    private static bool IsRightPressed(KeyboardState c, KeyboardState p)
        => IsKeyPressed(Keys.Right, c, p) || IsKeyPressed(Keys.D, c, p);

    private static bool IsActionPressed(KeyboardState c, KeyboardState p, Keys k1, Keys k2)
        => IsKeyPressed(k1, c, p) || IsKeyPressed(k2, c, p);
}
