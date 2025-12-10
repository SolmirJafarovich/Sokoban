using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Sokoban.App.Screens;
using Sokoban.Core;

namespace Sokoban.App;

public sealed class Game1 : Game
{
    private readonly GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch = null!;
    private Texture2D whiteTexture = null!;

    private Texture2D floorTexture = null!;
    private Texture2D wallTexture = null!;
    private Texture2D targetTexture = null!;
    private Texture2D crateTexture = null!;
    private Texture2D playerTexture = null!;
    private SpriteFont uiFont = null!;

    private KeyboardState previousKeyboardState;

    private ProfileRepository profileRepository = null!;
    private List<PlayerProfile> profiles = new();
    private PlayerProfile? currentProfile;

    private IReadOnlyList<LevelInfo> levelInfos = Array.Empty<LevelInfo>();

    private ProfileSelectionScreen profileSelectionScreen = null!;
    private LevelSelectionScreen levelSelectionScreen = null!;
    private PlayingScreen playingScreen = null!;
    private LeaderboardScreen leaderboardScreen = null!;
    private IGameScreen currentScreen = null!;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        profileRepository = new ProfileRepository(AppContext.BaseDirectory);

        var loadedProfiles = profileRepository.Load();
        profiles = new List<PlayerProfile>(loadedProfiles);

        if (profiles.Count == 0)
        {
            var settings = new GameSettings();
            profiles.Add(new PlayerProfile("Player 1", settings));
        }

        if (profiles.Count > 5)
            profiles = profiles.GetRange(0, 5);

        currentProfile = profiles[0];

        levelInfos = LevelDirectory.LoadLevels(AppContext.BaseDirectory);

        ApplyDisplayMode();

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

        profileSelectionScreen = new ProfileSelectionScreen(
            GraphicsDevice,
            uiFont,
            whiteTexture,
            profileRepository,
            profiles);

        levelSelectionScreen = new LevelSelectionScreen(
            GraphicsDevice,
            uiFont,
            whiteTexture,
            levelInfos);

        if (currentProfile != null)
            levelSelectionScreen.SetCurrentProfile(currentProfile);

        playingScreen = new PlayingScreen(
            GraphicsDevice,
            uiFont,
            floorTexture,
            wallTexture,
            targetTexture,
            crateTexture,
            playerTexture);

        leaderboardScreen = new LeaderboardScreen(
            GraphicsDevice,
            uiFont,
            whiteTexture);

        currentScreen = profileSelectionScreen;

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var currentKeyboard = Keyboard.GetState();

        var command = currentScreen.Update(gameTime, currentKeyboard, previousKeyboardState);
        HandleScreenCommand(command);

        previousKeyboardState = currentKeyboard;

        ApplyDisplayMode();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        spriteBatch.Begin();
        currentScreen.Draw(gameTime, spriteBatch);
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private void HandleScreenCommand(ScreenCommand command)
    {
        if (command.Type == ScreenCommandType.None)
            return;

        if (command.Type == ScreenCommandType.ExitGame)
        {
            Exit();
            return;
        }

        if (command.Type == ScreenCommandType.GoToProfileSelection)
        {
            currentScreen = profileSelectionScreen;
            SyncCurrentProfileFromSelection();
            levelSelectionScreen.SetCurrentProfile(currentProfile);
            return;
        }

        if (command.Type == ScreenCommandType.GoToLevelSelection)
        {
            if (command.Result != null && currentProfile != null)
            {
                var result = command.Result;
                currentProfile.UpdateLevelStats(result.LevelId, result.TimeMs, result.Steps);
                profileRepository.Save(profiles);
            }

            if (ReferenceEquals(currentScreen, profileSelectionScreen))
            {
                SyncCurrentProfileFromSelection();
                levelSelectionScreen.SetCurrentProfile(currentProfile);
            }

            currentScreen = levelSelectionScreen;
            return;
        }

        if (command.Type == ScreenCommandType.GoToPlaying)
        {
            StartSelectedLevel();
            currentScreen = playingScreen;
            return;
        }

        if (command.Type == ScreenCommandType.GoToLeaderboard)
        {
            OpenLeaderboardForSelectedLevel();
        }
    }

    private void SyncCurrentProfileFromSelection()
    {
        var selectedProfile = profileSelectionScreen.CurrentProfile;
        if (selectedProfile != null)
            currentProfile = selectedProfile;

        if (currentProfile != null)
            levelSelectionScreen.SetCurrentProfile(currentProfile);
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

    private void OpenLeaderboardForSelectedLevel()
    {
        if (levelInfos.Count == 0)
            return;

        var index = levelSelectionScreen.SelectedLevelIndex;
        if (index < 0 || index >= levelInfos.Count)
            index = 0;

        var info = levelInfos[index];
        var levelId = Path.GetFileName(info.FilePath);
        var levelName = info.Name;

        leaderboardScreen.SetLevel(levelId, levelName, profiles);
        currentScreen = leaderboardScreen;
    }

    private void ApplyDisplayMode()
    {
        var isFullScreen = currentProfile != null && currentProfile.Settings.IsFullScreen;
        graphics.IsFullScreen = isFullScreen;
        graphics.ApplyChanges();

        IsMouseVisible = !isFullScreen;
    }
}
