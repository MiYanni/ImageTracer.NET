using System.Collections.Generic;
using ImageTracerNet.Vectorization.Points;

namespace ImageTracerNet
{
    internal class ColorGroup : Point<int>
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

        public ColorGroup(IReadOnlyList<ColorReference> colors, int row, int column, int width)
        {
            X = row;
            Y = column;
            TopLeft =       colors[(row - 1) * width + (column - 1)];
            TopMid =        colors[(row - 1) * width + column];
            TopRight =      colors[(row - 1) * width + column + 1];
            MidLeft =       colors[row * width + (column - 1)];
            Mid =           colors[row * width + column];
            MidRight =      colors[row * width + column + 1];
            BottomLeft =    colors[(row + 1) * width + (column - 1)];
            BottomMid =     colors[(row + 1) * width + column];
            BottomRight =   colors[(row + 1) * width + column + 1];
        }
    }
}
