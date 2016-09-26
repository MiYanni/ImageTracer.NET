using System.Collections.Generic;

namespace ImageTracerNet.Vectorization.TraceTypes
{
    internal class Layer<T> where T : class
    {
        public IReadOnlyList<T> Paths { get; set; }
    }
}
