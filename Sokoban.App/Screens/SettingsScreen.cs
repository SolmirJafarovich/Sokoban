using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Sokoban.App;

namespace Sokoban.App.Screens;

public sealed class SettingsScreen : IGameScreen
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont uiFont;
    private readonly Texture2D whiteTexture;
    private readonly GameSettings settings;

    private int selectedIndex;

    public SettingsScreen(
        GraphicsDevice graphicsDevice,
        SpriteFont uiFont,
        Texture2D whiteTexture,
        GameSettings settings)
    {
        this.graphicsDevice = graphicsDevice;
        this.uiFont = uiFont;
        this.whiteTexture = whiteTexture;
        this.settings = settings;
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

        if (IsLeftPressed(current, previous))
            ChangeValue(-5);

        if (IsRightPressed(current, previous))
            ChangeValue(5);

        if (IsActionPressed(current, previous, Keys.Enter, Keys.Space) && selectedIndex == 2)
            settings.IsFullScreen = !settings.IsFullScreen;

        if (IsActionPressed(current, previous, Keys.Escape, Keys.Q))
            return new ScreenCommand(ScreenCommandType.GoToMainMenu);

        return ScreenCommand.None;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        var height = graphicsDevice.PresentationParameters.BackBufferHeight;

        const string title = "SETTINGS";
        var titleSize = uiFont.MeasureString(title);
        var titlePos = new Vector2(width / 2f - titleSize.X / 2f, 20f);
        spriteBatch.DrawString(uiFont, title, titlePos, Color.White);

        var panelRect = new Rectangle(40, 80, width - 80, height - 160);
        DrawPanel(spriteBatch, panelRect, Color.DimGray, Color.DarkSlateGray);

        var startY = panelRect.Y + 40;

        DrawItem(spriteBatch, 0, "MUSIC VOLUME", $"{settings.MusicVolume}%", startY);
        DrawItem(spriteBatch, 1, "EFFECTS VOLUME", $"{settings.EffectsVolume}%", startY + 60);
        DrawItem(spriteBatch, 2, "FULLSCREEN", settings.IsFullScreen ? "ON" : "OFF", startY + 120);

        UiTextUtils.DrawHint(
            spriteBatch,
            uiFont,
            "LEFT/RIGHT - change Q - menu",
            width,
            height);
    }

    private void DrawItem(SpriteBatch spriteBatch, int index, string name, string value, int y)
    {
        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        var nameSize = uiFont.MeasureString(name);
        var valueSize = uiFont.MeasureString(value);

        var namePos = new Vector2(80, y);
        var valuePos = new Vector2(width - valueSize.X - 80, y);

        var isSelected = index == selectedIndex;

        if (isSelected)
        {
            var rect = new Rectangle(
                (int)(namePos.X - 10),
                (int)(namePos.Y - 5),
                (int)(width - 140),
                (int)(nameSize.Y + 10));
            DrawRectangle(spriteBatch, rect, Color.DarkOliveGreen);
        }

        spriteBatch.DrawString(uiFont, name, namePos, Color.White);
        spriteBatch.DrawString(uiFont, value, valuePos, Color.Gold);
    }

    private void ChangeValue(int delta)
    {
        if (selectedIndex == 0)
            settings.MusicVolume = Clamp(settings.MusicVolume + delta, 0, 100);
        else if (selectedIndex == 1)
            settings.EffectsVolume = Clamp(settings.EffectsVolume + delta, 0, 100);
        else if (selectedIndex == 2)
            settings.IsFullScreen = !settings.IsFullScreen;
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
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
