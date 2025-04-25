﻿// "Therefore those skilled at the unorthodox
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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using Avalonia.Threading;
using TheArtOfDev.HtmlRenderer.Core;
using TheArtOfDev.HtmlRenderer.Core.Entities;
using Point = Avalonia.Point;
using Size = Avalonia.Size;

namespace TheArtOfDev.HtmlRenderer.Avalonia
{
    /// <summary>
    /// Provides HTML rendering using the text property.<br/>
    /// Avalonia control that will render html content in it's client rectangle.<br/>
    /// The control will handle mouse and keyboard events on it to support html text selection, copy-paste and mouse clicks.<br/>
    /// <para>
    /// The major differential to use HtmlPanel or HtmlLabel is size and scrollbars.<br/>
    /// If the size of the control depends on the html content the HtmlLabel should be used.<br/>
    /// If the size is set by some kind of layout then HtmlPanel is more suitable, also shows scrollbars if the html contents is larger than the control client rectangle.<br/>
    /// </para>
    /// <para>
    /// <h4>LinkClicked event:</h4>
    /// Raised when the user clicks on a link in the html.<br/>
    /// Allows canceling the execution of the link.
    /// </para>
    /// <para>
    /// <h4>StylesheetLoad event:</h4>
    /// Raised when a stylesheet is about to be loaded by file path or URI by link element.<br/>
    /// This event allows to provide the stylesheet manually or provide new source (file or uri) to load from.<br/>
    /// If no alternative data is provided the original source will be used.<br/>
    /// </para>
    /// <para>
    /// <h4>ImageLoad event:</h4>
    /// Raised when an image is about to be loaded by file path or URI.<br/>
    /// This event allows to provide the image manually, if not handled the image will be loaded from file or download from URI.
    /// </para>
    /// <para>
    /// <h4>RenderError event:</h4>
    /// Raised when an error occurred during html rendering.<br/>
    /// </para>
    /// </summary>
    public class HtmlControl : Control
    {
        #region Fields and Consts

        /// <summary>
        /// Underline html container instance.
        /// </summary>
        protected readonly HtmlContainer _htmlContainer;

        /// <summary>
        /// the base stylesheet data used in the control
        /// </summary>
        protected CssData _baseCssData;

        /// <summary>
        /// The last position of the scrollbars to know if it has changed to update mouse
        /// </summary>
        protected Point _lastScrollOffset;

        #endregion


        #region Dependency properties / routed events

        public static readonly AvaloniaProperty AvoidImagesLateLoadingProperty = 
            AvaloniaProperty.Register<HtmlControl, bool>(nameof(AvoidImagesLateLoading), defaultValue: false);
        public static readonly AvaloniaProperty IsSelectionEnabledProperty =
            AvaloniaProperty.Register<HtmlControl, bool>(nameof(IsSelectionEnabled), defaultValue: true);
        public static readonly AvaloniaProperty IsContextMenuEnabledProperty =
            AvaloniaProperty.Register<HtmlControl, bool>(nameof(IsContextMenuEnabled), defaultValue: true);

        public static readonly AvaloniaProperty BaseStylesheetProperty =
            AvaloniaProperty.Register<HtmlControl, string>(nameof(BaseStylesheet), defaultValue: null);

        public static readonly AvaloniaProperty TextProperty =
            AvaloniaProperty.Register<HtmlControl, string>(nameof(Text), defaultValue: null);

        public static readonly StyledProperty<IBrush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<HtmlControl>();

        public static readonly AvaloniaProperty BorderThicknessProperty =
            Border.BorderThicknessProperty.AddOwner<HtmlControl>();

        public static readonly AvaloniaProperty BorderBrushProperty =
            Border.BorderBrushProperty.AddOwner<HtmlControl>();

        public static readonly AvaloniaProperty PaddingProperty =
            Decorator.PaddingProperty.AddOwner<HtmlControl>();
        public static readonly RoutedEvent LoadCompleteEvent =
            RoutedEvent.Register<RoutedEventArgs>("LoadComplete",  RoutingStrategies.Bubble, typeof(HtmlControl));
        public static readonly RoutedEvent LinkClickedEvent =
            RoutedEvent.Register<HtmlRendererRoutedEventArgs<HtmlLinkClickedEventArgs>>("LinkClicked", RoutingStrategies.Bubble, typeof(HtmlControl));
        public static readonly RoutedEvent RenderErrorEvent 
            = RoutedEvent.Register<HtmlRendererRoutedEventArgs<HtmlRenderErrorEventArgs>>("RenderError", RoutingStrategies.Bubble, typeof(HtmlControl));
        public static readonly RoutedEvent RefreshEvent 
            = RoutedEvent.Register<HtmlRendererRoutedEventArgs<HtmlRefreshEventArgs>>("Refresh", RoutingStrategies.Bubble, typeof(HtmlControl));
        public static readonly RoutedEvent StylesheetLoadEvent 
            = RoutedEvent.Register<HtmlRendererRoutedEventArgs<HtmlStylesheetLoadEventArgs>>("StylesheetLoad", RoutingStrategies.Bubble, typeof(HtmlControl));
        public static readonly RoutedEvent ImageLoadEvent
            = RoutedEvent.Register<HtmlRendererRoutedEventArgs<HtmlImageLoadEventArgs>>("ImageLoad", RoutingStrategies.Bubble,
                typeof (HtmlControl));
        #endregion

        internal PointerEventArgs LastPointerArgs { get; private set; }

        /// <summary>
        /// Creates a new HtmlPanel and sets a basic css for it's styling.
        /// </summary>
        public HtmlControl()
        {
            _htmlContainer = new HtmlContainer(this);
            _htmlContainer.LoadComplete += OnLoadComplete;
            _htmlContainer.LinkClicked += OnLinkClicked;
            _htmlContainer.RenderError += OnRenderError;
            _htmlContainer.Refresh += OnRefresh;
            _htmlContainer.StylesheetLoad += OnStylesheetLoad;
            _htmlContainer.ImageLoad += OnImageLoad;

            PointerMoved += (sender, e) =>
            {
                LastPointerArgs = e;
                _htmlContainer.HandleMouseMove(this, e.GetPosition(this));
            };
            PointerExited += (sender, e) =>
            {
                LastPointerArgs = null;
                _htmlContainer.HandleMouseLeave(this);
            };
            PointerPressed += (sender, e) =>
            {
                LastPointerArgs = e;
                _htmlContainer?.HandleLeftMouseDown(this, e);
            };
            PointerReleased += (sender, e) =>
            {
                LastPointerArgs = e;
                _htmlContainer.HandleLeftMouseUp(this, e);
            };
            DoubleTapped += (sender, e) =>
            {
                _htmlContainer.HandleMouseDoubleClick(this, e);
            };
            KeyDown += (sender, e) =>
            {
                _htmlContainer.HandleKeyDown(this, e);
            };
        }

        /// <summary>
        /// Raised when the set html document has been fully loaded.<br/>
        /// Allows manipulation of the html dom, scroll position, etc.
        /// </summary>
        public event EventHandler<HtmlRendererRoutedEventArgs<EventArgs>>  LoadComplete
        {
            add { AddHandler(LoadCompleteEvent, value); }
            remove { RemoveHandler(LoadCompleteEvent, value); }
        }

        /// <summary>
        /// Raised when the user clicks on a link in the html.<br/>
        /// Allows canceling the execution of the link.
        /// </summary>
        public event EventHandler<HtmlRendererRoutedEventArgs<HtmlLinkClickedEventArgs>> LinkClicked
        {
            add { AddHandler(LinkClickedEvent, value); }
            remove { RemoveHandler(LinkClickedEvent, value); }
        }

        /// <summary>
        /// Raised when an error occurred during html rendering.<br/>
        /// </summary>
        public event EventHandler<HtmlRendererRoutedEventArgs<HtmlRenderErrorEventArgs>> RenderError
        {
            add { AddHandler(RenderErrorEvent, value); }
            remove { RemoveHandler(RenderErrorEvent, value); }
        }

        /// <summary>
        /// Raised when a stylesheet is about to be loaded by file path or URI by link element.<br/>
        /// This event allows to provide the stylesheet manually or provide new source (file or uri) to load from.<br/>
        /// If no alternative data is provided the original source will be used.<br/>
        /// </summary>
        public event EventHandler<HtmlRendererRoutedEventArgs<HtmlStylesheetLoadEventArgs>> StylesheetLoad
        {
            add { AddHandler(StylesheetLoadEvent, value); }
            remove { RemoveHandler(StylesheetLoadEvent, value); }
        }

        /// <summary>
        /// Raised when an image is about to be loaded by file path or URI.<br/>
        /// This event allows to provide the image manually, if not handled the image will be loaded from file or download from URI.
        /// </summary>
        public event EventHandler<HtmlRendererRoutedEventArgs<HtmlImageLoadEventArgs>> ImageLoad
        {
            add { AddHandler(ImageLoadEvent, value); }
            remove { RemoveHandler(ImageLoadEvent, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating if image loading only when visible should be avoided (default - false).<br/>
        /// True - images are loaded as soon as the html is parsed.<br/>
        /// False - images that are not visible because of scroll location are not loaded until they are scrolled to.
        /// </summary>
        /// <remarks>
        /// Images late loading improve performance if the page contains image outside the visible scroll area, especially if there is large 
        /// amount of images, as all image loading is delayed (downloading and loading into memory).<br/>
        /// Late image loading may effect the layout and actual size as image without set size will not have actual size until they are loaded
        /// resulting in layout change during user scroll.<br/>
        /// Early image loading may also effect the layout if image without known size above the current scroll location are loaded as they
        /// will push the html elements down.
        /// </remarks>
        [Category("Behavior")]
        [Description("If image loading only when visible should be avoided")]
        public bool AvoidImagesLateLoading
        {
            get { return (bool)GetValue(AvoidImagesLateLoadingProperty); }
            set { SetValue(AvoidImagesLateLoadingProperty, value); }
        }

        /// <summary>
        /// Is content selection is enabled for the rendered html (default - true).<br/>
        /// If set to 'false' the rendered html will be static only with ability to click on links.
        /// </summary>
        [Category("Behavior")]
        [Description("Is content selection is enabled for the rendered html.")]
        public bool IsSelectionEnabled
        {
            get { return (bool)GetValue(IsSelectionEnabledProperty); }
            set { SetValue(IsSelectionEnabledProperty, value); }
        }

        /// <summary>
        /// Is the build-in context menu enabled and will be shown on mouse right click (default - true)
        /// </summary>
        [Category("Behavior")]
        [Description("Is the build-in context menu enabled and will be shown on mouse right click.")]
        public bool IsContextMenuEnabled
        {
            get { return (bool)GetValue(IsContextMenuEnabledProperty); }
            set { SetValue(IsContextMenuEnabledProperty, value); }
        }

        /// <summary>
        /// Set base stylesheet to be used by html rendered in the panel.
        /// </summary>
        [Category("Appearance")]
        [Description("Set base stylesheet to be used by html rendered in the control.")]
        public string BaseStylesheet
        {
            get { return (string)GetValue(BaseStylesheetProperty); }
            set { SetValue(BaseStylesheetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text of this panel
        /// </summary>
        [Description("Sets the html of this control.")]
        [Content]
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public Thickness BorderThickness
        {
            get { return (Thickness) GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        public IBrush BorderBrush
        {
            get { return (IBrush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }

        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        public IBrush Background
        {
            get { return (IBrush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value);}
        }

        /// <summary>
        /// Get the currently selected text segment in the html.
        /// </summary>
        [Browsable(false)]
        public virtual string SelectedText
        {
            get { return _htmlContainer.SelectedText; }
        }

        /// <summary>
        /// Copy the currently selected html segment with style.
        /// </summary>
        [Browsable(false)]
        public virtual string SelectedHtml
        {
            get { return _htmlContainer.SelectedHtml; }
        }

        /// <summary>
        /// Gets inner html container of this control.
        /// </summary>
        public HtmlContainer Container => _htmlContainer;

        /// <summary>
        /// Get html from the current DOM tree with inline style.
        /// </summary>
        /// <returns>generated html</returns>
        public virtual string GetHtml()
        {
            return _htmlContainer != null ? _htmlContainer.GetHtml() : null;
        }

        /// <summary>
        /// Get the rectangle of html element as calculated by html layout.<br/>
        /// Element if found by id (id attribute on the html element).<br/>
        /// Note: to get the screen rectangle you need to adjust by the hosting control.<br/>
        /// </summary>
        /// <param name="elementId">the id of the element to get its rectangle</param>
        /// <returns>the rectangle of the element or null if not found</returns>
        public virtual Rect? GetElementRectangle(string elementId)
        {
            return _htmlContainer != null ? _htmlContainer.GetElementRectangle(elementId) : null;
        }

        /// <summary>
        /// Clear the current selection.
        /// </summary>
        public void ClearSelection()
        {
            if (_htmlContainer != null)
                _htmlContainer.ClearSelection();
        }

        #region Private methods

        private Size RenderSize => new Size(Bounds.Width, Bounds.Height);

        /// <summary>
        /// Perform paint of the html in the control.
        /// </summary>
        public override void Render(DrawingContext context)
        {
            context.FillRectangle(Background,  new Rect(RenderSize));

            if (BorderThickness != new Thickness(0) && BorderBrush != null)
            {
                var brush = new ImmutableSolidColorBrush(Colors.Black);
                if (BorderThickness.Top > 0)
                    context.FillRectangle(brush, new Rect(0, 0, RenderSize.Width, BorderThickness.Top));
                if (BorderThickness.Bottom > 0)
                    context.FillRectangle(brush, new Rect(0, RenderSize.Height - BorderThickness.Bottom, RenderSize.Width, BorderThickness.Bottom));
                if (BorderThickness.Left > 0)
                    context.FillRectangle(brush, new Rect(0, 0, BorderThickness.Left, RenderSize.Height));
                if (BorderThickness.Right > 0)
                    context.FillRectangle(brush, new Rect(RenderSize.Width - BorderThickness.Right, 0, BorderThickness.Right, RenderSize.Height));
            }

            var htmlWidth = HtmlWidth(RenderSize);
            var htmlHeight = HtmlHeight(RenderSize);
            if (_htmlContainer != null && htmlWidth > 0 && htmlHeight > 0)
            {
                using (context.PushClip(new Rect(Padding.Left + BorderThickness.Left, Padding.Top + BorderThickness.Top,
                    htmlWidth, (int) htmlHeight)))
                {
                    _htmlContainer.Location = new Point(Padding.Left + BorderThickness.Left,
                        Padding.Top + BorderThickness.Top);
                    _htmlContainer.PerformPaint(context,
                        new Rect(Padding.Left + BorderThickness.Left, Padding.Top + BorderThickness.Top, htmlWidth,
                            htmlHeight));
                }

                if (!_lastScrollOffset.Equals(_htmlContainer.ScrollOffset))
                {
                    _lastScrollOffset = _htmlContainer.ScrollOffset;
                    InvokeMouseMove();
                }
            }
        }

        void RaiseRouted<T>(RoutedEvent ev, T arg)
        {
            var e =new HtmlRendererRoutedEventArgs<T>
            {
                Event = arg,
                Source = this,
                RoutedEvent = ev,
                Route = ev.RoutingStrategies
            };
            RaiseEvent(e);
        }

        /// <summary>
        /// Propagate the LoadComplete event from root container.
        /// </summary>
        protected virtual void OnLoadComplete(EventArgs e) => RaiseRouted(LoadCompleteEvent, e);

        /// <summary>
        /// Propagate the LinkClicked event from root container.
        /// </summary>
        protected virtual void OnLinkClicked(HtmlLinkClickedEventArgs e) => RaiseRouted(LinkClickedEvent, e);

        /// <summary>
        /// Propagate the Render Error event from root container.
        /// </summary>
        protected virtual void OnRenderError(HtmlRenderErrorEventArgs e) => RaiseRouted(RenderErrorEvent, e);

        /// <summary>
        /// Propagate the stylesheet load event from root container.
        /// </summary>
        protected virtual void OnStylesheetLoad(HtmlStylesheetLoadEventArgs e) => RaiseRouted(StylesheetLoadEvent, e);

        /// <summary>
        /// Propagate the image load event from root container.
        /// </summary>
        protected virtual void OnImageLoad(HtmlImageLoadEventArgs e) => RaiseRouted(ImageLoadEvent, e);

        /// <summary>
        /// Handle html renderer invalidate and re-layout as requested.
        /// </summary>
        protected virtual void OnRefresh(HtmlRefreshEventArgs e)
        {
            if (e.Layout)
                InvalidateMeasure();
            InvalidateVisual();
        }

        /// <summary>
        /// Get the width the HTML has to render in (not including vertical scroll iff it is visible)
        /// </summary>
        protected virtual double HtmlWidth(Size size)
        {
            return size.Width - Padding.Left - Padding.Right - BorderThickness.Left - BorderThickness.Right;
        }

        /// <summary>
        /// Get the width the HTML has to render in (not including vertical scroll iff it is visible)
        /// </summary>
        protected virtual double HtmlHeight(Size size)
        {
            return size.Height - Padding.Top - Padding.Bottom - BorderThickness.Top - BorderThickness.Bottom;
        }

        /// <summary>
        /// call mouse move to handle paint after scroll or html change affecting mouse cursor.
        /// </summary>
        protected virtual void InvokeMouseMove()
        {
            if (LastPointerArgs != null)
            {
                _htmlContainer.HandleMouseMove(this, LastPointerArgs.GetPosition(this));
            }
        }

        /// <summary>
        /// Handle when dependency property value changes to update the underline HtmlContainer with the new value.
        /// </summary>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            var htmlContainer = _htmlContainer;
            if (e.Property == AvoidImagesLateLoadingProperty)
            {
                htmlContainer.AvoidImagesLateLoading = (bool)e.NewValue;
            }
            else if (e.Property == IsSelectionEnabledProperty)
            {
                htmlContainer.IsSelectionEnabled = (bool)e.NewValue;
            }
            else if (e.Property == IsContextMenuEnabledProperty)
            {
                htmlContainer.IsContextMenuEnabled = (bool)e.NewValue;
            }
            else if (e.Property == BaseStylesheetProperty)
            {
                var baseCssData = HtmlRender.ParseStyleSheet((string)e.NewValue);
                _baseCssData = baseCssData;
                htmlContainer.SetHtml(Text, baseCssData);
            }
            else if (e.Property == TextProperty)
            {
                htmlContainer.ScrollOffset = new Point(0, 0);
                htmlContainer.SetHtml((string)e.NewValue, _baseCssData);
                InvalidateMeasure();
                InvalidateVisual();

                if (VisualRoot != null)
                {
                    InvokeMouseMove();
                }
            }
        }


        #region Private event handlers

        private void OnLoadComplete(object sender, EventArgs e)
        {

            if (CheckAccess())
                OnLoadComplete(e);
            else
                Dispatcher.UIThread.InvokeAsync(() => OnLoadComplete(e)).Wait();

        }

        private void OnLinkClicked(object sender, HtmlLinkClickedEventArgs e)
        {
            if (CheckAccess())
                OnLinkClicked(e);
            else
                Dispatcher.UIThread.InvokeAsync(() => OnLinkClicked(e)).Wait();
        }

        private void OnRenderError(object sender, HtmlRenderErrorEventArgs e)
        {
            if (CheckAccess())
                OnRenderError(e);
            else
                Dispatcher.UIThread.InvokeAsync(() => OnRenderError(e)).Wait();
        }

        private void OnStylesheetLoad(object sender, HtmlStylesheetLoadEventArgs e)
        {
            if (CheckAccess())
                OnStylesheetLoad(e);
            else
                Dispatcher.UIThread.InvokeAsync(() => OnStylesheetLoad(e)).Wait();
        }

        private void OnImageLoad(object sender, HtmlImageLoadEventArgs e)
        {
            if (CheckAccess())
                OnImageLoad(e);
            else
                Dispatcher.UIThread.InvokeAsync(() => OnImageLoad(e)).Wait();
        }

        private void OnRefresh(object sender, HtmlRefreshEventArgs e)
        {
            if (CheckAccess())
                // Visual invalidation should always go on the dispatcher.
                Dispatcher.UIThread.InvokeAsync(() => OnRefresh(e));
            else
                Dispatcher.UIThread.InvokeAsync(() => OnRefresh(e)).Wait();
        }

        #endregion


        #endregion
    }
}
