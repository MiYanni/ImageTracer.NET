using System;

namespace ImageTracerNet.OptionTypes
{
    [Serializable]
    public class Tracing
    {
        // Threshold!
        public double LTres { get; set; } = 1f;
        // Threshold!
        public double QTres { get; set; } = 1f;
        public int PathOmit { get; set; } = 8;
    }
}
