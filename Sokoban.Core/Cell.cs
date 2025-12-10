namespace Sokoban.Core;

public class Cell
{
    public CellType Type { get; }

    public Cell(CellType type)
    {
        Type = type;
    }

    public bool IsWalkableBase => Type != CellType.Wall;
}