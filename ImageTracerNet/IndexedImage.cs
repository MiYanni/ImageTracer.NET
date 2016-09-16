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
        public IReadOnlyList<IndexedColor> ColorIndices { get; }
        public int IndicesWidth { get; }
        public int IndicesHeight { get; }
        // array[palettelength][4] RGBA color palette
        public IReadOnlyList<Color> Palette { get; }
        // tracedata
        public List<List<List<Segment>>> Layers { set; get; }

        //public IndexedImage(IReadOnlyList<IndexedColor> array, IReadOnlyList<Color> palette)
        private IndexedImage(IReadOnlyList<Color> palette, int imageHeight, int imageWidth)
        {
            Palette = palette;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            // Indexed color array adds +2 to the original width and height
            IndicesWidth = ImageWidth + 2;
            IndicesHeight = ImageHeight + 2;
            ColorIndices = CreateIndexedColors(Palette, IndicesHeight, IndicesWidth);
        }

        public PixelGroup GetPixelGroup(int row, int column)
        {
            return new PixelGroup(ColorIndices, row, column, IndicesWidth);
        }

        // Creating indexed color array arr which has a boundary filled with -1 in every direction
        // Example: 4x4 image becomes a 6x6 matrix:
        // -1 -1 -1 -1 -1 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1 -1 -1 -1 -1 -1
        //private static int[][] CreateIndexedColorArray(int height, int width)
        //{
        //    height += 2;
        //    width += 2;
        //    return new int[height][].Initialize(i =>
        //    i == 0 || i == height - 1
        //        ? new int[width].Initialize(-1)
        //        : new int[width].Initialize(-1, 0, width - 1));
        //}
        private static IReadOnlyList<IndexedColor> CreateIndexedColors(IReadOnlyList<Color> palette, int height, int width)
        {
            var indicesArray = new int[height][].Initialize(i =>
            i == 0 || i == height - 1
                ? new int[width].Initialize(-1)
                : new int[width].Initialize(-1, 0, width - 1));
            return indicesArray.Select((row, rowIndex) => row.Select((colorIndex, columnIndex) =>
                new IndexedColor { Palette = palette, PaletteIndex = colorIndex }))
                .SelectMany(i => i)
                .ToList();
        }

        public static IndexedImage Create(ImageData imageData, IReadOnlyList<Color> colorPalette)
        {
            //var arr = CreateIndexedColorArray(imageData.Height, imageData.Width);
            var image = new IndexedImage(colorPalette, imageData.Height, imageData.Width);

            for (var j = 0; j < imageData.Height; j++)
            {
                for (var i = 0; i < imageData.Width; i++)
                {
                    var pixel = imageData.Colors[j * imageData.Width + i];
                    var distance = 256 * 4;
                    var paletteIndex = 0;
                    // find closest color from palette by measuring (rectilinear) color distance between this pixel and all palette colors
                    for (var k = 0; k < colorPalette.Count; k++)
                    {
                        var color = colorPalette[k];
                        // In my experience, https://en.wikipedia.org/wiki/Rectilinear_distance works better than https://en.wikipedia.org/wiki/Euclidean_distance
                        var newDistance = color.CalculateRectilinearDistance(pixel);

                        if (newDistance >= distance) continue;

                        distance = newDistance;
                        paletteIndex = k;
                    }

                    image.ColorIndices[j * imageData.Width + i].PaletteIndex = paletteIndex;
                    //image.ColorIndices.Single(c => c.X == j + 1 && c.Y == i + 1).PaletteIndex = paletteIndex;
                    //arr[j + 1][i + 1] = paletteIndex;
                }
            }

            return image;
            //return new IndexedImage(arr, colorPalette);
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
