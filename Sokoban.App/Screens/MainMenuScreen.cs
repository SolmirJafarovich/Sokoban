using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Sokoban.App;

namespace Sokoban.App.Screens;

public sealed class MainMenuScreen : IGameScreen
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont uiFont;
    private readonly Texture2D whiteTexture;

    private int selectedIndex;

    public MainMenuScreen(GraphicsDevice graphicsDevice, SpriteFont uiFont, Texture2D whiteTexture)
    {
        this.graphicsDevice = graphicsDevice;
        this.uiFont = uiFont;
        this.whiteTexture = whiteTexture;
    }

    public ScreenCommand Update(GameTime gameTime, KeyboardState current, KeyboardState previous)
    {
        if (IsUpPressed(current, previous))
            selectedIndex--;

        if (IsDownPressed(current, previous))
            selectedIndex++;

        if (selectedIndex < 0)
            selectedIndex = 0;
        if (selectedIndex > 2)
            selectedIndex = 2;

        if (IsActionPressed(current, previous, Keys.Enter, Keys.Space))
        {
            return selectedIndex switch
            {
                0 => new ScreenCommand(ScreenCommandType.GoToProfileSelection),
                1 => new ScreenCommand(ScreenCommandType.GoToSettings),
                _ => new ScreenCommand(ScreenCommandType.ExitGame)
            };
        }

        if (IsActionPressed(current, previous, Keys.Escape, Keys.Q))
            return new ScreenCommand(ScreenCommandType.ExitGame);

        return ScreenCommand.None;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        var height = graphicsDevice.PresentationParameters.BackBufferHeight;

        const string title = "SOKOBAN";
        var titleSize = uiFont.MeasureString(title);
        var titlePos = new Vector2(width / 2f - titleSize.X / 2f, height / 4f - titleSize.Y / 2f);
        spriteBatch.DrawString(uiFont, title, titlePos, Color.White);

        var items = new[] { "PLAY", "SETTINGS", "EXIT" };
        var startY = height / 2f - items.Length * uiFont.LineSpacing / 2f;

        for (var i = 0; i < items.Length; i++)
        {
            var text = items[i];
            var size = uiFont.MeasureString(text);
            var position = new Vector2(width / 2f - size.X / 2f, startY + i * (uiFont.LineSpacing + 10));

            var isSelected = i == selectedIndex;
            var color = isSelected ? Color.Gold : Color.LightGray;

            if (isSelected)
            {
                var padding = 8;
                var rect = new Rectangle(
                    (int)(position.X - padding),
                    (int)(position.Y - padding / 2f),
                    (int)(size.X + padding * 2),
                    (int)(size.Y + padding));
                DrawRectangle(spriteBatch, rect, Color.DarkSlateGray);
            }

            spriteBatch.DrawString(uiFont, text, position, color);
        }

        UiTextUtils.DrawHint(
            spriteBatch,
            uiFont,
            "ENTER - confirm Q - exit",
            width,
            height);
    }

    private void DrawRectangle(SpriteBatch spriteBatch, Rectangle rectangle, Color color)
    {
        spriteBatch.Draw(whiteTexture, rectangle, color);
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

    private static bool IsActionPressed(KeyboardState current, KeyboardState previous, Keys primary, Keys secondary)
    {
        return IsKeyPressed(primary, current, previous) || IsKeyPressed(secondary, current, previous);
    }
}
