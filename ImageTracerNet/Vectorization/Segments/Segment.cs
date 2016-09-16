using ImageTracerNet.Vectorization.Points;

namespace ImageTracerNet.Vectorization.Segments
{
    internal abstract class Segment
    {
        public Point<double> Start { get; set; }
        public Point<double> End { get; set; }
    }
}
