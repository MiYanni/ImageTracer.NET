using System.Collections.Generic;
using System.Drawing;

namespace ImageTracerNet
{
    internal class IndexedColor
    {
        public int PaletteIndex { get; set; }
        public IReadOnlyList<Color> Palette { get; set; }

        public Color ToColor()
        {
            return Palette[PaletteIndex];
        }
    }
}
