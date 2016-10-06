using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
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
            ZoomPanControl.ContentScaleChanged += (o, args) => CanvasScroller.Visibility = Visibility.Visible;
            ZoomPanControl.AnimationDuration = 0.01;
        }

        ////////////////////////////////////////////////////////////

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

        private static readonly string[] ParameterNames =
        {
            "ltres", "qtres", "pathomit", "colorsampling", "numberofcolors",
            "mincolorratio", "colorquantcycles", "scale", "simplifytolerance",
            "roundcoords", "lcpr", "qcpr", "desc", "viewbox", "blurradius",
            "blurdelta", "outfilename"
        };

        private Bitmap _loadedImage;
        private Options _options;
        private string _outputFilename;
        private bool _part1Complete;
        private void Part1Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_part1Complete)
            {
                var args = new List<string>
                {
                    @"..\..\Images\1.png",
                    "outfilename", @"1.svg",
                    "ltres", "0.1",
                    "qtres", "1",
                    "scale", "44",
                    "numberofcolors", "256",
                    "pathomit", "0"
                };
                // Parameter parsing
                var outputFilename = args[0] + ".svg";
                var options = new Options();
                foreach (var name in ParameterNames)
                {
                    if (args.Contains(name.ToLower()))
                    {
                        var j = args.FindIndex(a => a == name.ToLower());
                        if (name == "outfilename")
                        {
                            if (j < args.Count - 1)
                            {
                                outputFilename = args[j + 1];
                            }
                        }
                        else
                        {
                            var f = (float)Convert.ToDouble(args[j + 1]);
                            if (f > -1)
                            {
                                options.SetOptionByName(name, f);
                            }
                        }
                    }
                }

                // Loading image, tracing, rendering SVG, saving SVG file
                var rbgImage = new Bitmap(args[0]).ChangeFormat(PixelFormat.Format32bppArgb);
                _loadedImage = rbgImage;
                _options = options;
                _outputFilename = outputFilename;
                _part1Complete = true;
            }
            CanvasScroller.Visibility = Visibility.Hidden;
            ImageDisplay.Source = BitmapToImageSource(_loadedImage);
        }

        private static Bitmap CreateBitmapFromColorPoints(IEnumerable<ColorPoint> colorPoints, int width, int height)
        {
            var bytes = new byte[height][].InitInner(width * 4);
            Parallel.ForEach(colorPoints, p =>
            {
                bytes[p.Y][p.X * 4] = p.Color.B;
                bytes[p.Y][p.X * 4 + 1] = p.Color.G;
                bytes[p.Y][p.X * 4 + 2] = p.Color.R;
                bytes[p.Y][p.X * 4 + 3] = p.Color.A;
            });
            return bytes.SelectMany(b => b).ToArray().ToBitmap(width, height, PixelFormat.Format32bppArgb);
        }

        private Bitmap _paletteImage;
        private IEnumerable<ColorGroup> _colorGroups;
        private bool _part2Complete;
        private void Part2Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_part2Complete)
            {
                var colors = _loadedImage.ToColorReferences();
                _colorGroups = ColorGrouping.Convert(colors, _loadedImage.Width, _loadedImage.Height, ImageTracer.Palette);

                var colorBytes = _colorGroups.Where(cg => cg.Mid != ColorReference.Empty).Select(cg => 
                    new[] { cg.Mid.Color.B, cg.Mid.Color.G, cg.Mid.Color.R, cg.Mid.Color.A}).SelectMany(b => b).ToArray();

                _paletteImage = colorBytes.ToBitmap(_loadedImage.Width, _loadedImage.Height, PixelFormat.Format32bppArgb);
                _part2Complete = true;
            }
            CanvasScroller.Visibility = Visibility.Hidden;
            ImageDisplay.Source = BitmapToImageSource(_paletteImage);
        }

        private Dictionary<ColorReference, RawLayer> _rawLayers;
        private Dictionary<ColorReference, RawLayer> _filteredRawLayers;
        private bool _part3Complete;
        private void Part3Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_part3Complete)
            {
                _rawLayers = Layering.Convert(_colorGroups, _loadedImage.Width, _loadedImage.Height, ImageTracer.Palette);
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
            CanvasScroller.Visibility = Visibility.Hidden;
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
                var imagePoints =_pathPointLayers.SelectMany(cl => cl.Value.Paths.SelectMany(p => p.Points).Select(p => new ColorPoint { X = p.X, Y = p.Y, Color = cl.Key.Color }));
                _pathPointImage = CreateBitmapFromColorPoints(imagePoints, _loadedImage.Width + 1, _loadedImage.Height + 1);
                _part4Complete = true;
                Part4ComboBox.ItemsSource = paths.Select((cp, i) => new ColorSelectionItem(cp.Color, i)).ToList();
                PathCount.Content = paths.Count;
            }
            CanvasScroller.Visibility = Visibility.Hidden;
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

        private static Bitmap DrawPointsImage(IEnumerable<Point<int>> points, int width, int height, DColor color)
        {
            //return DrawPointsImage(points.Select(p => Tuple.Create(p, color)), width, height);
            return CreateBitmapFromColorPoints(points.Select(p => new ColorPoint { X = p.X, Y = p.Y, Color = color }), width, height);
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

            //http://stackoverflow.com/questions/1624341/getting-pair-set-using-linq
            var groupedPoints = scaledPoints.Select((p, i) => new { First = p, Second = scaledPoints[i == scaledPoints.Count - 1 ? 0 : i + 1] });
            foreach (var point in groupedPoints)
            {
                yield return CreateLineDot(point.First, brush);
                yield return CreateLine(point.First, point.Second, brush, isAnimated);
            }
        }

        private void Part4ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems.OfType<ColorSelectionItem>().First();
            var index = selected.Index;
            var path = _pathPointLayers.SelectMany(cl => cl.Value.Paths.Select(p => new { Color = cl.Key, p.Points })).Where((cp, i) => i == index).Single();
            var color = path.Color.Color;
            var image = DrawPointsImage(path.Points, _loadedImage.Width + 1, _loadedImage.Height + 1, color);
            PathPointsCount.Content = path.Points.Count;
            CanvasScroller.Visibility = Visibility.Hidden;
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
                if (true)
                {
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
                        var points = path.Points.Select(p => new Point<double> {X = p.X, Y = p.Y}).ToList();
                        var pathLines = CreateOverlayLines(points, offset, brush, 10.0, false);
                        lines.AddRange(pathLines);
                    }
                    _interpLines = lines;
                }
                //_interpLines = new List<UIElement>();
                _part5Compete = true;
                InterpCount.Content = paths.Count;
            }

            LineGrid.Children.Clear();
            LineGrid.Width = _gridWidth;
            LineGrid.Height = _gridHeight;
            LineGrid.Children.AddRange(_interpLines);
            CanvasScroller.Visibility = Visibility.Hidden;
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
            //var initial = scaledPoints.First();
            //var previous = scaledPoints.First();
            //var pointCount = 0;
            //var brush = regularBrush;
            //foreach (var point in scaledPoints)
            //{

            //    if (previous != null)
            //    {
            //        brush = pointCount > selectedSequence.Start && (selectedSequence.End == 0 || pointCount <= selectedSequence.End)
            //            ? sequenceBrush
            //            : regularBrush;
            //        lines.Add(CreateLine(previous, point, brush, false));
            //    }
            //    brush = (pointCount >= selectedSequence.Start && (selectedSequence.End == 0  || pointCount <= selectedSequence.End)) || (selectedSequence.End == pointCount)
            //        ? sequenceBrush
            //        : regularBrush;
            //    lines.Add(CreateLineDot(point, brush));
            //    previous = point;
            //    pointCount++;
            //}
            //brush = selectedSequence.End == 0 ? sequenceBrush : regularBrush;
            //lines.Add(CreateLine(previous, initial, brush, false));

            //http://stackoverflow.com/questions/1624341/getting-pair-set-using-linq
            var groupedPoints = scaledPoints.Select((p, i) => new { First = p, Second = scaledPoints[i == scaledPoints.Count - 1 ? 0 : i + 1], Index = i });
            foreach (var point in groupedPoints)
            {
                var brush = (point.Index >= selectedSequence.Start && (selectedSequence.End == 0 || point.Index <= selectedSequence.End)) || (selectedSequence.End == point.Index) ? sequenceBrush : regularBrush;
                lines.Add(CreateLineDot(point.First, brush));
                brush = point.Index >= selectedSequence.Start && (selectedSequence.End == 0 || point.Index < selectedSequence.End) ? sequenceBrush : regularBrush;
                lines.Add(CreateLine(point.First, point.Second, brush, false));
            }

            var sequenceLength = selectedSequence.End - selectedSequence.Start;
            sequenceLength += sequenceLength < 0 ? selectedPath.Points.Count : 0;
            SequencePointCount.Content = sequenceLength;

            LineGrid.Children.Clear();
            LineGrid.Width = gridWidth;
            LineGrid.Height = gridHeight;
            LineGrid.Children.AddRange(lines);
            CanvasScroller.Visibility = Visibility.Hidden;
            ImageDisplay.Source = BitmapToImageSource(CreateTransparentBitmap(_loadedImage.Width + 1, _loadedImage.Height + 1));
        }

        private Dictionary<ColorReference, Layer<SegmentPath>> _segmentLayers;
        private bool _part7Complete;
        private void Part7Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_part7Complete)
            {
                _segmentLayers = _sequenceLayers.ToDictionary(ci => ci.Key, ci => new Layer<SegmentPath>
                {
                    Paths = ci.Value.Paths.Select(path => new SegmentPath
                    {
                        Segments = path.Sequences.Select(s => Segmentation.Fit(path.Path.Points, s, _options.Tracing, _options.SvgRendering)).SelectMany(s => s).ToList()
                    }).ToList()
                });
                var segments = _segmentLayers.SelectMany(cl => cl.Value.Paths.SelectMany(p => p.Segments.Select(s => new { Color = cl.Key, Type = s is LineSegment ? "Line" : (s is SplineSegment ? "Spline" : "Unknown") }))).ToList();
                Part7ComboBox.ItemsSource = segments.Select((cs, i) => new ColorSelectionItem(cs.Color, i) { Type = $"{i} {cs.Type}" }).ToList();
                SegmentCount.Content = segments.Count;
                _part7Complete = true;
            }

            if (true)
            {
                LineGrid.Children.Clear();
                double gridWidth = _loadedImage.Width;
                double gridHeight = _loadedImage.Height;
                var offset = CalculateScaledOffsets(ref gridWidth, ref gridHeight);

                var indices = _segmentLayers.SelectMany(cl => cl.Value.Paths.SelectMany(p => p.Segments)).ToList();
                var segmentLines = indices.SelectMany((s, i) => CreateSegmentLines(i, offset, false));
                
                LineGrid.Width = gridWidth;
                LineGrid.Height = gridHeight;
                LineGrid.Children.AddRange(segmentLines);
                CanvasScroller.Visibility = Visibility.Hidden;
                ImageDisplay.Source = BitmapToImageSource(CreateTransparentBitmap(_loadedImage.Width + 1, _loadedImage.Height + 1));
            }

            //Part7Button.IsEnabled = false;
        }

        private IEnumerable<UIElement> CreateSegmentLines(int index, MSize offset, bool drawMidPoint = true)
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
                lines.Add(CreateLine(previous, initial, brush, false));
            }
            //http://stackoverflow.com/a/21958079/294804
            //http://stackoverflow.com/a/5336694/294804
            if (segment is SplineSegment)
            {
                lines.Add(CreateLineDot(scaledPoints[0], brush));

                //http://stackoverflow.com/questions/13940983/how-to-draw-bezier-curve-by-several-points
                var b = Bezier.GetBezierApproximation(scaledPoints.Select(p => new System.Windows.Point(p.X, p.Y)).ToArray(), 256);
                PathFigure pf = new PathFigure(b.Points[0], new[] { b }, false);
                PathFigureCollection pfc = new PathFigureCollection();
                pfc.Add(pf);
                var pge = new PathGeometry {Figures = pfc};
                System.Windows.Shapes.Path path = new System.Windows.Shapes.Path
                {
                    Data = pge,
                    Stroke = brush
                };
                lines.Add(path);
                if (drawMidPoint)
                {
                    lines.Add(CreateLineDot(scaledPoints[1], brush));
                }
                
                lines.Add(CreateLineDot(scaledPoints[2], brush));
            }

            return lines;
        }

        private void Part7ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems.OfType<ColorSelectionItem>().First();
            var index = selected.Index;

            double gridWidth = _loadedImage.Width;
            double gridHeight = _loadedImage.Height;
            var offset = CalculateScaledOffsets(ref gridWidth, ref gridHeight);

            LineGrid.Children.Clear();
            var segmentLines = CreateSegmentLines(index, offset);
            CanvasScroller.Visibility = Visibility.Hidden;
            ImageDisplay.Source = BitmapToImageSource(CreateTransparentBitmap(_loadedImage.Width + 1, _loadedImage.Height + 1));

            LineGrid.Width = gridWidth;
            LineGrid.Height = gridHeight;
            LineGrid.Children.AddRange(segmentLines);
        }

        private string _svgImage;
        private DrawingGroup _renderedSvg;
        private bool _part8Complete;
        private void Part8Button_Click(object sender, RoutedEventArgs e)
        {
            if (!_part8Complete)
            {
                //var layersList = _segmentLayers.Select(p => p.Value).ToList();
                //_image.Layers = layersList;
                //_svgImage = ToSvgString(_image, _segmentLayers, _options.SvgRendering);
                _svgImage = new TracedImage(_segmentLayers, _loadedImage.Width, _loadedImage.Height).ToSvgString(_options.SvgRendering);
                File.WriteAllText(_outputFilename, _svgImage);

                //http://stackoverflow.com/questions/1879395/how-to-generate-a-stream-from-a-string
                using (MemoryStream stream = new MemoryStream())
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(_svgImage);
                    writer.Flush();
                    stream.Position = 0;

                    var wpfSettings = new WpfDrawingSettings();
                    wpfSettings.CultureInfo = wpfSettings.NeutralCultureInfo;
                    var reader = new FileSvgReader(wpfSettings);
                    _renderedSvg = reader.Read(stream);

                    //ZoomPanControl.ScaleToFit();
                    //CanvasScroller.Visibility = Visibility.Visible;
                }

                //SvgViewer.UnloadDiagrams();


                CanvasScroller.Visibility = Visibility.Hidden;
                SvgViewer.RenderDiagrams(_renderedSvg);

                Rect bounds = SvgViewer.Bounds;
                if (bounds.IsEmpty)
                {
                    bounds = new Rect(0, 0, CanvasScroller.ActualWidth, CanvasScroller.ActualHeight);
                }

                ZoomPanControl.AnimatedZoomTo(bounds);

                _part8Complete = true;
                //Part8Button.IsEnabled = false;
            }
            else
            {
                CanvasScroller.Visibility = Visibility.Visible;
            }

            ImageDisplay.Source = BitmapToImageSource(CreateTransparentBitmap(_loadedImage.Width + 1, _loadedImage.Height + 1));
        }
    }
}
