using System;
using System.Collections.Generic;

namespace Sokoban.App;

public sealed class GlobalLeaderboardService
{
    public IReadOnlyList<GlobalLeaderboardEntry> BuildLeaderboard(IEnumerable<PlayerProfile> profiles)
    {
        var result = new List<GlobalLeaderboardEntry>();

        foreach (var profile in profiles)
        {
            if (profile == null)
                continue;

            var completedLevels = profile.CompletedLevels.Count;

            var totalSteps = 0;
            var totalTimeMs = 0;

            foreach (var stats in profile.LevelStatsById.Values)
            {
                if (stats.BestSteps > 0)
                    totalSteps += stats.BestSteps;

                if (stats.BestTimeMs > 0)
                    totalTimeMs += stats.BestTimeMs;
            }

            var entry = new GlobalLeaderboardEntry(
                profile.Name,
                completedLevels,
                totalSteps,
                totalTimeMs);

            result.Add(entry);
        }

        result.Sort(CompareEntries);

        return result;
    }

    private static int CompareEntries(GlobalLeaderboardEntry x, GlobalLeaderboardEntry y)
    {
        var byCompleted = y.CompletedLevels.CompareTo(x.CompletedLevels);
        if (byCompleted != 0)
            return byCompleted;

        var bySteps = x.TotalSteps.CompareTo(y.TotalSteps);
        if (bySteps != 0)
            return bySteps;

        var byTime = x.TotalTimeMs.CompareTo(y.TotalTimeMs);
        if (byTime != 0)
            return byTime;

        return string.Compare(x.PlayerName, y.PlayerName, StringComparison.Ordinal);
    }
}