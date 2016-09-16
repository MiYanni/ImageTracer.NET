using ImageTracerNet.Vectorization.Points;

namespace ImageTracerNet.Vectorization.Segments
{
    internal class SplineSegment : Segment
    {
        public Point<double> Mid { get; set; }
    }
}
