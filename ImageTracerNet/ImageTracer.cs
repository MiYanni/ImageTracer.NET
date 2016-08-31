using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ImageTracerNet.Extensions;
using TriListIntArray = System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<int[]>>>; // ArrayList<ArrayList<ArrayList<Integer[]>>>
using TriListDoubleArray = System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>; // ArrayList<ArrayList<ArrayList<Double[]>>>

namespace ImageTracerNet
{
    public static class ImageTracer
    {
        public static string VersionNumber = "1.1.1";

        private static readonly Random Rng = new Random();

        ////////////////////////////////////////////////////////////
        //
        //  User friendly functions
        //
        ////////////////////////////////////////////////////////////

        // Loading an image from a file, tracing when loaded, then returning the SVG String
        public static string ImageToSvg(string filename, Options options, byte[][] palette) 
        {
            return ImageToSvg(new Bitmap(filename), options, palette);
        }

        public static string ImageToSvg(Bitmap image, Options options, byte[][] palette) 
        {
            return ImageDataToSvg(LoadImageData(image), options, palette);
        }

        // Loading an image from a file, tracing when loaded, then returning IndexedImage with tracedata in layers
        public static IndexedImage ImageToTraceData(string filename, Options options, byte[][] palette) 
        {
            return ImageToTraceData(new Bitmap(filename), options, palette);
        }

        public static IndexedImage ImageToTraceData(Bitmap image, Options options, byte[][] palette) 
        {
            return ImageDataToTraceData(LoadImageData(image), options, palette);
        }

        ////////////////////////////////////////////////////////////

        private static ImageData LoadImageData(Bitmap image)
        {
            var rbgImage = image.ChangeFormat(PixelFormat.Format32bppArgb);
            var data = rbgImage.ToRgbaByteArray();
            return new ImageData(image.Width, image.Height, data);
        }

        // Tracing ImageData, then returning the SVG String
        private static string ImageDataToSvg(ImageData imgd, Options options, byte[][] palette)
        {
            return GetSvgString(ImageDataToTraceData(imgd, options, palette), options);
        }

        // Tracing ImageData, then returning IndexedImage with tracedata in layers
        private static IndexedImage ImageDataToTraceData(ImageData imgd, Options options, byte[][] palette)
        {
            // Use custom palette if pal is defined or sample or generate custom length palette
            palette = palette ?? (options.ColorQuantization.ColorSampling.IsNotZero()
                    ? SamplePalette(options.ColorQuantization.NumberOfColors, imgd)
                    : GeneratePalette(options.ColorQuantization.NumberOfColors));

            // Selective Gaussian blur preprocessing
            if (options.Blur.BlurRadius > 0)
            {
                imgd = Blur(imgd, options.Blur.BlurRadius, options.Blur.BlurDelta);
            }

            // 1. Color quantization
            var ii = ColorQuantization(imgd, palette, options);

            // 2. Layer separation and edge detection
            var rawlayers = Layering(ii);
            // 3. Batch pathscan
            var bps = BatchPathScan(rawlayers, options.Tracing.PathOmit);
            // 4. Batch interpollation
            var bis = BatchInterNodes(bps);
            // 5. Batch tracing
            ii.Layers = BatchTraceLayers(bis, options.Tracing.LTres, options.Tracing.QTres);
            return ii;
        }

        ////////////////////////////////////////////////////////////
        //
        //  Vectorizing functions
        //
        ////////////////////////////////////////////////////////////

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

        // 1. Color quantization repeated "cycles" times, based on K-means clustering
        // https://en.wikipedia.org/wiki/Color_quantization
        // https://en.wikipedia.org/wiki/K-means_clustering
        private static IndexedImage ColorQuantization(ImageData imgd, byte[][] palette, Options options)
        {
            var arr = CreateIndexedColorArray(imgd.Height, imgd.Width);
            var colorPalette = ColorExtensions.FromRgbaByteArray(palette.SelectMany(c => c).ToArray());
            var newAccumulator = new PaletteAccumulator[colorPalette.Length].Initialize(() => new PaletteAccumulator());
            // Repeat clustering step "cycles" times
            for (var cnt = 0; cnt < options.ColorQuantization.ColorQuantCycles; cnt++)
            {
                // Average colors from the second iteration
                if (cnt > 0)
                {
                    // averaging paletteacc for palette
                    for (var k = 0; k < colorPalette.Length; k++)
                    {
                        // averaging
                        if (newAccumulator[k].A > 0) // Non-transparent accumulation
                        {
                            colorPalette[k] = newAccumulator[k].CalculateAverage();
                        }

                        var ratio = newAccumulator[k].Count / (double)(imgd.Width * imgd.Height);
                        // Randomizing a color, if there are too few pixels and there will be a new cycle
                        if ((ratio < options.ColorQuantization.MinColorRatio) && (cnt < options.ColorQuantization.ColorQuantCycles - 1))
                        {
                            colorPalette[k] = ColorExtensions.RandomColor();
                        }
                    }
                }

                // Reseting palette accumulator for averaging
                newAccumulator = new PaletteAccumulator[colorPalette.Length].Initialize(() => new PaletteAccumulator());

                for (var j = 0; j < imgd.Height; j++)
                {
                    for (var i = 0; i < imgd.Width; i++)
                    {
                        var pixel = imgd.Colors[j * imgd.Width + i];
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
                        newAccumulator[paletteIndex].R += pixel.R;
                        newAccumulator[paletteIndex].G += pixel.G;
                        newAccumulator[paletteIndex].B += pixel.B;
                        newAccumulator[paletteIndex].A += pixel.A;
                        newAccumulator[paletteIndex].Count++;

                        arr[j + 1][i + 1] = paletteIndex;
                    }
                }
            }// End of Repeat clustering step "cycles" times

            return new IndexedImage(arr, colorPalette.Select(c => c.ToRgbaByteArray()).ToArray());
        }

        // Generating a palette with numberofcolors, array[numberofcolors][4] where [i][0] = R ; [i][1] = G ; [i][2] = B ; [i][3] = A
        private static byte[][] GeneratePalette(int numberofcolors)
        {
            var palette = new byte[numberofcolors][].InitInner(4);
            if (numberofcolors < 8)
            {
                const int shift = 0; // MJY: From -128
                // Grayscale
                var graystep = (byte)Math.Floor(255 / (double)(numberofcolors - 1));
                for (byte ccnt = 0; ccnt < numberofcolors; ccnt++)
                {
                    palette[ccnt][0] = (byte)(shift + ccnt * graystep);
                    palette[ccnt][1] = (byte)(shift + ccnt * graystep);
                    palette[ccnt][2] = (byte)(shift + ccnt * graystep);
                    palette[ccnt][3] = 255;
                }
            }
            else
            {
                const int shift = 0; // MJY: From -128
                // RGB color cube
                var colorqnum = (int)Math.Floor(Math.Pow(numberofcolors, 1.0 / 3.0)); // Number of points on each edge on the RGB color cube
                var colorstep = (int)Math.Floor(255 / (double)(colorqnum - 1)); // distance between points
                var ccnt = 0;
                for (var rcnt = 0; rcnt < colorqnum; rcnt++)
                {
                    for (var gcnt = 0; gcnt < colorqnum; gcnt++)
                    {
                        for (var bcnt = 0; bcnt < colorqnum; bcnt++)
                        {
                            palette[ccnt][0] = (byte)(shift + rcnt * colorstep);
                            palette[ccnt][1] = (byte)(shift + gcnt * colorstep);
                            palette[ccnt][2] = (byte)(shift + bcnt * colorstep);
                            palette[ccnt][3] = 127;
                            ccnt++;
                        }// End of blue loop
                    }// End of green loop
                }// End of red loop

                // Rest is random
                for (var rcnt = ccnt; rcnt < numberofcolors; rcnt++)
                {
                    palette[ccnt][0] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                    palette[ccnt][1] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                    palette[ccnt][2] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                    palette[ccnt][3] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                }
            }// End of numberofcolors check
            return palette;
        }

        private static byte[][] SamplePalette(int numberofcolors, ImageData imgd)
        {
            var palette = new byte[numberofcolors][].InitInner(4);
            for (var i = 0; i < numberofcolors; i++)
            {
                var idx = (int)(Math.Floor(Rng.NextDouble() * imgd.Data.Length / 4) * 4);
                palette[i][0] = imgd.Data[idx];
                palette[i][1] = imgd.Data[idx + 1];
                palette[i][2] = imgd.Data[idx + 2];
                palette[i][3] = imgd.Data[idx + 3];
            }
            return palette;
        }

        // 2. Layer separation and edge detection
        // Edge node types ( ▓:light or 1; ░:dark or 0 )
        // 12  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓
        // 48  ░░  ░░  ░░  ░░  ░▓  ░▓  ░▓  ░▓  ▓░  ▓░  ▓░  ▓░  ▓▓  ▓▓  ▓▓  ▓▓
        //     0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
        //
        private static int[][][] Layering(IndexedImage ii)
        {
            // Creating layers for each indexed color in arr
            int aw = ii.Array[0].Length, ah = ii.Array.Length;
            var layers = new int[ii.Palette.Length][][].InitInner(ah, aw);

            // Looping through all pixels and calculating edge node type
            for (var j = 1; j < ah - 1; j++)
            {
                for (var i = 1; i < aw - 1; i++)
                {

                    // This pixel's indexed color
                    var val = ii.Array[j][i];

                    // Are neighbor pixel colors the same?
                    int n1;
                    if ((j > 0) && (i > 0)) { n1 = ii.Array[j - 1][i - 1] == val ? 1 : 0; } else { n1 = 0; }
                    int n2;
                    if (j > 0) { n2 = ii.Array[j - 1][i] == val ? 1 : 0; } else { n2 = 0; }
                    int n3;
                    if ((j > 0) && (i < aw - 1)) { n3 = ii.Array[j - 1][i + 1] == val ? 1 : 0; } else { n3 = 0; }
                    int n4;
                    if (i > 0) { n4 = ii.Array[j][i - 1] == val ? 1 : 0; } else { n4 = 0; }
                    int n5;
                    if (i < aw - 1) { n5 = ii.Array[j][i + 1] == val ? 1 : 0; } else { n5 = 0; }
                    int n6;
                    if ((j < ah - 1) && (i > 0)) { n6 = ii.Array[j + 1][i - 1] == val ? 1 : 0; } else { n6 = 0; }
                    int n7;
                    if (j < ah - 1) { n7 = ii.Array[j + 1][i] == val ? 1 : 0; } else { n7 = 0; }
                    int n8;
                    if ((j < ah - 1) && (i < aw - 1)) { n8 = ii.Array[j + 1][i + 1] == val ? 1 : 0; } else { n8 = 0; }

                    // this pixel"s type and looking back on previous pixels
                    layers[val][j + 1][i + 1] = 1 + n5 * 2 + n8 * 4 + n7 * 8;
                    if (n4 == 0) { layers[val][j + 1][i] = 0 + 2 + n7 * 4 + n6 * 8; }
                    if (n2 == 0) { layers[val][j][i + 1] = 0 + n3 * 2 + n5 * 4 + 8; }
                    if (n1 == 0) { layers[val][j][i] = 0 + n2 * 2 + 4 + n4 * 8; }

                }// End of i loop
            }// End of j loop

            return layers;
        }

        // 3. Walking through an edge node array, discarding edge node types 0 and 15 and creating paths from the rest.
        // Walk directions (dir): 0 > ; 1 ^ ; 2 < ; 3 v
        // Edge node types ( ▓:light or 1; ░:dark or 0 )
        // ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓
        // ░░  ░░  ░░  ░░  ░▓  ░▓  ░▓  ░▓  ▓░  ▓░  ▓░  ▓░  ▓▓  ▓▓  ▓▓  ▓▓
        // 0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
        //
        private static List<List<int[]>> PathScan(int[][] arr, int pathomit)
        {
            var paths = new List<List<int[]>>();
            int w = arr[0].Length, h = arr.Length, dir = 0;
            bool holepath = false;

            for (var j = 0; j < h; j++)
            {
                for (var i = 0; i < w; i++)
                {
                    if ((arr[j][i] != 0) && (arr[j][i] != 15))
                    {
                        // Init
                        var px = i; var py = j;
                        paths.Add(new List<int[]>());
                        var thispath = paths[paths.Count - 1];
                        var pathfinished = false;
                        // fill paths will be drawn, but hole paths are also required to remove unnecessary edge nodes
                        if (arr[py][px] == 1) { dir = 0; }
                        if (arr[py][px] == 2) { dir = 3; }
                        if (arr[py][px] == 3) { dir = 0; }
                        if (arr[py][px] == 4) { dir = 1; holepath = false; }
                        if (arr[py][px] == 5) { dir = 0; }
                        if (arr[py][px] == 6) { dir = 3; }
                        if (arr[py][px] == 7) { dir = 0; holepath = true; }
                        if (arr[py][px] == 8) { dir = 0; }
                        if (arr[py][px] == 9) { dir = 3; }
                        if (arr[py][px] == 10) { dir = 3; }
                        if (arr[py][px] == 11) { dir = 1; holepath = true; }
                        if (arr[py][px] == 12) { dir = 0; }
                        if (arr[py][px] == 13) { dir = 3; holepath = true; }
                        if (arr[py][px] == 14) { dir = 0; holepath = true; }
                        // Path points loop
                        while (!pathfinished)
                        {

                            // New path point
                            thispath.Add(new int[3]);
                            thispath[thispath.Count - 1][0] = px - 1;
                            thispath[thispath.Count - 1][1] = py - 1;
                            thispath[thispath.Count - 1][2] = arr[py][px];

                            // Node types
                            if (arr[py][px] == 1)
                            {
                                arr[py][px] = 0;
                                if (dir == 0)
                                {
                                    py--; dir = 1;
                                }
                                else if (dir == 3)
                                {
                                    px--; dir = 2;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            else if (arr[py][px] == 2)
                            {
                                arr[py][px] = 0;
                                if (dir == 3)
                                {
                                    px++; dir = 0;
                                }
                                else if (dir == 2)
                                {
                                    py--; dir = 1;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            else if (arr[py][px] == 3)
                            {
                                arr[py][px] = 0;
                                if (dir == 0)
                                {
                                    px++;
                                }
                                else if (dir == 2)
                                {
                                    px--;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            else if (arr[py][px] == 4)
                            {
                                arr[py][px] = 0;
                                if (dir == 1)
                                {
                                    px++; dir = 0;
                                }
                                else if (dir == 2)
                                {
                                    py++; dir = 3;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            else if (arr[py][px] == 5)
                            {
                                if (dir == 0)
                                {
                                    arr[py][px] = 13; py++; dir = 3;
                                }
                                else if (dir == 1)
                                {
                                    arr[py][px] = 13; px--; dir = 2;
                                }
                                else if (dir == 2)
                                {
                                    arr[py][px] = 7; py--; dir = 1;
                                }
                                else if (dir == 3)
                                {
                                    arr[py][px] = 7; px++; dir = 0;
                                }
                            }

                            else if (arr[py][px] == 6)
                            {
                                arr[py][px] = 0;
                                if (dir == 1)
                                {
                                    py--;
                                }
                                else if (dir == 3)
                                {
                                    py++;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            else if (arr[py][px] == 7)
                            {
                                arr[py][px] = 0;
                                if (dir == 0)
                                {
                                    py++; dir = 3;
                                }
                                else if (dir == 1)
                                {
                                    px--; dir = 2;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            else if (arr[py][px] == 8)
                            {
                                arr[py][px] = 0;
                                if (dir == 0)
                                {
                                    py++; dir = 3;
                                }
                                else if (dir == 1)
                                {
                                    px--; dir = 2;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            else if (arr[py][px] == 9)
                            {
                                arr[py][px] = 0;
                                if (dir == 1)
                                {
                                    py--;
                                }
                                else if (dir == 3)
                                {
                                    py++;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            else if (arr[py][px] == 10)
                            {
                                if (dir == 0)
                                {
                                    arr[py][px] = 11; py--; dir = 1;
                                }
                                else if (dir == 1)
                                {
                                    arr[py][px] = 14; px++; dir = 0;
                                }
                                else if (dir == 2)
                                {
                                    arr[py][px] = 14; py++; dir = 3;
                                }
                                else if (dir == 3)
                                {
                                    arr[py][px] = 11; px--; dir = 2;
                                }
                            }

                            else if (arr[py][px] == 11)
                            {
                                arr[py][px] = 0;
                                if (dir == 1)
                                {
                                    px++; dir = 0;
                                }
                                else if (dir == 2)
                                {
                                    py++; dir = 3;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            else if (arr[py][px] == 12)
                            {
                                arr[py][px] = 0;
                                if (dir == 0)
                                {
                                    px++;
                                }
                                else if (dir == 2)
                                {
                                    px--;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            else if (arr[py][px] == 13)
                            {
                                arr[py][px] = 0;
                                if (dir == 2)
                                {
                                    py--; dir = 1;
                                }
                                else if (dir == 3)
                                {
                                    px++; dir = 0;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            else if (arr[py][px] == 14)
                            {
                                arr[py][px] = 0;
                                if (dir == 0)
                                {
                                    py--; dir = 1;
                                }
                                else if (dir == 3)
                                {
                                    px--; dir = 2;
                                }
                                else { pathfinished = true; paths.Remove(thispath); }
                            }

                            // Close path
                            if ((px - 1 == thispath[0][0]) && (py - 1 == thispath[0][1]))
                            {
                                pathfinished = true;
                                // Discarding 'hole' type paths and paths shorter than pathomit
                                if (holepath || (thispath.Count < pathomit))
                                {
                                    paths.Remove(thispath);
                                }
                            }

                        }// End of Path points loop

                    }// End of Follow path

                }// End of i loop
            }// End of j loop

            return paths;
        }

        // 3. Batch pathscan
        private static TriListIntArray BatchPathScan(int[][][] layers, int pathomit)
        {
            var bpaths = new TriListIntArray();
            foreach (var layer in layers)
            {
                bpaths.Add(PathScan(layer, pathomit));
            }
            return bpaths;
        }

        // 4. interpolating between path points for nodes with 8 directions ( East, SouthEast, S, SW, W, NW, N, NE )
        private static List<List<double[]>> InterNodes(List<List<int[]>> paths)
        {
            var ins = new List<List<double[]>>();
            double[] nextpoint = new double[2];

            // paths loop
            for (var pacnt = 0; pacnt < paths.Count; pacnt++)
            {
                ins.Add(new List<double[]>());
                var thisinp = ins[ins.Count - 1];
                var palen = paths[pacnt].Count;
                // pathpoints loop
                for (var pcnt = 0; pcnt < palen; pcnt++)
                {
                    // interpolate between two path points
                    var nextidx = (pcnt + 1) % palen; var nextidx2 = (pcnt + 2) % palen;
                    thisinp.Add(new double[3]);
                    var thispoint = thisinp[thisinp.Count - 1];
                    var pp1 = paths[pacnt][pcnt];
                    var pp2 = paths[pacnt][nextidx];
                    var pp3 = paths[pacnt][nextidx2];
                    thispoint[0] = (pp1[0] + pp2[0]) / 2.0;
                    thispoint[1] = (pp1[1] + pp2[1]) / 2.0;
                    nextpoint[0] = (pp2[0] + pp3[0]) / 2.0;
                    nextpoint[1] = (pp2[1] + pp3[1]) / 2.0;

                    // line segment direction to the next point
                    if (thispoint[0] < nextpoint[0])
                    {
                        if (thispoint[1] < nextpoint[1]) { thispoint[2] = 1.0; }// SouthEast
                        else if (thispoint[1] > nextpoint[1]) { thispoint[2] = 7.0; }// NE
                        else { thispoint[2] = 0.0; } // E
                    }
                    else if (thispoint[0] > nextpoint[0])
                    {
                        if (thispoint[1] < nextpoint[1]) { thispoint[2] = 3.0; }// SW
                        else if (thispoint[1] > nextpoint[1]) { thispoint[2] = 5.0; }// NW
                        else { thispoint[2] = 4.0; }// N
                    }
                    else
                    {
                        if (thispoint[1] < nextpoint[1]) { thispoint[2] = 2.0; }// S
                        else if (thispoint[1] > nextpoint[1]) { thispoint[2] = 6.0; }// N
                        else { thispoint[2] = 8.0; }// center, this should not happen
                    }

                }// End of pathpoints loop

            }// End of paths loop

            return ins;
        }

        // 4. Batch interpollation
        private static TriListDoubleArray BatchInterNodes(TriListIntArray bpaths)
        {
            var binternodes = new TriListDoubleArray();
            for (var k = 0; k < bpaths.Count; k++)
            {
                binternodes.Add(InterNodes(bpaths[k]));
            }
            return binternodes;
        }

        // 5. tracepath() : recursively trying to fit straight and quadratic spline segments on the 8 direction internode path

        // 5.1. Find sequences of points with only 2 segment types
        // 5.2. Fit a straight line on the sequence
        // 5.3. If the straight line fails (an error>ltreshold), find the point with the biggest error
        // 5.4. Fit a quadratic spline through errorpoint (project this to get controlpoint), then measure errors on every point in the sequence
        // 5.5. If the spline fails (an error>qtreshold), find the point with the biggest error, set splitpoint = (fitting point + errorpoint)/2
        // 5.6. Split sequence and recursively apply 5.2. - 5.7. to startpoint-splitpoint and splitpoint-endpoint sequences
        // 5.7. TODO? If splitpoint-endpoint is a spline, try to add new points from the next sequence

        // This returns an SVG Path segment as a double[7] where
        // segment[0] ==1.0 linear  ==2.0 quadratic interpolation
        // segment[1] , segment[2] : x1 , y1
        // segment[3] , segment[4] : x2 , y2 ; middle point of Q curve, endpoint of L line
        // segment[5] , segment[6] : x3 , y3 for Q curve, should be 0.0 , 0.0 for L line
        //
        // path type is discarded, no check for path.size < 3 , which should not happen

        private static List<double[]> TracePath(List<double[]> path, double ltreshold, double qtreshold)
        {
            int pcnt = 0;
            var smp = new List<double[]>();
            //Double [] thissegment;
            var pathlength = path.Count;

            while (pcnt < pathlength)
            {
                // 5.1. Find sequences of points with only 2 segment types
                var segtype1 = path[pcnt][2]; double segtype2 = -1; var seqend = pcnt + 1;
                while ((path[seqend][2].AreEqual(segtype1) || path[seqend][2].AreEqual(segtype2) || segtype2.AreEqual(-1)) && (seqend < pathlength - 1))
                {
                    if (path[seqend][2].AreNotEqual(segtype1) && segtype2.AreEqual(-1)) { segtype2 = path[seqend][2]; }
                    seqend++;
                }
                if (seqend == pathlength - 1) { seqend = 0; }

                // 5.2. - 5.6. Split sequence and recursively apply 5.2. - 5.6. to startpoint-splitpoint and splitpoint-endpoint sequences
                smp.AddRange(FitSeq(path, ltreshold, qtreshold, pcnt, seqend));
                // 5.7. TODO? If splitpoint-endpoint is a spline, try to add new points from the next sequence

                // forward pcnt;
                if (seqend > 0) { pcnt = seqend; } else { pcnt = pathlength; }

            }// End of pcnt loop
            return smp;
        }

        // 5.2. - 5.6. recursively fitting a straight or quadratic line segment on this sequence of path nodes,
        // called from tracepath()
        private static List<double[]> FitSeq(List<double[]> path, double ltreshold, double qtreshold, int seqstart, int seqend)
        {
            var segment = new List<double[]>();
            double[] thissegment;
            var pathlength = path.Count;

            // return if invalid seqend
            if ((seqend > pathlength) || (seqend < 0)) { return segment; }

            var errorpoint = seqstart;
            var curvepass = true;
            double px, py, dist2, errorval = 0;
            double tl = seqend - seqstart; if (tl < 0) { tl += pathlength; }
            double vx = (path[seqend][0] - path[seqstart][0]) / tl,
                    vy = (path[seqend][1] - path[seqstart][1]) / tl;

            // 5.2. Fit a straight line on the sequence
            var pcnt = (seqstart + 1) % pathlength;
            while (pcnt != seqend)
            {
                double pl = pcnt - seqstart; if (pl < 0) { pl += pathlength; }
                px = path[seqstart][0] + vx * pl; py = path[seqstart][1] + vy * pl;
                dist2 = (path[pcnt][0] - px) * (path[pcnt][0] - px) + (path[pcnt][1] - py) * (path[pcnt][1] - py);
                if (dist2 > ltreshold) { curvepass = false; }
                if (dist2 > errorval) { errorpoint = pcnt; errorval = dist2; }
                pcnt = (pcnt + 1) % pathlength;
            }

            // return straight line if fits
            if (curvepass)
            {
                segment.Add(new double[7]);
                thissegment = segment[segment.Count - 1];
                thissegment[0] = 1.0;
                thissegment[1] = path[seqstart][0];
                thissegment[2] = path[seqstart][1];
                thissegment[3] = path[seqend][0];
                thissegment[4] = path[seqend][1];
                thissegment[5] = 0.0;
                thissegment[6] = 0.0;
                return segment;
            }

            // 5.3. If the straight line fails (an error>ltreshold), find the point with the biggest error
            var fitpoint = errorpoint; curvepass = true; errorval = 0;

            // 5.4. Fit a quadratic spline through this point, measure errors on every point in the sequence
            // helpers and projecting to get control point
            double t = (fitpoint - seqstart) / tl, t1 = (1.0 - t) * (1.0 - t), t2 = 2.0 * (1.0 - t) * t, t3 = t * t;
            double cpx = (t1 * path[seqstart][0] + t3 * path[seqend][0] - path[fitpoint][0]) / -t2,
                    cpy = (t1 * path[seqstart][1] + t3 * path[seqend][1] - path[fitpoint][1]) / -t2;

            // Check every point
            pcnt = seqstart + 1;
            while (pcnt != seqend)
            {

                t = (pcnt - seqstart) / tl; t1 = (1.0 - t) * (1.0 - t); t2 = 2.0 * (1.0 - t) * t; t3 = t * t;
                px = t1 * path[seqstart][0] + t2 * cpx + t3 * path[seqend][0];
                py = t1 * path[seqstart][1] + t2 * cpy + t3 * path[seqend][1];

                dist2 = (path[pcnt][0] - px) * (path[pcnt][0] - px) + (path[pcnt][1] - py) * (path[pcnt][1] - py);

                if (dist2 > qtreshold) { curvepass = false; }
                if (dist2 > errorval) { errorpoint = pcnt; errorval = dist2; }
                pcnt = (pcnt + 1) % pathlength;
            }

            // return spline if fits
            if (curvepass)
            {
                segment.Add(new double[7]);
                thissegment = segment[segment.Count - 1];
                thissegment[0] = 2.0;
                thissegment[1] = path[seqstart][0];
                thissegment[2] = path[seqstart][1];
                thissegment[3] = cpx;
                thissegment[4] = cpy;
                thissegment[5] = path[seqend][0];
                thissegment[6] = path[seqend][1];
                return segment;
            }

            // 5.5. If the spline fails (an error>qtreshold), find the point with the biggest error,
            // set splitpoint = (fitting point + errorpoint)/2
            var splitpoint = (fitpoint + errorpoint) / 2;

            // 5.6. Split sequence and recursively apply 5.2. - 5.6. to startpoint-splitpoint and splitpoint-endpoint sequences
            segment = FitSeq(path, ltreshold, qtreshold, seqstart, splitpoint);
            segment.AddRange(FitSeq(path, ltreshold, qtreshold, splitpoint, seqend));
            return segment;
        }

        // 5. Batch tracing paths
        private static List<List<double[]>> BatchTracePaths(List<List<double[]>> internodepaths, double ltres, double qtres)
        {
            var btracedpaths = new List<List<double[]>>();
            for (var k = 0; k < internodepaths.Count; k++)
            {
                btracedpaths.Add(TracePath(internodepaths[k], ltres, qtres));
            }
            return btracedpaths;
        }

        // 5. Batch tracing layers
        private static TriListDoubleArray BatchTraceLayers(TriListDoubleArray binternodes, double ltres, double qtres)
        {
            var btbis = new TriListDoubleArray();
            for (var k = 0; k < binternodes.Count; k++)
            {
                btbis.Add(BatchTracePaths(binternodes[k], ltres, qtres));
            }
            return btbis;
        }

        ////////////////////////////////////////////////////////////
        //
        //  SVG Drawing functions
        //
        ////////////////////////////////////////////////////////////

        private static double RoundToDec(double val, double places)
        {
            return Math.Round(val * Math.Pow(10, places)) / Math.Pow(10, places);
        }

        // Getting SVG path element string from a traced path
        private static void SvgPathString(StringBuilder sb, string desc, List<double[]> segments, string colorstr, Options options)
        {
            double scale = options.SvgRendering.Scale, lcpr = options.SvgRendering.LCpr, qcpr = options.SvgRendering.LCpr, roundcoords = Math.Floor(options.SvgRendering.RoundCoords);
            // Path
            sb.Append("<path ").Append(desc).Append(colorstr).Append("d=\"").Append("M ").Append(segments[0][1] * scale).Append(" ").Append(segments[0][2] * scale).Append(" ");

            if (roundcoords.AreEqual(-1))
            {
                for (var pcnt = 0; pcnt < segments.Count; pcnt++)
                {
                    if (segments[pcnt][0].AreEqual(1.0))
                    {
                        sb.Append("L ").Append(segments[pcnt][3] * scale).Append(" ").Append(segments[pcnt][4] * scale).Append(" ");
                    }
                    else
                    {
                        sb.Append("Q ").Append(segments[pcnt][3] * scale).Append(" ").Append(segments[pcnt][4] * scale).Append(" ").Append(segments[pcnt][5] * scale).Append(" ").Append(segments[pcnt][6] * scale).Append(" ");
                    }
                }
            }
            else
            {
                for (var pcnt = 0; pcnt < segments.Count; pcnt++)
                {
                    if (segments[pcnt][0].AreEqual(1.0))
                    {
                        sb.Append("L ").Append(RoundToDec(segments[pcnt][3] * scale, roundcoords)).Append(" ")
                        .Append(RoundToDec(segments[pcnt][4] * scale, roundcoords)).Append(" ");
                    }
                    else
                    {
                        sb.Append("Q ").Append(RoundToDec(segments[pcnt][3] * scale, roundcoords)).Append(" ")
                        .Append(RoundToDec(segments[pcnt][4] * scale, roundcoords)).Append(" ")
                        .Append(RoundToDec(segments[pcnt][5] * scale, roundcoords)).Append(" ")
                        .Append(RoundToDec(segments[pcnt][6] * scale, roundcoords)).Append(" ");
                    }
                }
            }// End of roundcoords check

            sb.Append("Z\" />");

            // Rendering control points
            for (var pcnt = 0; pcnt < segments.Count; pcnt++)
            {
                if ((lcpr > 0) && segments[pcnt][0].AreEqual(1.0))
                {
                    sb.Append("<circle cx=\"").Append(segments[pcnt][3] * scale).Append("\" cy=\"").Append(segments[pcnt][4] * scale).Append("\" r=\"").Append(lcpr).Append("\" fill=\"white\" stroke-width=\"").Append(lcpr * 0.2).Append("\" stroke=\"black\" />");
                }
                if ((qcpr > 0) && segments[pcnt][0].AreEqual(2.0))
                {
                    sb.Append("<circle cx=\"").Append(segments[pcnt][3] * scale).Append("\" cy=\"").Append(segments[pcnt][4] * scale).Append("\" r=\"").Append(qcpr).Append("\" fill=\"cyan\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"black\" />");
                    sb.Append("<circle cx=\"").Append(segments[pcnt][5] * scale).Append("\" cy=\"").Append(segments[pcnt][6] * scale).Append("\" r=\"").Append(qcpr).Append("\" fill=\"white\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"black\" />");
                    sb.Append("<line x1=\"").Append(segments[pcnt][1] * scale).Append("\" y1=\"").Append(segments[pcnt][2] * scale).Append("\" x2=\"").Append(segments[pcnt][3] * scale).Append("\" y2=\"").Append(segments[pcnt][4] * scale).Append("\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"cyan\" />");
                    sb.Append("<line x1=\"").Append(segments[pcnt][3] * scale).Append("\" y1=\"").Append(segments[pcnt][4] * scale).Append("\" x2=\"").Append(segments[pcnt][5] * scale).Append("\" y2=\"").Append(segments[pcnt][6] * scale).Append("\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"cyan\" />");
                }// End of quadratic control points
            }
        }

        // Converting tracedata to an SVG string, paths are drawn according to a Z-index
        // the optional lcpr and qcpr are linear and quadratic control point radiuses
        private static string GetSvgString(IndexedImage ii, Options options)
        {
            //options = checkoptions(options);
            // SVG start
            int w = (int)(ii.Width * options.SvgRendering.Scale), h = (int)(ii.Height * options.SvgRendering.Scale);
            var viewboxorviewport = options.SvgRendering.Viewbox.IsNotZero() ? "viewBox=\"0 0 " + w + " " + h + "\" " : "width=\"" + w + "\" height=\"" + h + "\" ";
            var svgstr = new StringBuilder("<svg " + viewboxorviewport + "version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" ");
            if (options.SvgRendering.Desc.IsNotZero()) { svgstr.Append("desc=\"Created with ImageTracer.java version " + VersionNumber + "\" "); }
            svgstr.Append(">");

            // creating Z-index
            var zindex = new SortedDictionary<double, int[]>(); //TreeMap<Double, Integer[]> zindex = new TreeMap<Double, Integer[]>();
            // Layer loop
            for (var k = 0; k < ii.Layers.Count; k++)
            {
                // Path loop
                for (var pcnt = 0; pcnt < ii.Layers[k].Count; pcnt++)
                {
                    // Label (Z-index key) is the startpoint of the path, linearized
                    var label = ii.Layers[k][pcnt][0][2] * w + ii.Layers[k][pcnt][0][1];
                    // Creating new list if required
                    if (!zindex.ContainsKey(label)) { zindex[label] = new int[2]; }
                    // Adding layer and path number to list
                    zindex[label][0] = k;
                    zindex[label][1] = pcnt;
                }// End of path loop
            }// End of layer loop

            // Sorting Z-index is not required, TreeMap is sorted automatically

            // Drawing
            // Z-index loop
            foreach(var entry in zindex)
            {
                var thisdesc = "";
                if (options.SvgRendering.Desc.IsNotZero()) { thisdesc = "desc=\"l " + entry.Value[0] + " p " + entry.Value[1] + "\" "; } else { thisdesc = ""; }
                SvgPathString(svgstr,
                        thisdesc,
                        ii.Layers[entry.Value[0]][entry.Value[1]],
                        ToSvgColorString(ii.Palette[entry.Value[0]]),
                        options);
            }

            // SVG End
            svgstr.Append("</svg>");

            return svgstr.ToString();

        }

        private static string ToSvgColorString(byte[] c)
        {
            const int shift = 0; // MJY: Try removing all the + 128 on the values. Might fix issues.
            return "fill=\"rgb(" + (c[0] + shift) + "," + (c[1] + shift) + "," + (c[2] + shift) + ")\" stroke=\"rgb(" + (c[0] + shift) + "," + (c[1] + shift) + "," + (c[2] + shift) + ")\" stroke-width=\"1\" opacity=\"" + (c[3] + shift) / 255.0 + "\" ";
        }

        // Gaussian kernels for blur
        private static readonly double[][] Gks = 
        {
            new []{0.27901, 0.44198, 0.27901},
            new []{0.135336, 0.228569, 0.272192, 0.228569, 0.135336},
            new []{0.086776, 0.136394, 0.178908, 0.195843, 0.178908, 0.136394, 0.086776},
            new []{0.063327, 0.093095, 0.122589, 0.144599, 0.152781, 0.144599, 0.122589, 0.093095, 0.063327},
            new []{0.049692, 0.069304, 0.089767, 0.107988, 0.120651, 0.125194, 0.120651, 0.107988, 0.089767, 0.069304, 0.049692}
        };

        // Selective Gaussian blur for preprocessing
        private static ImageData Blur(ImageData imgd, int rad, double del)
        {
            int i, j, k;
            int idx;
            double racc, gacc, bacc, aacc, wacc;
            var imgd2 = new ImageData(imgd.Width, imgd.Height, new byte[imgd.Width * imgd.Height * 4]);

            // radius and delta limits, this kernel
            var radius = rad; if (radius < 1) { return imgd; }
            if (radius > 5) { radius = 5; }
            var delta = (int)Math.Abs(del); if (delta > 1024) { delta = 1024; }
            var thisgk = Gks[radius - 1];

            // loop through all pixels, horizontal blur
            for (j = 0; j < imgd.Height; j++)
            {
                for (i = 0; i < imgd.Width; i++)
                {
                    racc = 0; gacc = 0; bacc = 0; aacc = 0; wacc = 0;
                    // gauss kernel loop
                    for (k = -radius; k < radius + 1; k++)
                    {
                        // add weighted color values
                        if ((i + k > 0) && (i + k < imgd.Width))
                        {
                            idx = (j * imgd.Width + i + k) * 4;
                            racc += imgd.Data[idx] * thisgk[k + radius];
                            gacc += imgd.Data[idx + 1] * thisgk[k + radius];
                            bacc += imgd.Data[idx + 2] * thisgk[k + radius];
                            aacc += imgd.Data[idx + 3] * thisgk[k + radius];
                            wacc += thisgk[k + radius];
                        }
                    }
                    // The new pixel
                    idx = (j * imgd.Width + i) * 4;
                    imgd2.Data[idx] = (byte)Math.Floor(racc / wacc);
                    imgd2.Data[idx + 1] = (byte)Math.Floor(gacc / wacc);
                    imgd2.Data[idx + 2] = (byte)Math.Floor(bacc / wacc);
                    imgd2.Data[idx + 3] = (byte)Math.Floor(aacc / wacc);

                }// End of width loop
            }// End of horizontal blur

            // copying the half blurred imgd2
            var himgd = imgd2.Data.Clone() as byte[];

            // loop through all pixels, vertical blur
            for (j = 0; j < imgd.Height; j++)
            {
                for (i = 0; i < imgd.Width; i++)
                {
                    racc = 0; gacc = 0; bacc = 0; aacc = 0; wacc = 0;
                    // gauss kernel loop
                    for (k = -radius; k < radius + 1; k++)
                    {
                        // add weighted color values
                        if ((j + k > 0) && (j + k < imgd.Height))
                        {
                            idx = ((j + k) * imgd.Width + i) * 4;
                            racc += himgd[idx] * thisgk[k + radius];
                            gacc += himgd[idx + 1] * thisgk[k + radius];
                            bacc += himgd[idx + 2] * thisgk[k + radius];
                            aacc += himgd[idx + 3] * thisgk[k + radius];
                            wacc += thisgk[k + radius];
                        }
                    }
                    // The new pixel
                    idx = (j * imgd.Width + i) * 4;
                    imgd2.Data[idx] = (byte)Math.Floor(racc / wacc);
                    imgd2.Data[idx + 1] = (byte)Math.Floor(gacc / wacc);
                    imgd2.Data[idx + 2] = (byte)Math.Floor(bacc / wacc);
                    imgd2.Data[idx + 3] = (byte)Math.Floor(aacc / wacc);
                }// End of width loop
            }// End of vertical blur

            // Selective blur: loop through all pixels
            for (j = 0; j < imgd.Height; j++)
            {
                for (i = 0; i < imgd.Width; i++)
                {
                    idx = (j * imgd.Width + i) * 4;
                    // d is the difference between the blurred and the original pixel
                    var d = Math.Abs(imgd2.Data[idx] - imgd.Data[idx]) + Math.Abs(imgd2.Data[idx + 1] - imgd.Data[idx + 1]) +
                            Math.Abs(imgd2.Data[idx + 2] - imgd.Data[idx + 2]) + Math.Abs(imgd2.Data[idx + 3] - imgd.Data[idx + 3]);
                    // selective blur: if d>delta, put the original pixel back
                    if (d > delta)
                    {
                        imgd2.Data[idx] = imgd.Data[idx];
                        imgd2.Data[idx + 1] = imgd.Data[idx + 1];
                        imgd2.Data[idx + 2] = imgd.Data[idx + 2];
                        imgd2.Data[idx + 3] = imgd.Data[idx + 3];
                    }
                }
            }// End of Selective blur
            return imgd2;
        }
    }
}
