using ImageTracerNet.Vectorization.TraceTypes;

namespace ImageTracerNet.Svg
{
    internal class ZPosition
    {
        public ColorReference Color { get; set; }
        //public IReadOnlyList<Segment> Segments { get; set; }
        public SegmentPath Path { get; set; }
        // Label (Z-index key) is the startpoint of the path, linearized
        public double Label { get; set; }
    }
}
