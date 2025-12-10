using System;

namespace Sokoban.App;

public class LevelInfo
{
    public LevelInfo(string name, string filePath)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public string Name { get; }
    public string FilePath { get; }
}