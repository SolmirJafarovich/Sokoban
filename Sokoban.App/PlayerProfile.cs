using System;
using System.Collections.Generic;

namespace Sokoban.App;

public sealed class PlayerProfile
{
    private const int MaxNameLength = 16;

    public PlayerProfile(string name, GameSettings settings)
    {
        Name = NormalizeName(name);
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        CompletedLevels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public PlayerProfile(string name, GameSettings settings, IEnumerable<string> completedLevels)
        : this(name, settings)
    {
        if (completedLevels == null)
            return;

        foreach (var id in completedLevels)
        {
            if (!string.IsNullOrWhiteSpace(id))
                CompletedLevels.Add(id);
        }
    }

    public string Name { get; private set; }
    public GameSettings Settings { get; }
    public HashSet<string> CompletedLevels { get; }

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