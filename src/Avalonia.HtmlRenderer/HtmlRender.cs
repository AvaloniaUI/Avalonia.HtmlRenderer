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
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TheArtOfDev.HtmlRenderer.Core;
using TheArtOfDev.HtmlRenderer.Core.Entities;
using TheArtOfDev.HtmlRenderer.Core.Utils;
using TheArtOfDev.HtmlRenderer.Avalonia.Adapters;
using TheArtOfDev.HtmlRenderer.Avalonia.Utilities;

namespace TheArtOfDev.HtmlRenderer.Avalonia
{
    /// <summary>
    /// Standalone static class for simple and direct HTML rendering.<br/>
    /// For Avalonia UI prefer using HTML controls: <see cref="HtmlPanel"/> or <see cref="HtmlLabel"/>.<br/>
    /// For low-level control and performance consider using <see cref="HtmlContainer"/>.<br/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Rendering to image</b><br/>
    /// // TODO:a update!
    /// See https://htmlrenderer.codeplex.com/wikipage?title=Image%20generation <br/>
    /// Because of GDI text rendering issue with alpha channel clear type text rendering rendering to image requires special handling.<br/>
    /// <u>Solid color background -</u> generate an image where the background is filled with solid color and all the html is rendered on top
    /// of the background color, GDI text rendering will be used. (RenderToImage method where the first argument is html string)<br/>
    /// <u>Image background -</u> render html on top of existing image with whatever currently exist but it cannot have transparent pixels, 
    /// GDI text rendering will be used. (RenderToImage method where the first argument is Image object)<br/>
    /// <u>Transparent background -</u> render html to empty image using GDI+ text rendering, the generated image can be transparent.
    /// </para>
    /// <para>
    /// <b>Overwrite stylesheet resolution</b><br/>
    /// Exposed by optional "stylesheetLoad" delegate argument.<br/>
    /// Invoked when a stylesheet is about to be loaded by file path or URL in 'link' element.<br/>
    /// Allows to overwrite the loaded stylesheet by providing the stylesheet data manually, or different source (file or URL) to load from.<br/>
    /// Example: The stylesheet 'href' can be non-valid URI string that is interpreted in the overwrite delegate by custom logic to pre-loaded stylesheet object<br/>
    /// If no alternative data is provided the original source will be used.<br/>
    /// </para>
    /// <para>
    /// <b>Overwrite image resolution</b><br/>
    /// Exposed by optional "imageLoad" delegate argument.<br/>
    /// Invoked when an image is about to be loaded by file path, URL or inline data in 'img' element or background-image CSS style.<br/>
    /// Allows to overwrite the loaded image by providing the image object manually, or different source (file or URL) to load from.<br/>
    /// Example: image 'src' can be non-valid string that is interpreted in the overwrite delegate by custom logic to resource image object<br/>
    /// Example: image 'src' in the html is relative - the overwrite intercepts the load and provide full source URL to load the image from<br/>
    /// Example: image download requires authentication - the overwrite intercepts the load, downloads the image to disk using custom code and provide 
    /// file path to load the image from.<br/>
    /// If no alternative data is provided the original source will be used.<br/>
    /// Note: Cannot use asynchronous scheme overwrite scheme.<br/>
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>
    /// <b>Simple rendering</b><br/>
    /// HtmlRender.Render(g, "<![CDATA[<div>Hello <b>World</b></div>]]>");<br/>
    /// HtmlRender.Render(g, "<![CDATA[<div>Hello <b>World</b></div>]]>", 10, 10, 500, CssData.Parse("body {font-size: 20px}")");<br/>
    /// </para>
    /// <para>
    /// <b>Image rendering</b><br/>
    /// HtmlRender.RenderToImage("<![CDATA[<div>Hello <b>World</b></div>]]>", new Size(600,400));<br/>
    /// HtmlRender.RenderToImage("<![CDATA[<div>Hello <b>World</b></div>]]>", 600);<br/>
    /// HtmlRender.RenderToImage(existingImage, "<![CDATA[<div>Hello <b>World</b></div>]]>");<br/>
    /// </para>
    /// </example>
    public static class HtmlRender
    {
        /// <summary>
        /// Parse the given stylesheet to <see cref="CssData"/> object.<br/>
        /// If <paramref name="combineWithDefault"/> is true the parsed css blocks are added to the 
        /// default css data (as defined by W3), merged if class name already exists. If false only the data in the given stylesheet is returned.
        /// </summary>
        /// <seealso cref="http://www.w3.org/TR/CSS21/sample.html"/>
        /// <param name="stylesheet">the stylesheet source to parse</param>
        /// <param name="combineWithDefault">true - combine the parsed css data with default css data, false - return only the parsed css data</param>
        /// <returns>the parsed css data</returns>
        public static CssData ParseStyleSheet(string stylesheet, bool combineWithDefault = true)
        {
            return CssData.Parse(new AvaloniaAdapter(null), stylesheet, combineWithDefault);
        }

        /// <summary>
        /// Measure the size (width and height) required to draw the given html under given max width restriction.<br/>
        /// If no max width restriction is given the layout will use the maximum possible width required by the content,
        /// it can be the longest text line or full image width.<br/>
        /// </summary>
        /// <param name="html">HTML source to render</param>
        /// <param name="maxWidth">optional: bound the width of the html to render in (default - 0, unlimited)</param>
        /// <param name="cssData">optional: the style to use for html rendering (default - use W3 default style)</param>
        /// <param name="stylesheetLoad">optional: can be used to overwrite stylesheet resolution logic</param>
        /// <param name="imageLoad">optional: can be used to overwrite image resolution logic</param>
        /// <returns>the size required for the html</returns>
        public static Size Measure(string html, double maxWidth = 0, CssData cssData = null,
            EventHandler<HtmlStylesheetLoadEventArgs> stylesheetLoad = null, EventHandler<HtmlImageLoadEventArgs> imageLoad = null)
        {
            Size actualSize = default;
            if (!string.IsNullOrEmpty(html))
            {
                using (var container = new HtmlContainer(null))
                {
                    container.MaxSize = new Size(maxWidth, 0);
                    container.AvoidAsyncImagesLoading = true;
                    container.AvoidImagesLateLoading = true;

                    if (stylesheetLoad != null)
                        container.StylesheetLoad += stylesheetLoad;
                    if (imageLoad != null)
                        container.ImageLoad += imageLoad;

                    container.SetHtml(html, cssData);
                    container.PerformLayout();

                    actualSize = container.ActualSize;
                }
            }
            return actualSize;
        }

        /// <summary>
        /// Renders the specified HTML source on the specified location and max width restriction.<br/>
        /// If <paramref name="maxWidth"/> is zero the html will use all the required width, otherwise it will perform line 
        /// wrap as specified in the html<br/>
        /// Returned is the actual width and height of the rendered html.<br/>
        /// </summary>
        /// <param name="g">Device to render with</param>
        /// <param name="html">HTML source to render</param>
        /// <param name="left">optional: the left most location to start render the html at (default - 0)</param>
        /// <param name="top">optional: the top most location to start render the html at (default - 0)</param>
        /// <param name="maxWidth">optional: bound the width of the html to render in (default - 0, unlimited)</param>
        /// <param name="cssData">optional: the style to use for html rendering (default - use W3 default style)</param>
        /// <param name="stylesheetLoad">optional: can be used to overwrite stylesheet resolution logic</param>
        /// <param name="imageLoad">optional: can be used to overwrite image resolution logic</param>
        /// <returns>the actual size of the rendered html</returns>
        public static Size Render(DrawingContext g, string html, double left = 0, double top = 0, double maxWidth = 0, CssData cssData = null,
            EventHandler<HtmlStylesheetLoadEventArgs> stylesheetLoad = null, EventHandler<HtmlImageLoadEventArgs> imageLoad = null)
        {
            ArgChecker.AssertArgNotNull(g, "g");
            return RenderClip(g, html, new Point(left, top), new Size(maxWidth, 0), cssData, stylesheetLoad, imageLoad);
        }

        /// <summary>
        /// Renders the specified HTML source on the specified location and max size restriction.<br/>
        /// If <paramref name="maxSize"/>.Width is zero the html will use all the required width, otherwise it will perform line 
        /// wrap as specified in the html<br/>
        /// If <paramref name="maxSize"/>.Height is zero the html will use all the required height, otherwise it will clip at the
        /// given max height not rendering the html below it.<br/>
        /// Returned is the actual width and height of the rendered html.<br/>
        /// </summary>
        /// <param name="g">Device to render with</param>
        /// <param name="html">HTML source to render</param>
        /// <param name="location">the top-left most location to start render the html at</param>
        /// <param name="maxSize">the max size of the rendered html (if height above zero it will be clipped)</param>
        /// <param name="cssData">optional: the style to use for html rendering (default - use W3 default style)</param>
        /// <param name="stylesheetLoad">optional: can be used to overwrite stylesheet resolution logic</param>
        /// <param name="imageLoad">optional: can be used to overwrite image resolution logic</param>
        /// <returns>the actual size of the rendered html</returns>
        public static Size Render(DrawingContext g, string html, Point location, Size maxSize, CssData cssData = null,
            EventHandler<HtmlStylesheetLoadEventArgs> stylesheetLoad = null, EventHandler<HtmlImageLoadEventArgs> imageLoad = null)
        {
            ArgChecker.AssertArgNotNull(g, "g");
            return RenderClip(g, html, location, maxSize, cssData, stylesheetLoad, imageLoad);
        }

        /// <summary>
        /// Renders the specified HTML into a new image of the requested size.<br/>
        /// The HTML will be layout by the given size but will be clipped if cannot fit.<br/>
        /// </summary>
        /// <param name="html">HTML source to render</param>
        /// <param name="size">The size of the image to render into, layout html by width and clipped by height</param>
        /// <param name="cssData">optional: the style to use for html rendering (default - use W3 default style)</param>
        /// <param name="stylesheetLoad">optional: can be used to overwrite stylesheet resolution logic</param>
        /// <param name="imageLoad">optional: can be used to overwrite image resolution logic</param>
        /// <returns>the generated image of the html</returns>
        public static Bitmap RenderToImage(string html, Size size, CssData cssData = null,
            EventHandler<HtmlStylesheetLoadEventArgs> stylesheetLoad = null, EventHandler<HtmlImageLoadEventArgs> imageLoad = null)
        {
            var renderTarget = new RenderTargetBitmap(new PixelSize((int)size.Width, (int)size.Height), new Vector(96, 96));

            if (!string.IsNullOrEmpty(html))
            {
                // render HTML into the visual
                using (var context = renderTarget.CreateDrawingContext())
                {
                    RenderHtml(context, html, new Point(), size, cssData, stylesheetLoad, imageLoad);
                }
            }

            return renderTarget;
        }

        /// <summary>
        /// Renders the specified HTML into a new image of unknown size that will be determined by max width/height and HTML layout.<br/>
        /// If <paramref name="maxWidth"/> is zero the html will use all the required width, otherwise it will perform line 
        /// wrap as specified in the html<br/>
        /// If <paramref name="maxHeight"/> is zero the html will use all the required height, otherwise it will clip at the
        /// given max height not rendering the html below it.<br/>
        /// <p>
        /// Limitation: The image cannot have transparent background, by default it will be white.<br/>
        /// See "Rendering to image" remarks section on <see cref="HtmlRender"/>.<br/>
        /// </p>
        /// </summary>
        /// <param name="html">HTML source to render</param>
        /// <param name="maxWidth">optional: the max width of the rendered html, if not zero and html cannot be layout within the limit it will be clipped</param>
        /// <param name="maxHeight">optional: the max height of the rendered html, if not zero and html cannot be layout within the limit it will be clipped</param>
        /// <param name="backgroundColor">optional: the color to fill the image with (default - white)</param>
        /// <param name="cssData">optional: the style to use for html rendering (default - use W3 default style)</param>
        /// <param name="stylesheetLoad">optional: can be used to overwrite stylesheet resolution logic</param>
        /// <param name="imageLoad">optional: can be used to overwrite image resolution logic</param>
        /// <returns>the generated image of the html</returns>
        public static Bitmap RenderToImage(string html, int maxWidth = 0, int maxHeight = 0, Color backgroundColor = new Color(), CssData cssData = null,
            EventHandler<HtmlStylesheetLoadEventArgs> stylesheetLoad = null, EventHandler<HtmlImageLoadEventArgs> imageLoad = null)
        {
            return RenderToImage(html, default, new Size(maxWidth, maxHeight), backgroundColor, cssData, stylesheetLoad, imageLoad);
        }

        /// <summary>
        /// Renders the specified HTML into a new image of unknown size that will be determined by min/max width/height and HTML layout.<br/>
        /// If <paramref name="maxSize.Width"/> is zero the html will use all the required width, otherwise it will perform line 
        /// wrap as specified in the html<br/>
        /// If <paramref name="maxSize.Height"/> is zero the html will use all the required height, otherwise it will clip at the
        /// given max height not rendering the html below it.<br/>
        /// If <paramref name="minSize"/> (Width/Height) is above zero the rendered image will not be smaller than the given min size.<br/>
        /// <p>
        /// Limitation: The image cannot have transparent background, by default it will be white.<br/>
        /// See "Rendering to image" remarks section on <see cref="HtmlRender"/>.<br/>
        /// </p>
        /// </summary>
        /// <param name="html">HTML source to render</param>
        /// <param name="minSize">optional: the min size of the rendered html (zero - not limit the width/height)</param>
        /// <param name="maxSize">optional: the max size of the rendered html, if not zero and html cannot be layout within the limit it will be clipped (zero - not limit the width/height)</param>
        /// <param name="backgroundColor">optional: the color to fill the image with (default - white)</param>
        /// <param name="cssData">optional: the style to use for html rendering (default - use W3 default style)</param>
        /// <param name="stylesheetLoad">optional: can be used to overwrite stylesheet resolution logic</param>
        /// <param name="imageLoad">optional: can be used to overwrite image resolution logic</param>
        /// <returns>the generated image of the html</returns>
        public static Bitmap RenderToImage(string html, Size minSize, Size maxSize, Color backgroundColor = new Color(), CssData cssData = null,
            EventHandler<HtmlStylesheetLoadEventArgs> stylesheetLoad = null, EventHandler<HtmlImageLoadEventArgs> imageLoad = null)
        {
            RenderTargetBitmap renderTarget;
            if (!string.IsNullOrEmpty(html))
            {
                using (var container = new HtmlContainer(null))
                {
                    container.AvoidAsyncImagesLoading = true;
                    container.AvoidImagesLateLoading = true;

                    if (stylesheetLoad != null)
                        container.StylesheetLoad += stylesheetLoad;
                    if (imageLoad != null)
                        container.ImageLoad += imageLoad;
                    container.SetHtml(html, cssData);

                    var finalSize = MeasureHtmlByRestrictions(container, minSize, maxSize);
                    container.MaxSize = finalSize;

                    renderTarget = new RenderTargetBitmap(new PixelSize((int)finalSize.Width, (int)finalSize.Height), new Vector(96, 96));

                    // render HTML into the visual
                    using (var context = renderTarget.CreateDrawingContext())
                    {
                        container.PerformPaint(context, new Rect(new Size(maxSize.Width > 0 ? maxSize.Width : double.MaxValue, maxSize.Height > 0 ? maxSize.Height : double.MaxValue)));
                    }
                }
            }
            else
            {
                renderTarget = new RenderTargetBitmap(new PixelSize(0, 0));
            }

            return renderTarget;
        }


        #region Private methods

        /// <summary>
        /// Measure the size of the html by performing layout under the given restrictions.
        /// </summary>
        /// <param name="htmlContainer">the html to calculate the layout for</param>
        /// <param name="minSize">the minimal size of the rendered html (zero - not limit the width/height)</param>
        /// <param name="maxSize">the maximum size of the rendered html, if not zero and html cannot be layout within the limit it will be clipped (zero - not limit the width/height)</param>
        /// <returns>return: the size of the html to be rendered within the min/max limits</returns>
        private static Size MeasureHtmlByRestrictions(HtmlContainer htmlContainer, Size minSize, Size maxSize)
        {
            // use desktop created graphics to measure the HTML
            using (var mg = new GraphicsAdapter(htmlContainer.AvaloniaAdapter))
            {
                var sizeInt = HtmlRendererUtils.MeasureHtmlByRestrictions(mg, htmlContainer.HtmlContainerInt, Utils.Convert(minSize), Utils.Convert(maxSize));
                if (maxSize.Width < 1 && sizeInt.Width > 4096)
                    sizeInt.Width = 4096;
                return Utils.ConvertRound(sizeInt);
            }
        }

        /// <summary>
        /// Renders the specified HTML source on the specified location and max size restriction.<br/>
        /// If <paramref name="maxSize"/>.Width is zero the html will use all the required width, otherwise it will perform line 
        /// wrap as specified in the html<br/>
        /// If <paramref name="maxSize"/>.Height is zero the html will use all the required height, otherwise it will clip at the
        /// given max height not rendering the html below it.<br/>
        /// Clip the graphics so the html will not be rendered outside the max height bound given.<br/>
        /// Returned is the actual width and height of the rendered html.<br/>
        /// </summary>
        /// <param name="g">Device to render with</param>
        /// <param name="html">HTML source to render</param>
        /// <param name="location">the top-left most location to start render the html at</param>
        /// <param name="maxSize">the max size of the rendered html (if height above zero it will be clipped)</param>
        /// <param name="cssData">optional: the style to use for html rendering (default - use W3 default style)</param>
        /// <param name="stylesheetLoad">optional: can be used to overwrite stylesheet resolution logic</param>
        /// <param name="imageLoad">optional: can be used to overwrite image resolution logic</param>
        /// <returns>the actual size of the rendered html</returns>
        private static Size RenderClip(DrawingContext g, string html, Point location, Size maxSize, CssData cssData, EventHandler<HtmlStylesheetLoadEventArgs> stylesheetLoad, EventHandler<HtmlImageLoadEventArgs> imageLoad)
        {
            DrawingContext.PushedState? state = null;
            if (maxSize.Height > 0)
                state = g.PushClip(new Rect(location, maxSize));

            var actualSize = RenderHtml(g, html, location, maxSize, cssData, stylesheetLoad, imageLoad);

            if (maxSize.Height > 0)
                state?.Dispose();

            return actualSize;
        }

        /// <summary>
        /// Renders the specified HTML source on the specified location and max size restriction.<br/>
        /// If <paramref name="maxSize"/>.Width is zero the html will use all the required width, otherwise it will perform line 
        /// wrap as specified in the html<br/>
        /// If <paramref name="maxSize"/>.Height is zero the html will use all the required height, otherwise it will clip at the
        /// given max height not rendering the html below it.<br/>
        /// Returned is the actual width and height of the rendered html.<br/>
        /// </summary>
        /// <param name="g">Device to render with</param>
        /// <param name="html">HTML source to render</param>
        /// <param name="location">the top-left most location to start render the html at</param>
        /// <param name="maxSize">the max size of the rendered html (if height above zero it will be clipped)</param>
        /// <param name="cssData">optional: the style to use for html rendering (default - use W3 default style)</param>
        /// <param name="stylesheetLoad">optional: can be used to overwrite stylesheet resolution logic</param>
        /// <param name="imageLoad">optional: can be used to overwrite image resolution logic</param>
        /// <returns>the actual size of the rendered html</returns>
        private static Size RenderHtml(DrawingContext g, string html, Point location, Size maxSize, CssData cssData, EventHandler<HtmlStylesheetLoadEventArgs> stylesheetLoad, EventHandler<HtmlImageLoadEventArgs> imageLoad)
        {
            Size actualSize = default;

            if (!string.IsNullOrEmpty(html))
            {
                using (var container = new HtmlContainer(null))
                {
                    container.Location = location;
                    container.MaxSize = maxSize;
                    container.AvoidAsyncImagesLoading = true;
                    container.AvoidImagesLateLoading = true;

                    if (stylesheetLoad != null)
                        container.StylesheetLoad += stylesheetLoad;
                    if (imageLoad != null)
                        container.ImageLoad += imageLoad;

                    container.SetHtml(html, cssData);
                    container.PerformLayout();
                    container.PerformPaint(g, new Rect(0, 0, double.MaxValue, double.MaxValue));

                    actualSize = container.ActualSize;
                }
            }

            return actualSize;
        }

        #endregion
    }
}