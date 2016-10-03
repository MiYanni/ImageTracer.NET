using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ImageTracerNet;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Vectorization;
using ImageTracerNet.Vectorization.TraceTypes;
using ImageTracerNet.Extensions;
using ImageTracerNet.Svg;
using ImageTracerNet.Vectorization.Points;
using ImageTracerNet.Vectorization.Segments;
using Brushes = System.Windows.Media.Brushes;
using MColor = System.Windows.Media.Color;
using DColor = System.Drawing.Color;
using MBrush = System.Windows.Media.Brush;
using MSize = System.Windows.Size;
using DSize = System.Drawing.Size;
using MRectangle = System.Windows.Shapes.Rectangle;
using LineSegment = ImageTracerNet.Vectorization.Segments.LineSegment;
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

        private static Bitmap CreateTransparentBitmap(int width, int height)
        {
            var image = new Bitmap(width, height);
            using (var gfx = Graphics.FromImage(image))
            using (var transBrush = new SolidBrush(DColor.Transparent))
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
            var image = CreateTransparentBitmap(_loadedImage.Width, _loadedImage.Height);
            var pixelCount = 0;
            for (var row = 1; row < nodes.Length; ++row)
            {
                for (var column = 1; column < nodes[0].Length; ++column)
                {
                    var node = nodes[row][column];
                    if(node.IsLight())
                    {
                        image.SetPixel(column - 1, row - 1, DColor.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B));
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
                var paths = _pathPointLayers.SelectMany(cl => cl.Value.Paths.Select(p => new { Color = cl.Key, p.Points })).ToList();
                var imagePoints =_pathPointLayers.SelectMany(cl => cl.Value.Paths.SelectMany(p => p.Points).Select(p => Tuple.Create((Point<int>)p, cl.Key.Color))).ToList();
                _pathPointImage = DrawPointsImage(imagePoints, _loadedImage.Width + 1, _loadedImage.Height + 1);
                _part4Complete = true;
                Part4ComboBox.ItemsSource = paths.Select((cp, i) => new ColorSelectionItem(cp.Color, i)).ToList();
                PathCount.Content = paths.Count;
            }

            ImageDisplay.Source = BitmapToImageSource(_pathPointImage);
        }

        private static double CalculateScaledDimension(double dimension, double multiplier, out double offset)
        {
            var midPixelDimension = dimension + 1;
            var scaledDimension = dimension*multiplier;
            var scaledMidPixelDimension = midPixelDimension*multiplier;
            var delta = scaledMidPixelDimension - scaledDimension;
            offset = delta / 3.0;
            return scaledMidPixelDimension - offset;
        }

        private static Bitmap DrawPointsImage(IEnumerable<Tuple<Point<int>, DColor>> points, int width, int height)
        {
            var image = CreateTransparentBitmap(width, height);
            foreach (var point in points)
            {
                image.SetPixel(point.Item1.X, point.Item1.Y, point.Item2);
            }
            return image;
        }

        private static Bitmap DrawPointsImage(IEnumerable<Point<int>> points, int width, int height, DColor color)
        {
            return DrawPointsImage(points.Select(p => Tuple.Create(p, color)), width, height);
        }

        private static MSize CalculateScaledOffsets(ref double width, ref double height, double multiplier = 10.0)
        {
            double heightOffset;
            double widthOffset;
            width = CalculateScaledDimension(width, multiplier, out widthOffset);
            height = CalculateScaledDimension(height, multiplier, out heightOffset);
            return new MSize(widthOffset, heightOffset);
        }

        private static Line CreateLine(Point<double> first, Point<double> second, MBrush brush, bool isAnimated = true)
        {
            var line = new Line
            {
                X1 = first.X,
                Y1 = first.Y,
                X2 = second.X,
                Y2 = second.Y,
                Stroke = brush,
                Fill = brush
            };
            //http://stackoverflow.com/questions/16561639/horizontal-dashed-line-stretched-to-container-width
            //http://stackoverflow.com/questions/16023995/moving-dotted-line-for-cropping
            //http://stackoverflow.com/questions/15469283/how-do-you-animate-a-line-on-a-canvas-in-c
            if (isAnimated)
            {
                line.StrokeDashArray = new DoubleCollection(new[] { 2.0, 0.0, 2.0 });
                var sb = new Storyboard();
                var da = new DoubleAnimation
                {
                    To = -200,
                    Duration = new TimeSpan(0, 0, 20),
                    RepeatBehavior = RepeatBehavior.Forever,
                    By = -3
                };
                Storyboard.SetTargetProperty(da, new PropertyPath("(Line.StrokeDashOffset)"));
                sb.Children.Add(da);
                line.BeginStoryboard(sb);

            }
            return line;
        }

        private static Ellipse CreateLineDot(Point<double> point, MBrush brush, double size = 2.5)
        {
            var dot = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = brush
            };
            var yPoint = point.Y - (size / 2.0);
            var xPoint = point.X - (size / 2.0);
            Canvas.SetTop(dot, yPoint < 0 ? 0 : yPoint);
            Canvas.SetLeft(dot, xPoint < 0 ? 0 : xPoint);
            return dot;
        }

        private static IEnumerable<UIElement> CreateOverlayLines(IReadOnlyList<Point<double>> points, MSize offset, MBrush brush, double multiplier = 10.0, bool isAnimated = true)
        {
            if (!points.Any())
            {
                yield break;
            }

            Func<Point<double>, Point<double>> scale = p => new Point<double>
            {
                X = p.X * multiplier + offset.Width,
                Y = p.Y * multiplier + offset.Height
            };
            var scaledPoints = points.Select(p => scale(p)).ToList();
            var initial = scaledPoints.First();
            var previous = scaledPoints.First();
            foreach (var point in scaledPoints)
            {
                if (previous != null)
                {
                    yield return CreateLine(previous, point, brush, isAnimated);
                }
                yield return CreateLineDot(point, brush);
                previous = point;
            }
            yield return CreateLine(previous, initial, brush, isAnimated);
        }

        private void Part4ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems.OfType<ColorSelectionItem>().First();
            var index = selected.Index;
            var path = _pathPointLayers.SelectMany(cl => cl.Value.Paths.Select(p => new { Color = cl.Key, p.Points })).Where((cp, i) => i == index).Single();
            var color = path.Color.Color;
            var image = DrawPointsImage(path.Points, _loadedImage.Width + 1, _loadedImage.Height + 1, color);
            PathPointsCount.Content = path.Points.Count;
            ImageDisplay.Source = BitmapToImageSource(image);

            LineGrid.Children.Clear();
            //http://stackoverflow.com/a/1165145/294804
            //var oppositeColor = ColorExtensions.FromAhsb(color.A, 360 - color.GetHue(), color.GetSaturation(), color.GetBrightness());
            //var oppositeColor = color.Invert();
            //http://jacobmsaylor.com/?p=1250
            var oppositeColor = MColor.FromRgb((byte)~color.R, (byte)~color.G, (byte)~color.B);
            var oppositeBrush = new SolidColorBrush(MColor.FromArgb(oppositeColor.A, oppositeColor.R, oppositeColor.G, oppositeColor.B));
            double gridWidth = _loadedImage.Width;
            double gridHeight = _loadedImage.Height;
            var offset = CalculateScaledOffsets(ref gridWidth, ref gridHeight);
            var points = path.Points.Select(p => new Point<double> {X = p.X, Y = p.Y}).ToList();
            var lines = CreateOverlayLines(points, offset, oppositeBrush);
            LineGrid.Width = gridWidth;
            LineGrid.Height = gridHeight;
            LineGrid.Children.AddRange(lines);
        }

        private Dictionary<ColorReference, Layer<InterpolationPointPath>> _interpolationPointLayers;
        private bool _part5Compete;
        private IEnumerable<UIElement> _interpLines;
        private double _gridWidth;
        private double _gridHeight;
        private void Part5Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_part5Compete)
            {
                _interpolationPointLayers = _pathPointLayers.ToDictionary(cp => cp.Key, cp => Interpolation.Convert(cp.Value));
                var paths = _interpolationPointLayers.SelectMany(cl => cl.Value.Paths.Select(p => new { Color = cl.Key, p.Points })).ToList();
                double gridWidth = _loadedImage.Width;
                double gridHeight = _loadedImage.Height;
                var offset = CalculateScaledOffsets(ref gridWidth, ref gridHeight);
                _gridWidth = gridWidth;
                _gridHeight = gridHeight;

                var lines = new List<UIElement>();
                foreach (var path in paths)
                {
                    var color = path.Color.Color;
                    var brush = new SolidColorBrush(MColor.FromArgb(color.A, color.R, color.G, color.B));
                    var points = path.Points.Select(p => new Point<double> { X = p.X, Y = p.Y }).ToList();
                    var pathLines = CreateOverlayLines(points, offset, brush, 10.0, false);
                    lines.AddRange(pathLines);
                }

                _interpLines = lines;
                _part5Compete = true;
                InterpCount.Content = paths.Count;
            }

            LineGrid.Children.Clear();
            LineGrid.Width = _gridWidth;
            LineGrid.Height = _gridHeight;
            LineGrid.Children.AddRange(_interpLines);
            ImageDisplay.Source = BitmapToImageSource(CreateTransparentBitmap(_loadedImage.Width + 1, _loadedImage.Height + 1));
        }

        private Dictionary<ColorReference, Layer<SequencePath>> _sequenceLayers;
        //private bool _part6Compete;
        private void Part6Button_Click(object sender, RoutedEventArgs e)
        {
            _sequenceLayers = _interpolationPointLayers.ToDictionary(ci => ci.Key, ci => new Layer<SequencePath>
            {
                Paths = ci.Value.Paths.Select(path => new SequencePath
                {
                    Path = path,
                    Sequences = Sequencing.Create(path.Points.Select(p => p.Direction).ToList()).ToList()
                }).ToList()
            });
            var sequences = _sequenceLayers.SelectMany(cl => cl.Value.Paths.SelectMany(p => p.Sequences.Select(s => new { Color = cl.Key }))).ToList();
            Part6ComboBox.ItemsSource = sequences.Select((cs, i) => new ColorSelectionItem(cs.Color, i)).ToList();
            SequenceCount.Content = sequences.Count;
            Part6Button.IsEnabled = false;
        }

        private void Part6ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems.OfType<ColorSelectionItem>().First();
            var index = selected.Index;

            var pathsWithSequences = _sequenceLayers.SelectMany(cl => cl.Value.Paths.SelectMany(p => p.Sequences.Select(s => new { Color = cl.Key, p.Path, Indices = s }))).ToList();
            var selectedSequence = pathsWithSequences[index].Indices;
            var selectedPath = pathsWithSequences[index].Path;

            var color = pathsWithSequences[index].Color.Color;
            var regularBrush = new SolidColorBrush(MColor.FromArgb(color.A, color.R, color.G, color.B));
            var sequenceBrush = new SolidColorBrush(Colors.Blue);
            var points = selectedPath.Points.Select(p => new Point<double> { X = p.X, Y = p.Y }).ToList();
            double gridWidth = _loadedImage.Width;
            double gridHeight = _loadedImage.Height;
            var offset = CalculateScaledOffsets(ref gridWidth, ref gridHeight);
            //var pathLines = CreateOverlayLines(points, offset, brush, 10.0, false);


            var multiplier = 10.0;
            var lines = new List<UIElement>();
            Func<Point<double>, Point<double>> scale = p => new Point<double>
            {
                X = p.X * multiplier + offset.Width,
                Y = p.Y * multiplier + offset.Height
            };
            var scaledPoints = points.Select(p => scale(p)).ToList();
            var initial = scaledPoints.First();
            var previous = scaledPoints.First();
            var pointCount = 0;
            var brush = regularBrush;
            foreach (var point in scaledPoints)
            {
                
                if (previous != null)
                {
                    brush = pointCount > selectedSequence.Start && (selectedSequence.End == 0 || pointCount <= selectedSequence.End)
                        ? sequenceBrush
                        : regularBrush;
                    lines.Add(CreateLine(previous, point, brush, false));
                }
                brush = (pointCount >= selectedSequence.Start && (selectedSequence.End == 0  || pointCount <= selectedSequence.End)) || (selectedSequence.End == pointCount)
                    ? sequenceBrush
                    : regularBrush;
                lines.Add(CreateLineDot(point, brush));
                previous = point;
                pointCount++;
            }
            brush = selectedSequence.End == 0 ? sequenceBrush : regularBrush;
            lines.Add(CreateLine(previous, initial, brush, false));

            var sequenceLength = selectedSequence.End - selectedSequence.Start;
            sequenceLength += sequenceLength < 0 ? selectedPath.Points.Count : 0;
            SequencePointCount.Content = sequenceLength;

            LineGrid.Children.Clear();
            LineGrid.Width = gridWidth;
            LineGrid.Height = gridHeight;
            LineGrid.Children.AddRange(lines);
            ImageDisplay.Source = BitmapToImageSource(CreateTransparentBitmap(_loadedImage.Width + 1, _loadedImage.Height + 1));
        }

        private Dictionary<ColorReference, Layer<SegmentPath>> _segmentLayers;
        private void Part7Button_Click(object sender, RoutedEventArgs e)
        {
            _segmentLayers = _sequenceLayers.ToDictionary(ci => ci.Key, ci => new Layer<SegmentPath>
            {
                Paths = ci.Value.Paths.Select(path => new SegmentPath
                {
                    Path = path.Path,
                    Segments = path.Sequences.Select(s => Segmentation.Fit(path.Path.Points, _options.Tracing, s)).SelectMany(s => s).ToList()
                }).ToList()
            });
            var segments = _segmentLayers.SelectMany(cl => cl.Value.Paths.SelectMany(p => p.Segments.Select(s => new { Color = cl.Key, Type = s is LineSegment ? "Line" : (s is SplineSegment ? "Spline" : "Unknown") }))).ToList();

            LineGrid.Children.Clear();
            foreach (var index in segments.Select((s, i) => i))
            {
                DrawSegment(index);
            }
            ImageDisplay.Source = BitmapToImageSource(CreateTransparentBitmap(_loadedImage.Width + 1, _loadedImage.Height + 1));

            Part7ComboBox.ItemsSource = segments.Select((cs, i) => new ColorSelectionItem(cs.Color, i) { Type = $"{i} {cs.Type}" }).ToList();
            SegmentCount.Content = segments.Count;
            Part7Button.IsEnabled = false;
        }

        private void DrawSegment(int index)
        {
            var segments =
                _segmentLayers.SelectMany(
                    cl => cl.Value.Paths.SelectMany(p => p.Segments.Select(s => new { Color = cl.Key, Segment = s })))
                    .ToList();
            var segmentAndColor = segments[index];
            var segment = segmentAndColor.Segment;

            var color = segmentAndColor.Color.Color;
            var regularBrush = new SolidColorBrush(MColor.FromArgb(color.A, color.R, color.G, color.B));
            var points = segment is SplineSegment
                ? new[] { segment.Start, ((SplineSegment)segment).Mid, segment.End }
                : new[] { segment.Start, segment.End };
            double gridWidth = _loadedImage.Width;
            double gridHeight = _loadedImage.Height;
            var offset = CalculateScaledOffsets(ref gridWidth, ref gridHeight);

            var multiplier = 10.0;
            var lines = new List<UIElement>();
            Func<Point<double>, Point<double>> scale = p => new Point<double>
            {
                X = p.X * multiplier + offset.Width,
                Y = p.Y * multiplier + offset.Height
            };
            var scaledPoints = points.Select(p => scale(p)).ToList();
            var initial = scaledPoints.First();
            var previous = scaledPoints.First();
            var brush = regularBrush;

            if (segment is LineSegment)
            {
                foreach (var point in scaledPoints)
                {
                    if (previous != null)
                    {
                        lines.Add(CreateLine(previous, point, brush, false));
                    }
                    lines.Add(CreateLineDot(point, brush));
                    previous = point;
                }
            }
            //http://stackoverflow.com/a/21958079/294804
            //http://stackoverflow.com/a/5336694/294804
            if (segment is SplineSegment)
            {
                lines.Add(CreateLineDot(scaledPoints[0], brush));
                var path = new System.Windows.Shapes.Path();
                path.Stroke = brush;
                var geometry = new PathGeometry();
                var pathFigure = new PathFigure();
                pathFigure.StartPoint = new System.Windows.Point(scaledPoints[0].X, scaledPoints[0].Y);

                // Radius Calculation
                //http://www.purplemath.com/modules/midpoint.htm
                var midpoint = new Point<double> { X = (scaledPoints[0].X + scaledPoints[2].X) / 2, Y = (scaledPoints[0].Y + scaledPoints[2].Y) / 2 };
                //http://www.mathwarehouse.com/algebra/distance_formula/index.php
                var triangleHeight = Math.Sqrt(Math.Pow(midpoint.X - scaledPoints[1].X, 2) + Math.Pow(midpoint.Y - scaledPoints[1].Y, 2));
                //http://keisan.casio.com/exec/system/1273850202
                var baseLength = Math.Sqrt(Math.Pow(scaledPoints[0].X - scaledPoints[2].X, 2) + Math.Pow(scaledPoints[0].Y - scaledPoints[2].Y, 2));
                var sideLength = Math.Sqrt(Math.Pow(triangleHeight, 2) + (Math.Pow(baseLength, 2) / 4));
                // Might be useful: http://stackoverflow.com/questions/22408769/how-can-i-draw-a-circular-arc-with-three-points-in-a-streamgeometry
                var arc = new ArcSegment(
                    new System.Windows.Point(scaledPoints[2].X, scaledPoints[2].Y),
                    new System.Windows.Size(sideLength, sideLength),
                    0, // TODO: Determine if this needs calculated.
                    false,
                    scaledPoints[0].Y < scaledPoints[1].Y && scaledPoints[1].X < scaledPoints[2].X ? SweepDirection.Counterclockwise : SweepDirection.Clockwise,
                    true);
                pathFigure.Segments.Add(arc);
                geometry.Figures.Add(pathFigure);
                path.Data = geometry;
                lines.Add(path);

                lines.Add(CreateLineDot(scaledPoints[1], brush));
                lines.Add(CreateLineDot(scaledPoints[2], brush));
            }

            lines.Add(CreateLine(previous, initial, brush, false));

            
            LineGrid.Width = gridWidth;
            LineGrid.Height = gridHeight;
            LineGrid.Children.AddRange(lines);
            
        }

        private void Part7ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems.OfType<ColorSelectionItem>().First();
            var index = selected.Index;

            LineGrid.Children.Clear();
            DrawSegment(index);
            ImageDisplay.Source = BitmapToImageSource(CreateTransparentBitmap(_loadedImage.Width + 1, _loadedImage.Height + 1));
        }
    }
}
