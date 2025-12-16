using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Sokoban.App;

namespace Sokoban.App.Screens;

public sealed class ProfileSelectionScreen : IGameScreen
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont uiFont;
    private readonly Texture2D whiteTexture;
    private readonly IList<PlayerProfile> profiles;

    private int selectedIndex;
    private bool isEditingName;
    private string editingNameBuffer = string.Empty;

    public ProfileSelectionScreen(
        GraphicsDevice graphicsDevice,
        SpriteFont uiFont,
        Texture2D whiteTexture,
        IList<PlayerProfile> profiles)
    {
        this.graphicsDevice = graphicsDevice;
        this.uiFont = uiFont;
        this.whiteTexture = whiteTexture;
        this.profiles = profiles;
    }

    public PlayerProfile? CurrentProfile =>
        profiles.Count == 0 || selectedIndex < 0 || selectedIndex >= profiles.Count
            ? null
            : profiles[selectedIndex];

    public ScreenCommand Update(GameTime gameTime, KeyboardState current, KeyboardState previous)
    {
        if (isEditingName)
        {
            HandleNameEditing(current, previous);
            return ScreenCommand.None;
        }

        if (IsUpPressed(current, previous))
            selectedIndex--;

        if (IsDownPressed(current, previous))
            selectedIndex++;

        if (profiles.Count == 0)
            selectedIndex = -1;
        else
        {
            if (selectedIndex < 0)
                selectedIndex = 0;
            if (selectedIndex >= profiles.Count)
                selectedIndex = profiles.Count - 1;
        }

        if (IsActionPressed(current, previous, Keys.Enter, Keys.Space))
        {
            if (profiles.Count > 0 && selectedIndex >= 0)
                return new ScreenCommand(ScreenCommandType.GoToLevelSelection);
        }

        if (IsActionPressed(current, previous, Keys.N, Keys.Insert))
            CreateProfile();

        if (IsActionPressed(current, previous, Keys.Delete, Keys.X))
            DeleteProfile();

        if (IsActionPressed(current, previous, Keys.F2, Keys.R))
            StartRename();

        if (IsKeyPressed(Keys.Escape, current, previous))
            return new ScreenCommand(ScreenCommandType.GoToMainMenu);

        return ScreenCommand.None;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        var height = graphicsDevice.PresentationParameters.BackBufferHeight;

        const string title = "PROFILES";
        var titleSize = uiFont.MeasureString(title);
        var titlePos = new Vector2(width / 2f - titleSize.X / 2f, 20f);
        spriteBatch.DrawString(uiFont, title, titlePos, Color.White);

        var panelRect = new Rectangle(40, 80, width - 80, height - 160);
        DrawPanel(spriteBatch, panelRect, Color.DimGray, Color.DarkSlateGray);

        if (profiles.Count == 0)
        {
            const string noProfiles = "NO PROFILES. PRESS N TO CREATE.";
            var size = uiFont.MeasureString(noProfiles);
            var pos = new Vector2(
                panelRect.X + (panelRect.Width - size.X) / 2f,
                panelRect.Y + (panelRect.Height - size.Y) / 2f);
            spriteBatch.DrawString(uiFont, noProfiles, pos, Color.LightGray);
        }
        else
        {
            var startY = panelRect.Y + 40;
            var lineHeight = uiFont.LineSpacing + 10;

            for (var i = 0; i < profiles.Count; i++)
            {
                var profile = profiles[i];
                var isSelected = i == selectedIndex;

                var name = profile.Name;
                if (isSelected && isEditingName)
                    name = editingNameBuffer + "_";

                var text = $"{i + 1}. {name}";
                var size = uiFont.MeasureString(text);
                var pos = new Vector2(panelRect.X + 40, startY + i * lineHeight);

                if (isSelected)
                {
                    var rect = new Rectangle(
                        (int)(pos.X - 10),
                        (int)(pos.Y - 5),
                        (int)(panelRect.Width - 80),
                        (int)(size.Y + 10));
                    DrawRectangle(spriteBatch, rect, Color.DarkOliveGreen);
                }

                spriteBatch.DrawString(uiFont, text, pos, Color.White);
            }
        }

        var hint = isEditingName
            ? "Type name, BACKSPACE - delete, ENTER - confirm, ESC - cancel"
            : "N - new profile   X - delete   R - rename   ESC - menu";

        UiTextUtils.DrawHint(spriteBatch, uiFont, hint, width, height);
    }

    private void CreateProfile()
    {
        if (profiles.Count >= 5)
            return;

        var defaultName = $"Player {profiles.Count + 1}";
        var profile = new PlayerProfile(defaultName);
        profiles.Add(profile);
        selectedIndex = profiles.Count - 1;

        isEditingName = true;
        editingNameBuffer = defaultName;
    }

    private void DeleteProfile()
    {
        if (profiles.Count == 0 || selectedIndex < 0 || selectedIndex >= profiles.Count)
            return;

        profiles.RemoveAt(selectedIndex);

        if (profiles.Count == 0)
        {
            selectedIndex = -1;
            isEditingName = false;
            editingNameBuffer = string.Empty;
            return;
        }

        if (selectedIndex >= profiles.Count)
            selectedIndex = profiles.Count - 1;
    }

    private void StartRename()
    {
        if (profiles.Count == 0 || selectedIndex < 0 || selectedIndex >= profiles.Count)
            return;

        isEditingName = true;
        editingNameBuffer = profiles[selectedIndex].Name;
    }

    private void HandleNameEditing(KeyboardState current, KeyboardState previous)
    {
        if (!isEditingName || selectedIndex < 0 || selectedIndex >= profiles.Count)
            return;

        if (IsKeyPressed(Keys.Escape, current, previous))
        {
            isEditingName = false;
            editingNameBuffer = string.Empty;
            return;
        }

        if (IsKeyPressed(Keys.Back, current, previous))
        {
            if (editingNameBuffer.Length > 0)
                editingNameBuffer = editingNameBuffer[..^1];
            return;
        }

        if (IsActionPressed(current, previous, Keys.Enter, Keys.Space))
        {
            var profile = profiles[selectedIndex];
            profile.Rename(editingNameBuffer);
            isEditingName = false;
            return;
        }

        foreach (var key in current.GetPressedKeys())
        {
            if (!previous.IsKeyDown(key))
            {
                var ch = KeyToChar(key);
                if (ch.HasValue && editingNameBuffer.Length < 16)
                    editingNameBuffer += ch.Value;
            }
        }
    }

    private static char? KeyToChar(Keys key)
    {
        if (key >= Keys.A && key <= Keys.Z)
        {
            var offset = key - Keys.A;
            return (char)('A' + offset);
        }

        if (key >= Keys.D0 && key <= Keys.D9)
        {
            var offset = key - Keys.D0;
            return (char)('0' + offset);
        }

        if (key == Keys.Space)
            return ' ';

        return null;
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

    private static bool IsActionPressed(KeyboardState current, KeyboardState previous, Keys primary, Keys secondary)
    {
        return IsKeyPressed(primary, current, previous) || IsKeyPressed(secondary, current, previous);
    }
}
