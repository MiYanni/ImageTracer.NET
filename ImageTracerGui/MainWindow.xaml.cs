﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ImageTracerNet;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Vectorization;
using ImageTracerNet.Vectorization.TraceTypes;
using ImageTracerNet.Extensions;
using ImageTracerNet.Svg;
using ImageTracerNet.Vectorization.Points;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
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
            //var line = new Line
            //{
            //    X1 = 3,
            //    Y1 = 3,
            //    X2 = 100,
            //    Y2 = 100,
            //    Stroke = Brushes.Black,
            //    Fill = Brushes.Black
            //};
            //LineGrid.Children.Add(line);
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
                SaveTracedImage(new[] { @"..\..\Images\1.png", "outfilename", @"chronotrigger2.svg", "ltres", "0.1", "qtres", "1", "scale", "30", "numberofcolors", "256", "pathomit", "0" });
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
                Part3ComboBox.ItemsSource = _filteredRawLayers.Keys.Select((k, i) => new ColorSelectionItem(k, i)).ToList();
                Part3Button.IsEnabled = false;
                LayerCount.Content = _filteredRawLayers.Count;
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

        private void Part3ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems.OfType<ColorSelectionItem>().First();
            var brush = selected.Color;
            var index = selected.Index;
            //Console.WriteLine($"Selected: {brush.Color} {index}");
            var nodes = _filteredRawLayers.ElementAt(index).Value.Nodes;
            var image = MakeClearBitmap();
            var pixelCount = 0;
            for (var row = 1; row < nodes.Length; ++row)
            {
                for (var column = 1; column < nodes[0].Length; ++column)
                {
                    var node = nodes[row][column];
                    if(node.IsLight())
                    {
                        image.SetPixel(column - 1, row - 1, System.Drawing.Color.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B));
                        pixelCount++;
                    }
                }
            }
            LayerPixelCount.Content = pixelCount;
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
                var paths = _pathPointLayers.SelectMany(cl => cl.Value.Paths.Select(p => new { Color = cl.Key, p.Points })).ToList();
                foreach (var path in paths) //.Where((p, i) => i == 1) TODO: Only first layer
                {
                    foreach (var point in path.Points)//.Where(p => p.EdgeNode.IsLight());
                    {
                        image.SetPixel(point.X, point.Y, path.Color.Color);
                    }
                }
                _pathPointImage = image;
                _part4Complete = true;
                Part4ComboBox.ItemsSource = paths.Select((cp, i) => new ColorSelectionItem(cp.Color, i)).ToList();
                PathCount.Content = paths.Count;
            }

            ImageDisplay.Source = BitmapToImageSource(_pathPointImage);
        }

        private static int CalculateLineGridDimension(int dimension, int multiplier, out int offset)
        {
            var pathSized = dimension + 1;
            var multipliedDimension = dimension*multiplier;
            var multipliedPathSized = pathSized*multiplier;
            var delta = multipliedPathSized - multipliedDimension;
            offset = (int) Math.Floor(delta/3.0);
            return multipliedPathSized - offset;
        }

        //private static int CalculateLineOffset(int dimension)
        //{
        //    var pathSized = dimension + 1;
        //    var delta = pathSized - dimension;
        //    return (int)Math.Floor(delta / 2.0);
        //}

        private void Part4ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems.OfType<ColorSelectionItem>().First();
            var index = selected.Index;
            var path = _pathPointLayers.SelectMany(cl => cl.Value.Paths.Select(p => new { Color = cl.Key, p.Points })).Where((cp, i) => i == index).Single();
            var color = path.Color.Color;
            var image = MakeClearBitmap(_loadedImage.Width + 1, _loadedImage.Height + 1);
            foreach (var point in path.Points)
            {
                image.SetPixel(point.X, point.Y, color);
            }
            PathPointsCount.Content = path.Points.Count;
            ImageDisplay.Source = BitmapToImageSource(image);

            LineGrid.Children.Clear();
            PathPoint previous = null;
            //http://stackoverflow.com/a/1165145/294804
            //var oppositeColor = ColorExtensions.FromAhsb(color.A, 360 - color.GetHue(), color.GetSaturation(), color.GetBrightness());
            //var oppositeColor = color.Invert();
            //http://jacobmsaylor.com/?p=1250
            var oppositeColor = Color.FromRgb((byte)~color.R, (byte)~color.G, (byte)~color.B);
            var oppositeBrush = new SolidColorBrush(Color.FromArgb(oppositeColor.A, oppositeColor.R, oppositeColor.G, oppositeColor.B));
            var multiplier = 10;
            int heightOffset;
            int widthOffset;
            LineGrid.Height = CalculateLineGridDimension(_loadedImage.Height, multiplier, out heightOffset);
            LineGrid.Width = CalculateLineGridDimension(_loadedImage.Width, multiplier, out widthOffset);
            //var heightOffset = CalculateLineOffset(_loadedImage.Height);
            //var widthOffset = CalculateLineOffset(_loadedImage.Width);
            PathPoint initial = null;
            foreach (var point in path.Points)
            {
                if (previous != null)
                {
                    var line = new Line
                    {
                        X1 = previous.X * multiplier + widthOffset,
                        Y1 = previous.Y * multiplier + heightOffset,
                        X2 = point.X * multiplier + widthOffset,
                        Y2 = point.Y * multiplier + heightOffset,
                        Stroke = oppositeBrush,
                        Fill = oppositeBrush
                    };
                    
                    LineGrid.Children.Add(line);
                } else { initial = point; }
                previous = point;
            }
            LineGrid.Children.Add(new Line
            {
                X1 = previous.X * multiplier + widthOffset,
                Y1 = previous.Y * multiplier + heightOffset,
                X2 = initial.X * multiplier + widthOffset,
                Y2 = initial.Y * multiplier + heightOffset,
                Stroke = oppositeBrush,
                Fill = oppositeBrush
            });
        }
    }
}
