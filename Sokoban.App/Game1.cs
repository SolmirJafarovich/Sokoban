﻿using System;
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

    private SettingsRepository settingsRepository = null!;
    private ProfileRepository profileRepository = null!;

    private GameSettings settings = null!;
    private List<PlayerProfile> profiles = new();
    private PlayerProfile? currentProfile;

    private IReadOnlyList<LevelInfo> levelInfos = Array.Empty<LevelInfo>();

    private AppController controller = null!;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        var baseDirectory = AppContext.BaseDirectory;

        settingsRepository = new SettingsRepository(baseDirectory);
        profileRepository = new ProfileRepository(baseDirectory);

        settings = settingsRepository.Load();

        var loadedProfiles = profileRepository.Load();
        profiles = new List<PlayerProfile>(loadedProfiles);

        if (profiles.Count == 0)
            profiles.Add(new PlayerProfile("Player 1"));

        if (profiles.Count > 5)
            profiles = profiles.GetRange(0, 5);

        currentProfile = profiles[0];

        levelInfos = LevelDirectory.LoadLevels(baseDirectory);

        ApplyDisplayMode();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);

        whiteTexture = new Texture2D(GraphicsDevice, 1, 1);
        whiteTexture.SetData(new[] { Color.White });

        floorTexture = Content.Load<Texture2D>("Tiles/ground");
        wallTexture = Content.Load<Texture2D>("Tiles/wall");
        targetTexture = Content.Load<Texture2D>("Tiles/target");
        crateTexture = Content.Load<Texture2D>("Tiles/crate");
        playerTexture = Content.Load<Texture2D>("Tiles/player");
        uiFont = Content.Load<SpriteFont>("Fonts/UiFont");

        var globalLeaderboardService = new GlobalLeaderboardService();

        var mainMenuScreen = new MainMenuScreen(GraphicsDevice, uiFont, whiteTexture);

        var settingsScreen = new SettingsScreen(GraphicsDevice, uiFont, whiteTexture, settings);

        var profileSelectionScreen = new ProfileSelectionScreen(
            GraphicsDevice,
            uiFont,
            whiteTexture,
            profiles);

        var levelSelectionScreen = new LevelSelectionScreen(
            GraphicsDevice,
            uiFont,
            whiteTexture,
            levelInfos);

        var playingScreen = new PlayingScreen(
            GraphicsDevice,
            uiFont,
            floorTexture,
            wallTexture,
            targetTexture,
            crateTexture,
            playerTexture);

        var leaderboardScreen = new LeaderboardScreen(
            GraphicsDevice,
            uiFont,
            whiteTexture,
            globalLeaderboardService);

        controller = new AppController(
            GraphicsDevice,
            uiFont,
            settingsRepository,
            profileRepository,
            settings,
            profiles,
            currentProfile,
            levelInfos,
            mainMenuScreen,
            settingsScreen,
            profileSelectionScreen,
            levelSelectionScreen,
            playingScreen,
            leaderboardScreen);

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var currentKeyboard = Keyboard.GetState();

        var exitRequested = controller.Update(gameTime, currentKeyboard, previousKeyboardState);

        previousKeyboardState = currentKeyboard;

        ApplyDisplayMode();

        if (exitRequested)
        {
            Exit();
            return;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        spriteBatch.Begin();
        controller.Draw(gameTime, spriteBatch);
        spriteBatch.End();

        base.Draw(gameTime);
    }


    private void ApplyDisplayMode()
    {
        graphics.IsFullScreen = settings.IsFullScreen;
        graphics.ApplyChanges();

        IsMouseVisible = !settings.IsFullScreen;
    }
}
