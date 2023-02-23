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
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using TheArtOfDev.HtmlRenderer.Demo.Common;
using TheArtOfDev.HtmlRenderer.Avalonia;

namespace TheArtOfDev.HtmlRenderer.Demo.Avalonia
{
    /// <summary>
    /// Interaction logic for GenerateImageWindow.xaml
    /// </summary>
    public partial class GenerateImageWindow : Window
    {
        private readonly string _html;
        private Bitmap _generatedImage;

        public GenerateImageWindow(string html)
        {
            _html = html;

            InitializeComponent();

            Loaded += (sender, args) => GenerateImage();
        }

        private async void OnSaveToFile_click(object sender, RoutedEventArgs e)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                FileTypeChoices = new []
                {
                    FilePickerFileTypes.ImagePng
                },
                SuggestedFileName = "image",
                DefaultExtension = "png"
            });

            if (file is null)
            {
                return;
            }

#if NET6_0_OR_GREATER
            await using var stream = await file.OpenWriteAsync();
#else
            var stream = await file.OpenWriteAsync();
#endif
            
            _generatedImage.Save(stream);

            await stream.FlushAsync();
        }

        private void OnGenerateImage_Click(object sender, RoutedEventArgs e)
        {
            GenerateImage();
        }

        private void GenerateImage()
        {
            if (_imageBoxBorder.Bounds.Width > 0 && _imageBoxBorder.Bounds.Height > 0)
            {
                _generatedImage = HtmlRender.RenderToImage(_html, _imageBoxBorder.Bounds.Size, null, DemoUtils.OnStylesheetLoad, HtmlRenderingHelper.OnImageLoad);
                _imageBox.Source = _generatedImage;
            }
        }
    }
}