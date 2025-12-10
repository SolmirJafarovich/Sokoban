using System;
using System.Collections.Generic;
using System.Linq;

namespace Sokoban.Core;

public class Level
{
    private readonly Cell[,] cells;
    private readonly HashSet<Position> boxes;
    private readonly HashSet<Position> targets;
    private Position playerPosition;

    public int Width { get; }
    public int Height { get; }

    public Position PlayerPosition => playerPosition;

    public Level(
        Cell[,] cells,
        Position playerPosition,
        IEnumerable<Position> boxPositions,
        IEnumerable<Position> targetPositions)
    {
        if (cells == null)
            throw new ArgumentNullException(nameof(cells));

        this.cells = cells;

        Width = cells.GetLength(0);
        Height = cells.GetLength(1);

        this.playerPosition = playerPosition;

        boxes = new HashSet<Position>(boxPositions ?? Enumerable.Empty<Position>());
        targets = new HashSet<Position>(targetPositions ?? Enumerable.Empty<Position>());
    }

    public Cell GetCell(Position position)
    {
        return cells[position.X, position.Y];
    }

    public bool IsInside(Position position)
    {
        return position.X >= 0 && position.X < Width &&
               position.Y >= 0 && position.Y < Height;
    }

    public bool HasBox(Position position)
    {
        return boxes.Contains(position);
    }

    public bool IsTarget(Position position)
    {
        return targets.Contains(position);
    }

    public MoveResult TryMove(Direction direction)
    {
        var newPlayerPosition = playerPosition.Offset(direction);

        if (!IsInside(newPlayerPosition))
            return MoveResult.None;

        var cell = GetCell(newPlayerPosition);
        if (!cell.IsWalkableBase)
            return MoveResult.None;

        if (HasBox(newPlayerPosition))
        {
            var boxNewPosition = newPlayerPosition.Offset(direction);

            if (!IsInside(boxNewPosition))
                return MoveResult.None;

            var nextCell = GetCell(boxNewPosition);
            if (!nextCell.IsWalkableBase || HasBox(boxNewPosition))
                return MoveResult.None;

            boxes.Remove(newPlayerPosition);
            boxes.Add(boxNewPosition);

            playerPosition = newPlayerPosition;
            return MoveResult.PushedBox;
        }

        playerPosition = newPlayerPosition;
        return MoveResult.Moved;
    }

    public bool IsCompleted()
    {
        return boxes.SetEquals(targets);
    }

    public bool HasBoxOnTarget(Position position)
    {
        return HasBox(position) && IsTarget(position);
    }
}
