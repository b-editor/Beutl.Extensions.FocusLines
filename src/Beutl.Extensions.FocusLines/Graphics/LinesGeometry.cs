using Beutl.Media;

namespace Beutl.Extensions.FocusLines.Graphics;

/// <summary>The outline a drawable of this extension builds from its evaluated values.</summary>
/// <remarks>
/// It has no properties of its own: the drawable hands it new figures whenever what they depend on changes, and it
/// is never saved.
/// </remarks>
internal sealed partial class LinesGeometry : Geometry
{
    public partial class Resource
    {
        private FigureSet _figures = FigureSet.Empty;

        /// <summary>Replaces the figures; the cached path is rebuilt on its next use.</summary>
        public void SetFigures(FigureSet figures)
        {
            if (ReferenceEquals(_figures, figures))
                return;

            _figures = figures;
            Version++;
        }

        public override void ApplyTo(IGeometryContext context)
        {
            _figures.ApplyTo(context);
        }
    }
}
