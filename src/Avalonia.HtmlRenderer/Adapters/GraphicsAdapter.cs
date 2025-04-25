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
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;
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
        /// <param name="adapter">Avalonia adapter.</param>
        /// <param name="g">the Avalonia graphics object to use</param>
        /// <param name="initialClip">the initial clip of the graphics</param>
        /// <param name="releaseGraphics">optional: if to release the graphics object on dispose (default - false)</param>
        public GraphicsAdapter(AvaloniaAdapter adapter, DrawingContext g, RRect initialClip, bool releaseGraphics = false)
            : base(adapter, initialClip)
        {
            ArgChecker.AssertArgNotNull(g, "g");

            _g = g;
            _releaseGraphics = releaseGraphics;
        }

        /// <summary>
        /// Init.
        /// </summary>
        public GraphicsAdapter(AvaloniaAdapter adapter)
            : base(adapter, RRect.Empty)
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

        public override RFormattedLine FormatLine(ReadOnlyMemory<char> memory, RFont font)
        {
            var fontAdapter = (FontAdapter)font;
            var formattedLine = TextFormatter.Current.FormatLine(
                new CustomTextSource(memory, fontAdapter.TextRunProperties),
                0, double.MaxValue, fontAdapter.TextParagraphProperties);
            return new FormattedLineAdapter(formattedLine);
        }

        public override void DrawFormattedLine(RFormattedLine line, RColor color, RPoint point, RSize size, bool rtl)
        {
            var colorConv = ((BrushAdapter)_adapter.GetSolidBrush(color)).Brush;
            var lineConv = (FormattedLineAdapter)line;

            point.X += rtl ? lineConv.Size.Width : 0;
            var (currentX, currentY) = Utils.ConvertRound(point) + new Point(lineConv.TextLine.Start, 0);

            foreach (var run in lineConv.TextLine.TextRuns)
            {
                if (run is ShapedTextRun shapedTextRun)
                {
                    using (_g.PushTransform(Matrix.CreateTranslation(new Vector(currentX, currentY))))
                    {
                        _g.DrawGlyphRun(colorConv, shapedTextRun.GlyphRun);
                    }
                }
            }
        }

        public override RSize MeasureString(string str, RFont font)
        {
            var fontAdapter = (FontAdapter)font;
            var formattedLine = TextFormatter.Current.FormatLine(
                new CustomTextSource(str.AsMemory(), fontAdapter.TextRunProperties),
                0, double.MaxValue, fontAdapter.TextParagraphProperties);
            
            return new RSize(formattedLine.WidthIncludingTrailingWhitespace, formattedLine.Height);
        }

        public override void MeasureString(string str, RFont font, double maxWidth, out int charFit, out double charFitWidth)
        {
            var fontAdapter = (FontAdapter)font;
            var formattedLine = TextFormatter.Current.FormatLine(
                new CustomTextSource(str.AsMemory(), fontAdapter.TextRunProperties),
                0, maxWidth, fontAdapter.TextParagraphProperties);

            charFit = formattedLine.Length;
            charFitWidth = formattedLine.WidthIncludingTrailingWhitespace;
        }

        public override void DrawString(string str, RFont font, RColor color, RPoint point, RSize size, bool rtl)
        {
            var colorConv = ((BrushAdapter)_adapter.GetSolidBrush(color)).Brush;

            var formattedText = new FormattedText(str, CultureInfo.CurrentCulture, rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, ((FontAdapter)font).Font, 96d / 72d * font.Size, colorConv);
            point.X += rtl ? formattedText.Width : 0;
            _g.DrawText(formattedText, Utils.ConvertRound(point));
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

        
        internal readonly struct CustomTextSource : ITextSource
        {
            private readonly TextRunProperties _defaultProperties;
            private readonly ReadOnlyMemory<char> _text;

            public CustomTextSource(ReadOnlyMemory<char> text, TextRunProperties defaultProperties)
            {
                _text = text;
                _defaultProperties = defaultProperties;
            }
            
            public TextRun GetTextRun(int textSourceIndex)
            {
                if (textSourceIndex >= _text.Length)
                {
                    return new TextEndOfParagraph();
                }

                return new TextCharacters(_text, _defaultProperties);
            }
        }
        
        #endregion
    }
}