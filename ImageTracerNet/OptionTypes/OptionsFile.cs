using System;

namespace ImageTracerNet.OptionTypes
{
    [Serializable]
    public class OptionsFile
    {
        public Tracing Tracing { get; set; } = new Tracing();
        public ColorQuantization ColorQuantization { get; set; } = new ColorQuantization();
        public SvgRendering SvgRendering { get; set; } = new SvgRendering();
        public Blur Blur { get; set; } = new Blur();
    }
}
