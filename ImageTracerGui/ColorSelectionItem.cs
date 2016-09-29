using System.Windows.Media;
using ImageTracerNet;

namespace ImageTracerGui
{
    internal class ColorSelectionItem
    {
        public SolidColorBrush Color { get; set; }
        public int Index { get; set; }

        public ColorSelectionItem() { }

        public ColorSelectionItem(ColorReference colorReference, int index)
        {
            Color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(colorReference.A, colorReference.R, colorReference.G, colorReference.B));
            Index = index;
        }
    }
}
