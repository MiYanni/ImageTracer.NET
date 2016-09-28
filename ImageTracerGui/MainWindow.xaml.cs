using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageTracerNet;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Vectorization;
using ImageTracerNet.Vectorization.TraceTypes;
using ImageTracerNet.Extensions;
using ImageTracerNet.Svg;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

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
            WindowState = WindowState.Maximized;
            //SaveTracedImage(new[] { @"..\..\Images\Chrono Trigger2.png", "outfilename", @"chronotrigger2.svg", "ltres", "0.1", "qtres", "1", "scale", "30", "numberofcolors", "256", "pathomit", "0" });
            //SvgParser.MaximumSize = new System.Drawing.Size(10000, 10000);
            ////var image = SvgDocument.OpenAsBitmap(@"chronotrigger2.svg");
            ////var document = SvgParser.GetSvgDocument(@"chronotrigger2.svg");
            //var image = SvgParser.GetBitmapFromSVG(@"chronotrigger2.svg");
            //Height = image.Height / 10;
            //Width = image.Width / 10;
            //image.Save(@"chronotrigger2.png");
            //var imageSource = BitmapToImageSource(image);
            //ImageDisplay.Source = imageSource;
            ////Browser.Source = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"chronotrigger2.png"));
            ////http://stackoverflow.com/questions/11880946/how-to-load-image-to-wpf-in-runtime
            ////ImageDisplay.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"chronotrigger2.png")));
            //WindowState = WindowState.Maximized;
        }

        private void SaveTracedImage(string[] args)
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
                //File.WriteAllText(outfilename, ImageToSvg(args[0], options));
                ImageToSvg(args[0], options);
            }// End of parameter parsing and processing

            //}
            //catch (Exception e) { Console.WriteLine(e.StackTrace); }
        }

        private Bitmap _loadedImage;
        private Options _options;

        public void ImageToSvg(string filename, Options options)
        {
            //return ImageToSvg(new Bitmap(filename), options);

            // 1. Color quantization
            var rbgImage = new Bitmap(filename).ChangeFormat(PixelFormat.Format32bppArgb);
            _loadedImage = rbgImage;
            _options = options;
        }
        //public void ImageToSvg(Bitmap image, Options options) 
        //{
        //    //var colors = rbgImage.ToColorReferences();
        //    //var paddedPaletteImage = new PaddedPaletteImage(colors, rbgImage.Height, rbgImage.Width, ImageTracer.Palette);

        //    //var paletteImageBitmap = new Bitmap(paddedPaletteImage.ImageWidth, paddedPaletteImage.ImageHeight);
        //    //paddedPaletteImage.ColorGroups.Where(cg => cg.Mid != ColorReference.Empty).ToList().ForEach(cg => paletteImageBitmap.SetPixel(cg.X - 1, cg.Y - 1, cg.Mid.Color));
        //    //ImageDisplay.Source = BitmapToImageSource(paletteImageBitmap);

        //    //return PaddedPaletteImageToTraceData(paddedPaletteImage, options.Tracing).ToSvgString(options.SvgRendering);
        //}

        private Bitmap _paletteImage;
        private PaddedPaletteImage _image;
        public void ImageToSvg2()
        {
            // 1. Color quantization
            //var rbgImage = image.ChangeFormat(PixelFormat.Format32bppArgb);

            //ImageDisplay.Source = null;
            //ImageDisplay.Source = BitmapToImageSource(image);

            var colors = _loadedImage.ToColorReferences();
            var paddedPaletteImage = new PaddedPaletteImage(colors, _loadedImage.Height, _loadedImage.Width, ImageTracer.Palette);

            var paletteImageBitmap = new Bitmap(paddedPaletteImage.ImageWidth, paddedPaletteImage.ImageHeight);
            paddedPaletteImage.ColorGroups.Where(cg => cg.Mid != ColorReference.Empty).ToList().ForEach(cg => paletteImageBitmap.SetPixel(cg.X - 1, cg.Y - 1, cg.Mid.Color));
            
            _paletteImage = paletteImageBitmap;
            _image = paddedPaletteImage;
            //return PaddedPaletteImageToTraceData(paddedPaletteImage, options.Tracing).ToSvgString(options.SvgRendering);
        }

        ////////////////////////////////////////////////////////////

        // Tracing ImageData, then returning PaddedPaletteImage with tracedata in layers
        private PaddedPaletteImage PaddedPaletteImageToTraceData()
        {
            // 2. Layer separation and edge detection
            var rawLayers = Layering.Convert(_image);
            // 3. Batch pathscan
            var pathPointLayers = rawLayers.Select(layer => new Layer<PathPointPath> { Paths = Pathing.Scan(layer.Value, _options.Tracing.PathOmit).ToList() });
            // 4. Batch interpollation
            var interpolationPointLayers = pathPointLayers.Select(Interpolation.Convert);
            // 5. Batch tracing
            //image.Layers = interpolationPointLayers.Select(l => l.Select(p => Pathing.Trace(p.ToList(), options).ToList()).ToList()).ToList();
            _image.Layers = interpolationPointLayers.Select(layer => 
                new Layer<SegmentPath> { Paths = layer.Paths.Select(path => 
                    new SegmentPath { Segments = 
                        Pathing.Trace(path, _options.Tracing).ToList() }).ToList() }).ToList();

            return _image;
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
        private static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                var bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                //http://stackoverflow.com/questions/10518986/image-does-not-refresh-in-custom-picture-box
                //bitmapimage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        //private void GoButton_Click(object sender, RoutedEventArgs e)
        //{
        //    SaveTracedImage(new[] { @"..\..\Images\Chrono Trigger2.png", "outfilename", @"chronotrigger2.svg", "ltres", "0.1", "qtres", "1", "scale", "30", "numberofcolors", "256", "pathomit", "0" });
        //    SvgParser.MaximumSize = new System.Drawing.Size(10000, 10000);
        //    //var image = SvgDocument.OpenAsBitmap(@"chronotrigger2.svg");
        //    //var document = SvgParser.GetSvgDocument(@"chronotrigger2.svg");
        //    var image = SvgParser.GetBitmapFromSVG(@"chronotrigger2.svg");
        //    Height = image.Height / 10;
        //    Width = image.Width / 10;
        //    image.Save(@"chronotrigger2.png");
        //    var imageSource = BitmapToImageSource(image);
        //    ImageDisplay.Source = imageSource;
        //    //Browser.Source = new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"chronotrigger2.png"));
        //    //http://stackoverflow.com/questions/11880946/how-to-load-image-to-wpf-in-runtime
        //    //ImageDisplay.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"chronotrigger2.png")));
        //    //WindowState = WindowState.Maximized;
        //}


        private bool _part1Complete;
        private void Part1Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_part1Complete)
            {
                SaveTracedImage(new[] { @"..\..\Images\9.png", "outfilename", @"chronotrigger2.svg", "ltres", "0.1", "qtres", "1", "scale", "30", "numberofcolors", "256", "pathomit", "0" });
                _part1Complete = true;
            }
            ImageDisplay.Source = BitmapToImageSource(_loadedImage);
        }

        private bool _part2Complete;
        private void Part2Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_part2Complete)
            {
                ImageToSvg2();
                _part2Complete = true;
            }
            ImageDisplay.Source = BitmapToImageSource(_paletteImage);
        }

        private Dictionary<ColorReference, RawLayer> _rawLayers;
        private Dictionary<ColorReference, RawLayer> _filteredRawLayers;
        private bool _part3Complete;
        private void Part3Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_part3Complete)
            {
                _rawLayers = Layering.Convert(_image);
                _part3Complete = true;
                _filteredRawLayers =
                    _rawLayers.Where(cl => cl.Value.Nodes.Any(r => r.Any(n => n.IsLight())))
                        .ToDictionary(cl => cl.Key, cl => cl.Value);

                //http://www.wpf-tutorial.com/list-controls/combobox-control/
                //http://stackoverflow.com/questions/7719164/databinding-a-color-in-wpf-datatemplate
                Part3ComboBox.ItemsSource = _filteredRawLayers.Keys.Select((k, i) => new { Color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(k.A, k.R, k.G, k.B)), Index = i });
                Part3Button.IsEnabled = false;
            }
        }

        private Bitmap MakeClearBitmap(int? width = null, int? height = null)
        {
            var image = new Bitmap(width ?? _loadedImage.Width, height ?? _loadedImage.Height);
            using (var gfx = Graphics.FromImage(image))
            using (var transBrush = new SolidBrush(System.Drawing.Color.Transparent))
            {
                gfx.FillRectangle(transBrush, 0, 0, image.Width, image.Height);
            }
            return image;
        }

        private void Part3ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems.OfType<dynamic>().First();
            var brush = (SolidColorBrush)selected.Color;
            var index = (int)selected.Index;
            //Console.WriteLine($"Selected: {brush.Color} {index}");
            var nodes = _filteredRawLayers.ElementAt(index).Value.Nodes;

            var image = MakeClearBitmap();

            for (var row = 1; row < nodes.Length; ++row)
            {
                for (var column = 1; column < nodes[0].Length; ++column)
                {
                    var node = nodes[row][column];
                    //if (node == EdgeNode.DDDD || node == EdgeNode.DDDL || node == EdgeNode.DDLD || node == EdgeNode.DDLL ||
                    //    node == EdgeNode.DLDD || node == EdgeNode.DLDL || node == EdgeNode.DLLD || node == EdgeNode.DLLL)
                    if(node.IsLight())
                    {
                        image.SetPixel(column - 1, row - 1, System.Drawing.Color.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B));
                    }
                }
            }

            ImageDisplay.Source = BitmapToImageSource(image);
        }

        private Dictionary<ColorReference, Layer<PathPointPath>> _pathPointLayers;
        private bool _part4Complete;
        private Bitmap _pathPointImage;
        private void Part4Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_part4Complete)
            {
                _pathPointLayers = _filteredRawLayers.ToDictionary(cl => cl.Key, cl => new Layer<PathPointPath> { Paths = Pathing.Scan(cl.Value, _options.Tracing.PathOmit).ToList() });
                var image = MakeClearBitmap(_loadedImage.Width + 1, _loadedImage.Height + 1);
                var paths = _pathPointLayers.SelectMany(cl => cl.Value.Paths.Select(p => new { Color = cl.Key, p.Points }));
                foreach (var path in paths) // TODO: Only first layer
                {
                    var points = path.Points;//.Where(p => p.EdgeNode.IsLight());
                    foreach (var point in points)
                    {
                        image.SetPixel(point.X, point.Y, path.Color.Color);
                    }
                }
                _pathPointImage = image;
                _part4Complete = true;
            }

            ImageDisplay.Source = BitmapToImageSource(_pathPointImage);
        }
    }
}
