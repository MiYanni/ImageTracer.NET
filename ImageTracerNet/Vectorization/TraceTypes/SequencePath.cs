using System.Collections.Generic;

namespace ImageTracerNet.Vectorization.TraceTypes
{
    internal class SequencePath
    {
        public InterpolationPointPath Path { get; set; }
        public IReadOnlyList<SequenceIndices> Sequences { get; set; }
    }
}
