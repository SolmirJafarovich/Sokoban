using System;
using System.Collections.Generic;

namespace Sokoban.App;

public sealed class PlayerProfile
{
    private const int MaxNameLength = 16;

    public PlayerProfile(string name)
    {
        Name = NormalizeName(name);
        CompletedLevels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        LevelStatsById = new Dictionary<string, LevelStats>(StringComparer.OrdinalIgnoreCase);
    }

    public string Name { get; private set; }

    public HashSet<string> CompletedLevels { get; }

    public Dictionary<string, LevelStats> LevelStatsById { get; }

    public void Rename(string newName)
    {
        Name = NormalizeName(newName);
    }

    public void MarkLevelCompleted(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return;

        CompletedLevels.Add(levelId);
    }

    public bool HasCompletedLevel(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return false;

        return CompletedLevels.Contains(levelId);
    }

    public void UpdateLevelStats(string levelId, int timeMs, int steps)
    {
        if (string.IsNullOrWhiteSpace(levelId))
            return;

        if (timeMs <= 0 || steps <= 0)
        {
            CompletedLevels.Add(levelId);
            return;
        }

        if (!LevelStatsById.TryGetValue(levelId, out var stats))
        {
            stats = new LevelStats(timeMs, steps);
            LevelStatsById[levelId] = stats;
        }
        else
        {
            var isBetterSteps = steps < stats.BestSteps;
            var isSameStepsBetterTime = steps == stats.BestSteps && timeMs < stats.BestTimeMs;

            if (isBetterSteps || isSameStepsBetterTime)
            {
                stats.BestSteps = steps;
                stats.BestTimeMs = timeMs;
            }
        }

        CompletedLevels.Add(levelId);
    }

    public bool TryGetLevelStats(string levelId, out LevelStats stats)
    {
        return LevelStatsById.TryGetValue(levelId, out stats);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Player";

        var trimmed = name.Trim();

        if (trimmed.Length > MaxNameLength)
            trimmed = trimmed[..MaxNameLength];

        return trimmed;
    }
}

public sealed class LevelStats
{
    public LevelStats(int bestTimeMs, int bestSteps)
    {
        BestTimeMs = bestTimeMs;
        BestSteps = bestSteps;
    }

    public int BestTimeMs { get; set; }

    public int BestSteps { get; set; }
}
