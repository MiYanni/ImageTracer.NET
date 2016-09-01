using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ImageTracerNet.Palettes
{
    internal static class SmartPalette
    {
        public static Color[] Generate(Bitmap image)
        {
            var blurred = BlurImage(image);
            var blocks = DivideImage(blurred);
            return blocks.Select(AverageImageColor).ToArray();
        }

        private static Bitmap BlurImage(Bitmap image)
        {
            var blurredImage = new Bitmap(image.Width, image.Height);
            var gaussianBlur = new GaussianBlur();
            var rectangle = new Rectangle(0, 0, image.Width, image.Height);
            gaussianBlur.Apply(blurredImage, image, rectangle, 0, image.Height - 1);
            return blurredImage;
        }

        private static IEnumerable<Bitmap> DivideImage(Bitmap image, int rows = 4, int columns = 4)
        {
            // Will lose pixels because of naive divison when size doesn't divide evently.
            var blockHeight = image.Height / rows;
            var blockWidth = image.Width / columns;
            for (var i = 0; i < rows; ++i)
            {
                for (var j = 0; j < columns; ++j)
                {
                    var rectangle = new Rectangle(j * blockWidth, i * blockHeight, blockWidth, blockHeight);
                    yield return image.Clone(rectangle, image.PixelFormat);
                }
            }
        }

        private static Color AverageImageColor(Bitmap image)
        {
            long RTotal = 0;
            long GTotal = 0;
            long BTotal = 0;
            long ATotal = 0;
            for (var i = 0; i < image.Width; ++i)
            {
                for (var j = 0; j < image.Height; ++j)
                {
                    var pixel = image.GetPixel(i, j);
                    RTotal += pixel.R;
                    GTotal += pixel.G;
                    BTotal += pixel.B;
                    ATotal += pixel.A;
                }
            }
            var totalPixels = image.Width * image.Height;
            return Color.FromArgb((int)(ATotal/totalPixels), (int)(RTotal /totalPixels), (int)(GTotal /totalPixels), (int)(BTotal /totalPixels));
        }
    }
}
