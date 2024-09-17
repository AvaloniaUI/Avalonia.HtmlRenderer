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

using System.Collections.Generic;
using System.Linq;

using TheArtOfDev.HtmlRenderer.Core.Entities;
using TheArtOfDev.HtmlRenderer.Core.Parse;
using TheArtOfDev.HtmlRenderer.Core.Utils;

namespace TheArtOfDev.HtmlRenderer.Core.Dom
{
    /// <summary>
    /// CSS boxes that have ":hover" selector on them.
    /// </summary>
    internal sealed class HoverBoxBlock
    {
        /// <summary>
        /// the box that has :hover css on
        /// </summary>
        private readonly CssBox _cssBox;

        /// <summary>
        /// the :hover style block data
        /// </summary>
        private readonly CssBlock _cssBlock;

        /// <summary>
        /// Init.
        /// </summary>
        public HoverBoxBlock(CssBox cssBox, CssBlock cssBlock)
        {
            _cssBox = cssBox;
            _cssBlock = cssBlock;
        }

        /// <summary>
        /// the box that has :hover css on
        /// </summary>
        public CssBox CssBox
        {
            get { return _cssBox; }
        }

        /// <summary>
        /// the :hover style block data
        /// </summary>
        public CssBlock CssBlock
        {
            get { return _cssBlock; }
        }

        /// <summary>
        /// Used to determine if :hover is active or not and set in
        /// </summary>
        private bool _isHovering;

        /// <summary>
        /// A lookup of all text element style properties before :hover style is applied and used to restore styling after hover
        /// </summary>
        private Dictionary<CssBox, Dictionary<string, string>> _originalBlocks = [];

        /// <summary>
        /// Toggles hover styling 
        /// </summary>
        /// <param name="isHovering">Whether to apply or remove :hover style</param>
        /// <returns>returns true if state changed</returns>
        public bool SetIsHovering(bool isHovering)
        {
            if (_isHovering == isHovering)
            {
                // no change
                return false;
            }
            _isHovering = isHovering;

            if (_isHovering)
            {
                // find and cache current child text element styles
                var textElms = new List<CssBox>();
                FindTextElements(CssBox, textElms);
                _originalBlocks.Clear();
                foreach (var textElm in textElms)
                {
                    var props = CssBlock.Properties.ToDictionary(x => x.Key, x => CssUtils.GetPropertyValue(textElm, x.Key));
                    _originalBlocks.Add(textElm, props);
                }
                // assign the :hover style
                foreach (var textElm in textElms)
                {
                    DomParser.AssignCssProps(textElm, CssBlock.Properties);
                }
            } else
            {
                // restore non :hover styles
                foreach (var textBlock_kvp in _originalBlocks)
                {
                    DomParser.AssignCssProps(textBlock_kvp.Key, textBlock_kvp.Value);
                }
            }
            return true;
        }

        /// <summary>
        /// Helper method that populates <paramref name="textElms"/> with all self and descending TextElements from <paramref name="elm"/>
        /// </summary>
        /// <param name="elm">Box to search</param>
        /// <param name="textElms">A list to add found anon text element boxes</param>
        private void FindTextElements(CssBox elm, List<CssBox> textElms)
        {
            if (elm.HtmlTag == null)
            {
                textElms.Add(elm);
            }
            foreach (var celm in elm.Boxes)
            {
                FindTextElements(celm, textElms);
            }
        }
    }
}