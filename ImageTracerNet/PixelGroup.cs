using System.Collections.Generic;
using System.Linq;

namespace ImageTracerNet
{
    internal class PixelGroup
    {
        public int TopLeft { get; }
        public int TopMid { get; }
        public int TopRight { get; }
        public int MidLeft { get; }
        public int Mid { get; }
        public int MidRight { get; }
        public int BottomLeft { get; }
        public int BottomMid { get; }
        public int BottomRight { get; }

        public PixelGroup(IReadOnlyList<IndexedColor> pixels, int row, int column, int width)
        {
            TopLeft =       pixels[(row - 1) * width + (column - 1)].PaletteIndex;
            TopMid =        pixels[(row - 1) * width + column].PaletteIndex;
            TopRight =      pixels[(row - 1) * width + column + 1].PaletteIndex;
            MidLeft =       pixels[row * width + (column - 1)].PaletteIndex;
            Mid =           pixels[row * width + column].PaletteIndex;
            MidRight =      pixels[row * width + column + 1].PaletteIndex;
            BottomLeft =    pixels[(row + 1) * width + (column - 1)].PaletteIndex;
            BottomMid =     pixels[(row + 1) * width + column].PaletteIndex;
            BottomRight =   pixels[(row + 1) * width + column + 1].PaletteIndex;
        }
    }
}
