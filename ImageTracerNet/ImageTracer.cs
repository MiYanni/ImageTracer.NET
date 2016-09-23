using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using ImageTracerNet.Extensions;
using System.Windows.Media.Imaging;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Svg;
using ImageTracerNet.Vectorization;

namespace ImageTracerNet
{
    public static class ImageTracer
    {
        public static readonly string VersionNumber = typeof(ImageTracer).Assembly.GetName().Version.ToString();

        private static readonly List<ColorReference> Palette = BitmapPalettes.Halftone256.Colors.Select(c => new ColorReference(c.A, c.R, c.G, c.B)).ToList();

        ////////////////////////////////////////////////////////////
        //
        //  User friendly functions
        //
        ////////////////////////////////////////////////////////////

        // Loading an image from a file, tracing when loaded, then returning the SVG String
        public static string ImageToSvg(string filename, Options options) 
        {
            return ImageToSvg(new Bitmap(filename), options);
        }

        public static string ImageToSvg(Bitmap image, Options options) 
        {
            // 1. Color quantization
            var rbgImage = image.ChangeFormat(PixelFormat.Format32bppArgb);
            var colors = rbgImage.ToColorReferences();
            var paddedPaletteImage = new PaddedPaletteImage(colors, rbgImage.Height, rbgImage.Width, Palette);

            return PaddedPaletteImageToTraceData(paddedPaletteImage, options.Tracing).ToSvgString(options.SvgRendering);
        }

        ////////////////////////////////////////////////////////////

        // Tracing ImageData, then returning PaddedPaletteImage with tracedata in layers
        private static PaddedPaletteImage PaddedPaletteImageToTraceData(PaddedPaletteImage image, Tracing options)
        {
            // Selective Gaussian blur preprocessing
            //if (options.Blur.BlurRadius > 0)
            //{
            //    // TODO: This seems to not work currently.
            //    imgd = Blur(imgd, options.Blur.BlurRadius, options.Blur.BlurDelta);
            //}

            // 2. Layer separation and edge detection
            var rawLayers = Layering.Convert(image);
            // 3. Batch pathscan
            var bps = rawLayers.Select(layer => Pathing.Scan(layer.Value, options.PathOmit).Select(p => p.ToList()));
            // 4. Batch interpollation
            var bis = bps.Select(Interpolation.Convert).ToList();
            // 5. Batch tracing
            image.Layers = bis.Select(l => l.Select(p => Pathing.Trace(p, options).ToList()).ToList()).ToList();
            
            return image;
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
