using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Sokoban.App.Screens;
using Sokoban.Core;

namespace Sokoban.App;

/// <summary>
/// Orchestrates screen navigation and high-level game flow.
/// Keeps Game1 thin: Game1 owns framework lifecycle and rendering;
/// AppController owns application state and navigation.
/// </summary>
public sealed class AppController
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteFont uiFont;

    private readonly SettingsRepository settingsRepository;
    private readonly ProfileRepository profileRepository;

    private readonly GameSettings settings;
    private readonly List<PlayerProfile> profiles;
    private PlayerProfile? currentProfile;

    private readonly IReadOnlyList<LevelInfo> levelInfos;

    private readonly MainMenuScreen mainMenuScreen;
    private readonly SettingsScreen settingsScreen;
    private readonly ProfileSelectionScreen profileSelectionScreen;
    private readonly LevelSelectionScreen levelSelectionScreen;
    private readonly PlayingScreen playingScreen;
    private readonly LeaderboardScreen leaderboardScreen;

    private LevelResultScreen? levelResultScreen;

    private IGameScreen currentScreen;

    public AppController(
        GraphicsDevice graphicsDevice,
        SpriteFont uiFont,
        SettingsRepository settingsRepository,
        ProfileRepository profileRepository,
        GameSettings settings,
        List<PlayerProfile> profiles,
        PlayerProfile? currentProfile,
        IReadOnlyList<LevelInfo> levelInfos,
        MainMenuScreen mainMenuScreen,
        SettingsScreen settingsScreen,
        ProfileSelectionScreen profileSelectionScreen,
        LevelSelectionScreen levelSelectionScreen,
        PlayingScreen playingScreen,
        LeaderboardScreen leaderboardScreen)
    {
        this.graphicsDevice = graphicsDevice;
        this.uiFont = uiFont;

        this.settingsRepository = settingsRepository;
        this.profileRepository = profileRepository;

        this.settings = settings;
        this.profiles = profiles;
        this.currentProfile = currentProfile;
        this.levelInfos = levelInfos;

        this.mainMenuScreen = mainMenuScreen;
        this.settingsScreen = settingsScreen;
        this.profileSelectionScreen = profileSelectionScreen;
        this.levelSelectionScreen = levelSelectionScreen;
        this.playingScreen = playingScreen;
        this.leaderboardScreen = leaderboardScreen;

        if (this.currentProfile != null)
            this.levelSelectionScreen.SetCurrentProfile(this.currentProfile);

        currentScreen = this.mainMenuScreen;
    }

    public bool Update(GameTime gameTime, KeyboardState currentKeyboard, KeyboardState previousKeyboard)
    {
        var command = currentScreen.Update(gameTime, currentKeyboard, previousKeyboard);
        return HandleScreenCommand(command);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        currentScreen.Draw(gameTime, spriteBatch);
    }

    private bool HandleScreenCommand(ScreenCommand command)
    {
        if (command.Type == ScreenCommandType.None)
            return false;

        if (command.Type == ScreenCommandType.ExitGame)
        {
            settingsRepository.Save(settings);
            profileRepository.Save(profiles);
            return true;
        }

        if (command.Type == ScreenCommandType.GoToMainMenu)
        {
            if (ReferenceEquals(currentScreen, settingsScreen))
                settingsRepository.Save(settings);

            if (ReferenceEquals(currentScreen, profileSelectionScreen))
                profileRepository.Save(profiles);

            currentScreen = mainMenuScreen;
            return false;
        }

        if (command.Type == ScreenCommandType.GoToSettings)
        {
            currentScreen = settingsScreen;
            return false;
        }

        if (command.Type == ScreenCommandType.GoToProfileSelection)
        {
            currentScreen = profileSelectionScreen;
            return false;
        }

        if (command.Type == ScreenCommandType.GoToLevelSelection)
        {
            // Special case: level just completed in PlayingScreen.
            if (ReferenceEquals(currentScreen, playingScreen) && command.Result != null)
            {
                HandleLevelCompleted(command.Result);
                return false;
            }

            // Legacy behavior: if a result is passed from another screen, store it.
            if (command.Result != null && currentProfile != null)
            {
                var result = command.Result;
                currentProfile.UpdateLevelStats(result.LevelId, result.TimeMs, result.Steps);
                profileRepository.Save(profiles);
            }

            if (ReferenceEquals(currentScreen, profileSelectionScreen))
                SyncCurrentProfileFromSelection();

            currentScreen = levelSelectionScreen;
            return false;
        }

        if (command.Type == ScreenCommandType.GoToPlaying)
        {
            StartSelectedLevel();
            currentScreen = playingScreen;
            return false;
        }

        if (command.Type == ScreenCommandType.GoToLeaderboard)
        {
            OpenLeaderboard();
            return false;
        }

        return false;
    }

    private void HandleLevelCompleted(LevelResult result)
    {
        if (currentProfile != null)
        {
            currentProfile.UpdateLevelStats(result.LevelId, result.TimeMs, result.Steps);
            profileRepository.Save(profiles);
        }

        int? bestProfileSteps = null;
        int? bestProfileMilliseconds = null;
        if (currentProfile != null && currentProfile.TryGetLevelStats(result.LevelId, out var profileStats))
        {
            bestProfileSteps = profileStats.BestSteps;
            bestProfileMilliseconds = profileStats.BestTimeMs;
        }

        string? bestGlobalPlayerName = null;
        int? bestGlobalSteps = null;
        int? bestGlobalMilliseconds = null;

        foreach (var profile in profiles)
        {
            if (!profile.TryGetLevelStats(result.LevelId, out var stats))
                continue;

            if (bestGlobalSteps == null)
            {
                bestGlobalPlayerName = profile.Name;
                bestGlobalSteps = stats.BestSteps;
                bestGlobalMilliseconds = stats.BestTimeMs;
                continue;
            }

            var isBetterSteps = stats.BestSteps < bestGlobalSteps.Value;
            var isSameStepsBetterTime = stats.BestSteps == bestGlobalSteps.Value &&
                                        stats.BestTimeMs < (bestGlobalMilliseconds ?? int.MaxValue);

            if (isBetterSteps || isSameStepsBetterTime)
            {
                bestGlobalPlayerName = profile.Name;
                bestGlobalSteps = stats.BestSteps;
                bestGlobalMilliseconds = stats.BestTimeMs;
            }
        }

        levelResultScreen = new LevelResultScreen(
            graphicsDevice,
            uiFont,
            result.LevelId,
            result.Steps,
            result.TimeMs,
            bestProfileSteps,
            bestProfileMilliseconds,
            bestGlobalPlayerName,
            bestGlobalSteps,
            bestGlobalMilliseconds);

        currentScreen = levelResultScreen;
    }

    private void SyncCurrentProfileFromSelection()
    {
        var selectedProfile = profileSelectionScreen.CurrentProfile;
        if (selectedProfile != null)
        {
            currentProfile = selectedProfile;
            levelSelectionScreen.SetCurrentProfile(currentProfile);
        }

        profileRepository.Save(profiles);
    }

    private void StartSelectedLevel()
    {
        if (levelInfos.Count == 0)
            throw new InvalidOperationException("No levels loaded.");

        var index = levelSelectionScreen.SelectedLevelIndex;
        if (index < 0 || index >= levelInfos.Count)
            index = 0;

        var info = levelInfos[index];
        var lines = File.ReadAllLines(info.FilePath);
        var level = LevelLoader.LoadFromLines(lines);
        var levelId = Path.GetFileName(info.FilePath);

        playingScreen.SetLevel(level, levelId);
    }

    private void OpenLeaderboard()
    {
        if (levelInfos.Count == 0)
            return;

        var selectedIndex = levelSelectionScreen.SelectedLevelIndex;
        if (selectedIndex < 0 || selectedIndex >= levelInfos.Count)
            selectedIndex = 0;

        leaderboardScreen.SetData(levelInfos, profiles, selectedIndex);
        currentScreen = leaderboardScreen;
    }
}
