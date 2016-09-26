using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ImageTracerNet;
using Svg;

namespace ImageTracerGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SaveTracedImage(new[] { @"..\..\Images\Chrono Trigger2.png", "outfilename", @"chronotrigger2.svg", "ltres", "0.1", "qtres", "1", "scale", "30", "numberofcolors", "256", "pathomit", "0" });
            SvgParser.MaximumSize = new System.Drawing.Size(10000, 10000);
            //var image = SvgDocument.OpenAsBitmap(@"chronotrigger2.svg");
            //var document = SvgParser.GetSvgDocument(@"chronotrigger2.svg");
            var image = SvgParser.GetBitmapFromSVG(@"chronotrigger2.svg");
            Height = image.Height / 10;
            Width = image.Width / 10;
            image.Save(@"chronotrigger2.png");
            // TODO: The BitmapToImageSource does not render transparency properly.
            //var imageSource = BitmapToImageSource(image);
            //http://stackoverflow.com/questions/11880946/how-to-load-image-to-wpf-in-runtime
            ImageDisplay.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"chronotrigger2.png")));
            WindowState = WindowState.Maximized;
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

        private static int arraycontains(String[] arr, String str)
        {
            for (int j = 0; j < arr.Length; j++) { if (arr[j].ToLower().Equals(str)) { return j; } }
            return -1;
        }

        private static float parsenext(String[] arr, int i)
        {
            if (i < (arr.Length - 1))
            {
                try
                {
                    return (float)Convert.ToDouble(arr[i + 1]);
                }
                catch (Exception) { }
            }
            return -1;
        }

        //http://stackoverflow.com/questions/22499407/how-to-display-a-bitmap-in-a-wpf-image
        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
    }
}
