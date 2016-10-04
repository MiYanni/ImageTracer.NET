using System.Collections.Generic;
using ImageTracerNet.Vectorization.Segments;

namespace ImageTracerNet.Vectorization.TraceTypes
{
    internal class SegmentPath
    {
        public IReadOnlyList<Segment> Segments { get; set; }
    }
}
