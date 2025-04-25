using TheArtOfDev.HtmlRenderer.Adapters.Entities;

namespace TheArtOfDev.HtmlRenderer.Adapters
{
    public abstract class RFormattedLine
    {
        public abstract int Length { get; } 
        
        public abstract RSize Size { get; }
    }
}