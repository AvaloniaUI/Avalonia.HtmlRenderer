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
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using TheArtOfDev.HtmlRenderer.Demo.Common;
using TheArtOfDev.HtmlRenderer.Avalonia;

namespace TheArtOfDev.HtmlRenderer.Demo.Avalonia
{
    /// <summary>
    /// Interaction logic for DemoWindow.xaml
    /// </summary>
    public partial class DemoWindow : Window
    {
        #region Fields/Consts

        /// <summary>
        /// the private font used for the demo
        /// </summary>
        //private readonly PrivateFontCollection _privateFont = new PrivateFontCollection();

        #endregion
        public DemoWindow()
        {
            SamplesLoader.Init("Avalonia", typeof(HtmlRender).Assembly.GetName().Version.ToString());

            InitializeComponent();
            DataContext = this;

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            LoadCustomFonts();
        }

        /// <summary>
        /// Load custom fonts to be used by renderer HTMLs
        /// </summary>
        private static void LoadCustomFonts()
        {
            HtmlRender.AddFontFamily(new FontFamily(
                new Uri("avares://HtmlRenderer.Demo.Avalonia/fonts/CustomFont.ttf"),
                "1 Smoothy DNA"));
        }

        /// <summary>
        /// Open sample window.
        /// </summary>
        private void OnOpenSampleWindow_click(object sender, RoutedEventArgs e)
        {
            var w = new SampleWindow();
            w.Width = Width * 0.8;
            w.Height = Height * 0.8;
            _ = w.ShowDialog(this);
        }
        
        /// <summary>
        /// Open the current html is external process - the default user browser.
        /// </summary>
        private void OnOpenInExternalView_Click(object sender, RoutedEventArgs e)
        {
            var tmpFile = Path.ChangeExtension(Path.GetTempFileName(), ".htm");
            File.WriteAllText(tmpFile, _mainControl.GetHtml());
            
            new Process
            {
                StartInfo = new ProcessStartInfo(tmpFile)
                {
                    UseShellExecute = true
                }
            }.Start();
        }

        /// <summary>
        /// Toggle the use generated html button state.
        /// </summary>
        private void OnUseGeneratedHtml_Click(object sender, RoutedEventArgs e)
        {
            _mainControl.UseGeneratedHtml = _useGeneratedHtml.IsChecked.GetValueOrDefault(false);
        }

        /// <summary>
        /// Open generate image window for the current html.
        /// </summary>
        private void OnGenerateImage_Click(object sender, RoutedEventArgs e)
        {
            var w = new GenerateImageWindow(_mainControl.GetHtml());
            w.Width = Width * 0.8;
            w.Height = Height * 0.8;
            _ = w.ShowDialog(this);
        }

        /// <summary>
        /// Execute performance test by setting all sample HTMLs in a loop.
        /// </summary>
        private void OnRunPerformance_Click(object sender, RoutedEventArgs e)
        {
            _mainControl.UpdateLock = true;
            _toolBar.IsEnabled = false;
            Dispatcher.UIThread.RunJobs();

            var msg = DemoUtils.RunSamplesPerformanceTest(html =>
            {
                _mainControl.SetHtml(html);
                Dispatcher.UIThread.RunJobs(); // so paint will be called
            });

            var window = new Window
            {
                Content = new SelectableTextBlock
                {
                    Text = msg,
                    TextWrapping = TextWrapping.Wrap
                },
                Width = 400,
                Title = "Test run results"
            };
            window.Show();

            _mainControl.UpdateLock = false;
            _toolBar.IsEnabled = true;
        }
    }
}