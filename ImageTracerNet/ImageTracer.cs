using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ImageTracerNet.Extensions;
using ImageTracerNet.Palettes;
using System.Windows.Media.Imaging;
using TriListIntArray = System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<int[]>>>; // ArrayList<ArrayList<ArrayList<Integer[]>>>
using TriListDoubleArray = System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>; // ArrayList<ArrayList<ArrayList<Double[]>>>

namespace ImageTracerNet
{
    public static class ImageTracer
    {
        public static string VersionNumber = "1.1.1";

        private static readonly Random Rng = new Random();

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
            var data = rbgImage.ToRgbaByteArray();
            return new ImageData(image.Width, image.Height, data);
        }

        // Tracing ImageData, then returning the SVG String
        private static string ImageDataToSvg(Bitmap image, ImageData imgd, Options options, byte[][] palette)
        {
            return GetSvgString(ImageDataToTraceData(image, imgd, options, palette), options);
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

            var colorPalette = BitmapPalettes.Halftone256.Colors.Select(c => Color.FromArgb(c.A, c.R, c.G, c.B)).ToArray();

            // Selective Gaussian blur preprocessing
            if (options.Blur.BlurRadius > 0)
            {
                // TODO: This seems to not work currently.
                imgd = Blur(imgd, options.Blur.BlurRadius, options.Blur.BlurDelta);
            }

            // 1. Color quantization
            var ii = IndexedImage.Create(imgd, colorPalette, options.ColorQuantization);
            // 2. Layer separation and edge detection
            var rawLayers = Layering(ii);
            // 3. Batch pathscan
            var bps = rawLayers.Select(layer => PathScan(layer, options.Tracing.PathOmit)).ToList();
            // 4. Batch interpollation
            var bis = BatchInterNodes(bps);
            // 5. Batch tracing
            ii.Layers = BatchTraceLayers(bis, options.Tracing.LTres, options.Tracing.QTres);
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
        private static int[][][] Layering(IndexedImage ii)
        {
            // Creating layers for each indexed color in arr
            var layers = new int[ii.Palette.Length][][].InitInner(ii.ArrayHeight, ii.ArrayWidth);

            // Looping through all pixels and calculating edge node type
            for (var j = 1; j < ii.ArrayHeight - 1; j++)
            {
                for (var i = 1; i < ii.ArrayWidth - 1; i++)
                {
                    // This pixel's indexed color
                    var pg = ii.GetPixelGroup(j, i);

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

        // 3. Walking through an edge node array, discarding edge node types 0 and 15 and creating paths from the rest.
        // Walk directions (dir): 0 > ; 1 ^ ; 2 < ; 3 v

        // Edge node types ( ▓:light or 1; ░:dark or 0 )

        // ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓
        // ░░  ░░  ░░  ░░  ░▓  ░▓  ░▓  ░▓  ▓░  ▓░  ▓░  ▓░  ▓▓  ▓▓  ▓▓  ▓▓
        // 0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
        private static List<List<int[]>> PathScan(int[][] arr, int pathOmit)
        {
            var paths = new List<List<int[]>>();
            var w = arr[0].Length;
            var h = arr.Length;
            //var dir = 0;
            var holePath = false;

            for (var j = 0; j < h; j++)
            {
                for (var i = 0; i < w; i++)
                {
                    var initialNodeValue = arr[j][i];

                    // Follow path
                    // MJY: Logically, arr[j][i] cannot equal 0
                    if ((initialNodeValue == 0) || (initialNodeValue == 15)) continue;

                    // fill paths will be drawn, but hole paths are also required to remove unnecessary edge nodes
                    //var initialZeroNodes = new []{1, 3, 5, 7, 8, 12, 14};
                    var initialOneNodes = new[] { 4, 11 };
                    var initialThreeNodes = new[] { 2, 6, 9, 10, 13 };
                    var dir = initialOneNodes.Contains(initialNodeValue) ? 1 : (initialThreeNodes.Contains(initialNodeValue) ? 3 : 0);

                    var holeNodes = new[] { 7, 11, 13, 14 };
                    const int nonHoleNode = 4;
                    holePath = holeNodes.Contains(initialNodeValue) || (nonHoleNode != initialNodeValue && holePath);

                    // Init
                    var px = i;
                    var py = j;
                    var thisPath = new List<int[]>();
                    paths.Add(thisPath);
                    var pathFinished = false;

                    // Path points loop
                    while (!pathFinished)
                    {
                        var nodeValue = arr[py][px];

                        // New path point
                        var pathPoint = new int[3];
                        pathPoint[0] = px - 1;
                        pathPoint[1] = py - 1;
                        pathPoint[2] = nodeValue;
                        thisPath.Add(pathPoint);

                        var nonZeroNodes = new Dictionary<int, int[]>
                        {
                            [5] = new []{13, 13, 7, 7},
                            [10] = new []{11, 14, 14, 11}
                        };
                        arr[py][px] = nonZeroNodes.ContainsKey(nodeValue) ? nonZeroNodes[nodeValue][dir] : 0;

                        var nodeValueDirPair = new Tuple<int, int>(nodeValue, dir);
                        var minusOneYs = new List<Tuple<int, int>>
                        {
                            {1, 0},
                            {2, 2},
                            {5, 2},
                            {6, 1},
                            {9, 1},
                            {10, 0},
                            {13, 2},
                            {14, 0}
                        };
                        var plusOneYs = new List<Tuple<int, int>>
                        {
                            {4, 2},
                            {5, 0},
                            {6, 3},
                            {7, 0},
                            {8, 0},
                            {9, 3},
                            {10, 2},
                            {11, 2}
                        };
                        py += minusOneYs.Contains(nodeValueDirPair) ? -1 : (plusOneYs.Contains(nodeValueDirPair) ? 1 : 0);

                        var minusOneXs = new List<Tuple<int, int>>
                        {
                            {1, 3},
                            {3, 2},
                            {5, 1},
                            {7, 1},
                            {8, 1},
                            {10, 3},
                            {12, 2},
                            {14, 3}
                        };
                        var plusOneXs = new List<Tuple<int, int>>
                        {
                            {2, 3},
                            {3, 0},
                            {4, 1},
                            {5, 3},
                            {10, 1},
                            {11, 1},
                            {12, 0},
                            {13, 3}
                        };
                        px += minusOneXs.Contains(nodeValueDirPair) ? -1 : (plusOneXs.Contains(nodeValueDirPair) ? 1 : 0);

                        //Action closePath = () =>
                        //{
                        //    pathFinished = true;
                        //    paths.Remove(thisPath);
                        //};

                        // This is a list that is distinct from all the x/y plus/minus combined.
                        //var endPaths = new List<Tuple<int, int>>
                        //{
                        //    {1, 1},
                        //    {1, 2},
                        //    {2, 0},
                        //    {2, 1},
                        //    {3, 1},
                        //    {3, 3},
                        //    {4, 0},
                        //    {4, 3},
                        //    {6, 0},
                        //    {6, 2},
                        //    {7, 2},
                        //    {7, 3},
                        //    {8, 2},
                        //    {8, 3},
                        //    {9, 0},
                        //    {9, 2},
                        //    {11, 0},
                        //    {11, 3},
                        //    {12, 1},
                        //    {12, 3},
                        //    {13, 0},
                        //    {13, 1},
                        //    {14, 1},
                        //    {14, 2}
                        //};

                        var allInOne = minusOneYs.Concat(minusOneXs.Concat(plusOneYs.Concat(plusOneXs))).ToList();
                        //var worked = true;
                        //foreach (var xy in allInOne)
                        //{
                        //    worked &= !endPaths.Contains(xy);
                        //}
                        //var combined = endPaths.Intersect(allInOne).ToList();
                        //var together = endPaths.Concat(allInOne).ToList();
                        //var distinct = together.Distinct().Count() == together.Count();

                        //var worked2 = true;
                        //for (var k = 1; k < 15; ++k)
                        //{
                        //    for (var l = 0; l < 4; ++l)
                        //    {
                        //        worked2 &= together.Contains(new Tuple<int, int>(k, l));
                        //    }
                        //}

                        if (!allInOne.Contains(nodeValueDirPair))
                        {
                            pathFinished = true;
                            paths.Remove(thisPath);
                        }

                        // Node types
                        if (nodeValue == 1)
                        {
                            if (dir == 0)
                            {
                                dir = 1;
                            }
                            else if (dir == 3)
                            {
                                dir = 2;
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }
                        else if (nodeValue == 2)
                        {
                            if (dir == 3)
                            {
                                dir = 0;
                            }
                            else if (dir == 2)
                            {
                                dir = 1;
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }
                        else if (nodeValue == 3)
                        {
                            if (dir == 0)
                            {
                            }
                            else if (dir == 2)
                            {
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }
                        else if (nodeValue == 4)
                        {
                            if (dir == 1)
                            {
                                dir = 0;
                            }
                            else if (dir == 2)
                            {
                                dir = 3;
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }
                        else if (nodeValue == 5)
                        {
                            if (dir == 0)
                            {
                                dir = 3;
                            }
                            else if (dir == 1)
                            {
                                dir = 2;
                            }
                            else if (dir == 2)
                            {
                                dir = 1;
                            }
                            else if (dir == 3)
                            {
                                dir = 0;
                            }
                        }
                        else if (nodeValue == 6)
                        {
                            if (dir == 1)
                            {
                            }
                            else if (dir == 3)
                            {
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }
                        else if (nodeValue == 7)
                        {
                            if (dir == 0)
                            {
                                dir = 3;
                            }
                            else if (dir == 1)
                            {
                                dir = 2;
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }
                        else if (nodeValue == 8)
                        {
                            if (dir == 0)
                            {
                                dir = 3;
                            }
                            else if (dir == 1)
                            {
                                dir = 2;
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }
                        else if (nodeValue == 9)
                        {
                            if (dir == 1)
                            {
                            }
                            else if (dir == 3)
                            {
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }
                        else if (nodeValue == 10)
                        {
                            if (dir == 0)
                            {
                                dir = 1;
                            }
                            else if (dir == 1)
                            {
                                dir = 0;
                            }
                            else if (dir == 2)
                            {
                                dir = 3;
                            }
                            else if (dir == 3)
                            {
                                dir = 2;
                            }
                        }
                        else if (nodeValue == 11)
                        {
                            if (dir == 1)
                            {
                                dir = 0;
                            }
                            else if (dir == 2)
                            {
                                dir = 3;
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }
                        else if (nodeValue == 12)
                        {
                            if (dir == 0)
                            {
                            }
                            else if (dir == 2)
                            {
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }
                        else if (nodeValue == 13)
                        {
                            if (dir == 2)
                            {
                                dir = 1;
                            }
                            else if (dir == 3)
                            {
                                dir = 0;
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }
                        else if (nodeValue == 14)
                        {
                            if (dir == 0)
                            {
                                dir = 1;
                            }
                            else if (dir == 3)
                            {
                                dir = 2;
                            }
                            else
                            {
                                //pathFinished = true;
                                //paths.Remove(thisPath);
                            }
                        }

                        // Close path
                        if ((px - 1 == thisPath[0][0]) && (py - 1 == thisPath[0][1]))
                        {
                            pathFinished = true;
                            // Discarding 'hole' type paths and paths shorter than pathomit
                            if (holePath || (thisPath.Count < pathOmit))
                            {
                                paths.Remove(thisPath);
                            }
                        }
                    }// End of Path points loop
                }// End of i loop
            }// End of j loop

            return paths;
        }

        // 4. interpolating between path points for nodes with 8 directions ( East, SouthEast, S, SW, W, NW, N, NE )
        private static List<List<double[]>> InterNodes(List<List<int[]>> paths)
        {
            var ins = new List<List<double[]>>();
            double[] nextpoint = new double[2];

            // paths loop
            for (var pacnt = 0; pacnt < paths.Count; pacnt++)
            {
                ins.Add(new List<double[]>());
                var thisinp = ins[ins.Count - 1];
                var palen = paths[pacnt].Count;
                // pathpoints loop
                for (var pcnt = 0; pcnt < palen; pcnt++)
                {
                    // interpolate between two path points
                    var nextidx = (pcnt + 1) % palen; var nextidx2 = (pcnt + 2) % palen;
                    thisinp.Add(new double[3]);
                    var thispoint = thisinp[thisinp.Count - 1];
                    var pp1 = paths[pacnt][pcnt];
                    var pp2 = paths[pacnt][nextidx];
                    var pp3 = paths[pacnt][nextidx2];
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
        }

        // 4. Batch interpollation
        private static TriListDoubleArray BatchInterNodes(TriListIntArray bpaths)
        {
            var binternodes = new TriListDoubleArray();
            for (var k = 0; k < bpaths.Count; k++)
            {
                binternodes.Add(InterNodes(bpaths[k]));
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

        private static List<double[]> TracePath(List<double[]> path, double ltreshold, double qtreshold)
        {
            int pcnt = 0;
            var smp = new List<double[]>();
            //Double [] thissegment;
            var pathlength = path.Count;

            while (pcnt < pathlength)
            {
                // 5.1. Find sequences of points with only 2 segment types
                var segtype1 = path[pcnt][2]; double segtype2 = -1; var seqend = pcnt + 1;
                while ((path[seqend][2].AreEqual(segtype1) || path[seqend][2].AreEqual(segtype2) || segtype2.AreEqual(-1)) && (seqend < pathlength - 1))
                {
                    if (path[seqend][2].AreNotEqual(segtype1) && segtype2.AreEqual(-1)) { segtype2 = path[seqend][2]; }
                    seqend++;
                }
                if (seqend == pathlength - 1) { seqend = 0; }

                // 5.2. - 5.6. Split sequence and recursively apply 5.2. - 5.6. to startpoint-splitpoint and splitpoint-endpoint sequences
                smp.AddRange(FitSeq(path, ltreshold, qtreshold, pcnt, seqend));
                // 5.7. TODO? If splitpoint-endpoint is a spline, try to add new points from the next sequence

                // forward pcnt;
                if (seqend > 0) { pcnt = seqend; } else { pcnt = pathlength; }

            }// End of pcnt loop
            return smp;
        }

        // 5.2. - 5.6. recursively fitting a straight or quadratic line segment on this sequence of path nodes,
        // called from tracepath()
        private static List<double[]> FitSeq(List<double[]> path, double ltreshold, double qtreshold, int seqstart, int seqend)
        {
            var segment = new List<double[]>();
            double[] thissegment;
            var pathlength = path.Count;

            // return if invalid seqend
            if ((seqend > pathlength) || (seqend < 0)) { return segment; }

            var errorpoint = seqstart;
            var curvepass = true;
            double px, py, dist2, errorval = 0;
            double tl = seqend - seqstart; if (tl < 0) { tl += pathlength; }
            double vx = (path[seqend][0] - path[seqstart][0]) / tl,
                    vy = (path[seqend][1] - path[seqstart][1]) / tl;

            // 5.2. Fit a straight line on the sequence
            var pcnt = (seqstart + 1) % pathlength;
            while (pcnt != seqend)
            {
                double pl = pcnt - seqstart; if (pl < 0) { pl += pathlength; }
                px = path[seqstart][0] + vx * pl; py = path[seqstart][1] + vy * pl;
                dist2 = (path[pcnt][0] - px) * (path[pcnt][0] - px) + (path[pcnt][1] - py) * (path[pcnt][1] - py);
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
            var fitpoint = errorpoint; curvepass = true; errorval = 0;

            // 5.4. Fit a quadratic spline through this point, measure errors on every point in the sequence
            // helpers and projecting to get control point
            double t = (fitpoint - seqstart) / tl, t1 = (1.0 - t) * (1.0 - t), t2 = 2.0 * (1.0 - t) * t, t3 = t * t;
            double cpx = (t1 * path[seqstart][0] + t3 * path[seqend][0] - path[fitpoint][0]) / -t2,
                    cpy = (t1 * path[seqstart][1] + t3 * path[seqend][1] - path[fitpoint][1]) / -t2;

            // Check every point
            pcnt = seqstart + 1;
            while (pcnt != seqend)
            {

                t = (pcnt - seqstart) / tl; t1 = (1.0 - t) * (1.0 - t); t2 = 2.0 * (1.0 - t) * t; t3 = t * t;
                px = t1 * path[seqstart][0] + t2 * cpx + t3 * path[seqend][0];
                py = t1 * path[seqstart][1] + t2 * cpy + t3 * path[seqend][1];

                dist2 = (path[pcnt][0] - px) * (path[pcnt][0] - px) + (path[pcnt][1] - py) * (path[pcnt][1] - py);

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
            var splitpoint = (fitpoint + errorpoint) / 2;

            // 5.6. Split sequence and recursively apply 5.2. - 5.6. to startpoint-splitpoint and splitpoint-endpoint sequences
            segment = FitSeq(path, ltreshold, qtreshold, seqstart, splitpoint);
            segment.AddRange(FitSeq(path, ltreshold, qtreshold, splitpoint, seqend));
            return segment;
        }

        // 5. Batch tracing paths
        private static List<List<double[]>> BatchTracePaths(List<List<double[]>> internodepaths, double ltres, double qtres)
        {
            var btracedpaths = new List<List<double[]>>();
            for (var k = 0; k < internodepaths.Count; k++)
            {
                btracedpaths.Add(TracePath(internodepaths[k], ltres, qtres));
            }
            return btracedpaths;
        }

        // 5. Batch tracing layers
        private static TriListDoubleArray BatchTraceLayers(TriListDoubleArray binternodes, double ltres, double qtres)
        {
            var btbis = new TriListDoubleArray();
            for (var k = 0; k < binternodes.Count; k++)
            {
                btbis.Add(BatchTracePaths(binternodes[k], ltres, qtres));
            }
            return btbis;
        }

        ////////////////////////////////////////////////////////////
        //
        //  SVG Drawing functions
        //
        ////////////////////////////////////////////////////////////

        private static double RoundToDec(double val, double places)
        {
            return Math.Round(val * Math.Pow(10, places)) / Math.Pow(10, places);
        }

        // Getting SVG path element string from a traced path
        private static void SvgPathString(StringBuilder sb, string desc, List<double[]> segments, string colorstr, Options options)
        {
            double scale = options.SvgRendering.Scale, lcpr = options.SvgRendering.LCpr, qcpr = options.SvgRendering.LCpr, roundcoords = Math.Floor(options.SvgRendering.RoundCoords);
            // Path
            sb.Append("<path ").Append(desc).Append(colorstr).Append("d=\"").Append("M ").Append(segments[0][1] * scale).Append(" ").Append(segments[0][2] * scale).Append(" ");

            if (roundcoords.AreEqual(-1))
            {
                for (var pcnt = 0; pcnt < segments.Count; pcnt++)
                {
                    if (segments[pcnt][0].AreEqual(1.0))
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
                for (var pcnt = 0; pcnt < segments.Count; pcnt++)
                {
                    if (segments[pcnt][0].AreEqual(1.0))
                    {
                        sb.Append("L ").Append(RoundToDec(segments[pcnt][3] * scale, roundcoords)).Append(" ")
                        .Append(RoundToDec(segments[pcnt][4] * scale, roundcoords)).Append(" ");
                    }
                    else
                    {
                        sb.Append("Q ").Append(RoundToDec(segments[pcnt][3] * scale, roundcoords)).Append(" ")
                        .Append(RoundToDec(segments[pcnt][4] * scale, roundcoords)).Append(" ")
                        .Append(RoundToDec(segments[pcnt][5] * scale, roundcoords)).Append(" ")
                        .Append(RoundToDec(segments[pcnt][6] * scale, roundcoords)).Append(" ");
                    }
                }
            }// End of roundcoords check

            sb.Append("Z\" />");

            // Rendering control points
            for (var pcnt = 0; pcnt < segments.Count; pcnt++)
            {
                if ((lcpr > 0) && segments[pcnt][0].AreEqual(1.0))
                {
                    sb.Append("<circle cx=\"").Append(segments[pcnt][3] * scale).Append("\" cy=\"").Append(segments[pcnt][4] * scale).Append("\" r=\"").Append(lcpr).Append("\" fill=\"white\" stroke-width=\"").Append(lcpr * 0.2).Append("\" stroke=\"black\" />");
                }
                if ((qcpr > 0) && segments[pcnt][0].AreEqual(2.0))
                {
                    sb.Append("<circle cx=\"").Append(segments[pcnt][3] * scale).Append("\" cy=\"").Append(segments[pcnt][4] * scale).Append("\" r=\"").Append(qcpr).Append("\" fill=\"cyan\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"black\" />");
                    sb.Append("<circle cx=\"").Append(segments[pcnt][5] * scale).Append("\" cy=\"").Append(segments[pcnt][6] * scale).Append("\" r=\"").Append(qcpr).Append("\" fill=\"white\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"black\" />");
                    sb.Append("<line x1=\"").Append(segments[pcnt][1] * scale).Append("\" y1=\"").Append(segments[pcnt][2] * scale).Append("\" x2=\"").Append(segments[pcnt][3] * scale).Append("\" y2=\"").Append(segments[pcnt][4] * scale).Append("\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"cyan\" />");
                    sb.Append("<line x1=\"").Append(segments[pcnt][3] * scale).Append("\" y1=\"").Append(segments[pcnt][4] * scale).Append("\" x2=\"").Append(segments[pcnt][5] * scale).Append("\" y2=\"").Append(segments[pcnt][6] * scale).Append("\" stroke-width=\"").Append(qcpr * 0.2).Append("\" stroke=\"cyan\" />");
                }// End of quadratic control points
            }
        }

        // Converting tracedata to an SVG string, paths are drawn according to a Z-index
        // the optional lcpr and qcpr are linear and quadratic control point radiuses
        private static string GetSvgString(IndexedImage ii, Options options)
        {
            //options = checkoptions(options);
            // SVG start
            int w = (int)(ii.ImageWidth * options.SvgRendering.Scale), h = (int)(ii.ImageHeight * options.SvgRendering.Scale);
            var viewboxorviewport = options.SvgRendering.Viewbox.IsNotZero() ? "viewBox=\"0 0 " + w + " " + h + "\" " : "width=\"" + w + "\" height=\"" + h + "\" ";
            var svgstr = new StringBuilder("<svg " + viewboxorviewport + "version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" ");
            if (options.SvgRendering.Desc.IsNotZero()) { svgstr.Append("desc=\"Created with ImageTracer.java version " + VersionNumber + "\" "); }
            svgstr.Append(">");

            // creating Z-index
            var zindex = new SortedDictionary<double, int[]>(); //TreeMap<Double, Integer[]> zindex = new TreeMap<Double, Integer[]>();
            // Layer loop
            for (var k = 0; k < ii.Layers.Count; k++)
            {
                // Path loop
                for (var pcnt = 0; pcnt < ii.Layers[k].Count; pcnt++)
                {
                    // Label (Z-index key) is the startpoint of the path, linearized
                    var label = ii.Layers[k][pcnt][0][2] * w + ii.Layers[k][pcnt][0][1];
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
            foreach(var entry in zindex)
            {
                var thisdesc = "";
                if (options.SvgRendering.Desc.IsNotZero()) { thisdesc = "desc=\"l " + entry.Value[0] + " p " + entry.Value[1] + "\" "; } else { thisdesc = ""; }
                SvgPathString(svgstr,
                        thisdesc,
                        ii.Layers[entry.Value[0]][entry.Value[1]],
                        ToSvgColorString(ii.Palette[entry.Value[0]]),
                        options);
            }

            // SVG End
            svgstr.Append("</svg>");

            return svgstr.ToString();

        }

        private static string ToSvgColorString(byte[] c)
        {
            const int shift = 0; // MJY: Try removing all the + 128 on the values. Might fix issues.
            return "fill=\"rgb(" + (c[0] + shift) + "," + (c[1] + shift) + "," + (c[2] + shift) + ")\" stroke=\"rgb(" + (c[0] + shift) + "," + (c[1] + shift) + "," + (c[2] + shift) + ")\" stroke-width=\"1\" opacity=\"" + (c[3] + shift) / 255.0 + "\" ";
        }

        // Gaussian kernels for blur
        private static readonly double[][] Gks = 
        {
            new []{0.27901, 0.44198, 0.27901},
            new []{0.135336, 0.228569, 0.272192, 0.228569, 0.135336},
            new []{0.086776, 0.136394, 0.178908, 0.195843, 0.178908, 0.136394, 0.086776},
            new []{0.063327, 0.093095, 0.122589, 0.144599, 0.152781, 0.144599, 0.122589, 0.093095, 0.063327},
            new []{0.049692, 0.069304, 0.089767, 0.107988, 0.120651, 0.125194, 0.120651, 0.107988, 0.089767, 0.069304, 0.049692}
        };

        // Selective Gaussian blur for preprocessing
        private static ImageData Blur(ImageData imgd, int rad, double del)
        {
            int i, j, k;
            int idx;
            double racc, gacc, bacc, aacc, wacc;
            var imgd2 = new ImageData(imgd.Width, imgd.Height, new byte[imgd.Width * imgd.Height * 4]);

            // radius and delta limits, this kernel
            var radius = rad; if (radius < 1) { return imgd; }
            if (radius > 5) { radius = 5; }
            var delta = (int)Math.Abs(del); if (delta > 1024) { delta = 1024; }
            var thisgk = Gks[radius - 1];

            // loop through all pixels, horizontal blur
            for (j = 0; j < imgd.Height; j++)
            {
                for (i = 0; i < imgd.Width; i++)
                {
                    racc = 0; gacc = 0; bacc = 0; aacc = 0; wacc = 0;
                    // gauss kernel loop
                    for (k = -radius; k < radius + 1; k++)
                    {
                        // add weighted color values
                        if ((i + k > 0) && (i + k < imgd.Width))
                        {
                            idx = (j * imgd.Width + i + k) * 4;
                            racc += imgd.Data[idx] * thisgk[k + radius];
                            gacc += imgd.Data[idx + 1] * thisgk[k + radius];
                            bacc += imgd.Data[idx + 2] * thisgk[k + radius];
                            aacc += imgd.Data[idx + 3] * thisgk[k + radius];
                            wacc += thisgk[k + radius];
                        }
                    }
                    // The new pixel
                    idx = (j * imgd.Width + i) * 4;
                    imgd2.Data[idx] = (byte)Math.Floor(racc / wacc);
                    imgd2.Data[idx + 1] = (byte)Math.Floor(gacc / wacc);
                    imgd2.Data[idx + 2] = (byte)Math.Floor(bacc / wacc);
                    imgd2.Data[idx + 3] = (byte)Math.Floor(aacc / wacc);

                }// End of width loop
            }// End of horizontal blur

            // copying the half blurred imgd2
            var himgd = imgd2.Data.Clone() as byte[];

            // loop through all pixels, vertical blur
            for (j = 0; j < imgd.Height; j++)
            {
                for (i = 0; i < imgd.Width; i++)
                {
                    racc = 0; gacc = 0; bacc = 0; aacc = 0; wacc = 0;
                    // gauss kernel loop
                    for (k = -radius; k < radius + 1; k++)
                    {
                        // add weighted color values
                        if ((j + k > 0) && (j + k < imgd.Height))
                        {
                            idx = ((j + k) * imgd.Width + i) * 4;
                            racc += himgd[idx] * thisgk[k + radius];
                            gacc += himgd[idx + 1] * thisgk[k + radius];
                            bacc += himgd[idx + 2] * thisgk[k + radius];
                            aacc += himgd[idx + 3] * thisgk[k + radius];
                            wacc += thisgk[k + radius];
                        }
                    }
                    // The new pixel
                    idx = (j * imgd.Width + i) * 4;
                    imgd2.Data[idx] = (byte)Math.Floor(racc / wacc);
                    imgd2.Data[idx + 1] = (byte)Math.Floor(gacc / wacc);
                    imgd2.Data[idx + 2] = (byte)Math.Floor(bacc / wacc);
                    imgd2.Data[idx + 3] = (byte)Math.Floor(aacc / wacc);
                }// End of width loop
            }// End of vertical blur

            // Selective blur: loop through all pixels
            for (j = 0; j < imgd.Height; j++)
            {
                for (i = 0; i < imgd.Width; i++)
                {
                    idx = (j * imgd.Width + i) * 4;
                    // d is the difference between the blurred and the original pixel
                    var d = Math.Abs(imgd2.Data[idx] - imgd.Data[idx]) + Math.Abs(imgd2.Data[idx + 1] - imgd.Data[idx + 1]) +
                            Math.Abs(imgd2.Data[idx + 2] - imgd.Data[idx + 2]) + Math.Abs(imgd2.Data[idx + 3] - imgd.Data[idx + 3]);
                    // selective blur: if d>delta, put the original pixel back
                    if (d > delta)
                    {
                        imgd2.Data[idx] = imgd.Data[idx];
                        imgd2.Data[idx + 1] = imgd.Data[idx + 1];
                        imgd2.Data[idx + 2] = imgd.Data[idx + 2];
                        imgd2.Data[idx + 3] = imgd.Data[idx + 3];
                    }
                }
            }// End of Selective blur
            return imgd2;
        }
    }
}
