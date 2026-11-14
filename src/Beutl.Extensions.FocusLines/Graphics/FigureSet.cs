using Beutl.Graphics;
using Beutl.Media;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>
/// Closed polygons, all wound the same way, so that filling them with the nonzero rule paints their union.
/// </summary>
internal sealed class FigureSet
{
    public static readonly FigureSet Empty = new([], []);

    private readonly Point[] _points;
    private readonly int[] _ends;

    private FigureSet(Point[] points, int[] ends)
    {
        _points = points;
        _ends = ends;
    }

    public int Count => _ends.Length;

    public ReadOnlySpan<Point> this[int index]
    {
        get
        {
            int start = index == 0 ? 0 : _ends[index - 1];
            return _points.AsSpan(start, _ends[index] - start);
        }
    }

    public void ApplyTo(IGeometryContext context)
    {
        int start = 0;
        foreach (int end in _ends)
        {
            context.MoveTo(_points[start]);
            for (int i = start + 1; i < end; i++)
                context.LineTo(_points[i]);

            context.Close();
            start = end;
        }
    }

    public sealed class Builder
    {
        private readonly List<Point> _points = [];
        private readonly List<int> _ends = [];
        private int _start;

        public void Add(float x, float y) => _points.Add(new Point(x, y));

        public void Add(Point point) => _points.Add(point);

        /// <summary>
        /// Ends the polygon added since the last call and turns it the common way round. A polygon that encloses no
        /// area, or that has a point that is not a number, is dropped.
        /// </summary>
        public void Close()
        {
            int count = _points.Count - _start;
            double twiceArea = 0;
            bool finite = count >= 3;
            for (int i = 0; i < count && finite; i++)
            {
                Point a = _points[_start + i];
                Point b = _points[_start + (i + 1) % count];
                finite = float.IsFinite(a.X) && float.IsFinite(a.Y);
                twiceArea += (double)a.X * b.Y - (double)b.X * a.Y;
            }

            if (!finite || Math.Abs(twiceArea) < 1e-6)
            {
                _points.RemoveRange(_start, count);
                return;
            }

            if (twiceArea < 0)
                _points.Reverse(_start, count);

            _ends.Add(_points.Count);
            _start = _points.Count;
        }

        public FigureSet ToFigureSet()
        {
            if (_ends.Count == 0)
                return Empty;

            return new FigureSet(_points.GetRange(0, _start).ToArray(), [.. _ends]);
        }
    }
}
