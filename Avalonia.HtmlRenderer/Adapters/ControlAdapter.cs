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

using Avalonia.Controls;
using Avalonia.Input;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Core.Utils;
using TheArtOfDev.HtmlRenderer.Avalonia.Utilities;

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
        /// Init.
        /// </summary>
        public ControlAdapter(Control control)
            : base(AvaloniaAdapter.Instance)
        {
            ArgChecker.AssertArgNotNull(control, "control");

            _control = control;
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
            get { return Utils.Convert((_control as HtmlControl)?.LastPointerArgs?.GetPosition(null) ?? default); }
        }

        public override bool LeftMouseButton
        {
            get { return (_control as HtmlControl)?.LastPointerArgs?.GetCurrentPoint(null).Properties.IsLeftButtonPressed ?? false; }
        }

        public override bool RightMouseButton
        {
            get { return (_control as HtmlControl)?.LastPointerArgs?.GetCurrentPoint(null).Properties.IsRightButtonPressed ?? false; }
        }

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
            var args = (_control as HtmlControl)?.LastPointerArgs;
            if (args != null)
            {
                DragDrop.DoDragDrop(args, (IDataObject)dragDropData, DragDropEffects.Copy);
            }
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