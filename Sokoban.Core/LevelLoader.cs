using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sokoban.Core;

public static class LevelLoader
{
    public static Level LoadFromLines(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            throw new ArgumentException("Level lines are empty", nameof(lines));

        var height = lines.Length;
        var width = lines.Max(line => line.Length);

        var cells = new Cell[width, height];
        var boxPositions = new List<Position>();
        var targetPositions = new List<Position>();
        Position? playerPosition = null;

        for (var y = 0; y < height; y++)
        {
            var line = lines[y];

            for (var x = 0; x < width; x++)
            {
                var ch = x < line.Length ? line[x] : ' ';
                var position = new Position(x, y);
                var type = CellType.Empty;

                switch (ch)
                {
                    case '#':
                        type = CellType.Wall;
                        break;
                    case '.':
                        type = CellType.Target;
                        targetPositions.Add(position);
                        break;
                    case '$':
                        type = CellType.Empty;
                        boxPositions.Add(position);
                        break;
                    case '*':
                        type = CellType.Target;
                        targetPositions.Add(position);
                        boxPositions.Add(position);
                        break;
                    case '@':
                        type = CellType.Empty;
                        playerPosition = position;
                        break;
                    case '+':
                        type = CellType.Target;
                        targetPositions.Add(position);
                        playerPosition = position;
                        break;
                    default:
                        type = CellType.Empty;
                        break;
                }

                cells[x, y] = new Cell(type);
            }
        }

        if (playerPosition == null)
            throw new InvalidDataException("Player position is not found");

        if (boxPositions.Count == 0)
            throw new InvalidDataException("No boxes in level");

        if (boxPositions.Count != targetPositions.Count)
            throw new InvalidDataException("Boxes count must equal targets count");

        return new Level(cells, playerPosition.Value, boxPositions, targetPositions);
    }

    public static Level LoadFromFile(string path)
    {
        var lines = File.ReadAllLines(path);
        return LoadFromLines(lines);
    }
}
