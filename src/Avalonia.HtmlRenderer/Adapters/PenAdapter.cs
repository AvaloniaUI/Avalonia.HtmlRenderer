// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using Avalonia.Media;
using Avalonia.Media.Immutable;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;

namespace TheArtOfDev.HtmlRenderer.Avalonia.Adapters
{
    /// <summary>
    /// Adapter for Avalonia pens objects for core.
    /// </summary>
    internal sealed class PenAdapter : RPen
    {
        /// <summary>
        /// The actual Avalonia brush instance.
        /// </summary>
        private readonly IImmutableBrush _brush;

        /// <summary>
        /// the width of the pen
        /// </summary>
        private double _width;

        /// <summary>
        /// the dash style of the pen
        /// </summary>
        private IDashStyle _dashStyle = null;

        /// <summary>
        /// Init.
        /// </summary>
        public PenAdapter(IImmutableBrush brush)
        {
            _brush = brush;
        }

        public override double Width
        {
            get { return _width; }
            set { _width = value; }
        }

        public override RDashStyle DashStyle
        {
            set
            {
                switch (value)
                {
                    case RDashStyle.Dash:
                        _dashStyle = global::Avalonia.Media.DashStyle.Dash;
                        break;
                    case RDashStyle.Dot:
                        _dashStyle = global::Avalonia.Media.DashStyle.Dot;
                        break;
                    case RDashStyle.DashDot:
                        _dashStyle = global::Avalonia.Media.DashStyle.DashDot;
                        break;
                    case RDashStyle.DashDotDot:
                        _dashStyle = global::Avalonia.Media.DashStyle.DashDotDot;
                        break;
                    default:
                        _dashStyle = null;
                        break;
                }
            }
        }

        /// <summary>
        /// Create the actual Avalonia pen instance.
        /// </summary>
        public IPen CreatePen()
        {
            return new ImmutablePen(_brush, _width, (ImmutableDashStyle)_dashStyle);
        }
    }
}