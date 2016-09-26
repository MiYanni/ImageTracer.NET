using System.Collections.Generic;
using ImageTracerNet.Vectorization.Points;

namespace ImageTracerNet.Vectorization.TraceTypes
{
    internal class InterpolationPointPath
    {
        public IReadOnlyList<InterpolationPoint> Points { get; set; }
    }
}
