using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ImageTracerNet;
using ImageTracerNet.Extensions;
using ImageTracerNet.Palettes;

//using Options = System.Collections.Generic.Dictionary<string, float>; // HashMap<String, Float>()

namespace ImageTracerTester
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //SaveTracedImage(args);
            SaveTracedImage(new [] { @"..\..\Images\Chrono Trigger2.png", "outfilename", @"chronotrigger2-traced-new.svg", "ltres", "0.1", "qtres", "1", "scale", "1", "numberofcolors", "256", "pathomit", "0" });
            //ColorArrayTest(100, 200);
            //GaussianBlurTest(@"..\..\Images\Chrono Trigger2.png", @"chronotrigger2-blurred.png");
            //ColorPaletteTest(@"..\..\Images\Chrono Trigger2.png", @"chronotrigger2-palette.png", 8, 8);
        }

        private static void SaveTracedImage(string[] args)
        {
            //try
            //{
                if (args.Length < 1)
                {
                    Console.WriteLine("ERROR: there's no input filename. Basic usage: \r\n\r\njava -jar ImageTracer.jar <filename>" +
                            "\r\n\r\nor\r\n\r\njava -jar ImageTracer.jar help");
                }
                else if (arraycontains(args, "help") > -1)
                {
                    Console.WriteLine("Example usage:\r\n\r\njava -jar ImageTracer.jar <filename> outfilename test.svg " +
                            "ltres 1 qtres 1 pathomit 8 colorsampling 1 numberofcolors 16 mincolorratio 0.02 colorquantcycles 3 " +
                            "scale 1 simplifytolerance 0 roundcoords 1 lcpr 0 qcpr 0 desc 1 viewbox 0 blurradius 0 blurdelta 20 \r\n" +
                            "\r\nOnly <filename> is mandatory, if some of the other optional parameters are missing, they will be set to these defaults. " +
                            "\r\nWarning: if outfilename is not specified, then <filename>.svg will be overwritten." +
                            "\r\nSee https://github.com/jankovicsandras/imagetracerjava for details. \r\nThis is version " + ImageTracer.VersionNumber);
                }
                else
                {

                    // Parameter parsing
                    String outfilename = args[0] + ".svg";
                    Options options = new Options();
                    String[] parameternames = { "ltres", "qtres", "pathomit", "colorsampling", "numberofcolors", "mincolorratio", "colorquantcycles", "scale", "simplifytolerance", "roundcoords", "lcpr", "qcpr", "desc", "viewbox", "blurradius", "blurdelta", "outfilename" };
                    int j = -1; float f = -1;
                    foreach (var parametername in parameternames)
                    {
                        j = arraycontains(args, parametername);
                        if (j > -1)
                        {
                            if (parametername == "outfilename")
                            {
                                if (j < (args.Length - 1)) { outfilename = args[j + 1]; }
                            }
                            else
                            {
                                f = parsenext(args, j);
                                if (f > -1)
                                {
                                    //options[parametername] = f;
                                    options.SetOptionByName(parametername, f);
                                }
                            }
                        }
                    }// End of parameternames loop

                    // Loading image, tracing, rendering SVG, saving SVG file
                    File.WriteAllText(outfilename, ImageTracer.ImageToSvg(args[0], options));

                }// End of parameter parsing and processing

            //}
            //catch (Exception e) { Console.WriteLine(e.StackTrace); }
        }

        public static int arraycontains(String[] arr, String str)
        {
            for (int j = 0; j < arr.Length; j++) { if (arr[j].ToLower().Equals(str)) { return j; } }
            return -1;
        }

        public static float parsenext(String[] arr, int i)
        {
            if (i < (arr.Length - 1))
            {
                try
                {
                    return (float)Convert.ToDouble(arr[i + 1]);
                } catch (Exception) { }
            }
            return -1;
        }

        //////////////////////////////////////////////////////////

        private static void ColorArrayTest(int height, int width)
        {
            height += 2;
            width += 2;
            var arr = new int[height][].InitInner(width);
            for (var j = 0; j < height; j++)
            {
                arr[j][0] = -1;
                arr[j][width - 1] = -1;
            }
            for (var i = 0; i < width; i++)
            {
                arr[0][i] = -1;
                arr[height - 1][i] = -1;
            }
            Console.WriteLine("Initial");

            //Enumerable.Repeat()
            //var arr2 = new int[height].Initialize(-1);
            //arr2[0] = 1;
            //arr2.Initialize();
            //var arr3 = new int[height][].Initialize(new int[1]);
            //arr3[0][0] = 2;
            //var arr4 = new int[height][].Initialize(() => new int[1]);
            //arr4[0][0] = 2;
            //Console.WriteLine("Test");

            //var arr2 = new int[height][].Initialize(i => 
            //i == 0 || i == width - 1 
            //    ? new int[width].Initialize(-1, 0, width - 1)
            //    : new int[width]);

            var arr2 = new int[height][].Initialize(i =>
            i == 0 || i == height - 1
                ? new int[width].Initialize(-1)
                : new int[width].Initialize(-1, 0, width - 1));

            var result = true;
            for (var i = 0; i < height; ++i)
            {
                result &= arr[i].SequenceEqual(arr2[i]);
            }
            Console.WriteLine("Test: " + result);
        }

        private static void GaussianBlurTest(string imagePath, string outputPath)
        {
            var image = new Bitmap(imagePath);
            var outputImage = new Bitmap(image.Width, image.Height);
            var gaussianBlur = new GaussianBlur();
            var rectangle = new Rectangle(0, 0, image.Width, image.Height);
            gaussianBlur.Apply(outputImage, image, rectangle, 0, image.Height - 1);
            outputImage.Save(outputPath, ImageFormat.Png);
        }

        private static void ColorPaletteTest(string imagePath, string outputPath, int rows = 4, int columns = 4)
        {
            var image = new Bitmap(imagePath);
            var palette = SmartPalette.Generate(image, rows, columns);
            var blockHeight = image.Height / rows;
            var blockWidth = image.Width / columns;
            var paletteImage = new Bitmap(image.Width, image.Height);
            for (var i = 0; i < rows; ++i)
            {
                for (var j = 0; j < columns; ++j)
                {
                    var rectangle = new Rectangle(j * blockWidth, i * blockHeight, blockWidth, blockHeight);
                    var blockIndex = (i * rows) + j;
                    var color = palette[blockIndex];
                    //http://stackoverflow.com/a/12502497/294804
                    //var colorImage = new Bitmap(rectangle.Width, rectangle.Height);
                    //using (var graphics = Graphics.FromImage(colorImage))
                    //{
                    //    var block = new Rectangle(0, 0, rectangle.Width, rectangle.Height);
                    //    //http://stackoverflow.com/questions/5641078/convert-from-color-to-brush
                    //    graphics.FillRectangle(new SolidBrush(color), block);
                    //}

                    //https://msdn.microsoft.com/en-us/library/aa457087.aspx
                    using (var g = Graphics.FromImage(paletteImage))
                    {
                        //g.DrawImage(colorImage, 0, 0, rectangle, GraphicsUnit.Pixel);
                        //http://stackoverflow.com/a/15889822/294804
                        g.FillRectangle(new SolidBrush(color), rectangle);
                    }
                    //paletteImage.Save(outputPath + "_" + blockIndex + ".png", ImageFormat.Png);
                }
            }
            paletteImage.Save(outputPath, ImageFormat.Png);
        }

        private static readonly Random Rng = new Random();
        private static readonly Func<bool> GetRandom = () => Convert.ToBoolean(Rng.Next(0, 1));
        private static int BooleanTest()
        {
            var a = false;
            var b = false;
            var c = GetRandom();

            while (!(a || b))
            {
                a = GetRandom();
                b = GetRandom();
            }

            return a || (b && c) ? 0 : 1337;
        }
    }
}
