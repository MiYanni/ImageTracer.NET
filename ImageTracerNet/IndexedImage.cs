using System.Drawing;
using System.Linq;
using ImageTracerNet.Extensions;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Palettes;
using TriListDoubleArray = System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;

namespace ImageTracerNet
{
    //https://en.wikipedia.org/wiki/Indexed_color
    // Container for the color-indexed image before and tracedata after vectorizing
    internal class IndexedImage
    {
        public int ImageWidth { get; }
        public int ImageHeight { get; }
        // array[x][y] of palette colors
        private readonly int[][] _array;
        public int ArrayWidth { get; }
        public int ArrayHeight { get; }
        // array[palettelength][4] RGBA color palette
        public byte[][] Palette { get;  }
        // tracedata
        public TriListDoubleArray Layers { set; get; }

        public IndexedImage(int[][] array, byte[][] palette)
        {
            _array = array;
            Palette = palette;
            ArrayWidth = _array[0].Length;
            ArrayHeight = _array.Length;
            // Indexed color array adds +2 to the original width and height
            ImageWidth = ArrayWidth - 2;
            ImageHeight = ArrayHeight - 2;
        }

        public PixelGroup GetPixelGroup(int row, int column)
        {
            return new PixelGroup(_array, row, column);
        }

        // Creating indexed color array arr which has a boundary filled with -1 in every direction
        // Example: 4x4 image becomes a 6x6 matrix:
        // -1 -1 -1 -1 -1 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1 -1 -1 -1 -1 -1
        private static int[][] CreateIndexedColorArray(int height, int width)
        {
            height += 2;
            width += 2;
            return new int[height][].Initialize(i =>
            i == 0 || i == height - 1
                ? new int[width].Initialize(-1)
                : new int[width].Initialize(-1, 0, width - 1));
        }

        public static IndexedImage Create(ImageData imageData, Color[] colorPalette, ColorQuantization colorQuant)
        {
            var arr = CreateIndexedColorArray(imageData.Height, imageData.Width);
            // Repeat clustering step "cycles" times
            for (var cycleCount = 0; cycleCount < colorQuant.ColorQuantCycles; cycleCount++)
            {
                for (var j = 0; j < imageData.Height; j++)
                {
                    for (var i = 0; i < imageData.Width; i++)
                    {
                        var pixel = imageData.Colors[j * imageData.Width + i];
                        var distance = 256 * 4;
                        var paletteIndex = 0;
                        // find closest color from palette by measuring (rectilinear) color distance between this pixel and all palette colors
                        for (var k = 0; k < colorPalette.Length; k++)
                        {
                            var color = colorPalette[k];
                            // In my experience, https://en.wikipedia.org/wiki/Rectilinear_distance works better than https://en.wikipedia.org/wiki/Euclidean_distance
                            var newDistance = color.CalculateRectilinearDistance(pixel);

                            if (newDistance >= distance) continue;

                            distance = newDistance;
                            paletteIndex = k;
                        }

                        arr[j + 1][i + 1] = paletteIndex;
                    }
                }
            }

            return new IndexedImage(arr, colorPalette.Select(c => c.ToRgbaByteArray()).ToArray());
        }

        // THIS IS NOW UNUSED
        // 1. Color quantization repeated "cycles" times, based on K-means clustering
        // https://en.wikipedia.org/wiki/Color_quantization
        // https://en.wikipedia.org/wiki/K-means_clustering
        private static IndexedImage ColorQuantization(ImageData imageData, Color[] colorPalette, Options options)
        {
            var arr = CreateIndexedColorArray(imageData.Height, imageData.Width);
            // Repeat clustering step "cycles" times
            for (var cycleCount = 0; cycleCount < options.ColorQuantization.ColorQuantCycles; cycleCount++)
            {
                // Reseting palette accumulator for averaging
                var accumulatorPaletteIndexer = Enumerable.Range(0, colorPalette.Length).ToDictionary(i => i, i => new PaletteAccumulator());

                for (var j = 0; j < imageData.Height; j++)
                {
                    for (var i = 0; i < imageData.Width; i++)
                    {
                        var pixel = imageData.Colors[j * imageData.Width + i];
                        var distance = 256 * 4;
                        var paletteIndex = 0;
                        // find closest color from palette by measuring (rectilinear) color distance between this pixel and all palette colors
                        for (var k = 0; k < colorPalette.Length; k++)
                        {
                            var color = colorPalette[k];
                            // In my experience, https://en.wikipedia.org/wiki/Rectilinear_distance works better than https://en.wikipedia.org/wiki/Euclidean_distance
                            var newDistance = color.CalculateRectilinearDistance(pixel);

                            if (newDistance >= distance) continue;

                            distance = newDistance;
                            paletteIndex = k;
                        }

                        // add to palettacc
                        accumulatorPaletteIndexer[paletteIndex].Accumulate(pixel);
                        arr[j + 1][i + 1] = paletteIndex;
                    }
                }

                // averaging paletteacc for palette
                for (var k = 0; k < colorPalette.Length; k++)
                {
                    // averaging
                    if (accumulatorPaletteIndexer[k].A > 0) // Non-transparent accumulation
                    {
                        colorPalette[k] = accumulatorPaletteIndexer[k].CalculateAverage();
                    }

                    //https://github.com/jankovicsandras/imagetracerjava/issues/2
                    // Randomizing a color, if there are too few pixels and there will be a new cycle
                    if (cycleCount >= options.ColorQuantization.ColorQuantCycles - 1) continue;
                    var ratio = accumulatorPaletteIndexer[k].Count / (double)(imageData.Width * imageData.Height);
                    if ((ratio < options.ColorQuantization.MinColorRatio) && (cycleCount < options.ColorQuantization.ColorQuantCycles - 1))
                    {
                        colorPalette[k] = ColorExtensions.RandomColor();
                    }
                }
            }

            return new IndexedImage(arr, colorPalette.Select(c => c.ToRgbaByteArray()).ToArray());
        }
    }
}
