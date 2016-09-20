using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using ImageTracerNet.Extensions;
using System.Windows.Media.Imaging;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Vectorization;
using ImageTracerNet.Vectorization.Points;
using ImageTracerNet.Vectorization.Segments;

namespace ImageTracerNet
{
    public static class ImageTracer
    {
        public static readonly string VersionNumber = typeof(ImageTracer).Assembly.GetName().Version.ToString();

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
            return ImageDataToSvg(image, LoadImageData(image), options, palette);
        }

        // Loading an image from a file, tracing when loaded, then returning IndexedImage with tracedata in layers
        internal static IndexedImage ImageToTraceData(string filename, Options options, byte[][] palette) 
        {
            return ImageToTraceData(new Bitmap(filename), options, palette);
        }

        internal static IndexedImage ImageToTraceData(Bitmap image, Options options, byte[][] palette) 
        {
            return ImageDataToTraceData(image, LoadImageData(image), options, palette);
        }

        ////////////////////////////////////////////////////////////

        private static ImageData LoadImageData(Bitmap image)
        {
            var rbgImage = image.ChangeFormat(PixelFormat.Format32bppArgb);
            //var data = rbgImage.ToRgbaByteArray();
            return new ImageData(image.Width, image.Height, rbgImage);
        }

        // Tracing ImageData, then returning the SVG String
        private static string ImageDataToSvg(Bitmap image, ImageData imgd, Options options, byte[][] palette)
        {
            return ImageDataToTraceData(image, imgd, options, palette).ToSvgString(options.SvgRendering);
        }

        // Tracing ImageData, then returning IndexedImage with tracedata in layers
        private static IndexedImage ImageDataToTraceData(Bitmap image, ImageData imgd, Options options, byte[][] palette)
        {
            //var paletteRowsColumns = (int)Math.Sqrt(options.ColorQuantization.NumberOfColors);
            // Use custom palette if pal is defined or sample or generate custom length palette
            //var colorPalette = palette != null 
            //    ? ColorExtensions.FromRgbaByteArray(palette.SelectMany(c => c).ToArray()) 
            //    : SmartPalette.Generate(image, paletteRowsColumns, paletteRowsColumns);

            //colorPalette = colorPalette ?? (options.ColorQuantization.ColorSampling.IsNotZero()
            //        ? PaletteGenerator.SamplePalette(options.ColorQuantization.NumberOfColors, imgd)
            //        : PaletteGenerator.GeneratePalette(options.ColorQuantization.NumberOfColors));

            var colorPalette = BitmapPalettes.Halftone256.Colors.Select(c => new ColorReference(c.A, c.R, c.G, c.B)).ToList();

            // Selective Gaussian blur preprocessing
            //if (options.Blur.BlurRadius > 0)
            //{
            //    // TODO: This seems to not work currently.
            //    imgd = Blur(imgd, options.Blur.BlurRadius, options.Blur.BlurDelta);
            //}

            // 1. Color quantization
            var ii = IndexedImage.Create(imgd, colorPalette);
            // 2. Layer separation and edge detection
            var rawLayers = Layering(ii);
            // 3. Batch pathscan
            var bps = rawLayers.Select(layer => Pathing.Scan(layer.Value, options.Tracing.PathOmit)).ToList();
            // 4. Batch interpollation
            var bis = bps.Select(Interpolation.Convert).ToList();
            // 5. Batch tracing
            ii.Layers = bis.Select(l => l.Select(p => TracePath(p, options.Tracing).ToList()).ToList()).ToList();
            
            return ii;
        }

        ////////////////////////////////////////////////////////////
        //
        //  Vectorizing functions
        //
        ////////////////////////////////////////////////////////////

        // 2. Layer separation and edge detection

        // Edge node types ( ▓:light or 1; ░:dark or 0 )

        // 12  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓

        // 48  ░░  ░░  ░░  ░░  ░▓  ░▓  ░▓  ░▓  ▓░  ▓░  ▓░  ▓░  ▓▓  ▓▓  ▓▓  ▓▓
        //     0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
        private static Dictionary<ColorReference, int[][]> Layering(IndexedImage ii)
        {
            // Creating layers for each indexed color in arr
            //var layers = new int[ii.Palette.Count][][].InitInner(ii.ArrayHeight, ii.ArrayWidth);
            var layers = ii.Palette.ToDictionary(p => p, p => new int[ii.ArrayHeight][].InitInner(ii.ArrayWidth));
            
            // Looping through all pixels and calculating edge node type
            for (var j = 1; j < ii.ArrayHeight - 1; j++)
            {
                for (var i = 1; i < ii.ArrayWidth - 1; i++)
                {
                    // This pixel's indexed color
                    var pg = ii.GetPixelGroup(j, i, ii.ArrayWidth);

                    // Are neighbor pixel colors the same?
                    // this pixel's type and looking back on previous pixels
                    // X
                    // 1, 3, 5, 7, 9, 11, 13, 15
                    layers[pg.Mid][j + 1][i + 1] = 1 + Convert.ToInt32(pg.MidRight == pg.Mid) * 2 + Convert.ToInt32(pg.BottomRight == pg.Mid) * 4 + Convert.ToInt32(pg.BottomMid == pg.Mid) * 8;
                    if (pg.MidLeft != pg.Mid)
                    {
                        // A
                        // 2, 6, 10, 14
                        layers[pg.Mid][j + 1][i] = 2 + Convert.ToInt32(pg.BottomMid == pg.Mid) * 4 + Convert.ToInt32(pg.BottomLeft == pg.Mid) * 8;
                    }
                    if (pg.TopMid != pg.Mid)
                    {
                        // B
                        // 8, 10, 12, 14
                        layers[pg.Mid][j][i + 1] = 8 + Convert.ToInt32(pg.TopRight == pg.Mid) * 2 + Convert.ToInt32(pg.MidRight == pg.Mid) * 4;
                    }
                    if (pg.TopLeft != pg.Mid)
                    {
                        // C
                        // 4, 6, 12, 14
                        layers[pg.Mid][j][i] = 4 + Convert.ToInt32(pg.TopMid == pg.Mid) * 2 + Convert.ToInt32(pg.MidLeft == pg.Mid) * 8;
                    }
                }
            }

            return layers;
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

        private static IEnumerable<Segment> TracePath(List<InterpolationPoint> path, Tracing tracingOptions)
        {
            var sequences = Sequencing.Create(path.Select(p => p.Direction).ToList());
            // Fit the sequences into segments, and return them.
            return sequences.Select(s => Segmentation.Fit(path, tracingOptions, s)).SelectMany(s => s);
        }

        ////////////////////////////////////////////////////////////
        //
        //  SVG Drawing functions
        //
        ////////////////////////////////////////////////////////////

        // Gaussian kernels for blur
        //private static readonly double[][] Gks = 
        //{
        //    new []{0.27901, 0.44198, 0.27901},
        //    new []{0.135336, 0.228569, 0.272192, 0.228569, 0.135336},
        //    new []{0.086776, 0.136394, 0.178908, 0.195843, 0.178908, 0.136394, 0.086776},
        //    new []{0.063327, 0.093095, 0.122589, 0.144599, 0.152781, 0.144599, 0.122589, 0.093095, 0.063327},
        //    new []{0.049692, 0.069304, 0.089767, 0.107988, 0.120651, 0.125194, 0.120651, 0.107988, 0.089767, 0.069304, 0.049692}
        //};

        // Selective Gaussian blur for preprocessing
        //private static ImageData Blur(ImageData imgd, int rad, double del)
        //{
        //    int i, j, k;
        //    int idx;
        //    double racc, gacc, bacc, aacc, wacc;
        //    var imgd2 = new ImageData(imgd.Width, imgd.Height, new byte[imgd.Width * imgd.Height * 4]);

        //    // radius and delta limits, this kernel
        //    var radius = rad; if (radius < 1) { return imgd; }
        //    if (radius > 5) { radius = 5; }
        //    var delta = (int)Math.Abs(del); if (delta > 1024) { delta = 1024; }
        //    var thisgk = Gks[radius - 1];

        //    // loop through all pixels, horizontal blur
        //    for (j = 0; j < imgd.Height; j++)
        //    {
        //        for (i = 0; i < imgd.Width; i++)
        //        {
        //            racc = 0; gacc = 0; bacc = 0; aacc = 0; wacc = 0;
        //            // gauss kernel loop
        //            for (k = -radius; k < radius + 1; k++)
        //            {
        //                // add weighted color values
        //                if ((i + k > 0) && (i + k < imgd.Width))
        //                {
        //                    idx = (j * imgd.Width + i + k) * 4;
        //                    racc += imgd.Data[idx] * thisgk[k + radius];
        //                    gacc += imgd.Data[idx + 1] * thisgk[k + radius];
        //                    bacc += imgd.Data[idx + 2] * thisgk[k + radius];
        //                    aacc += imgd.Data[idx + 3] * thisgk[k + radius];
        //                    wacc += thisgk[k + radius];
        //                }
        //            }
        //            // The new pixel
        //            idx = (j * imgd.Width + i) * 4;
        //            imgd2.Data[idx] = (byte)Math.Floor(racc / wacc);
        //            imgd2.Data[idx + 1] = (byte)Math.Floor(gacc / wacc);
        //            imgd2.Data[idx + 2] = (byte)Math.Floor(bacc / wacc);
        //            imgd2.Data[idx + 3] = (byte)Math.Floor(aacc / wacc);

        //        }// End of width loop
        //    }// End of horizontal blur

        //    // copying the half blurred imgd2
        //    var himgd = imgd2.Data.Clone() as byte[];

        //    // loop through all pixels, vertical blur
        //    for (j = 0; j < imgd.Height; j++)
        //    {
        //        for (i = 0; i < imgd.Width; i++)
        //        {
        //            racc = 0; gacc = 0; bacc = 0; aacc = 0; wacc = 0;
        //            // gauss kernel loop
        //            for (k = -radius; k < radius + 1; k++)
        //            {
        //                // add weighted color values
        //                if ((j + k > 0) && (j + k < imgd.Height))
        //                {
        //                    idx = ((j + k) * imgd.Width + i) * 4;
        //                    racc += himgd[idx] * thisgk[k + radius];
        //                    gacc += himgd[idx + 1] * thisgk[k + radius];
        //                    bacc += himgd[idx + 2] * thisgk[k + radius];
        //                    aacc += himgd[idx + 3] * thisgk[k + radius];
        //                    wacc += thisgk[k + radius];
        //                }
        //            }
        //            // The new pixel
        //            idx = (j * imgd.Width + i) * 4;
        //            imgd2.Data[idx] = (byte)Math.Floor(racc / wacc);
        //            imgd2.Data[idx + 1] = (byte)Math.Floor(gacc / wacc);
        //            imgd2.Data[idx + 2] = (byte)Math.Floor(bacc / wacc);
        //            imgd2.Data[idx + 3] = (byte)Math.Floor(aacc / wacc);
        //        }// End of width loop
        //    }// End of vertical blur

        //    // Selective blur: loop through all pixels
        //    for (j = 0; j < imgd.Height; j++)
        //    {
        //        for (i = 0; i < imgd.Width; i++)
        //        {
        //            idx = (j * imgd.Width + i) * 4;
        //            // d is the difference between the blurred and the original pixel
        //            var d = Math.Abs(imgd2.Data[idx] - imgd.Data[idx]) + Math.Abs(imgd2.Data[idx + 1] - imgd.Data[idx + 1]) +
        //                    Math.Abs(imgd2.Data[idx + 2] - imgd.Data[idx + 2]) + Math.Abs(imgd2.Data[idx + 3] - imgd.Data[idx + 3]);
        //            // selective blur: if d>delta, put the original pixel back
        //            if (d > delta)
        //            {
        //                imgd2.Data[idx] = imgd.Data[idx];
        //                imgd2.Data[idx + 1] = imgd.Data[idx + 1];
        //                imgd2.Data[idx + 2] = imgd.Data[idx + 2];
        //                imgd2.Data[idx + 3] = imgd.Data[idx + 3];
        //            }
        //        }
        //    }// End of Selective blur
        //    return imgd2;
        //}
    }
}
