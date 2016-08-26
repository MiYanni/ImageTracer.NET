using System;

namespace ImageTracerNet.OptionTypes
{
    [Serializable]
    public class ColorQuantization
    {
        public double ColorSampling { get; set; } = 1f;
        public double NumberOfColors { get; set; } = 16f;
        public double MinColorRatio { get; set; } = .02f;
        public double ColorQuantCycles { get; set; } = 3f;
    }
}
