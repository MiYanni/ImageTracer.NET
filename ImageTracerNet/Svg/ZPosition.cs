using System.Collections.Generic;
using ImageTracerNet.Vectorization.Segments;

namespace ImageTracerNet.Svg
{
    internal class ZPosition
    {
        public int Layer { get; set; }
        public int Path { get; set; }
        public ColorReference Color { get; set; }
        public IReadOnlyList<Segment> Segments { get; set; }
    }
}
