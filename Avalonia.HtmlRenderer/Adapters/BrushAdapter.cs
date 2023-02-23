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
using TheArtOfDev.HtmlRenderer.Adapters;

namespace TheArtOfDev.HtmlRenderer.Avalonia.Adapters
{
    /// <summary>
    /// Adapter for Avalonia brushes.
    /// </summary>
    internal sealed class BrushAdapter : RBrush
    {
        /// <summary>
        /// The actual Avalonia brush instance.
        /// </summary>
        private readonly IImmutableBrush _brush;

        /// <summary>
        /// Init.
        /// </summary>
        public BrushAdapter(IImmutableBrush brush)
        {
            _brush = brush;
        }

        /// <summary>
        /// The actual Avalonia brush instance.
        /// </summary>
        public IImmutableBrush Brush
        {
            get { return _brush; }
        }

        public override void Dispose()
        { }
    }
}