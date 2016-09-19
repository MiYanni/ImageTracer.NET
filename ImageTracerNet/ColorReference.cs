using System.Drawing;

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
    }
}
