using Avalonia.Media.TextFormatting;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;

namespace TheArtOfDev.HtmlRenderer.Avalonia.Adapters
{
    public class FormattedLineAdapter : RFormattedLine
    {
        public TextLine TextLine { get; }

        public FormattedLineAdapter(TextLine textLine)
        {
            TextLine = textLine;
        }

        public override int Length => TextLine.Length;
        public override RSize Size => new RSize(TextLine.WidthIncludingTrailingWhitespace, TextLine.Height);
    }
}