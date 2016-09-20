using System.Collections.Generic;

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

        public PixelGroup(IReadOnlyList<ColorReference> pixels, int row, int column, int width)
        {
            TopLeft = pixels[row - 1][column - 1];
            TopMid = pixels[row - 1][column];
            TopRight = pixels[row - 1][column + 1];
            MidLeft = pixels[row][column - 1];
            Mid = pixels[row][column];
            MidRight = pixels[row][column + 1];
            BottomLeft = pixels[row + 1][column - 1];
            BottomMid = pixels[row + 1][column];
            BottomRight = pixels[row + 1][column + 1];
        }
    }
}
