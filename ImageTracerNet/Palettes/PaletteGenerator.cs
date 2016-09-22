using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImageTracerNet.Extensions;

namespace ImageTracerNet.Palettes
{
    internal static class PaletteGenerator
    {
        public static Color[] GenerateGrayscale(int numberOfColors)
        {
            var step = 255 / (numberOfColors - 1); // distance between points
            return new Color[numberOfColors].Initialize(i =>
            {
                var component = (byte)(i * step);
                return Color.FromArgb(255, component, component, component);
            });
        }

        private static IEnumerable<Color> GenerateRgbCube(int numberOfColors)
        {
            var step = 255 / (numberOfColors - 1); // distance between points
            var colorQNum = (int)Math.Floor(Math.Pow(numberOfColors, 1.0 / 3.0)); // Number of points on each edge on the RGB color cube

            for (var redCount = 0; redCount < colorQNum; redCount++)
            {
                for (var greenCount = 0; greenCount < colorQNum; greenCount++)
                {
                    for (var blueCount = 0; blueCount < colorQNum; blueCount++)
                    {
                        yield return Color.FromArgb(255, (byte)(redCount * step), (byte)(greenCount * step), (byte)(blueCount * step));
                    }
                }
            }
        }

        // Generating a palette with numberofcolors, array[numberofcolors][4] where [i][0] = R ; [i][1] = G ; [i][2] = B ; [i][3] = A
        public static Color[] GeneratePalette(int numberOfColors)
        {
            if (numberOfColors < 8)
            {
                return GenerateGrayscale(numberOfColors);
            }

            // Number of points on each edge on the RGB color cube total
            var colorQNumTotal = (int)Math.Floor(Math.Pow(numberOfColors, 1.0 / 3.0)) * 3;
            var rgbCube = GenerateRgbCube(numberOfColors);

            // RGB color cube used for part of the palette; the rest is random
            return new Color[numberOfColors].Initialize(i => i < colorQNumTotal ? rgbCube.ElementAt(i) : ColorExtensions.RandomColor());
        }

        //private static readonly Random Rng = new Random();
        //// This palette randomly samples the image
        //public static Color[] SamplePalette(int numberOfColors, ImageData imageData)
        //{
        //    return new Color[numberOfColors].Initialize(() =>
        //    {
        //        var index = (int)(Math.Floor(Rng.NextDouble() * imageData.Data.Length / 4) * 4);
        //        return Color.FromArgb(imageData.Data[index + 3], imageData.Data[index], imageData.Data[index + 1], imageData.Data[index + 2]);
        //    });
        //}
    }
}
