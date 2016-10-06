using System.Drawing;
using ImageTracerNet.Vectorization.Points;

namespace ImageTracerGui
{
    internal class ColorPoint : Point<int>
    {
        public Color Color { get; set; }
    }
}
