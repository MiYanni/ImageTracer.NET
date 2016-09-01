using System.Drawing;
using System.Linq;
using ImageTracerNet.Extensions;

namespace ImageTracerNet.Palettes
{
    internal class PaletteAccumulator
    {
        //public Color Color { get; set; } = Color.Empty;
        public long R { get; set; }
        public long G { get; set; }
        public long B { get; set; }
        public long A { get; set; }
        public int Count { get; set; }

        public Color CalculateAverage()
        {
            return ColorExtensions.FromRgbaByteArray(new []{R, G, B, A}.Select(comp => (byte)(comp / Count)).ToArray()).Single();
        }

        public void Accumulate(Color color)
        {
            R += color.R;
            G += color.G;
            B += color.B;
            A += color.A;
            Count++;
        }
    }
}
