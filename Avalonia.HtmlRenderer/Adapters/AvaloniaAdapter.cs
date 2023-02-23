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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform.Storage;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Avalonia.Utilities;
using Color = Avalonia.Media.Color;

namespace TheArtOfDev.HtmlRenderer.Avalonia.Adapters
{
    /// <summary>
    /// Adapter for Avalonia platform.
    /// </summary>
    internal sealed class AvaloniaAdapter : RAdapter
    {
        #region Fields and Consts

        /// <summary>
        /// Singleton instance of global adapter.
        /// </summary>
        private static readonly AvaloniaAdapter _instance = new AvaloniaAdapter();

        #endregion

        /// <summary>
        /// Init installed font families and set default font families mapping.
        /// </summary>
        private AvaloniaAdapter()
        {
            AddFontFamilyMapping("monospace", "Courier New");
            AddFontFamilyMapping("Helvetica", "Arial");

            foreach (var family in FontManager.Current.GetInstalledFontFamilyNames())
            {
	            try
	            {
	                AddFontFamily(new FontFamilyAdapter(family));
	            }
	            catch
	            {
	            }
            }
        }

        /// <summary>
        /// Singleton instance of global adapter.
        /// </summary>
        public static AvaloniaAdapter Instance
        {
            get { return _instance; }
        }

        protected override RColor GetColorInt(string colorName)
        {
            return Utils.Convert(Color.TryParse(colorName, out var color) ? color : Colors.Black);
        }

        protected override RPen CreatePen(RColor color)
        {
            return new PenAdapter(GetSolidColorBrush(color));
        }

        protected override RBrush CreateSolidBrush(RColor color)
        {
            return new BrushAdapter(GetSolidColorBrush(color));
        }

        protected override RBrush CreateLinearGradientBrush(RRect rect, RColor color1, RColor color2, double angle)
        {
            var startColor = angle <= 180 ? Utils.Convert(color1) : Utils.Convert(color2);
            var endColor = angle <= 180 ? Utils.Convert(color2) : Utils.Convert(color1);
            angle = angle <= 180 ? angle : angle - 180;
            double x = angle < 135 ? Math.Max((angle - 45) / 90, 0) : 1;
            double y = angle <= 45 ? Math.Max(0.5 - angle / 90, 0) : angle > 135 ? Math.Abs(1.5 - angle / 90) : 0;
            return new BrushAdapter(new ImmutableLinearGradientBrush(new[]
                {
                    new ImmutableGradientStop(0, startColor),
                    new ImmutableGradientStop(1, endColor)
                }, startPoint: new RelativePoint(x, y, RelativeUnit.Relative),
                endPoint: new RelativePoint(1 - x, 1 - y, RelativeUnit.Relative)));
        }

        protected override RImage ConvertImageInt(object image)
        {
            return image != null ? new ImageAdapter((Bitmap)image) : null;
        }

        protected override RImage ImageFromStreamInt(Stream memoryStream)
        {
            var bitmap = new Bitmap(memoryStream);
            return new ImageAdapter(bitmap);
        }

        protected override RFont CreateFontInt(string family, double size, RFontStyle style)
        {
            return new FontAdapter(new Typeface(family, GetFontStyle(style), GetFontWidth(style)), size);
        }

        protected override RFont CreateFontInt(RFontFamily family, double size, RFontStyle style)
        {
            return new FontAdapter(new Typeface(((FontFamilyAdapter)family).FontFamily, GetFontStyle(style), GetFontWidth(style)), size);
        }

        protected override object GetClipboardDataObjectInt(string html, string plainText)
        {
            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Text, plainText);
            return dataObject;
        }

        protected override void SetToClipboardInt(string text)
        {
            _ = Application.Current?.Clipboard?.SetTextAsync(text);
        }

        protected override void SetToClipboardInt(string html, string plainText)
        {
            _ = Application.Current?.Clipboard?.SetTextAsync(plainText);
        }

        protected override void SetToClipboardInt(RImage image)
        {
            //Do not crash, just ignore
            //TODO: implement image clipboard support
        }

        protected override RContextMenu CreateContextMenuInt()
        {
            return new ContextMenuAdapter();
        }

        protected override async void SaveToFileInt(RImage image, string name, string extension, RControl control = null)
        {
            var topLevel = TopLevel.GetTopLevel(((ControlAdapter)control)?.Control)
                ?? (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (topLevel is null)
            {
                throw new InvalidOperationException("No TopLevel available");
            }

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                FileTypeChoices = new []
                {
                    FilePickerFileTypes.ImagePng
                },
                SuggestedFileName = name,
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
            
            ((ImageAdapter)image).Image.Save(stream);

            await stream.FlushAsync();
        }


        #region Private/Protected methods

        /// <summary>
        /// Get solid color brush for the given color.
        /// </summary>
        private static IImmutableBrush GetSolidColorBrush(RColor color)
        {
            IImmutableBrush solidBrush;
            if (color == RColor.White)
                solidBrush = Brushes.White;
            else if (color == RColor.Black)
                solidBrush = Brushes.Black;
            else if (color.A < 1)
                solidBrush = Brushes.Transparent;
            else
                solidBrush = new ImmutableSolidColorBrush(Utils.Convert(color));
            return solidBrush;
        }

        /// <summary>
        /// Get Avalonia font style for the given style.
        /// </summary>
        private static FontStyle GetFontStyle(RFontStyle style)
        {
            if ((style & RFontStyle.Italic) == RFontStyle.Italic)
                return FontStyle.Italic;

            return FontStyle.Normal;
        }

        /// <summary>
        /// Get Avalonia font style for the given style.
        /// </summary>
        private static FontWeight GetFontWidth(RFontStyle style)
        {
            if ((style & RFontStyle.Bold) == RFontStyle.Bold)
                return FontWeight.Bold;

            return FontWeight.Normal;
        }

        #endregion
    }
}