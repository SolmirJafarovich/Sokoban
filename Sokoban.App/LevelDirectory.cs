using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sokoban.App;

public static class LevelDirectory
{
    public static IReadOnlyList<LevelInfo> LoadLevels(string baseDirectory)
    {
        var levelsPath = Path.Combine(baseDirectory, "Levels");

        if (!Directory.Exists(levelsPath))
            throw new DirectoryNotFoundException($"Levels directory not found: {levelsPath}");

        var files = Directory
            .GetFiles(levelsPath, "*.txt", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
            throw new InvalidOperationException($"No level files found in {levelsPath}");

        var result = new List<LevelInfo>();

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            result.Add(new LevelInfo(name, file));
        }

        return result;
    }
}