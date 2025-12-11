namespace Sokoban.App;

public sealed class GameSettings
{
    private const int MinVolume = 0;
    private const int MaxVolume = 100;

    public GameSettings(int musicVolume = 50, int effectsVolume = 50, bool isFullScreen = false)
    {
        MusicVolume = ClampVolume(musicVolume);
        EffectsVolume = ClampVolume(effectsVolume);
        IsFullScreen = isFullScreen;
    }

    public int MusicVolume { get; set; }

    public int EffectsVolume { get; set; }

    public bool IsFullScreen { get; set; }

    private static int ClampVolume(int value)
    {
        if (value < MinVolume)
            return MinVolume;
        if (value > MaxVolume)
            return MaxVolume;
        return value;
    }
}