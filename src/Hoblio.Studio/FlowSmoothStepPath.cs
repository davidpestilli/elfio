using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Hoblio.Studio;

/// <summary>Orthogonal smooth-step routing aligned with React Flow / Hoblio Rust Studio.</summary>
static class FlowSmoothStepPath
{
    enum PortSide { Left, Right, Top, Bottom }

    public static Geometry Build(IReadOnlyList<Point> anchors, double borderRadius = 14, double offset = 20)
    {
        if (anchors.Count < 2)
            return Geometry.Empty;

        if (anchors.Count == 2)
        {
            var data = GetSmoothStepPathData(
                anchors[0], PortSide.Right,
                anchors[1], PortSide.Left,
                borderRadius, offset);
            return Geometry.Parse(data);
        }

        var figure = new PathFigure { StartPoint = anchors[0], IsClosed = false };
        for (var i = 1; i < anchors.Count; i++)
            figure.Segments.Add(new LineSegment(anchors[i], true));
        return new PathGeometry { Figures = { figure } };
    }

    static string GetSmoothStepPathData(
        Point source,
        PortSide sourcePosition,
        Point target,
        PortSide targetPosition,
        double borderRadius,
        double offset)
    {
        var (points, _, _, _, _) = GetPoints(source, sourcePosition, target, targetPosition, offset, 0.5, null, null);
        var path = FormattableString.Invariant($"M{points[0].X},{points[0].Y}");
        for (var i = 1; i < points.Count - 1; i++)
            path += GetBend(points[i - 1], points[i], points[i + 1], borderRadius);
        path += FormattableString.Invariant($"L{points[^1].X},{points[^1].Y}");
        return path;
    }

    static (List<Point> points, double labelX, double labelY, double offsetX, double offsetY) GetPoints(
        Point source,
        PortSide sourcePosition,
        Point target,
        PortSide targetPosition,
        double offset,
        double stepPosition,
        double? centerX,
        double? centerY)
    {
        var sourceDir = Dir(sourcePosition);
        var targetDir = Dir(targetPosition);
        var sourceGapped = new Point(source.X + sourceDir.X * offset, source.Y + sourceDir.Y * offset);
        var targetGapped = new Point(target.X + targetDir.X * offset, target.Y + targetDir.Y * offset);
        var dir = GetDirection(sourceGapped, sourcePosition, targetGapped);
        var dirAccessor = dir.X != 0 ? 'x' : 'y';
        var currDir = dirAccessor == 'x' ? dir.X : dir.Y;

        List<Point> points;
        double cx;
        double cy;
        var sourceGapOffset = new Point(0, 0);
        var targetGapOffset = new Point(0, 0);

        var defaultOffsetX = Math.Abs(target.X - source.X) / 2;
        var defaultOffsetY = Math.Abs(target.Y - source.Y) / 2;

        var srcDirVal = dirAccessor == 'x' ? sourceDir.X : sourceDir.Y;
        var tgtDirVal = dirAccessor == 'x' ? targetDir.X : targetDir.Y;

        if (srcDirVal * tgtDirVal == -1)
        {
            if (dirAccessor == 'x')
            {
                cx = centerX ?? sourceGapped.X + (targetGapped.X - sourceGapped.X) * stepPosition;
                cy = centerY ?? (sourceGapped.Y + targetGapped.Y) / 2;
            }
            else
            {
                cx = centerX ?? (sourceGapped.X + targetGapped.X) / 2;
                cy = centerY ?? sourceGapped.Y + (targetGapped.Y - sourceGapped.Y) * stepPosition;
            }

            var verticalSplit = new List<Point> { new(cx, sourceGapped.Y), new(cx, targetGapped.Y) };
            var horizontalSplit = new List<Point> { new(sourceGapped.X, cy), new(targetGapped.X, cy) };
            points = dirAccessor == 'x'
                ? (srcDirVal == currDir ? verticalSplit : horizontalSplit)
                : (srcDirVal == currDir ? horizontalSplit : verticalSplit);
        }
        else
        {
            var sourceTarget = new List<Point> { new(sourceGapped.X, targetGapped.Y) };
            var targetSource = new List<Point> { new(targetGapped.X, sourceGapped.Y) };
            if (dirAccessor == 'x')
                points = sourceDir.X == currDir ? targetSource : sourceTarget;
            else
                points = sourceDir.Y == currDir ? sourceTarget : targetSource;

            if (sourcePosition == targetPosition)
            {
                var srcVal = dirAccessor == 'x' ? source.X : source.Y;
                var tgtVal = dirAccessor == 'x' ? target.X : target.Y;
                var diff = Math.Abs(srcVal - tgtVal);
                if (diff <= offset)
                {
                    var gapOffset = Math.Min(offset - 1, offset - diff);
                    if (sourceDir.X == currDir || sourceDir.Y == currDir)
                    {
                        if (dirAccessor == 'x')
                            sourceGapOffset.X = (sourceGapped.X > source.X ? -1 : 1) * gapOffset;
                        else
                            sourceGapOffset.Y = (sourceGapped.Y > source.Y ? -1 : 1) * gapOffset;
                    }
                    else
                    {
                        if (dirAccessor == 'x')
                            targetGapOffset.X = (targetGapped.X > target.X ? -1 : 1) * gapOffset;
                        else
                            targetGapOffset.Y = (targetGapped.Y > target.Y ? -1 : 1) * gapOffset;
                    }
                }
            }

            if (sourcePosition != targetPosition)
            {
                var opposite = dirAccessor == 'x' ? 'y' : 'x';
                var srcOppo = dirAccessor == 'x' ? sourceGapped.Y : sourceGapped.X;
                var tgtOppo = dirAccessor == 'x' ? targetGapped.Y : targetGapped.X;
                var isSameDir = (dirAccessor == 'x' ? sourceDir.X : sourceDir.Y) ==
                                (opposite == 'x' ? targetDir.X : targetDir.Y);
                var flip = (srcDirVal == 1 && ((!isSameDir && srcOppo > tgtOppo) || (isSameDir && srcOppo < tgtOppo))) ||
                           (srcDirVal != 1 && ((!isSameDir && srcOppo < tgtOppo) || (isSameDir && srcOppo > tgtOppo)));
                if (flip)
                    points = dirAccessor == 'x' ? sourceTarget : targetSource;
            }

            var sourceGapPoint = new Point(sourceGapped.X + sourceGapOffset.X, sourceGapped.Y + sourceGapOffset.Y);
            var targetGapPoint = new Point(targetGapped.X + targetGapOffset.X, targetGapped.Y + targetGapOffset.Y);
            var maxX = Math.Max(Math.Abs(sourceGapPoint.X - points[0].X), Math.Abs(targetGapPoint.X - points[0].X));
            var maxY = Math.Max(Math.Abs(sourceGapPoint.Y - points[0].Y), Math.Abs(targetGapPoint.Y - points[0].Y));
            if (maxX >= maxY)
            {
                cx = (sourceGapPoint.X + targetGapPoint.X) / 2;
                cy = points[0].Y;
            }
            else
            {
                cx = points[0].X;
                cy = (sourceGapPoint.Y + targetGapPoint.Y) / 2;
            }
        }

        var gappedSource = new Point(sourceGapped.X + sourceGapOffset.X, sourceGapped.Y + sourceGapOffset.Y);
        var gappedTarget = new Point(targetGapped.X + targetGapOffset.X, targetGapped.Y + targetGapOffset.Y);
        var pathPoints = new List<Point>();
        foreach (var point in new[] { source, gappedSource }.Concat(points).Append(gappedTarget).Append(target))
        {
            if (pathPoints.Count >= 2)
            {
                var a = pathPoints[^2];
                var b = pathPoints[^1];
                if (IsBetween(a, b, point))
                {
                    pathPoints.RemoveAt(pathPoints.Count - 1);
                }
            }
            pathPoints.Add(point);
        }

        return (pathPoints, cx, cy, defaultOffsetX, defaultOffsetY);
    }

    static Point Dir(PortSide side) => side switch
    {
        PortSide.Left => new(-1, 0),
        PortSide.Right => new(1, 0),
        PortSide.Top => new(0, -1),
        _ => new(0, 1),
    };

    static Point GetDirection(Point source, PortSide sourcePosition, Point target)
    {
        if (sourcePosition is PortSide.Left or PortSide.Right)
            return source.X < target.X ? new(1, 0) : new(-1, 0);
        return source.Y < target.Y ? new(0, 1) : new(0, -1);
    }

    static bool IsBetween(Point a, Point b, Point c) =>
        (a.X == b.X && b.X == c.X && (b.Y - a.Y) * (c.Y - b.Y) >= 0) ||
        (a.Y == b.Y && b.Y == c.Y && (b.X - a.X) * (c.X - b.X) >= 0);

    static double Dist(Point a, Point b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));

    static string GetBend(Point a, Point b, Point c, double size)
    {
        var bendSize = Math.Min(Math.Min(Dist(a, b) / 2, Dist(b, c) / 2), size);
        var x = b.X;
        var y = b.Y;
        if (a.X == x && x == c.X)
            return string.Create(CultureInfo.InvariantCulture, $"L{x},{y}");
        if (a.Y == y)
        {
            var xDir = a.X < c.X ? -1 : 1;
            var yDir = a.Y < c.Y ? 1 : -1;
            return string.Create(CultureInfo.InvariantCulture,
                $"L{x + bendSize * xDir},{y}Q{x},{y} {x},{y + bendSize * yDir}");
        }
        var xDir2 = a.X < c.X ? 1 : -1;
        var yDir2 = a.Y < c.Y ? -1 : 1;
        return string.Create(CultureInfo.InvariantCulture,
            $"L{x},{y + bendSize * yDir2}Q{x},{y} {x + bendSize * xDir2},{y}");
    }
}
