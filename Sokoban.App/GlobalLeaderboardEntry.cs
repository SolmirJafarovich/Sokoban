namespace Sokoban.App;

public sealed class GlobalLeaderboardEntry
{
    public GlobalLeaderboardEntry(string playerName, int completedLevels, int totalSteps, int totalTimeMs)
    {
        PlayerName = playerName;
        CompletedLevels = completedLevels;
        TotalSteps = totalSteps;
        TotalTimeMs = totalTimeMs;
    }

    public string PlayerName { get; }
    public int CompletedLevels { get; }
    public int TotalSteps { get; }
    public int TotalTimeMs { get; }
}