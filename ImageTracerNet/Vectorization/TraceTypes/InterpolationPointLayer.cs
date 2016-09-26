using System.Collections.Generic;

namespace ImageTracerNet.Vectorization.TraceTypes
{
    internal class InterpolationPointLayer
    {
        public IReadOnlyList<InterpolationPointPath> Paths { get; set; }
    }
}
