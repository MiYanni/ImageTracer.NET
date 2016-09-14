using System;

namespace ImageTracerNet.OptionTypes
{
    [Serializable]
    public class Tracing
    {
        // LineThreshold
        public double LTres { get; set; } = 1f;
        // QuadraticSplineThreshold!
        public double QTres { get; set; } = 1f;
        public int PathOmit { get; set; } = 8;
    }
}
