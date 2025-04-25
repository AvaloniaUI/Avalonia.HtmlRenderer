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
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TheArtOfDev.HtmlRenderer.Core.Utils;

namespace TheArtOfDev.HtmlRenderer.Core.Handlers
{
    /// <summary>
    /// On download file async complete, success or fail.
    /// </summary>
    /// <param name="imageUri">The online image uri</param>
    /// <param name="filePath">the path to the downloaded file</param>
    /// <param name="error">the error if download failed</param>
    /// <param name="canceled">is the file download request was canceled</param>
    public delegate void DownloadFileAsyncCallback(Uri imageUri, string filePath, Exception error, bool canceled);

    /// <summary>
    /// Handler for downloading images from the web.<br/>
    /// Single instance of the handler used for all images downloaded in a single html, this way if the html contains more
    /// than one reference to the same image it will be downloaded only once.<br/>
    /// Also handles corrupt, partial and canceled downloads by first downloading to temp file and only if successful moving to cached 
    /// file location.
    /// </summary>
    internal sealed class ImageDownloader : IDisposable
    {
        /// <summary>
        /// the client used to download image from URL
        /// </summary>
        private readonly HttpClient _client = new HttpClient();

        /// <summary>
        /// dictionary of image cache path to callbacks of download to handle multiple requests to download the same image 
        /// </summary>
        private readonly Dictionary<string, List<DownloadFileAsyncCallback>> _imageDownloadCallbacks = new Dictionary<string, List<DownloadFileAsyncCallback>>();

        /// <summary>
        /// Cancellation token source for cancelling pending requests
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Makes a request to download the image from the server and raises the <see cref="cachedFileCallback"/> when it's down.<br/>
        /// </summary>
        /// <param name="imageUri">The online image uri</param>
        /// <param name="filePath">the path on disk to download the file to</param>
        /// <param name="async">is to download the file sync or async (true-async)</param>
        /// <param name="cachedFileCallback">This callback will be called with local file path. If something went wrong in the download it will return null.</param>
        public void DownloadImage(Uri imageUri, string filePath, bool async, DownloadFileAsyncCallback cachedFileCallback)
        {
            ArgChecker.AssertArgNotNull(imageUri, "imageUri");
            ArgChecker.AssertArgNotNull(cachedFileCallback, "cachedFileCallback");

            // to handle if the file is already been downloaded
            bool download = true;
            lock (_imageDownloadCallbacks)
            {
                if (_imageDownloadCallbacks.ContainsKey(filePath))
                {
                    download = false;
                    _imageDownloadCallbacks[filePath].Add(cachedFileCallback);
                }
                else
                {
                    _imageDownloadCallbacks[filePath] = new List<DownloadFileAsyncCallback> { cachedFileCallback };
                }
            }

            if (download)
            {
                var tempPath = Path.GetTempFileName();
                if (async)
                    _ = DownloadImageFromUrlAsync(imageUri, tempPath, filePath);
                else
                    DownloadImageFromUrl(imageUri, tempPath, filePath);
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            ReleaseObjects();
        }


        #region Private/Protected methods

        /// <summary>
        /// Download the requested file in the URI to the given file path.<br/>
        /// Use HttpClient to download from web.
        /// </summary>
        private void DownloadImageFromUrl(Uri source, string tempPath, string filePath)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, source))
#if NET6_0_OR_GREATER
                using (var response = _client.Send(request, _cancellationTokenSource.Token))
#else
                using (var response = _client.SendAsync(request, _cancellationTokenSource.Token).Result)
#endif
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
#if NET6_0_OR_GREATER
                            response.Content.CopyTo(fileStream, null, _cancellationTokenSource.Token);
#else
                            response.Content.CopyToAsync(fileStream).GetAwaiter().GetResult();
#endif
                        }
                        OnDownloadImageCompleted(response, source, tempPath, filePath, null, false);
                    }
                    else
                    {
                        OnDownloadImageCompleted(response, source, tempPath, filePath, 
                            new Exception($"Failed to download image: {response.StatusCode}"), false);
                    }
                }
            }
            catch (Exception ex)
            {
                OnDownloadImageCompleted(null, source, tempPath, filePath, ex, false);
            }
        }

        /// <summary>
        /// Download the requested file in the URI to the given file path.<br/>
        /// Use async HttpClient to download from web.
        /// </summary>
        private async Task DownloadImageFromUrlAsync(Uri source, string tempPath, string filePath)
        {
            try
            {
                var response = await _client.GetAsync(source, _cancellationTokenSource.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                    OnDownloadImageCompleted(response, source, tempPath, filePath, null, false);
                }
                else
                {
                    OnDownloadImageCompleted(response, source, tempPath, filePath, 
                        new Exception($"Failed to download image: {response.StatusCode}"), false);
                }
            }
            catch (TaskCanceledException)
            {
                OnDownloadImageCompleted(null, source, tempPath, filePath, null, true);
            }
            catch (Exception ex)
            {
                OnDownloadImageCompleted(null, source, tempPath, filePath, ex, false);
            }
        }

        /// <summary>
        /// Checks if the file was downloaded and raises the cachedFileCallback from <see cref="_imageDownloadCallbacks"/>
        /// </summary>
        private void OnDownloadImageCompleted(HttpResponseMessage response, Uri source, string tempPath, string filePath, Exception error, bool cancelled)
        {
            if (!cancelled)
            {
                if (error == null)
                {
                    var contentType = response?.Content?.Headers?.ContentType?.MediaType;
                    if (contentType == null || !contentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                    {
                        error = new Exception("Failed to load image, not image content type: " + contentType);
                    }
                }

                if (error == null)
                {
                    try
                    {
                        if (File.Exists(filePath))
                            File.Delete(filePath);

                        File.Move(tempPath, filePath);
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                }
            }

            List<DownloadFileAsyncCallback> callbacksList;
            lock (_imageDownloadCallbacks)
            {
                callbacksList = _imageDownloadCallbacks[filePath];
                _imageDownloadCallbacks.Remove(filePath);
            }

            foreach (var callback in callbacksList)
            {
                callback(source, error == null && !cancelled ? filePath : null, error, cancelled);
            }

            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            { }
        }

        /// <summary>
        /// Release the objects used for image download.
        /// </summary>
        private void ReleaseObjects()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _client.Dispose();
            }
            catch
            { }
        }

        #endregion
    }
}
