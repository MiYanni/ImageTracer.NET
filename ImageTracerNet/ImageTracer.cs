using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using ImageTracerNet.Extensions;
using System.Windows.Media.Imaging;
using ImageTracerNet.Svg;
using ImageTracerNet.Vectorization;

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

        // Loading an image from a file, tracing when loaded, then returning PaddedPaletteImage with tracedata in layers
        internal static PaddedPaletteImage ImageToTraceData(string filename, Options options, byte[][] palette) 
        {
            return ImageToTraceData(new Bitmap(filename), options, palette);
        }

        internal static PaddedPaletteImage ImageToTraceData(Bitmap image, Options options, byte[][] palette) 
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

        // Tracing ImageData, then returning PaddedPaletteImage with tracedata in layers
        private static PaddedPaletteImage ImageDataToTraceData(Bitmap image, ImageData imgd, Options options, byte[][] palette)
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
            var ii = new PaddedPaletteImage(imgd, colorPalette);
            // 2. Layer separation and edge detection
            var rawLayers = Layering.Convert(ii);
            // 3. Batch pathscan
            var bps = rawLayers.Select(layer => Pathing.Scan(layer.Value, options.Tracing.PathOmit)).ToList();
            // 4. Batch interpollation
            var bis = bps.Select(Interpolation.Convert).ToList();
            // 5. Batch tracing
            ii.Layers = bis.Select(l => l.Select(p => Pathing.Trace(p, options.Tracing).ToList()).ToList()).ToList();
            
            return ii;
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
