using System;

namespace ImageTracerNet.OptionTypes
{
    [Serializable]
    public class Blur
    {
        public double BlurRadius { get; set; } = 0f;
        public double BlurDelta { get; set; } = 20f;
    }
}
