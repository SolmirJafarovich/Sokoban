using System;

namespace Sokoban.App;

public sealed class PlayerProfile
{
    public PlayerProfile(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }
}