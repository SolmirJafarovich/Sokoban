namespace Sokoban.App.Screens;

public enum ScreenCommandType
{
    None,
    ExitGame,
    GoToProfileSelection,
    GoToLevelSelection,
    GoToPlaying,
    GoToLeaderboard,
    GoToGlobalLeaderboard
}

public sealed class LevelResult
{
    public LevelResult(string levelId, int timeMs, int steps)
    {
        LevelId = levelId;
        TimeMs = timeMs;
        Steps = steps;
    }

    public string LevelId { get; }
    public int TimeMs { get; }
    public int Steps { get; }
}

public readonly struct ScreenCommand
{
    public static readonly ScreenCommand None = new(ScreenCommandType.None, null);

    public ScreenCommandType Type { get; }
    public LevelResult? Result { get; }

    public ScreenCommand(ScreenCommandType type, LevelResult? result)
    {
        Type = type;
        Result = result;
    }

    public ScreenCommand(ScreenCommandType type)
        : this(type, null)
    {
    }
}