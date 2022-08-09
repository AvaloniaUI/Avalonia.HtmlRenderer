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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Html;
using Avalonia.Input;
using Avalonia.VisualTree;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Core.Utils;
using TheArtOfDev.HtmlRenderer.Avalonia.Utilities;
// ReSharper disable ConvertPropertyToExpressionBody

namespace TheArtOfDev.HtmlRenderer.Avalonia.Adapters
{
    /// <summary>
    /// Adapter for Avalonia Control for core.
    /// </summary>
    internal sealed class ControlAdapter : RControl
    {
        /// <summary>
        /// the underline Avalonia control.
        /// </summary>
        private readonly Control _control;
        /// <summary>
        /// Last known pointer position
        /// </summary>
        private Point? _lastPosition;
        private bool _leftMouseButton;
        private bool _rightMouseButton;

        /// <summary>
        /// Init.
        /// </summary>
        public ControlAdapter(Control control)
            : base(AvaloniaAdapter.Instance)
        {
            ArgChecker.AssertArgNotNull(control, "control");

            _control = control;

            _control.PointerMoved += (sender, args) =>
            {
                var visualRoot = _control.GetVisualRoot();
                if (visualRoot != null)
                {
                    _lastPosition = args.GetPosition(visualRoot);
                }
            };
            
            _control.PointerPressed += (sender, args) =>
            {
                _leftMouseButton = true;
            };
            
            _control.PointerReleased += (sender, args) =>
            {
                switch (args.InitialPressMouseButton)
                {
                    case MouseButton.Left:
                        _leftMouseButton = false;
                        break;
                    case MouseButton.Right:
                        _rightMouseButton = false;
                        break;
                }
            };
            
            _control.ContextRequested += (sender, args) =>
            {
                _rightMouseButton = true;
            };
        }

        /// <summary>
        /// Get the underline Avalonia control
        /// </summary>
        public Control Control
        {
            get { return _control; }
        }

        public override RPoint MouseLocation
        {
            get
            {
                return Util.Convert(_lastPosition.GetValueOrDefault());
            }
        }

        public override bool LeftMouseButton => _leftMouseButton;

        public override bool RightMouseButton => _rightMouseButton;

        public override void SetCursorDefault()
        {
            _control.Cursor = new Cursor(StandardCursorType.Arrow);
        }

        public override void SetCursorHand()
        {
            _control.Cursor = new Cursor(StandardCursorType.Hand);
        }

        public override void SetCursorIBeam()
        {
            _control.Cursor = new Cursor(StandardCursorType.Ibeam);
        }

        public override void DoDragDropCopy(object dragDropData)
        {
            //TODO: Implement DragDropCopy
            //DragDrop.DoDragDrop(_control, dragDropData, DragDropEffects.Copy);
        }

        public override void MeasureString(string str, RFont font, double maxWidth, out int charFit, out double charFitWidth)
        {
            using (var g = new GraphicsAdapter())
            {
                g.MeasureString(str, font, maxWidth, out charFit, out charFitWidth);
            }
        }

        public override void Invalidate()
        {
            _control.InvalidateVisual();
        }
    }
}