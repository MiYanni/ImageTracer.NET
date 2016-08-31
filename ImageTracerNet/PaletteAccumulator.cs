using System;
using System.Drawing;
using System.Linq;
using ImageTracerNet.Extensions;

namespace ImageTracerNet
{
    internal class PaletteAccumulator
    {
        //public Color Color { get; set; } = Color.Empty;
        public long R { get; set; }
        public long B { get; set; }
        public long G { get; set; }
        public long A { get; set; }
        public int Count { get; set; }

        public Color CalculateAverage()
        {
            return ColorExtensions.FromRgbaByteArray(new []{R, B, G, A}.Select(comp => (byte)(comp / Count)).ToArray()).Single();
            //return ColorExtensions.FromRgbaByteArray(new[] { R, B, G, A }.Select(comp => (byte)Math.Floor(comp / (double)Count)).ToArray()).Single();
        } 
    }
}
