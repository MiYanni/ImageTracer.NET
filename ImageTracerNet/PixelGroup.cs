using System.Collections.Generic;

namespace ImageTracerNet
{
    internal class PixelGroup
    {
        public ColorReference TopLeft { get; }
        public ColorReference TopMid { get; }
        public ColorReference TopRight { get; }
        public ColorReference MidLeft { get; }
        public ColorReference Mid { get; }
        public ColorReference MidRight { get; }
        public ColorReference BottomLeft { get; }
        public ColorReference BottomMid { get; }
        public ColorReference BottomRight { get; }

        public PixelGroup(IReadOnlyList<ColorReference> pixels, int row, int column, int width)
        {
            TopLeft = pixels[(row - 1) * width + (column - 1)];
            TopMid = pixels[(row - 1) * width + column];
            TopRight = pixels[(row - 1) * width + column + 1];
            MidLeft = pixels[row * width + (column - 1)];
            Mid = pixels[row * width + column];
            MidRight = pixels[row * width + column + 1];
            BottomLeft = pixels[(row + 1) * width + (column - 1)];
            BottomMid = pixels[(row + 1) * width + column];
            BottomRight = pixels[(row + 1) * width + column + 1];
        }
    }
}
