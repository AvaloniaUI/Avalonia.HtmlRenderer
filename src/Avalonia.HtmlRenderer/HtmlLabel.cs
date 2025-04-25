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
using System.ComponentModel;
using Avalonia;
using Avalonia.Media;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Core;
using TheArtOfDev.HtmlRenderer.Avalonia.Adapters;

namespace TheArtOfDev.HtmlRenderer.Avalonia
{
    /// <summary>
    /// Provides HTML rendering using the text property.<br/>
    /// Avalonia control that will render html content in it's client rectangle.<br/>
    /// Using <see cref="AutoSize"/> and <see cref="AutoSizeHeightOnly"/> client can control how the html content effects the
    /// size of the label. Either case scrollbars are never shown and html content outside of client bounds will be clipped.
    /// MaxWidth/MaxHeight and MinWidth/MinHeight with AutoSize can limit the max/min size of the control<br/>
    /// The control will handle mouse and keyboard events on it to support html text selection, copy-paste and mouse clicks.<br/>
    /// </summary>
    /// <remarks>
    /// See <see cref="HtmlControl"/> for more info.
    /// </remarks>
    public class HtmlLabel : HtmlControl
    {
        #region Dependency properties

        public static readonly StyledProperty<bool> AutoSizeProperty =
            AvaloniaProperty.Register<HtmlLabel, bool>("AutoSize", true);
        public static readonly StyledProperty<bool> AutoSizeHeightOnlyProperty =
            AvaloniaProperty.Register<HtmlLabel, bool>("AutoSizeHeightOnly", false);

        #endregion


        /// <summary>
        /// Init.
        /// </summary>
        static HtmlLabel()
        {
            BackgroundProperty.OverrideDefaultValue<HtmlLabel>(Brushes.Transparent);
            AffectsMeasure<HtmlLabel>(AutoSizeProperty, AutoSizeHeightOnlyProperty);
        }

        /// <summary>
        /// Automatically sets the size of the label by content size
        /// </summary>
        [Category("Layout")]
        [Description("Automatically sets the size of the label by content size.")]
        public bool AutoSize
        {
            get { return (bool)GetValue(AutoSizeProperty); }
            set { SetValue(AutoSizeProperty, value); }
        }

        /// <summary>
        /// Automatically sets the height of the label by content height (width is not effected).
        /// </summary>
        [Category("Layout")]
        [Description("Automatically sets the height of the label by content height (width is not effected)")]
        public virtual bool AutoSizeHeightOnly
        {
            get { return (bool)GetValue(AutoSizeHeightOnlyProperty); }
            set { SetValue(AutoSizeHeightOnlyProperty, value); }
        }


        #region Private methods

        /// <summary>
        /// Perform the layout of the html in the control.
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            if (_htmlContainer != null)
            {
                using (var ig = new GraphicsAdapter(Container.AvaloniaAdapter))
                {
                    var horizontal = Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right;
                    var vertical = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom;

                    var size = new RSize(constraint.Width < Double.PositiveInfinity ? constraint.Width - horizontal : 0, constraint.Height < Double.PositiveInfinity ? constraint.Height - vertical : 0);
                    var minSize = new RSize(MinWidth < Double.PositiveInfinity ? MinWidth - horizontal : 0, MinHeight < Double.PositiveInfinity ? MinHeight - vertical : 0);
                    var maxSize = new RSize(MaxWidth < Double.PositiveInfinity ? MaxWidth - horizontal : 0, MaxHeight < Double.PositiveInfinity ? MaxHeight - vertical : 0);

                    var newSize = HtmlRendererUtils.Layout(ig, _htmlContainer.HtmlContainerInt, size, minSize, maxSize, AutoSize, AutoSizeHeightOnly);

                    constraint = new Size(newSize.Width + horizontal, newSize.Height + vertical);
                }
            }

            if (double.IsPositiveInfinity(constraint.Width) || double.IsPositiveInfinity(constraint.Height))
                constraint = default;

            return constraint;
        }

        /// <summary>
        /// Handle when dependency property value changes to update the underline HtmlContainer with the new value.
        /// </summary>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == AutoSizeProperty)
            {
                if (e.GetNewValue<bool>())
                {
                    SetValue(AutoSizeHeightOnlyProperty, false);
                }
            }
            else if (e.Property == AutoSizeHeightOnlyProperty)
            {
                if (e.GetNewValue<bool>())
                {
                    SetValue(AutoSizeProperty, false);
                }
            }
        }

        #endregion
    }
}