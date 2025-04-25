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
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Core.Entities;
using TheArtOfDev.HtmlRenderer.Core.Handlers;
using TheArtOfDev.HtmlRenderer.Core.Utils;

namespace TheArtOfDev.HtmlRenderer.Core.Dom
{
    /// <summary>
    /// CSS box for iframe element.<br/>
    /// If the iframe is of embedded YouTube or Vimeo video it will show image with play.
    /// </summary>
    internal sealed class CssBoxFrame : CssBox
    {
        #region Fields and Consts

        /// <summary>
        /// the image word of this image box
        /// </summary>
        private readonly CssRectImage _imageWord;

        /// <summary>
        /// is the iframe is of embeded
        /// </summary>
        private readonly bool _hasEmbed;

        /// <summary>
        /// the title of the video
        /// </summary>
        private string _videoTitle;

        /// <summary>
        /// the url of the video thumbnail image
        /// </summary>
        private string _videoImageUrl;

        /// <summary>
        /// link to the video on the site
        /// </summary>
        private string _videoLinkUrl;

        /// <summary>
        /// handler used for image loading by source
        /// </summary>
        private ImageLoadHandler _imageLoadHandler;

        /// <summary>
        /// is image load is finished, used to know if no image is found
        /// </summary>
        private bool _imageLoadingComplete;

        // Known oEmbed providers
        private static readonly Dictionary<string, string> OEmbedProviders = new()
        {
            { "youtube.com", "https://www.youtube.com/oembed?url={0}&format=json" },
            { "youtu.be", "https://www.youtube.com/oembed?url={0}&format=json" },
            { "twitter.com", "https://publish.twitter.com/oembed?url={0}&format=json" },
            { "x.com", "https://publish.twitter.com/oembed?url={0}&format=json" },
            { "instagram.com", "https://api.instagram.com/oembed?url={0}&format=json" },
            { "reddit.com", "https://www.reddit.com/oembed?url={0}&format=json" },
            { "bsky.app", "https://bsky.app/oembed?url={0}&format=json" },
            { "threads.net", "https://www.threads.net/oembed?url={0}&format=json" },
            { "tiktok.com", "https://www.tiktok.com/oembed?url={0}" }
        };

        #endregion


        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="parent">the parent box of this box</param>
        /// <param name="tag">the html tag data of this box</param>
        public CssBoxFrame(CssBox parent, HtmlTag tag)
            : base(parent, tag)
        {
            _imageWord = new CssRectImage(this);
            Words.Add(_imageWord);

            if (Uri.TryCreate(GetAttribute("src"), UriKind.Absolute, out var uri))
            {
                _hasEmbed = true;
                _ = LoadEmbedDataInternalAsync(uri);
            }

            if (!_hasEmbed)
            {
                SetErrorBorder();
            }
        }

        /// <summary>
        /// Is the css box clickable ("a" element is clickable)
        /// </summary>
        public override bool IsClickable
        {
            get { return true; }
        }

        /// <summary>
        /// Get the href link of the box (by default get "href" attribute)
        /// </summary>
        public override string HrefLink
        {
            get { return _videoLinkUrl ?? GetAttribute("src"); }
        }

        /// <summary>
        /// is the iframe is of embeded video
        /// </summary>
        public bool IsEmbed
        {
            get { return _hasEmbed; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (_imageLoadHandler != null)
                _imageLoadHandler.Dispose();
            base.Dispose();
        }


        #region Private methods

        /// <summary>
        /// Internal async method to load oEmbed data
        /// </summary>
        private async Task LoadEmbedDataInternalAsync(Uri uri)
        {
            try
            {
                // Try to get oEmbed endpoint
                string oembedEndpoint = await DiscoverOEmbedEndpointAsync(uri);

                if (string.IsNullOrEmpty(oembedEndpoint))
                {
                    // Try to use known provider endpoints
                    oembedEndpoint = GetKnownProviderEndpoint(uri);

                    if (string.IsNullOrEmpty(oembedEndpoint))
                    {
                        _imageLoadingComplete = true;
                        SetErrorBorder();
                        HtmlContainer.ReportError(HtmlRenderErrorType.Iframe, "No oEmbed endpoint found for: " + uri,
                            null);
                        HtmlContainer.RequestRefresh(false);
                        return;
                    }
                }

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "HtmlRenderer");

                // Properly await the async call
                string response = await client.GetStringAsync(oembedEndpoint);
                ProcessOEmbedResponse(response);
            }
            catch (Exception ex)
            {
                _imageLoadingComplete = true;
                SetErrorBorder();
                HtmlContainer.ReportError(HtmlRenderErrorType.Iframe, "Failed to get oEmbed data: " + uri, ex);
                HtmlContainer.RequestRefresh(false);
            }
        }

        /// <summary>
        /// Discovers oEmbed endpoint by checking the HTML page's link tags
        /// </summary>
        private async Task<string> DiscoverOEmbedEndpointAsync(Uri uri)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "HtmlRenderer");

                // Set a timeout to avoid hanging
                client.Timeout = TimeSpan.FromSeconds(5);

                string html = await client.GetStringAsync(uri);

                // Look for oEmbed link in the HTML
                var match = Regex.Match(html,
                    @"<link\s+[^>]*rel\s*=\s*[""']alternate[""'][^>]*type\s*=\s*[""']application/json\+oembed[""'][^>]*href\s*=\s*[""']([^""']+)[""'][^>]*>|" +
                    @"<link\s+[^>]*type\s*=\s*[""']application/json\+oembed[""'][^>]*rel\s*=\s*[""']alternate[""'][^>]*href\s*=\s*[""']([^""']+)[""'][^>]*>");

                if (match.Success)
                {
                    return match.Groups[1].Value.Length > 0 ? match.Groups[1].Value : match.Groups[2].Value;
                }

                return null;
            }
            catch
            {
                // If discovery fails, we'll fall back to known providers
                return null;
            }
        }

        /// <summary>
        /// Paints the fragment
        /// </summary>
        /// <param name="g">the device to draw to</param>
        protected override void PaintImp(RGraphics g)
        {
            if (_videoImageUrl != null && _imageLoadHandler == null)
            {
                _imageLoadHandler = new ImageLoadHandler(HtmlContainer, OnLoadImageComplete);
                _imageLoadHandler.LoadImage(_videoImageUrl, HtmlTag != null ? HtmlTag.Attributes : null);
            }

            var rects = CommonUtils.GetFirstValueOrDefault(Rectangles);

            RPoint offset = (HtmlContainer != null && !IsFixed) ? HtmlContainer.ScrollOffset : RPoint.Empty;
            rects.Offset(offset);

            var clipped = RenderUtils.ClipGraphicsByOverflow(g, this);

            PaintBackground(g, rects, true, true);

            BordersDrawHandler.DrawBoxBorders(g, this, rects, true, true);

            var word = Words[0];
            var tmpRect = word.Rectangle;
            tmpRect.Offset(offset);
            tmpRect.Height -= ActualBorderTopWidth + ActualBorderBottomWidth + ActualPaddingTop + ActualPaddingBottom;
            tmpRect.Y += ActualBorderTopWidth + ActualPaddingTop;
            tmpRect.X = Math.Floor(tmpRect.X);
            tmpRect.Y = Math.Floor(tmpRect.Y);
            var rect = tmpRect;

            DrawImage(g, offset, rect);

            DrawTitle(g, rect);

            DrawPlay(g, rect);

            if (clipped)
                g.PopClip();
        }

        /// <summary>
        /// Draw video image over the iframe if found.
        /// </summary>
        private void DrawImage(RGraphics g, RPoint offset, RRect rect)
        {
            if (_imageWord.Image != null)
            {
                if (rect.Width > 0 && rect.Height > 0)
                {
                    if (_imageWord.ImageRectangle == RRect.Empty)
                        g.DrawImage(_imageWord.Image, rect);
                    else
                        g.DrawImage(_imageWord.Image, rect, _imageWord.ImageRectangle);

                    if (_imageWord.Selected)
                    {
                        g.DrawRectangle(GetSelectionBackBrush(g, true), _imageWord.Left + offset.X, _imageWord.Top + offset.Y, _imageWord.Width + 2, DomUtils.GetCssLineBoxByWord(_imageWord).LineHeight);
                    }
                }
            }
            else if (_hasEmbed && !_imageLoadingComplete)
            {
                RenderUtils.DrawImageLoadingIcon(g, HtmlContainer, rect);
                if (rect.Width > 19 && rect.Height > 19)
                {
                    g.DrawRectangle(g.GetPen(RColor.LightGray), rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }

        /// <summary>
        /// Draw video title on top of the iframe if found.
        /// </summary>
        private void DrawTitle(RGraphics g, RRect rect)
        {
            if (_videoTitle != null && _imageWord.Width > 40 && _imageWord.Height > 40)
            {
                var font = HtmlContainer.Adapter.GetFont("Arial", 9f, RFontStyle.Regular);
                g.DrawRectangle(g.GetSolidBrush(RColor.FromArgb(160, 0, 0, 0)), rect.Left, rect.Top, rect.Width, ActualFont.Height + 7);

                var titleRect = new RRect(rect.Left + 3, rect.Top + 3, rect.Width - 6, rect.Height - 6);
                g.DrawString(_videoTitle, font, RColor.WhiteSmoke, titleRect.Location, RSize.Empty, false);
            }
        }

        /// <summary>
        /// Draw play over the iframe if we found link url.
        /// </summary>
        private void DrawPlay(RGraphics g, RRect rect)
        {
            if (_hasEmbed && _imageWord.Width > 70 && _imageWord.Height > 50)
            {
                var prevMode = g.SetAntiAliasSmoothingMode();

                var size = new RSize(60, 40);
                var left = rect.Left + (rect.Width - size.Width) / 2;
                var top = rect.Top + (rect.Height - size.Height) / 2;
                g.DrawRectangle(g.GetSolidBrush(RColor.FromArgb(160, 0, 0, 0)), left, top, size.Width, size.Height);

                RPoint[] points =
                {
                    new RPoint(left + size.Width / 3f + 1,top + 3 * size.Height / 4f),
                    new RPoint(left + size.Width / 3f + 1, top + size.Height / 4f),
                    new RPoint(left + 2 * size.Width / 3f + 1, top + size.Height / 2f)
                };
                g.DrawPolygon(g.GetSolidBrush(RColor.White), points);
                
                g.ReturnPreviousSmoothingMode(prevMode);
            }
        }

        /// <summary>
        /// Gets oEmbed endpoint for known providers
        /// </summary>
        private string GetKnownProviderEndpoint(Uri uri)
        {
            string host = uri.Host.ToLower();

            // Remove www. prefix if present
            if (host.StartsWith("www."))
                host = host.Substring(4);

            // Special handling for YouTube embed links
            if ((host == "youtube.com" || host == "www.youtube.com") && 
                uri.AbsolutePath.StartsWith("/embed/"))
            {
                // Extract video ID from /embed/VIDEO_ID path
                var videoId = uri.AbsolutePath.Substring("/embed/".Length);
        
                // Create a watch URL instead
                string watchUrl = $"https://www.youtube.com/watch?v={videoId}";
        
                return string.Format(OEmbedProviders["youtube.com"], Uri.EscapeDataString(watchUrl));
            }

            // Check if we have a direct match for the host
            if (OEmbedProviders.TryGetValue(host, out string endpoint))
            {
                return string.Format(endpoint, Uri.EscapeDataString(uri.ToString()));
            }

            // Check for partial matches (e.g., youtu.be is youtube)
            foreach (var provider in OEmbedProviders)
            {
                if (host.Contains(provider.Key) || provider.Key.Contains(host))
                {
                    return string.Format(provider.Value, Uri.EscapeDataString(uri.ToString()));
                }
            }

            return null;
        }

        /// <summary>
        /// Process the oEmbed response to extract video information
        /// </summary>
        private void ProcessOEmbedResponse(string response)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(response);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("type", out var typeElement) &&
                    (typeElement.GetString() == "video" || typeElement.GetString() == "rich"))
                {
                    // Extract title
                    if (root.TryGetProperty("title", out var titleElement))
                        _videoTitle = titleElement.GetString();

                    // Extract thumbnail URL
                    if (root.TryGetProperty("thumbnail_url", out var thumbnailElement))
                        _videoImageUrl = thumbnailElement.GetString();

                    // Extract provider URL or original URL
                    if (root.TryGetProperty("provider_url", out var providerElement))
                        _videoLinkUrl = providerElement.GetString();
                    else if (root.TryGetProperty("url", out var urlElement))
                        _videoLinkUrl = urlElement.GetString();

                    // Load the thumbnail image
                    if (!string.IsNullOrEmpty(_videoImageUrl))
                    {
                        LoadImageAsync(_videoImageUrl);
                    }
                    else
                    {
                        _imageLoadingComplete = true;
                        SetErrorBorder();
                        HtmlContainer.RequestRefresh(false);
                    }
                }
                else
                {
                    _imageLoadingComplete = true;
                    SetErrorBorder();
                    HtmlContainer.RequestRefresh(false);
                }
            }
            catch (Exception ex)
            {
                _imageLoadingComplete = true;
                SetErrorBorder();
                HtmlContainer.ReportError(HtmlRenderErrorType.Iframe, "Failed to parse oEmbed response", ex);
                HtmlContainer.RequestRefresh(false);
            }
        }

        /// <summary>
        /// Asynchronously load image from the given source.
        /// </summary>
        private void LoadImageAsync(string source)
        {
            _imageLoadHandler = new ImageLoadHandler(HtmlContainer, OnLoadImageComplete);
            _imageLoadHandler.LoadImage(source, HtmlTag != null ? HtmlTag.Attributes : null);
        }

        /// <summary>
        /// Assigns words its width and height
        /// </summary>
        /// <param name="g">the device to use</param>
        internal override void MeasureWordsSize(RGraphics g)
        {
            if (!_wordsSizeMeasured)
            {
                MeasureWordSpacing(g);
                _wordsSizeMeasured = true;
            }
            CssLayoutEngine.MeasureImageSize(_imageWord);
        }

        /// <summary>
        /// Set error image border on the image box.
        /// </summary>
        private void SetErrorBorder()
        {
            SetAllBorders(CssConstants.Solid, "2px", "#A0A0A0");
            BorderRightColor = BorderBottomColor = "#E3E3E3";
        }

        /// <summary>
        /// On image load process is complete with image or without update the image box.
        /// </summary>
        /// <param name="image">the image loaded or null if failed</param>
        /// <param name="rectangle">the source rectangle to draw in the image (empty - draw everything)</param>
        /// <param name="async">is the callback was called async to load image call</param>
        private void OnLoadImageComplete(RImage image, RRect rectangle, bool async)
        {
            _imageWord.Image = image;
            _imageWord.ImageRectangle = rectangle;
            _imageLoadingComplete = true;
            _wordsSizeMeasured = false;

            if (_imageLoadingComplete && image == null)
            {
                SetErrorBorder();
            }

            if (async)
            {
                HtmlContainer.RequestRefresh(IsLayoutRequired());
            }
        }

        private bool IsLayoutRequired()
        {
            var width = new CssLength(Width);
            var height = new CssLength(Height);
            return (width.Number <= 0 || width.Unit != CssUnit.Pixels) || (height.Number <= 0 || height.Unit != CssUnit.Pixels);
        }

        #endregion
    }
}