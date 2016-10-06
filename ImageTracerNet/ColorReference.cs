using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImageTracerNet.Extensions;

namespace ImageTracerNet
{
    internal class ColorReference
    {
        public Color Color { get; }

        public ColorReference(Color color)
        {
            Color = color;
        }

        public ColorReference(byte alpha, byte red, byte green, byte blue)
            : this(Color.FromArgb(alpha, red, green, blue))
        {
        }

        public byte A => Color.A;
        public byte R => Color.R;
        public byte G => Color.G;
        public byte B => Color.B;

        public int CalculateRectilinearDistance(ColorReference other)
        {
            return Color.CalculateRectilinearDistance(other.Color);
        }

        private ColorReference() { }
        public static ColorReference Empty { get; } = new ColorReference();

        // find closest color from palette by measuring (rectilinear) color distance between this pixel and all palette colors
        // In my experience, https://en.wikipedia.org/wiki/Rectilinear_distance works better than https://en.wikipedia.org/wiki/Euclidean_distance
        public ColorReference FindClosest(IReadOnlyList<ColorReference> palette)
        {
            var distance = 256 * 4;
            var paletteColor = palette.First();
            foreach (var color in palette)
            {
                var newDistance = color.CalculateRectilinearDistance(this);
                if (newDistance >= distance) continue;

                distance = newDistance;
                paletteColor = color;
            }
            return paletteColor;
        }

        public string ToSvgString()
        {
            return $"fill=\"rgb({R},{G},{B})\" stroke=\"rgb({R},{G},{B})\" stroke-width=\"1\" opacity=\"{A / 255.0}\" ";
        }
    }
}
