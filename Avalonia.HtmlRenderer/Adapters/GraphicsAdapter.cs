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

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Core.Utils;
using TheArtOfDev.HtmlRenderer.Avalonia.Utilities;

namespace TheArtOfDev.HtmlRenderer.Avalonia.Adapters
{
    /// <summary>
    /// Adapter for Avalonia Graphics.
    /// </summary>
    internal sealed class GraphicsAdapter : RGraphics
    {
        #region Fields and Consts

        /// <summary>
        /// The wrapped Avalonia graphics object
        /// </summary>
        private readonly DrawingContext _g;

        /// <summary>
        /// if to release the graphics object on dispose
        /// </summary>
        private readonly bool _releaseGraphics;

        #endregion

        private readonly Stack<DrawingContext.PushedState> _clipStackInt = new Stack<DrawingContext.PushedState>();

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="g">the Avalonia graphics object to use</param>
        /// <param name="initialClip">the initial clip of the graphics</param>
        /// <param name="releaseGraphics">optional: if to release the graphics object on dispose (default - false)</param>
        public GraphicsAdapter(DrawingContext g, RRect initialClip, bool releaseGraphics = false)
            : base(AvaloniaAdapter.Instance, initialClip)
        {
            ArgChecker.AssertArgNotNull(g, "g");

            _g = g;
            _releaseGraphics = releaseGraphics;
        }

        /// <summary>
        /// Init.
        /// </summary>
        public GraphicsAdapter()
            : base(AvaloniaAdapter.Instance, RRect.Empty)
        {
            _g = null;
            _releaseGraphics = false;
        }

        public override void PopClip()
        {
            _clipStackInt.Pop().Dispose();
            _clipStack.Pop();
        }

        public override void PushClip(RRect rect)
        {
            _clipStackInt.Push(_g.PushClip(Utils.Convert(rect)));
            _clipStack.Push(rect);
        }

        public override void PushClipExclude(RRect rect)
        {
            var geometry = new CombinedGeometry();
            geometry.Geometry1 = new RectangleGeometry(Utils.Convert(_clipStack.Peek()));
            geometry.Geometry2 = new RectangleGeometry(Utils.Convert(rect));
            geometry.GeometryCombineMode = GeometryCombineMode.Exclude;
            
            _clipStack.Push(_clipStack.Peek());
            _clipStackInt.Push(_g.PushGeometryClip(geometry));
        }

        public override Object SetAntiAliasSmoothingMode()
        {
            return null;
        }

        public override void ReturnPreviousSmoothingMode(Object prevMode)
        { }

        public override RSize MeasureString(string str, RFont font)
        {
            var formattedText = new FormattedText(str, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, ((FontAdapter)font).Font, 96d / 72d * font.Size, Brushes.Red);
            return new RSize(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
        }

        public override void MeasureString(string str, RFont font, double maxWidth, out int charFit, out double charFitWidth)
        {
            charFit = 0;
            charFitWidth = 0;
            bool handled = false;
            IGlyphTypeface glyphTypeface = ((FontAdapter)font).GlyphTypeface;
            if (glyphTypeface != null)
            {
                handled = true;
                double width = 0;
                for (int i = 0; i < str.Length; i++)
                {
                    if (glyphTypeface.TryGetGlyphMetrics(str[i], out var metrics))
                    {
                        double advanceWidth = metrics.Width * font.Size * 96d / 72d;

                        if (!(width + advanceWidth < maxWidth))
                        {
                            charFit = i;
                            charFitWidth = width;
                            break;
                        }
                        width += advanceWidth;
                    }
                    else
                    {
                        handled = false;
                        break;
                    }
                }
            }

            if (!handled)
            {
                var formattedText = new FormattedText(str, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, ((FontAdapter)font).Font, 96d / 72d * font.Size, Brushes.Red);
                charFit = str.Length;
                charFitWidth = formattedText.WidthIncludingTrailingWhitespace;
            }
        }

        public override void DrawString(string str, RFont font, RColor color, RPoint point, RSize size, bool rtl)
        {
            var colorConv = ((BrushAdapter)_adapter.GetSolidBrush(color)).Brush;

            bool glyphRendered = false;
            IGlyphTypeface glyphTypeface = ((FontAdapter)font).GlyphTypeface;
            if (glyphTypeface != null)
            {
                double width = 0;
                ushort[] glyphs = new ushort[str.Length];
                double[] widths = new double[str.Length];

                int i = 0;
                for (; i < str.Length; i++)
                {
                    if (!glyphTypeface.TryGetGlyph(str[i], out var glyph))
                        break;
                    if (!glyphTypeface.TryGetGlyphMetrics(str[i], out var glyphMetrics))
                        break;

                    glyphs[i] = glyph;
                    width += glyphMetrics.Width;
                    widths[i] = 96d / 72d * font.Size * glyphMetrics.Width;
                }

                if (i >= str.Length)
                {
                    point.Y += glyphTypeface.Metrics.Ascent * font.Size * 96d / 72d;
                    point.X += rtl ? 96d / 72d * font.Size * width : 0;

                    glyphRendered = true;
                    var glyphRun = new GlyphRun(glyphTypeface, 96d / 72d * font.Size, str.AsMemory(), glyphs,
                        Utils.ConvertRound(point), rtl ? 1 : 0);
                    _g.DrawGlyphRun(colorConv, glyphRun);
                }
            }

            if (!glyphRendered)
            {
                var formattedText = new FormattedText(str, CultureInfo.CurrentCulture, rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, ((FontAdapter)font).Font, 96d / 72d * font.Size, colorConv);
                point.X += rtl ? formattedText.Width : 0;
                _g.DrawText(formattedText, Utils.ConvertRound(point));
            }
        }

        public override RBrush GetTextureBrush(RImage image, RRect dstRect, RPoint translateTransformLocation)
        {
            var brush = new ImageBrush(((ImageAdapter)image).Image);
            brush.Stretch = Stretch.None;
            brush.TileMode = TileMode.Tile;
            brush.DestinationRect = new RelativeRect(Utils.Convert(dstRect).Translate(Utils.Convert(translateTransformLocation) - new Point()), RelativeUnit.Absolute);
            brush.Transform = new TranslateTransform(translateTransformLocation.X, translateTransformLocation.Y);

            return new BrushAdapter(brush.ToImmutable());
        }
        
        public override RGraphicsPath GetGraphicsPath()
        {
            return new GraphicsPathAdapter();
        }

        public override void Dispose()
        {
            if (_releaseGraphics)
                _g.Dispose();
        }

    
        #region Delegate graphics methods

        public override void DrawLine(RPen pen, double x1, double y1, double x2, double y2)
        {
            x1 = (int)x1;
            x2 = (int)x2;
            y1 = (int)y1;
            y2 = (int)y2;

            var adj = pen.Width;
            if (Math.Abs(x1 - x2) < .1 && Math.Abs(adj % 2 - 1) < .1)
            {
                x1 += .5;
                x2 += .5;
            }
            if (Math.Abs(y1 - y2) < .1 && Math.Abs(adj % 2 - 1) < .1)
            {
                y1 += .5;
                y2 += .5;
            }

            _g.DrawLine(((PenAdapter)pen).CreatePen(), new Point(x1, y1), new Point(x2, y2));
        }
        
        public override void DrawRectangle(RPen pen, double x, double y, double width, double height)
        {
            var adj = pen.Width;
            if (Math.Abs(adj % 2 - 1) < .1)
            {
                x += .5;
                y += .5;
            }
            _g.DrawRectangle(((PenAdapter) pen).CreatePen(), new Rect(x, y, width, height));
        }

        public override void DrawRectangle(RBrush brush, double x, double y, double width, double height)
        {
            _g.FillRectangle(((BrushAdapter) brush).Brush, new Rect(x, y, width, height));
        }

        public override void DrawImage(RImage image, RRect destRect, RRect srcRect)
        {
            _g.DrawImage(((ImageAdapter) image).Image, Utils.Convert(srcRect), Utils.Convert(destRect));
        }

        public override void DrawImage(RImage image, RRect destRect)
        {
            _g.DrawImage(((ImageAdapter)image).Image, Utils.ConvertRound(destRect));
        }

        public override void DrawPath(RPen pen, RGraphicsPath path)
        {
            _g.DrawGeometry(null, ((PenAdapter)pen).CreatePen(), ((GraphicsPathAdapter)path).GetClosedGeometry());
        }

        public override void DrawPath(RBrush brush, RGraphicsPath path)
        {
            _g.DrawGeometry(((BrushAdapter)brush).Brush, null, ((GraphicsPathAdapter)path).GetClosedGeometry());
        }

        public override void DrawPolygon(RBrush brush, RPoint[] points)
        {
            if (points != null && points.Length > 0)
            {
                var g = new StreamGeometry();
                using (var context = g.Open())
                {
                    context.BeginFigure(Utils.Convert(points[0]), true);
                    for (int i = 1; i < points.Length; i++)
                        context.LineTo(Utils.Convert(points[i]));
                    context.EndFigure(false);
                }

                _g.DrawGeometry(((BrushAdapter)brush).Brush, null, g);
            }
        }

        #endregion
    }
}