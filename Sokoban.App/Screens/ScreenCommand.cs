namespace Sokoban.App.Screens;

public enum ScreenCommandType
{
    None,
    ExitGame,
    GoToProfileSelection,
    GoToLevelSelection,
    GoToPlaying
}

public readonly struct ScreenCommand
{
    public static readonly ScreenCommand None = new(ScreenCommandType.None, null);

    public ScreenCommandType Type { get; }
    public string? LevelId { get; }

    public ScreenCommand(ScreenCommandType type, string? levelId)
    {
        Type = type;
        LevelId = levelId;
    }

    public ScreenCommand(ScreenCommandType type)
        : this(type, null)
    {
    }
}