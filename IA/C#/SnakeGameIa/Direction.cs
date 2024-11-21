public enum Direction
{
    Up,
    Down,
    Left,
    Right,
}

// Dictionnaire pour associer Direction à un Point (X, Y)
public static class DirectionExtensions
{
    private static readonly Dictionary<Direction, Point> DirectionToPoint = new()
    {
        { Direction.Up, new Point(0, -1) },
        { Direction.Down, new Point(0, 1) },
        { Direction.Left, new Point(-1, 0) },
        { Direction.Right, new Point(1, 0) }
    };

    public static Point ToPoint(this Direction direction)
    {
        return DirectionToPoint[direction];
    }
}
