using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Sokoban.App.Screens;

public sealed class ProfileSelectionScreen : IGameScreen
{
    private const int MaxProfiles = 5;
    private const int SettingsItemCount = 3;

    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont uiFont;
    private readonly Texture2D whiteTexture;
    private readonly ProfileRepository profileRepository;
    private readonly List<PlayerProfile> profiles;

    private int selectedProfileIndex;
    private int selectedSettingsIndex;
    private bool isEditingProfileName;
    private string profileNameBuffer = string.Empty;

    public ProfileSelectionScreen(
        GraphicsDevice graphicsDevice,
        SpriteFont uiFont,
        Texture2D whiteTexture,
        ProfileRepository profileRepository,
        List<PlayerProfile> profiles)
    {
        this.graphicsDevice = graphicsDevice;
        this.uiFont = uiFont;
        this.whiteTexture = whiteTexture;
        this.profileRepository = profileRepository;
        this.profiles = profiles;

        if (profiles.Count > 0)
            selectedProfileIndex = 0;
    }

    public PlayerProfile? CurrentProfile
    {
        get
        {
            if (profiles.Count == 0)
                return null;

            if (selectedProfileIndex < 0 || selectedProfileIndex >= profiles.Count)
                return null;

            return profiles[selectedProfileIndex];
        }
    }

    public IReadOnlyList<PlayerProfile> Profiles => profiles;

    public ScreenCommand Update(GameTime gameTime, KeyboardState current, KeyboardState previous)
    {
        if (isEditingProfileName)
        {
            UpdateProfileNameEdit(current, previous);
            return ScreenCommand.None;
        }

        if (IsKeyPressed(Keys.Escape, current, previous))
            return new ScreenCommand(ScreenCommandType.ExitGame);

        if (profiles.Count == 0)
            return ScreenCommand.None;

        if (IsKeyPressed(Keys.Up, current, previous))
            ChangeSelectedProfile(-1);

        if (IsKeyPressed(Keys.Down, current, previous))
            ChangeSelectedProfile(1);

        if (IsKeyPressed(Keys.W, current, previous))
            ChangeSelectedSettingsIndex(-1);

        if (IsKeyPressed(Keys.S, current, previous))
            ChangeSelectedSettingsIndex(1);

        if (IsKeyPressed(Keys.A, current, previous) || IsKeyPressed(Keys.Left, current, previous))
            AdjustSelectedSetting(-1);

        if (IsKeyPressed(Keys.D, current, previous) || IsKeyPressed(Keys.Right, current, previous))
            AdjustSelectedSetting(1);

        if (IsKeyPressed(Keys.F11, current, previous) || IsKeyPressed(Keys.F, current, previous))
            ToggleFullScreenForCurrentProfile();

        if (IsKeyPressed(Keys.N, current, previous))
            CreateNewProfile();

        if (IsKeyPressed(Keys.R, current, previous))
            BeginProfileNameEditForCurrentProfile();

        if (IsKeyPressed(Keys.Enter, current, previous))
            return new ScreenCommand(ScreenCommandType.GoToLevelSelection);
        
        if (IsKeyPressed(Keys.L, current, previous))
            return new ScreenCommand(ScreenCommandType.GoToGlobalLeaderboard);

        
        return ScreenCommand.None;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var width = graphicsDevice.PresentationParameters.BackBufferWidth;
        var height = graphicsDevice.PresentationParameters.BackBufferHeight;

        var title = "SELECT PROFILE";
        var titleSize = uiFont.MeasureString(title);
        var titlePosition = new Vector2(width / 2f - titleSize.X / 2f, 40f);
        spriteBatch.DrawString(uiFont, title, titlePosition, Color.White);

        var panelWidth = width / 2 - 60;
        var panelHeight = height - 160;
        var leftPanelRect = new Rectangle(40, 100, panelWidth, panelHeight);
        var rightPanelRect = new Rectangle(width - panelWidth - 40, 100, panelWidth, panelHeight);

        DrawPanel(spriteBatch, leftPanelRect, Color.DimGray, Color.DarkSlateGray);
        DrawPanel(spriteBatch, rightPanelRect, Color.DimGray, Color.DarkSlateGray);

        DrawProfilesList(spriteBatch, leftPanelRect);
        DrawSettingsPanel(spriteBatch, rightPanelRect);

        var hint = isEditingProfileName
            ? "ENTER-save  ESC-cancel  BACKSPACE-delete"
            : "ENTER-select  N-new  R-rename L-Leaderboard  ESC-exit";
        var hintY = height - uiFont.LineSpacing - 20f;
        DrawCenteredScaledText(spriteBatch, hint, hintY, Color.LightGray);
    }

    private void ChangeSelectedProfile(int delta)
    {
        if (profiles.Count == 0)
            return;

        var index = selectedProfileIndex + delta;

        if (index < 0)
            index = 0;

        if (index >= profiles.Count)
            index = profiles.Count - 1;

        selectedProfileIndex = index;
    }

    private void ChangeSelectedSettingsIndex(int delta)
    {
        selectedSettingsIndex += delta;

        if (selectedSettingsIndex < 0)
            selectedSettingsIndex = 0;

        if (selectedSettingsIndex >= SettingsItemCount)
            selectedSettingsIndex = SettingsItemCount - 1;
    }

    private void AdjustSelectedSetting(int delta)
    {
        var profile = CurrentProfile;
        if (profile == null)
            return;

        var settings = profile.Settings;

        if (selectedSettingsIndex == 0)
            settings.ChangeMusicVolume(delta);
        else if (selectedSettingsIndex == 1)
            settings.ChangeEffectsVolume(delta);
        else if (selectedSettingsIndex == 2)
            settings.ToggleFullScreen();

        SaveProfiles();
    }

    private void ToggleFullScreenForCurrentProfile()
    {
        var profile = CurrentProfile;
        if (profile == null)
            return;

        profile.Settings.ToggleFullScreen();
        SaveProfiles();
    }

    private void CreateNewProfile()
    {
        if (profiles.Count >= MaxProfiles)
            return;

        var baseName = $"Player {profiles.Count + 1}";
        var settings = new GameSettings();

        var profile = new PlayerProfile(baseName, settings);
        profiles.Add(profile);
        selectedProfileIndex = profiles.Count - 1;

        BeginProfileNameEdit(baseName);
        SaveProfiles();
    }

    private void BeginProfileNameEditForCurrentProfile()
    {
        var profile = CurrentProfile;
        if (profile == null)
            return;

        BeginProfileNameEdit(profile.Name);
    }

    private void BeginProfileNameEdit(string initialName)
    {
        isEditingProfileName = true;
        profileNameBuffer = initialName ?? string.Empty;
    }

    private void EndProfileNameEdit(bool apply)
    {
        if (apply && CurrentProfile != null)
        {
            CurrentProfile.Rename(profileNameBuffer);
            SaveProfiles();
        }

        isEditingProfileName = false;
    }

    private void UpdateProfileNameEdit(KeyboardState current, KeyboardState previous)
    {
        if (IsKeyPressed(Keys.Enter, current, previous))
        {
            EndProfileNameEdit(true);
            return;
        }

        if (IsKeyPressed(Keys.Escape, current, previous))
        {
            EndProfileNameEdit(false);
            return;
        }

        if (IsKeyPressed(Keys.Back, current, previous) && profileNameBuffer.Length > 0)
        {
            profileNameBuffer = profileNameBuffer[..^1];
            return;
        }

        AppendLetterIfPressed(current, previous);
        AppendDigitIfPressed(current, previous);

        if (IsKeyPressed(Keys.Space, current, previous))
            AppendCharToProfileName(' ');
    }

    private void AppendLetterIfPressed(KeyboardState current, KeyboardState previous)
    {
        for (var key = Keys.A; key <= Keys.Z; key++)
        {
            if (IsKeyPressed(key, current, previous))
            {
                var offset = key - Keys.A;
                var c = (char)('A' + offset);
                AppendCharToProfileName(c);
                break;
            }
        }
    }

    private void AppendDigitIfPressed(KeyboardState current, KeyboardState previous)
    {
        for (var key = Keys.D0; key <= Keys.D9; key++)
        {
            if (IsKeyPressed(key, current, previous))
            {
                var offset = key - Keys.D0;
                var c = (char)('0' + offset);
                AppendCharToProfileName(c);
                break;
            }
        }
    }

    private void AppendCharToProfileName(char c)
    {
        const int maxLength = 16;

        if (profileNameBuffer.Length >= maxLength)
            return;

        if (c >= ' ' && c <= '~')
            profileNameBuffer += c;
    }

    private void DrawProfilesList(SpriteBatch spriteBatch, Rectangle panelRect)
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

            DrawPanel(spriteBatch, rect, outerColor, innerColor);

            var text = profile.Name;
            if (isEditingProfileName && i == selectedProfileIndex)
                text = profileNameBuffer + "_";

            var textSize = uiFont.MeasureString(text);
            var textPosition = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2f,
                rect.Y + (rect.Height - textSize.Y) / 2f);

            var textColor = isSelected ? Color.White : Color.LightGray;
            spriteBatch.DrawString(uiFont, text, textPosition, textColor);
        }
    }

    private void DrawSettingsPanel(SpriteBatch spriteBatch, Rectangle panelRect)
    {
        var profile = CurrentProfile;
        if (profile == null)
            return;

        var settings = profile.Settings;

        var x = panelRect.X + 20;
        var y = panelRect.Y + 40;
        var lineHeight = 60;

        DrawSliderWithSelection(spriteBatch, x, y, panelRect.Width - 40, "MUSIC", settings.MusicVolume, selectedSettingsIndex == 0);
        DrawSliderWithSelection(spriteBatch, x, y + lineHeight, panelRect.Width - 40, "SFX", settings.EffectsVolume, selectedSettingsIndex == 1);

        var fullScreenText = settings.IsFullScreen ? "ON" : "OFF";
        var label = $"FULLSCREEN: {fullScreenText}";
        var size = uiFont.MeasureString(label);
        var position = new Vector2(x, y + 2 * lineHeight);
        var color = selectedSettingsIndex == 2 ? Color.Gold : Color.White;
        spriteBatch.DrawString(uiFont, label, position, color);

        var hint = "W/S-select setting  A/D or arrows-change  F11-fullscreen";
        var hintY = panelRect.Bottom - uiFont.LineSpacing - 20f;
        DrawCenteredScaledText(spriteBatch, hint, hintY, Color.LightGray);
    }

    private void DrawSliderWithSelection(SpriteBatch spriteBatch, int x, int y, int width, string label, int value, bool selected)
    {
        const int maxValue = 10;
        var barHeight = 20;
        var labelColor = selected ? Color.Gold : Color.White;

        var labelPosition = new Vector2(x, y - uiFont.LineSpacing - 4);
        spriteBatch.DrawString(uiFont, label, labelPosition, labelColor);

        var barBackground = new Rectangle(x, y, width, barHeight);
        DrawRectangle(spriteBatch, barBackground, Color.DimGray);

        var filledWidth = width * value / maxValue;
        var barFilled = new Rectangle(x, y, filledWidth, barHeight);
        DrawRectangle(spriteBatch, barFilled, Color.DarkOliveGreen);

        var valueText = $"{value}/{maxValue}";
        var valueSize = uiFont.MeasureString(valueText);
        var valuePosition = new Vector2(x + width - valueSize.X, y - valueSize.Y - 4);
        spriteBatch.DrawString(uiFont, valueText, valuePosition, Color.LightGray);
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

    private void SaveProfiles()
    {
        profileRepository.Save(profiles);
    }

    private static bool IsKeyPressed(Keys key, KeyboardState current, KeyboardState previous)
    {
        return current.IsKeyDown(key) && !previous.IsKeyDown(key);
    }
}
