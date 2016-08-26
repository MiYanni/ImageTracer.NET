using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using ImageTracerNet.Extensions;
//using OptionsDictionary = System.Collections.Generic.Dictionary<string, float>; // HashMap<String, Float>()
using TriListIntArray = System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<int[]>>>; // ArrayList<ArrayList<ArrayList<Integer[]>>>
using TriListDoubleArray = System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>; // ArrayList<ArrayList<ArrayList<Double[]>>>

namespace ImageTracerNet
{
    public class ImageTracer
    {
        public static string versionnumber = "1.1.1";

        private static readonly Random Rng = new Random();

        public ImageTracer() { }

        // Loading a file to ImageData, ARGB byte order
        public static ImageData loadImageData(string filename)
        {
            var image = new Bitmap(filename);
            return loadImageData(image);
        }
            
        public static ImageData loadImageData(Bitmap image)
        {
            int width = image.Width; int height = image.Height;
            //var data1 = image.ToByteArray();
            //var colors = image.ToColorArray();
            //var sdata = image.ToSByteArray();
            //Color ARGB = Color.FromArgb(A, R, G, B)
            //var myArray = (byte[])new ImageConverter().ConvertTo(image, typeof(byte[]));
            //byte[] myArray;
            //using (var ms = new MemoryStream())
            //{
            //    image.Save(ms, image.RawFormat);
            //    myArray = ms.ToArray();
            //}
            var rbgImage = image.ChangeFormat(PixelFormat.Format32bppArgb);
            var data = rbgImage.ToRgbaByteArray();
            //int[] rawdata = rbgImage.ToIntArray();
            //byte[] data = new byte[rawdata.Length * 4];
            //for(int i = 0; i<rawdata.Length; i++)
            //{
            //    data[(i * 4) + 3] = bytetrans((byte)(rawdata[i] >> 24));
            //    data[i * 4] = bytetrans((byte)(rawdata[i] >> 16));
            //    data[(i * 4) + 1] = bytetrans((byte)(rawdata[i] >> 8));
            //    data[(i * 4) + 2] = bytetrans((byte)(rawdata[i]));
            //}
            //return new ImageData(width, height, data);
            return new ImageData(width, height, data);
        }

        // The bitshift method in loadImageData creates signed bytes where -1 -> 255 unsigned ; -128 -> 128 unsigned ;
        // 127 -> 127 unsigned ; 0 -> 0 unsigned ; These will be converted to -128 (representing 0 unsigned) ...
        // 127 (representing 255 unsigned) and tosvgcolorstr will add +128 to create RGB values 0..255
        public static byte bytetrans(byte b)
        {
            // MJY: This might be an issue.
            //if (b < 0) { return (byte)(b + 128); } else { return (byte)(b - 128); }
            return b;
        }

        ////////////////////////////////////////////////////////////
        //
        //  User friendly functions
        //
        ////////////////////////////////////////////////////////////

        // Loading an image from a file, tracing when loaded, then returning the SVG String
        public static String imageToSVG(string filename, Options options, byte[][] palette) 
        {
            //options = checkoptions(options);
            ImageData imgd = loadImageData(filename);
            return imagedataToSVG(imgd,options,palette);
        }// End of imageToSVG()
        public static String imageToSVG(Bitmap image, Options options, byte[][] palette) 
        {
            //options = checkoptions(options);
            ImageData imgd = loadImageData(image);
            return imagedataToSVG(imgd,options,palette);
        }// End of imageToSVG()

        // Tracing ImageData, then returning the SVG String
        public static String imagedataToSVG(ImageData imgd, Options options, byte[][] palette)
        {
            //options = checkoptions(options);
            IndexedImage ii = imagedataToTracedata(imgd, options, palette);
            return getsvgstring(ii, options);
        }// End of imagedataToSVG()

        // Loading an image from a file, tracing when loaded, then returning IndexedImage with tracedata in layers
        public IndexedImage imageToTracedata(string filename, Options options, byte[][] palette) 
        {
            //options = checkoptions(options);
            ImageData imgd = loadImageData(filename);
            return imagedataToTracedata(imgd,options,palette);
        }// End of imageToTracedata()
        public IndexedImage imageToTracedata(Bitmap image, Options options, byte[][] palette) 
        {
            //options = checkoptions(options);
            ImageData imgd = loadImageData(image);
            return imagedataToTracedata(imgd,options,palette);
        }// End of imageToTracedata()

        // Tracing ImageData, then returning IndexedImage with tracedata in layers
        public static IndexedImage imagedataToTracedata(ImageData imgd, Options options, byte[][] palette)
        {
            // 1. Color quantization
            IndexedImage ii = colorquantization(imgd, palette, options);
            // 2. Layer separation and edge detection
            int[][][] rawlayers = layering(ii);
            // 3. Batch pathscan
            TriListIntArray bps = batchpathscan(rawlayers, (int)Math.Floor(options.Tracing.PathOmit));
            // 4. Batch interpollation
            TriListDoubleArray bis = batchinternodes(bps);
            // 5. Batch tracing
            ii.layers = batchtracelayers(bis, (float)options.Tracing.LTres, (float)options.Tracing.QTres);
            return ii;
        }// End of imagedataToTracedata()

        //// creating options object, setting defaults for missing values
        //public static Options checkoptions(Dictionary<string, float> options)
        //{
        //    if (options == null) { options = new Options(); }
        //    // Tracing
        //    if (!options.ContainsKey("ltres")) { options["ltres"] = 1f; }
        //    if (!options.ContainsKey("qtres")) { options["qtres"] = 1f; }
        //    if (!options.ContainsKey("pathomit")) { options["pathomit"] = 8f; }
        //    // Color quantization
        //    if (!options.ContainsKey("colorsampling")) { options["colorsampling"] = 1f; }
        //    if (!options.ContainsKey("numberofcolors")) { options["numberofcolors"] = 16f; }
        //    if (!options.ContainsKey("mincolorratio")) { options["mincolorratio"] = 0.02f; }
        //    if (!options.ContainsKey("colorquantcycles")) { options["colorquantcycles"] = 3f; }
        //    // SVG rendering
        //    if (!options.ContainsKey("scale")) { options["scale"] = 1f; }
        //    if (!options.ContainsKey("simplifytolerance")) { options["simplifytolerance"] = 0f; }
        //    if (!options.ContainsKey("roundcoords")) { options["roundcoords"] = 1f; }
        //    if (!options.ContainsKey("lcpr")) { options["lcpr"] = 0f; }
        //    if (!options.ContainsKey("qcpr")) { options["qcpr"] =0f; }
        //    if (!options.ContainsKey("desc")) { options["desc"] = 1f; }
        //    if (!options.ContainsKey("viewbox")) { options["viewbox"] = 0f; }
        //    // Blur
        //    if (!options.ContainsKey("blurradius")) { options["blurradius"] = 0f; }
        //    if (!options.ContainsKey("blurdelta")) { options["blurdelta"] = 20f; }

        //    return options;
        //}// End of checkoptions()


        ////////////////////////////////////////////////////////////
        //
        //  Vectorizing functions
        //
        ////////////////////////////////////////////////////////////

        // 1. Color quantization repeated "cycles" times, based on K-means clustering
        // https://en.wikipedia.org/wiki/Color_quantization    https://en.wikipedia.org/wiki/K-means_clustering
        public static IndexedImage colorquantization(ImageData imgd, byte[][] palette, Options options)
        {
            int numberofcolors = (int)Math.Floor(options.ColorQuantization.NumberOfColors);
            float minratio = (float)options.ColorQuantization.MinColorRatio;
            int cycles = (int)Math.Floor(options.ColorQuantization.ColorQuantCycles);
            // Creating indexed color array arr which has a boundary filled with -1 in every direction
            int[][] arr = new int[imgd.height + 2][].InitInner(imgd.width + 2);
            for (int j = 0; j < (imgd.height + 2); j++) { arr[j][0] = -1; arr[j][imgd.width + 1] = -1; }
            for (int i = 0; i < (imgd.width + 2); i++) { arr[0][i] = -1; arr[imgd.height + 1][i] = -1; }

            int idx = 0, cd, cdl, ci, c1, c2, c3, c4;

            // Use custom palette if pal is defined or sample or generate custom length palette
            if (palette == null)
            {
                if (options.ColorQuantization.ColorSampling.IsNotZero())
                {
                    palette = samplepalette(numberofcolors, imgd);
                }
                else
                {
                    palette = generatepalette(numberofcolors);
                }
            }

            // Selective Gaussian blur preprocessing
            if (options.Blur.BlurRadius > 0) { imgd = blur(imgd, (float)options.Blur.BlurRadius, (float)options.Blur.BlurDelta); }

            long[][] paletteacc = new long[palette.Length][].InitInner(5);

            // Repeat clustering step "cycles" times
            for (int cnt = 0; cnt < cycles; cnt++)
            {

                // Average colors from the second iteration
                if (cnt > 0)
                {
                    // averaging paletteacc for palette
                    float ratio;
                    for (int k = 0; k < palette.Length; k++)
                    {
                        const int shift = 0; // MJY: From -128
                        // averaging
                        if (paletteacc[k][3] > 0)
                        {
                            palette[k][0] = (byte)(shift + Math.Floor((double)(paletteacc[k][0] / (double)paletteacc[k][4])));
                            palette[k][1] = (byte)(shift + Math.Floor((double)(paletteacc[k][1] / (double)paletteacc[k][4])));
                            palette[k][2] = (byte)(shift + Math.Floor((double)(paletteacc[k][2] / (double)paletteacc[k][4])));
                            palette[k][3] = (byte)(shift + Math.Floor((double)(paletteacc[k][3] / (double)paletteacc[k][4])));
                        }
                        ratio = paletteacc[k][4] / (float)(imgd.width * imgd.height);

                        // Randomizing a color, if there are too few pixels and there will be a new cycle
                        if ((ratio < minratio) && (cnt < (cycles - 1)))
                        {
                            palette[k][0] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                            palette[k][1] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                            palette[k][2] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                            palette[k][3] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                        }

                    }// End of palette loop
                }// End of Average colors from the second iteration

                // Reseting palette accumulator for averaging
                for (int i = 0; i < palette.Length; i++)
                {
                    paletteacc[i][0] = 0;
                    paletteacc[i][1] = 0;
                    paletteacc[i][2] = 0;
                    paletteacc[i][3] = 0;
                    paletteacc[i][4] = 0;
                }

                // loop through all pixels
                for (int j = 0; j < imgd.height; j++)
                {
                    for (int i = 0; i < imgd.width; i++)
                    {

                        idx = ((j * imgd.width) + i) * 4;

                        // find closest color from palette by measuring (rectilinear) color distance between this pixel and all palette colors
                        cdl = 256 + 256 + 256 + 256; ci = 0;
                        for (int k = 0; k < palette.Length; k++)
                        {
                            // In my experience, https://en.wikipedia.org/wiki/Rectilinear_distance works better than https://en.wikipedia.org/wiki/Euclidean_distance
                            c1 = Math.Abs(palette[k][0] - imgd.data[idx]);
                            c2 = Math.Abs(palette[k][1] - imgd.data[idx + 1]);
                            c3 = Math.Abs(palette[k][2] - imgd.data[idx + 2]);
                            c4 = Math.Abs(palette[k][3] - imgd.data[idx + 3]);
                            cd = c1 + c2 + c3 + (c4 * 4); // weighted alpha seems to help images with transparency

                            // Remember this color if this is the closest yet
                            if (cd < cdl) { cdl = cd; ci = k; }

                        }// End of palette loop

                        const int shift = 0; // MJY: From 128

                        // add to palettacc
                        paletteacc[ci][0] += shift + imgd.data[idx];
                        paletteacc[ci][1] += shift + imgd.data[idx + 1];
                        paletteacc[ci][2] += shift + imgd.data[idx + 2];
                        paletteacc[ci][3] += shift + imgd.data[idx + 3];
                        paletteacc[ci][4]++;

                        arr[j + 1][i + 1] = ci;
                    }// End of i loop
                }// End of j loop

            }// End of Repeat clustering step "cycles" times

            return new IndexedImage(arr, palette);
        }// End of colorquantization

        // Generating a palette with numberofcolors, array[numberofcolors][4] where [i][0] = R ; [i][1] = G ; [i][2] = B ; [i][3] = A
        public static byte[][] generatepalette(int numberofcolors)
        {
            byte[][] palette = new byte[numberofcolors][].InitInner(4);
            if (numberofcolors < 8)
            {
                const int shift = 0; // MJY: From -128
                // Grayscale
                byte graystep = (byte)Math.Floor((double)(255 / (double)(numberofcolors - 1)));
                for (byte ccnt = 0; ccnt < numberofcolors; ccnt++)
                {
                    palette[ccnt][0] = (byte)(shift + (ccnt * graystep));
                    palette[ccnt][1] = (byte)(shift + (ccnt * graystep));
                    palette[ccnt][2] = (byte)(shift + (ccnt * graystep));
                    palette[ccnt][3] = (byte)255;
                }
            }
            else
            {
                const int shift = 0; // MJY: From -128
                // RGB color cube
                int colorqnum = (int)Math.Floor(Math.Pow(numberofcolors, 1.0 / 3.0)); // Number of points on each edge on the RGB color cube
                int colorstep = (int)Math.Floor((double)(255 / (double)(colorqnum - 1))); // distance between points
                int ccnt = 0;
                for (int rcnt = 0; rcnt < colorqnum; rcnt++)
                {
                    for (int gcnt = 0; gcnt < colorqnum; gcnt++)
                    {
                        for (int bcnt = 0; bcnt < colorqnum; bcnt++)
                        {
                            palette[ccnt][0] = (byte)(shift + (rcnt * colorstep));
                            palette[ccnt][1] = (byte)(shift + (gcnt * colorstep));
                            palette[ccnt][2] = (byte)(shift + (bcnt * colorstep));
                            palette[ccnt][3] = (byte)127;
                            ccnt++;
                        }// End of blue loop
                    }// End of green loop
                }// End of red loop

                // Rest is random
                for (int rcnt = ccnt; rcnt < numberofcolors; rcnt++)
                {
                    palette[ccnt][0] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                    palette[ccnt][1] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                    palette[ccnt][2] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                    palette[ccnt][3] = (byte)(shift + Math.Floor(Rng.NextDouble() * 255));
                }
            }// End of numberofcolors check
            return palette;
        }// End of generatepalette()

        public static byte[][] samplepalette(int numberofcolors, ImageData imgd)
        {
            int idx = 0; byte[][] palette = new byte[numberofcolors][].InitInner(4);
            for (int i = 0; i < numberofcolors; i++)
            {
                idx = (int)(Math.Floor((Rng.NextDouble() * imgd.data.Length) / 4) * 4);
                palette[i][0] = imgd.data[idx];
                palette[i][1] = imgd.data[idx + 1];
                palette[i][2] = imgd.data[idx + 2];
                palette[i][3] = imgd.data[idx + 3];
            }
            return palette;
        }// End of samplepalette()

        // 2. Layer separation and edge detection
        // Edge node types ( ▓:light or 1; ░:dark or 0 )
        // 12  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓
        // 48  ░░  ░░  ░░  ░░  ░▓  ░▓  ░▓  ░▓  ▓░  ▓░  ▓░  ▓░  ▓▓  ▓▓  ▓▓  ▓▓
        //     0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
        //
        public static int[][][] layering(IndexedImage ii)
        {
            // Creating layers for each indexed color in arr
            int val = 0, aw = ii.array[0].Length, ah = ii.array.Length, n1, n2, n3, n4, n5, n6, n7, n8;
            int[][][] layers = new int[ii.palette.Length][][].InitInner(ah, aw);

            // Looping through all pixels and calculating edge node type
            for (int j = 1; j < (ah - 1); j++)
            {
                for (int i = 1; i < (aw - 1); i++)
                {

                    // This pixel's indexed color
                    val = ii.array[j][i];

                    // Are neighbor pixel colors the same?
                    if ((j > 0) && (i > 0)) { n1 = ii.array[j - 1][i - 1] == val ? 1 : 0; } else { n1 = 0; }
                    if (j > 0) { n2 = ii.array[j - 1][i] == val ? 1 : 0; } else { n2 = 0; }
                    if ((j > 0) && (i < (aw - 1))) { n3 = ii.array[j - 1][i + 1] == val ? 1 : 0; } else { n3 = 0; }
                    if (i > 0) { n4 = ii.array[j][i - 1] == val ? 1 : 0; } else { n4 = 0; }
                    if (i < (aw - 1)) { n5 = ii.array[j][i + 1] == val ? 1 : 0; } else { n5 = 0; }
                    if ((j < (ah - 1)) && (i > 0)) { n6 = ii.array[j + 1][i - 1] == val ? 1 : 0; } else { n6 = 0; }
                    if (j < (ah - 1)) { n7 = ii.array[j + 1][i] == val ? 1 : 0; } else { n7 = 0; }
                    if ((j < (ah - 1)) && (i < (aw - 1))) { n8 = ii.array[j + 1][i + 1] == val ? 1 : 0; } else { n8 = 0; }

                    // this pixel"s type and looking back on previous pixels
                    layers[val][j + 1][i + 1] = 1 + (n5 * 2) + (n8 * 4) + (n7 * 8);
                    if (n4 == 0) { layers[val][j + 1][i] = 0 + 2 + (n7 * 4) + (n6 * 8); }
                    if (n2 == 0) { layers[val][j][i + 1] = 0 + (n3 * 2) + (n5 * 4) + 8; }
                    if (n1 == 0) { layers[val][j][i] = 0 + (n2 * 2) + 4 + (n4 * 8); }

                }// End of i loop
            }// End of j loop

            return layers;
        }// End of layering()

        // 3. Walking through an edge node array, discarding edge node types 0 and 15 and creating paths from the rest.
        // Walk directions (dir): 0 > ; 1 ^ ; 2 < ; 3 v
        // Edge node types ( ▓:light or 1; ░:dark or 0 )
        // ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓
        // ░░  ░░  ░░  ░░  ░▓  ░▓  ░▓  ░▓  ▓░  ▓░  ▓░  ▓░  ▓▓  ▓▓  ▓▓  ▓▓
        // 0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
        //
        public static List<List<int[]>> pathscan(int[][] arr, float pathomit)
        {
            List<List<int[]>> paths = new List<List<int[]>>();
            List<int[]> thispath;
            int px = 0, py = 0, w = arr[0].Length, h = arr.Length, dir = 0;
            bool pathfinished = true, holepath = false;

            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    if ((arr[j][i] != 0) && (arr[j][i] != 15))
                    {
                        // Init
                        px = i; py = j;
                        paths.Add(new List<int[]>());
                        thispath = paths[paths.Count - 1];
                        pathfinished = false;
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
                            if (((px - 1) == thispath[0][0]) && ((py - 1) == thispath[0][1]))
                            {
                                pathfinished = true;
                                // Discarding 'hole' type paths and paths shorter than pathomit
                                if ((holepath) || (thispath.Count < pathomit))
                                {
                                    paths.Remove(thispath);
                                }
                            }

                        }// End of Path points loop

                    }// End of Follow path

                }// End of i loop
            }// End of j loop

            return paths;
        }// End of pathscan()

        // 3. Batch pathscan
        public static TriListIntArray batchpathscan(int[][][] layers, float pathomit)
        {
            TriListIntArray bpaths = new TriListIntArray();
            foreach (int[][] layer in layers)
            {
                bpaths.Add(pathscan(layer, pathomit));
            }
            return bpaths;
        }

        // 4. interpolating between path points for nodes with 8 directions ( East, SouthEast, S, SW, W, NW, N, NE )
        public static List<List<double[]>> internodes(List<List<int[]>> paths)
        {
            List<List<double[]>> ins = new List<List<double[]>>();
            List<double[]> thisinp;
            double[] thispoint, nextpoint = new double[2];
            int[] pp1, pp2, pp3;

            int palen = 0, nextidx = 0, nextidx2 = 0;
            // paths loop
            for (int pacnt = 0; pacnt < paths.Count; pacnt++)
            {
                ins.Add(new List<double[]>());
                thisinp = ins[ins.Count - 1];
                palen = paths[pacnt].Count;
                // pathpoints loop
                for (int pcnt = 0; pcnt < palen; pcnt++)
                {
                    // interpolate between two path points
                    nextidx = (pcnt + 1) % palen; nextidx2 = (pcnt + 2) % palen;
                    thisinp.Add(new double[3]);
                    thispoint = thisinp[thisinp.Count - 1];
                    pp1 = paths[pacnt][pcnt];
                    pp2 = paths[pacnt][nextidx];
                    pp3 = paths[pacnt][nextidx2];
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
        }// End of internodes()

        // 4. Batch interpollation
        private static TriListDoubleArray batchinternodes(TriListIntArray bpaths)
        {
            TriListDoubleArray binternodes = new TriListDoubleArray();
            for (int k = 0; k < bpaths.Count; k++)
            {
                binternodes.Add(internodes(bpaths[k]));
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

        public static List<double[]> tracepath(List<double[]> path, float ltreshold, float qtreshold)
        {
            int pcnt = 0, seqend = 0; double segtype1, segtype2;
            List<double[]> smp = new List<double[]>();
            //Double [] thissegment;
            int pathlength = path.Count;

            while (pcnt < pathlength)
            {
                // 5.1. Find sequences of points with only 2 segment types
                segtype1 = path[pcnt][2]; segtype2 = -1; seqend = pcnt + 1;
                while (((path[seqend][2] == segtype1) || (path[seqend][2] == segtype2) || (segtype2 == -1)) && (seqend < (pathlength - 1)))
                {
                    if ((path[seqend][2] != segtype1) && (segtype2 == -1)) { segtype2 = path[seqend][2]; }
                    seqend++;
                }
                if (seqend == (pathlength - 1)) { seqend = 0; }

                // 5.2. - 5.6. Split sequence and recursively apply 5.2. - 5.6. to startpoint-splitpoint and splitpoint-endpoint sequences
                smp.AddRange(fitseq(path, ltreshold, qtreshold, pcnt, seqend));
                // 5.7. TODO? If splitpoint-endpoint is a spline, try to add new points from the next sequence

                // forward pcnt;
                if (seqend > 0) { pcnt = seqend; } else { pcnt = pathlength; }

            }// End of pcnt loop
            return smp;
        }// End of tracepath()

        // 5.2. - 5.6. recursively fitting a straight or quadratic line segment on this sequence of path nodes,
        // called from tracepath()
        public static List<double[]> fitseq(List<double[]> path, float ltreshold, float qtreshold, int seqstart, int seqend)
        {
            List<double[]> segment = new List<double[]>();
            double[] thissegment;
            int pathlength = path.Count;

            // return if invalid seqend
            if ((seqend > pathlength) || (seqend < 0)) { return segment; }

            int errorpoint = seqstart;
            bool curvepass = true;
            double px, py, dist2, errorval = 0;
            double tl = (seqend - seqstart); if (tl < 0) { tl += pathlength; }
            double vx = (path[seqend][0] - path[seqstart][0]) / tl,
                    vy = (path[seqend][1] - path[seqstart][1]) / tl;

            // 5.2. Fit a straight line on the sequence
            int pcnt = (seqstart + 1) % pathlength;
            double pl;
            while (pcnt != seqend)
            {
                pl = pcnt - seqstart; if (pl < 0) { pl += pathlength; }
                px = path[seqstart][0] + (vx * pl); py = path[seqstart][1] + (vy * pl);
                dist2 = ((path[pcnt][0] - px) * (path[pcnt][0] - px)) + ((path[pcnt][1] - py) * (path[pcnt][1] - py));
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
            int fitpoint = errorpoint; curvepass = true; errorval = 0;

            // 5.4. Fit a quadratic spline through this point, measure errors on every point in the sequence
            // helpers and projecting to get control point
            double t = (fitpoint - seqstart) / tl, t1 = (1.0 - t) * (1.0 - t), t2 = 2.0 * (1.0 - t) * t, t3 = t * t;
            double cpx = (((t1 * path[seqstart][0]) + (t3 * path[seqend][0])) - path[fitpoint][0]) / -t2,
                    cpy = (((t1 * path[seqstart][1]) + (t3 * path[seqend][1])) - path[fitpoint][1]) / -t2;

            // Check every point
            pcnt = seqstart + 1;
            while (pcnt != seqend)
            {

                t = (pcnt - seqstart) / tl; t1 = (1.0 - t) * (1.0 - t); t2 = 2.0 * (1.0 - t) * t; t3 = t * t;
                px = (t1 * path[seqstart][0]) + (t2 * cpx) + (t3 * path[seqend][0]);
                py = (t1 * path[seqstart][1]) + (t2 * cpy) + (t3 * path[seqend][1]);

                dist2 = ((path[pcnt][0] - px) * (path[pcnt][0] - px)) + ((path[pcnt][1] - py) * (path[pcnt][1] - py));

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
            int splitpoint = (fitpoint + errorpoint) / 2;

            // 5.6. Split sequence and recursively apply 5.2. - 5.6. to startpoint-splitpoint and splitpoint-endpoint sequences
            segment = fitseq(path, ltreshold, qtreshold, seqstart, splitpoint);
            segment.AddRange(fitseq(path, ltreshold, qtreshold, splitpoint, seqend));
            return segment;
        }// End of fitseq()

        // 5. Batch tracing paths
        public static List<List<double[]>> batchtracepaths(List<List<double[]>> internodepaths, float ltres, float qtres)
        {
            List<List<double[]>> btracedpaths = new List<List<double[]>>();
            for (int k = 0; k < internodepaths.Count; k++)
            {
                btracedpaths.Add(tracepath(internodepaths[k], ltres, qtres));
            }
            return btracedpaths;
        }

        // 5. Batch tracing layers
        public static TriListDoubleArray batchtracelayers(TriListDoubleArray binternodes, float ltres, float qtres)
        {
            TriListDoubleArray btbis = new TriListDoubleArray();
            for (int k = 0; k < binternodes.Count; k++)
            {
                btbis.Add(batchtracepaths(binternodes[k], ltres, qtres));
            }
            return btbis;
        }

        ////////////////////////////////////////////////////////////
        //
        //  SVG Drawing functions
        //
        ////////////////////////////////////////////////////////////

        public static float roundtodec(float val, float places)
        {
            return (float)(Math.Round(val * Math.Pow(10, places)) / Math.Pow(10, places));
        }

        // Getting SVG path element string from a traced path
        public static void svgpathstring(StringBuilder sb, string desc, List<double[]> segments, string colorstr, Options options)
        {
            float scale = (float)options.SvgRendering.Scale, lcpr = (float)options.SvgRendering.LCpr, qcpr = (float)options.SvgRendering.LCpr, roundcoords = (float)Math.Floor(options.SvgRendering.RoundCoords);
            // Path
            sb.Append("<path ").Append(desc).Append(colorstr).Append("d=\"").Append("M ").Append(segments[0][1] * scale).Append(" ").Append(segments[0][2] * scale).Append(" ");

            if (roundcoords == -1)
            {
                for (int pcnt = 0; pcnt < segments.Count; pcnt++)
                {
                    if (segments[pcnt][0] == 1.0)
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
                for (int pcnt = 0; pcnt < segments.Count; pcnt++)
                {
                    if (segments[pcnt][0] == 1.0)
                    {
                        sb.Append("L ").Append(roundtodec((float)(segments[pcnt][3] * scale), roundcoords)).Append(" ")
                        .Append(roundtodec((float)(segments[pcnt][4] * scale), roundcoords)).Append(" ");
                    }
                    else
                    {
                        sb.Append("Q ").Append(roundtodec((float)(segments[pcnt][3] * scale), roundcoords)).Append(" ")
                        .Append(roundtodec((float)(segments[pcnt][4] * scale), roundcoords)).Append(" ")
                        .Append(roundtodec((float)(segments[pcnt][5] * scale), roundcoords)).Append(" ")
                        .Append(roundtodec((float)(segments[pcnt][6] * scale), roundcoords)).Append(" ");
                    }
                }
            }// End of roundcoords check

            sb.Append("Z\" />");

            // Rendering control points
            for (int pcnt = 0; pcnt < segments.Count; pcnt++)
            {
                if ((lcpr > 0) && (segments[pcnt][0] == 1.0))
                {
                    sb.Append("<circle cx=\"").Append(segments[pcnt][3] * scale).Append("\" cy=\"").Append(segments[pcnt][4] * scale).Append("\" r=\"").Append(lcpr).Append("\" fill=\"white\" stroke-width=\"").Append(lcpr * 0.2).Append("\" stroke=\"black\" />");
                }
                if ((qcpr > 0) && (segments[pcnt][0] == 2.0))
                {
                    sb.Append("<circle cx=\"").Append(segments[pcnt][3] * scale).Append("\" cy=\"").Append(segments[pcnt][4] * scale).Append("\" r=\"").Append(qcpr).Append("\" fill=\"cyan\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"black\" />");
                    sb.Append("<circle cx=\"").Append(segments[pcnt][5] * scale).Append("\" cy=\"").Append(segments[pcnt][6] * scale).Append("\" r=\"").Append(qcpr).Append("\" fill=\"white\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"black\" />");
                    sb.Append("<line x1=\"").Append(segments[pcnt][1] * scale).Append("\" y1=\"").Append(segments[pcnt][2] * scale).Append("\" x2=\"").Append(segments[pcnt][3] * scale).Append("\" y2=\"").Append(segments[pcnt][4] * scale).Append("\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"cyan\" />");
                    sb.Append("<line x1=\"").Append(segments[pcnt][3] * scale).Append("\" y1=\"").Append(segments[pcnt][4] * scale).Append("\" x2=\"").Append(segments[pcnt][5] * scale).Append("\" y2=\"").Append(segments[pcnt][6] * scale).Append("\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"cyan\" />");
                }// End of quadratic control points
            }
        }// End of svgpathstring()

        // Converting tracedata to an SVG string, paths are drawn according to a Z-index
        // the optional lcpr and qcpr are linear and quadratic control point radiuses
        public static String getsvgstring(IndexedImage ii, Options options)
        {
            //options = checkoptions(options);
            // SVG start
            int w = (int)(ii.width * options.SvgRendering.Scale), h = (int)(ii.height * options.SvgRendering.Scale);
            String viewboxorviewport = options.SvgRendering.Viewbox.IsNotZero() ? "viewBox=\"0 0 " + w + " " + h + "\" " : "width=\"" + w + "\" height=\"" + h + "\" ";
            StringBuilder svgstr = new StringBuilder("<svg " + viewboxorviewport + "version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" ");
            if (options.SvgRendering.Desc.IsNotZero()) { svgstr.Append("desc=\"Created with ImageTracer.java version " + versionnumber + "\" "); }
            svgstr.Append(">");

            // creating Z-index
            SortedDictionary<double, int[]> zindex = new SortedDictionary<double, int[]>(); //TreeMap<Double, Integer[]> zindex = new TreeMap<Double, Integer[]>();
            double label;
            // Layer loop
            for (int k = 0; k < ii.layers.Count; k++)
            {
                // Path loop
                for (int pcnt = 0; pcnt < ii.layers[k].Count; pcnt++)
                {
                    // Label (Z-index key) is the startpoint of the path, linearized
                    label = (ii.layers[k][pcnt][0][2] * w) + ii.layers[k][pcnt][0][1];
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
            String thisdesc = "";
            foreach(KeyValuePair<double, int[]> entry in zindex)
            {
                if (options.SvgRendering.Desc.IsNotZero()) { thisdesc = "desc=\"l " + entry.Value[0] + " p " + entry.Value[1] + "\" "; } else { thisdesc = ""; }
                svgpathstring(svgstr,
                        thisdesc,
                        ii.layers[entry.Value[0]][entry.Value[1]],
                        tosvgcolorstr(ii.palette[entry.Value[0]]),
                        options);
            }

            // SVG End
            svgstr.Append("</svg>");

            return svgstr.ToString();

        }// End of getsvgstring()

        static String tosvgcolorstr(byte[] c)
        {
            const int shift = 0; // MJY: Try removing all the + 128 on the values. Might fix issues.
            return "fill=\"rgb(" + (c[0] + shift) + "," + (c[1] + shift) + "," + (c[2] + shift) + ")\" stroke=\"rgb(" + (c[0] + shift) + "," + (c[1] + shift) + "," + (c[2] + shift) + ")\" stroke-width=\"1\" opacity=\"" + ((c[3] + shift) / 255.0) + "\" ";
        }

        // Gaussian kernels for blur
        static double[][] gks = 
        {
            new []{0.27901, 0.44198, 0.27901},
            new []{0.135336, 0.228569, 0.272192, 0.228569, 0.135336},
            new []{0.086776, 0.136394, 0.178908, 0.195843, 0.178908, 0.136394, 0.086776},
            new []{0.063327, 0.093095, 0.122589, 0.144599, 0.152781, 0.144599, 0.122589, 0.093095, 0.063327},
            new []{0.049692, 0.069304, 0.089767, 0.107988, 0.120651, 0.125194, 0.120651, 0.107988, 0.089767, 0.069304, 0.049692}
        };

        // Selective Gaussian blur for preprocessing
        static ImageData blur(ImageData imgd, float rad, float del)
        {
            int i, j, k, d, idx;
            double racc, gacc, bacc, aacc, wacc;
            ImageData imgd2 = new ImageData(imgd.width, imgd.height, new byte[imgd.width * imgd.height * 4]);

            // radius and delta limits, this kernel
            int radius = (int)Math.Floor(rad); if (radius < 1) { return imgd; }
            if (radius > 5) { radius = 5; }
            int delta = (int)Math.Abs(del); if (delta > 1024) { delta = 1024; }
            double[] thisgk = gks[radius - 1];

            // loop through all pixels, horizontal blur
            for (j = 0; j < imgd.height; j++)
            {
                for (i = 0; i < imgd.width; i++)
                {
                    racc = 0; gacc = 0; bacc = 0; aacc = 0; wacc = 0;
                    // gauss kernel loop
                    for (k = -radius; k < (radius + 1); k++)
                    {
                        // add weighted color values
                        if (((i + k) > 0) && ((i + k) < imgd.width))
                        {
                            idx = ((j * imgd.width) + i + k) * 4;
                            racc += imgd.data[idx] * thisgk[k + radius];
                            gacc += imgd.data[idx + 1] * thisgk[k + radius];
                            bacc += imgd.data[idx + 2] * thisgk[k + radius];
                            aacc += imgd.data[idx + 3] * thisgk[k + radius];
                            wacc += thisgk[k + radius];
                        }
                    }
                    // The new pixel
                    idx = ((j * imgd.width) + i) * 4;
                    imgd2.data[idx] = (byte)Math.Floor(racc / wacc);
                    imgd2.data[idx + 1] = (byte)Math.Floor(gacc / wacc);
                    imgd2.data[idx + 2] = (byte)Math.Floor(bacc / wacc);
                    imgd2.data[idx + 3] = (byte)Math.Floor(aacc / wacc);

                }// End of width loop
            }// End of horizontal blur

            // copying the half blurred imgd2
            byte[] himgd = imgd2.data.Clone() as byte[];

            // loop through all pixels, vertical blur
            for (j = 0; j < imgd.height; j++)
            {
                for (i = 0; i < imgd.width; i++)
                {
                    racc = 0; gacc = 0; bacc = 0; aacc = 0; wacc = 0;
                    // gauss kernel loop
                    for (k = -radius; k < (radius + 1); k++)
                    {
                        // add weighted color values
                        if (((j + k) > 0) && ((j + k) < imgd.height))
                        {
                            idx = (((j + k) * imgd.width) + i) * 4;
                            racc += himgd[idx] * thisgk[k + radius];
                            gacc += himgd[idx + 1] * thisgk[k + radius];
                            bacc += himgd[idx + 2] * thisgk[k + radius];
                            aacc += himgd[idx + 3] * thisgk[k + radius];
                            wacc += thisgk[k + radius];
                        }
                    }
                    // The new pixel
                    idx = ((j * imgd.width) + i) * 4;
                    imgd2.data[idx] = (byte)Math.Floor(racc / wacc);
                    imgd2.data[idx + 1] = (byte)Math.Floor(gacc / wacc);
                    imgd2.data[idx + 2] = (byte)Math.Floor(bacc / wacc);
                    imgd2.data[idx + 3] = (byte)Math.Floor(aacc / wacc);
                }// End of width loop
            }// End of vertical blur

            // Selective blur: loop through all pixels
            for (j = 0; j < imgd.height; j++)
            {
                for (i = 0; i < imgd.width; i++)
                {
                    idx = ((j * imgd.width) + i) * 4;
                    // d is the difference between the blurred and the original pixel
                    d = Math.Abs(imgd2.data[idx] - imgd.data[idx]) + Math.Abs(imgd2.data[idx + 1] - imgd.data[idx + 1]) +
                            Math.Abs(imgd2.data[idx + 2] - imgd.data[idx + 2]) + Math.Abs(imgd2.data[idx + 3] - imgd.data[idx + 3]);
                    // selective blur: if d>delta, put the original pixel back
                    if (d > delta)
                    {
                        imgd2.data[idx] = imgd.data[idx];
                        imgd2.data[idx + 1] = imgd.data[idx + 1];
                        imgd2.data[idx + 2] = imgd.data[idx + 2];
                        imgd2.data[idx + 3] = imgd.data[idx + 3];
                    }
                }
            }// End of Selective blur
            return imgd2;
        }// End of blur()
    }// End of ImageTracer class
}
