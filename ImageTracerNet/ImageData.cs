using System.Drawing;
using System.Linq;

namespace ImageTracerNet
{
    // https://developer.mozilla.org/en-US/docs/Web/API/ImageData
    internal class ImageData
    {
        public int Width { get; }
        public int Height { get; }
        // raw byte data: R G B A R G B A ...
        public byte[] Data { get; }
        public Color[] Colors { get; }

        public ImageData(int width, int height, byte[] data)
        {
            Width = width;
            Height = height;
            Data = data;
            // RGBA to ARGB Color
            Colors = Data.Select((comp, i) => new {Color = i/4, Component = comp})
                .GroupBy(x => x.Color, x => x.Component).Select(comps =>
                    Color.FromArgb(comps.ElementAt(3), comps.ElementAt(0), comps.ElementAt(1), comps.ElementAt(2)))
                .ToArray();
        }
    }
}
