using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImageTracerNet.Extensions;

namespace ImageTracerNet
{
    internal class ColorReference
    {
        private readonly Color _color;

        public ColorReference(Color color)
        {
            _color = color;
        }

        public ColorReference(byte alpha, byte red, byte green, byte blue)
        {
            _color = Color.FromArgb(alpha, red, green, blue);
        }

        public byte A => _color.A;
        public byte R => _color.R;
        public byte G => _color.G;
        public byte B => _color.B;

        public int CalculateRectilinearDistance(ColorReference other)
        {
            return _color.CalculateRectilinearDistance(other._color);
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
    }
}
