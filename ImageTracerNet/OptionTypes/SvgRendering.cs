using System;

namespace ImageTracerNet.OptionTypes
{
    [Serializable]
    public class SvgRendering
    {
        public double Scale { get; set; } = 1f;
        public double SimplifyTolerance { get; set; } = 0f;
        public int RoundCoords { get; set; } = 1;
        // LinearControlPointRadius
        public double LCpr { get; set; } = 0f;
        // QuadraticControlPointRadius
        public double QCpr { get; set; } = 0f;
        public bool Viewbox { get; set; } = false;
    }
}
