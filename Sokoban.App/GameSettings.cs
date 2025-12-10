namespace Sokoban.App;

public sealed class GameSettings
{
    private const int MinVolume = 0;
    private const int MaxVolume = 10;

    public int MusicVolume { get; private set; } = 5;
    public int EffectsVolume { get; private set; } = 5;
    public bool IsFullScreen { get; private set; }

    public void ChangeMusicVolume(int delta)
    {
        var value = MusicVolume + delta;
        if (value < MinVolume)
            value = MinVolume;
        if (value > MaxVolume)
            value = MaxVolume;

        MusicVolume = value;
    }

    public void ChangeEffectsVolume(int delta)
    {
        var value = EffectsVolume + delta;
        if (value < MinVolume)
            value = MinVolume;
        if (value > MaxVolume)
            value = MaxVolume;

        EffectsVolume = value;
    }

    public void ToggleFullScreen()
    {
        IsFullScreen = !IsFullScreen;
    }
}