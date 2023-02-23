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

using Avalonia.Interactivity;

namespace TheArtOfDev.HtmlRenderer.Avalonia
{
    public class HtmlRendererRoutedEventArgs<T> : RoutedEventArgs
    {
        public T Event { get; set; }
    }
}