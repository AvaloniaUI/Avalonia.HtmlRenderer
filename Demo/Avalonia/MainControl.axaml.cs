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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using TheArtOfDev.HtmlRenderer.Core.Entities;
using TheArtOfDev.HtmlRenderer.Demo.Common;
using TheArtOfDev.HtmlRenderer.Avalonia;

namespace TheArtOfDev.HtmlRenderer.Demo.Avalonia
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        #region Fields and Consts

        /// <summary>
        /// the name of the tree node root for all performance samples
        /// </summary>
        private const string PerformanceSamplesTreeNodeName = "Performance Samples";

        /// <summary>
        /// timer to update the rendered html when html in editor changes with delay
        /// </summary>
        private readonly Timer _updateHtmlTimer;

        /// <summary>
        /// used ignore html editor updates when updating separately
        /// </summary>
        private bool _updateLock;

        /// <summary>
        /// In IE view if to show original html or the html generated from the html control
        /// </summary>
        private bool _useGeneratedHtml;

        #endregion


        public MainControl()
        {
            InitializeComponent();

            _htmlPanel.RenderError += OnRenderError;
            _htmlPanel.LinkClicked += OnLinkClicked;
            _htmlPanel.StylesheetLoad += HtmlRenderingHelper.OnStylesheetLoad;
            _htmlPanel.ImageLoad += HtmlRenderingHelper.OnImageLoad;
            _htmlPanel.LoadComplete += (sender, args) => _htmlPanel.ScrollToElement("C4");

            _htmlTooltipLabel.AvoidImagesLateLoading = true;
            _htmlTooltipLabel.StylesheetLoad += HtmlRenderingHelper.OnStylesheetLoad;
            _htmlTooltipLabel.ImageLoad += HtmlRenderingHelper.OnImageLoad;
            _htmlTooltipLabel.Text = "<div class='htmltooltip'>" + Common.Resources.Tooltip + "</div>";


            LoadSamples();

            _updateHtmlTimer = new Timer(OnUpdateHtmlTimerTick);
        }


        /// <summary>
        /// used ignore html editor updates when updating separately
        /// </summary>
        public bool UpdateLock
        {
            get { return _updateLock; }
            set { _updateLock = value; }
        }

        /// <summary>
        /// In IE view if to show original html or the html generated from the html control
        /// </summary>
        public bool UseGeneratedHtml
        {
            get { return _useGeneratedHtml; }
            set { _useGeneratedHtml = value; }
        }

        public string GetHtml()
        {
            return _useGeneratedHtml ? _htmlPanel.GetHtml() : GetHtmlEditorText();
        }

        public void SetHtml(string html)
        {
            _htmlPanel.Text = html;
            if (string.IsNullOrWhiteSpace(html))
            {
                _htmlPanel.InvalidateMeasure();
                _htmlPanel.InvalidateVisual();
            }
        }


        #region Private methods

        /// <summary>
        /// Loads the tree of document samples
        /// </summary>
        private void LoadSamples()
        {
            var showcaseRoot = new TreeViewItem();
            showcaseRoot.Header = "HTML Renderer";
            ((IList<object>)_samplesTreeView.Items!).Add(showcaseRoot);

            foreach (var sample in SamplesLoader.ShowcaseSamples)
            {
                AddTreeItem(showcaseRoot, sample);
            }

            var testSamplesRoot = new TreeViewItem();
            testSamplesRoot.Header = "Test Samples";
            ((IList<object>)_samplesTreeView.Items!).Add(testSamplesRoot);

            foreach (var sample in SamplesLoader.TestSamples)
            {
                AddTreeItem(testSamplesRoot, sample);
            }

            if (SamplesLoader.PerformanceSamples.Count > 0)
            {
                var perfTestSamplesRoot = new TreeViewItem();
                perfTestSamplesRoot.Header = PerformanceSamplesTreeNodeName;
                ((IList<object>)_samplesTreeView.Items!).Add(perfTestSamplesRoot);

                foreach (var sample in SamplesLoader.PerformanceSamples)
                {
                    AddTreeItem(perfTestSamplesRoot, sample);
                }
            }

            showcaseRoot.IsExpanded = true;
            
            if (((IList<object>)showcaseRoot.Items!).Count > 0)
                ((TreeViewItem)((IList<object>)showcaseRoot.Items!)[0]).IsSelected = true;
        }

        /// <summary>
        /// Add an html sample to the tree and to all samples collection
        /// </summary>
        private void AddTreeItem(TreeViewItem root, HtmlSample sample)
        {
            var html = sample.Html.Replace("$$Release$$", _htmlPanel.GetType().Assembly.GetName().Version.ToString());

            var node = new TreeViewItem();
            node.Header = sample.Name;
            node.Tag = new HtmlSample(sample.Name, sample.FullName, html);
            ((IList<object>)root.Items!).Add(node);
        }

        /// <summary>
        /// On tree view node click load the html to the html panel and html editor.
        /// </summary>
        private void OnTreeView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = e.AddedItems.OfType<TreeViewItem>().FirstOrDefault();
            var sample = item?.Tag as HtmlSample;
            if (sample != null)
            {
                _updateLock = true;

                _htmlEditor.Text = sample.Html;

                Cursor = new Cursor(StandardCursorType.Wait);

                try
                {
                    _htmlPanel.AvoidImagesLateLoading = !sample.FullName.Contains("Many images");
                    _htmlPanel.Text = sample.Html;
                }
                catch (Exception ex)
                {
                    MessageBox(ex.ToString(), "Failed to render HTML");
                }

                Cursor = new Cursor(StandardCursorType.Arrow);
                _updateLock = false;
            }
        }

        /// <summary>
        /// On text change in the html editor update 
        /// </summary>
        private void OnHtmlEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_updateLock)
            {
                _updateHtmlTimer.Change(1000, int.MaxValue);
            }
        }

        /// <summary>
        /// Update the html renderer with text from html editor.
        /// </summary>
        private void OnUpdateHtmlTimerTick(object state)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _updateLock = true;

                try
                {
                    _htmlPanel.Text = GetHtmlEditorText();
                }
                catch (Exception ex)
                {
                    MessageBox(ex.ToString(), "Failed to render HTML");
                }

                _updateLock = false;
            });
        }

        /// <summary>
        /// Fix the raw html by replacing bridge object properties calls with path to file with the data returned from the property.
        /// </summary>
        /// <returns>fixed html</returns>
        private string GetFixedHtml()
        {
            var html = GetHtmlEditorText();

            html = Regex.Replace(html, @"src=\""(\w.*?)\""", match =>
            {
                var img = HtmlRenderingHelper.TryLoadResourceImage(match.Groups[1].Value);
                if (img != null)
                {
                    var tmpFile = Path.GetTempFileName();
                    img.Save(tmpFile);
                    return string.Format("src=\"{0}\"", tmpFile);
                }
                return match.Value;
            }, RegexOptions.IgnoreCase);

            html = Regex.Replace(html, @"href=\""(\w.*?)\""", match =>
            {
                var stylesheet = DemoUtils.GetStylesheet(match.Groups[1].Value);
                if (stylesheet != null)
                {
                    var tmpFile = Path.GetTempFileName();
                    File.WriteAllText(tmpFile, stylesheet);
                    return string.Format("href=\"{0}\"", tmpFile);
                }
                return match.Value;
            }, RegexOptions.IgnoreCase);

            return html;
        }

        /// <summary>
        /// Show error raised from html renderer.
        /// </summary>
        private void OnRenderError(object sender, HtmlRendererRoutedEventArgs<HtmlRenderErrorEventArgs> args)
        {
            Dispatcher.UIThread.InvokeAsync(() => MessageBox(args.Event.Message + (args.Event.Exception != null ? "\r\n" + args.Event.Exception : null), "Error in Html Renderer"));
        }

        /// <summary>
        /// On specific link click handle it here.
        /// </summary>
        private void OnLinkClicked(object sender, HtmlRendererRoutedEventArgs<HtmlLinkClickedEventArgs> args)
        {
            if (args.Event.Link == "SayHello")
            {
                MessageBox("Hello you!");
                args.Event.Handled = true;
            }
            else if (args.Event.Link == "ShowSampleForm")
            {
                var w = new SampleWindow();
                var window = TopLevel.GetTopLevel(this) as Window;
                if (window != null)
                {
                    w.Width = window.Width * 0.8;
                    w.Height = window.Height * 0.8;
                    _ = w.ShowDialog(window);
                }
                args.Event.Handled = true;
            }
        }

        /// <summary>
        /// Get the html text from the html editor control.
        /// </summary>
        private string GetHtmlEditorText()
        {
            return _htmlEditor.Text;
        }

        private void MessageBox(string text, string title = null)
        {
            var window = new Window
            {
                Content = new SelectableTextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                },
                SizeToContent = SizeToContent.Height,
                Width = 400,
                Title = title ?? "Message"
            };
            window.Show();
        }
        
        #endregion
    }
}