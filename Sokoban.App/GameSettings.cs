namespace Sokoban.App;

public sealed class GameSettings
{
    private const int MinVolume = 0;
    private const int MaxVolume = 10;

    public GameSettings()
    {
    }

    public GameSettings(int musicVolume, int effectsVolume, bool isFullScreen)
    {
        MusicVolume = Clamp(musicVolume);
        EffectsVolume = Clamp(effectsVolume);
        IsFullScreen = isFullScreen;
    }

    public int MusicVolume { get; set; } = 5;
    public int EffectsVolume { get; set; } = 5;
    public bool IsFullScreen { get; set; }

    public void ChangeMusicVolume(int delta)
    {
        MusicVolume = Clamp(MusicVolume + delta);
    }

    public void ChangeEffectsVolume(int delta)
    {
        EffectsVolume = Clamp(EffectsVolume + delta);
    }

    public void ToggleFullScreen()
    {
        IsFullScreen = !IsFullScreen;
    }

    private static int Clamp(int value)
    {
        if (value < MinVolume)
            return MinVolume;
        if (value > MaxVolume)
            return MaxVolume;
        return value;
    }
}