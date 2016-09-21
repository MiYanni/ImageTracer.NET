using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImageTracerNet.Extensions;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Palettes;
using ImageTracerNet.Vectorization.Segments;

namespace ImageTracerNet
{
    //https://en.wikipedia.org/wiki/Indexed_color
    // Container for the color-indexed image before and tracedata after vectorizing
    internal class IndexedImage
    {
        public int ImageWidth { get; }
        public int ImageHeight { get; }
        // array[x][y] of palette colors
        //private readonly int[][] _array;
        private readonly IReadOnlyList<ColorReference> _colors;
        public int ArrayWidth { get; }
        public int ArrayHeight { get; }
        // array[palettelength][4] RGBA color palette
        public IReadOnlyList<ColorReference> Palette { get;  }
        // tracedata
        public List<List<List<Segment>>> Layers { set; get; }

        public IndexedImage(IReadOnlyList<ColorReference> colors, IReadOnlyList<ColorReference> palette, int imageHeight, int imageWidth)
        {
            //_array = array;
            _colors = colors;
            Palette = palette;
            ArrayWidth = imageWidth + 2;
            ArrayHeight = imageHeight + 2;
            // Indexed color array adds +2 to the original width and height
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
        }

        public PixelGroup GetPixelGroup(int row, int column, int width)
        {
            return new PixelGroup(_colors, row, column, width);
        }

        // Creating indexed color array arr which has a boundary filled with -1 in every direction
        // Imagine the -1's being ColorReference.Empty and the 0's being null.
        // Example: 4x4 image becomes a 6x6 matrix:
        // -1 -1 -1 -1 -1 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1 -1 -1 -1 -1 -1
        private static IEnumerable<ColorReference[]> CreatePaddedColorMatrix(int height, int width)
        {
            height += 2;
            width += 2;
            return new ColorReference[height][].Initialize(i =>
            i == 0 || i == height - 1
                ? new ColorReference[width].Initialize(j => ColorReference.Empty)
                : new ColorReference[width].Initialize(j => ColorReference.Empty, 0, width - 1));
        }

        //// find closest color from palette by measuring (rectilinear) color distance between this pixel and all palette colors
        //// In my experience, https://en.wikipedia.org/wiki/Rectilinear_distance works better than https://en.wikipedia.org/wiki/Euclidean_distance
        //private static ColorReference FindClosest(ColorReference imageColor, IReadOnlyList<ColorReference> palette)
        //{
        //    var distance = 256 * 4;
        //    var paletteColor = palette.First();
        //    foreach (var color in palette)
        //    {
        //        var newDistance = color.CalculateRectilinearDistance(imageColor);
        //        if (newDistance >= distance) continue;

        //        distance = newDistance;
        //        paletteColor = color;
        //    }
        //    return paletteColor;
        //}

        public static IndexedImage Create(ImageData imageData, IReadOnlyList<ColorReference> palette)
        {
            var imageColorQueue = new Queue<ColorReference>(imageData.Colors.Select(c => c.FindClosest(palette)));
            var paddedColorMatrix = CreatePaddedColorMatrix(imageData.Height, imageData.Width);
            var colors = paddedColorMatrix.SelectMany(c => c).Select(c => c ?? imageColorQueue.Dequeue()).ToList();

            return new IndexedImage(colors, palette, imageData.Height, imageData.Width);
        }

        // THIS IS NOW UNUSED
        // 1. Color quantization repeated "cycles" times, based on K-means clustering
        // https://en.wikipedia.org/wiki/Color_quantization
        // https://en.wikipedia.org/wiki/K-means_clustering
        //private static IndexedImage ColorQuantization(ImageData imageData, Color[] colorPalette, Options options)
        //{
        //    var arr = CreateIndexedColorArray(imageData.Height, imageData.Width);
        //    // Repeat clustering step "cycles" times
        //    for (var cycleCount = 0; cycleCount < options.ColorQuantization.ColorQuantCycles; cycleCount++)
        //    {
        //        // Reseting palette accumulator for averaging
        //        var accumulatorPaletteIndexer = Enumerable.Range(0, colorPalette.Length).ToDictionary(i => i, i => new PaletteAccumulator());

        //        for (var j = 0; j < imageData.Height; j++)
        //        {
        //            for (var i = 0; i < imageData.Width; i++)
        //            {
        //                var pixel = imageData.Colors[j * imageData.Width + i];
        //                var distance = 256 * 4;
        //                var paletteIndex = 0;
        //                // find closest color from palette by measuring (rectilinear) color distance between this pixel and all palette colors
        //                for (var k = 0; k < colorPalette.Length; k++)
        //                {
        //                    var color = colorPalette[k];
        //                    // In my experience, https://en.wikipedia.org/wiki/Rectilinear_distance works better than https://en.wikipedia.org/wiki/Euclidean_distance
        //                    var newDistance = color.CalculateRectilinearDistance(pixel);

        //                    if (newDistance >= distance) continue;

        //                    distance = newDistance;
        //                    paletteIndex = k;
        //                }

        //                // add to palettacc
        //                accumulatorPaletteIndexer[paletteIndex].Accumulate(pixel);
        //                arr[j + 1][i + 1] = paletteIndex;
        //            }
        //        }

        //        // averaging paletteacc for palette
        //        for (var k = 0; k < colorPalette.Length; k++)
        //        {
        //            // averaging
        //            if (accumulatorPaletteIndexer[k].A > 0) // Non-transparent accumulation
        //            {
        //                colorPalette[k] = accumulatorPaletteIndexer[k].CalculateAverage();
        //            }

        //            //https://github.com/jankovicsandras/imagetracerjava/issues/2
        //            // Randomizing a color, if there are too few pixels and there will be a new cycle
        //            if (cycleCount >= options.ColorQuantization.ColorQuantCycles - 1) continue;
        //            var ratio = accumulatorPaletteIndexer[k].Count / (double)(imageData.Width * imageData.Height);
        //            if ((ratio < options.ColorQuantization.MinColorRatio) && (cycleCount < options.ColorQuantization.ColorQuantCycles - 1))
        //            {
        //                colorPalette[k] = ColorExtensions.RandomColor();
        //            }
        //        }
        //    }

        //    return new IndexedImage(arr, colorPalette);
        //}
    }
}
