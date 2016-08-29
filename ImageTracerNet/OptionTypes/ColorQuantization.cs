using System;

namespace ImageTracerNet.OptionTypes
{
    [Serializable]
    public class ColorQuantization
    {
        public double ColorSampling { get; set; } = 1f;
        public int NumberOfColors { get; set; } = 16;
        public double MinColorRatio { get; set; } = .02f;
        public int ColorQuantCycles { get; set; } = 3;
    }
}
