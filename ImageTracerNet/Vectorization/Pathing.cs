using System;
using System.Collections.Generic;
using System.Linq;
using ImageTracerNet.Vectorization.Points;
using NodeDirList = System.Collections.Generic.List<System.Tuple<ImageTracerNet.Vectorization.EdgeNode, ImageTracerNet.Vectorization.WalkDirection>>;
using ImageTracerNet.Extensions;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Vectorization.Segments;
using static ImageTracerNet.Vectorization.EdgeNode;
using static ImageTracerNet.Vectorization.WalkDirection;

namespace ImageTracerNet.Vectorization
{
    internal static class Pathing
    {
        private static readonly EdgeNode[] InitialOneNodes = { DDDL, LLLD };
        private static readonly EdgeNode[] InitialThreeNodes = { DLDD, DLDL, LDLD, DLLD, LDLL };

        private static readonly EdgeNode[] HoleNodes = { LLDL, LLLD, LDLL, DLLL };
        private const EdgeNode NonHoleNode = DDDL;

        private static readonly Dictionary<EdgeNode, EdgeNode[]> NonZeroNodes = new Dictionary<EdgeNode, EdgeNode[]>
        {
                          // 0 > ; 1 ^ ; 2 < ; 3 v
            [LDDL] = new[] { LDLL, LDLL, LLDL, LLDL },
            [DLLD] = new[] { LLLD, DLLL, DLLL, LLLD }
        };

        private static readonly NodeDirList MinusOneYs = new NodeDirList
        {
            {LDDD, Right},
            {DLDD, Left},
            {LDDL, Left},
            {DLDL, Up},
            {LDLD, Up},
            {DLLD, Right},
            {LDLL, Left},
            {DLLL, Right}
        };
        private static readonly NodeDirList PlusOneYs = new NodeDirList
        {
            {DDDL, Left},
            {LDDL, Right},
            {DLDL, Down},
            {LLDL, Right},
            {DDLD, Right},
            {LDLD, Down},
            {DLLD, Left},
            {LLLD, Left}
        };

        private static readonly NodeDirList MinusOneXs = new NodeDirList
        {
            {LDDD, Down},
            {LLDD, Left},
            {LDDL, Up},
            {LLDL, Up},
            {DDLD, Up},
            {DLLD, Down},
            {DDLL, Left},
            {DLLL, Down}
        };
        private static readonly NodeDirList PlusOneXs = new NodeDirList
        {
            {DLDD, Down},
            {LLDD, Right},
            {DDDL, Up},
            {LDDL, Down},
            {DLLD, Up},
            {LLLD, Up},
            {DDLL, Right},
            {LDLL, Down}
        };

        private static readonly NodeDirList RightAssignments = new NodeDirList
        {
            {DLDD, Down},
            {DDDL, Up},
            {LDDL, Down},
            {DLLD, Up},
            {LLLD, Up},
            {LDLL, Down}
        };
        private static readonly NodeDirList UpAssignments = new NodeDirList
        {
            {LDDD, Right},
            {DLDD, Left},
            {LDDL, Left},
            {DLLD, Right},
            {LDLL, Left},
            {DLLL, Right}
        };
        private static readonly NodeDirList LeftAssignments = new NodeDirList
        {
            {LDDD, Down},
            {LDDL, Up},
            {LLDL, Up},
            {DDLD, Up},
            {DLLD, Down},
            {DLLL, Down}
        };
        private static readonly NodeDirList DownAssignments = new NodeDirList
        {
            {DDDL, Left},
            {LDDL, Right},
            {LLDL, Right},
            {DDLD, Right},
            {DLLD, Left},
            {LLLD, Left}
        };

        // 3. Walking through an edge node array, discarding edge node types 0 and 15 and creating paths from the rest.
        // Walk directions (dir): 0 > ; 1 ^ ; 2 < ; 3 v

        // Edge node types ( ▓:light or 1; ░:dark or 0 )

        // ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓
        // ░░  ░░  ░░  ░░  ░▓  ░▓  ░▓  ░▓  ▓░  ▓░  ▓░  ▓░  ▓▓  ▓▓  ▓▓  ▓▓
        // 0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
        public static List<List<PathPoint>> Scan(EdgeNode[][] arr, int pathOmit)
        {
            var paths = new List<List<PathPoint>>();
            var w = arr[0].Length;
            var h = arr.Length;
            var holePath = false;

            for (var j = 0; j < h; j++)
            {
                for (var i = 0; i < w; i++)
                {
                    var initialNodeValue = arr[j][i];

                    // Follow path
                    if ((initialNodeValue == DDDD) || (initialNodeValue == LLLL)) continue;

                    // fill paths will be drawn, but hole paths are also required to remove unnecessary edge nodes
                    var dir = InitialOneNodes.Contains(initialNodeValue) ? Up : (InitialThreeNodes.Contains(initialNodeValue) ? Down : Right);
                    holePath = HoleNodes.Contains(initialNodeValue) || (NonHoleNode != initialNodeValue && holePath);

                    // Init
                    var px = i;
                    var py = j;
                    var thisPath = new List<PathPoint>();
                    paths.Add(thisPath);
                    var pathFinished = false;

                    // Path points loop
                    while (!pathFinished)
                    {
                        var nodeValue = arr[py][px];

                        // New path point
                        thisPath.Add(new PathPoint { X = px - 1, Y = py - 1, EdgeNode = nodeValue });

                        // Node types
                        arr[py][px] = NonZeroNodes.ContainsKey(nodeValue) ? NonZeroNodes[nodeValue][(int)dir] : DDDD;

                        var nodeValueDirPair = new Tuple<EdgeNode, WalkDirection>(nodeValue, dir);
                        py += MinusOneYs.Contains(nodeValueDirPair) ? -1 : (PlusOneYs.Contains(nodeValueDirPair) ? 1 : 0);
                        px += MinusOneXs.Contains(nodeValueDirPair) ? -1 : (PlusOneXs.Contains(nodeValueDirPair) ? 1 : 0);
                        dir = RightAssignments.Contains(nodeValueDirPair) ? Right :
                            (UpAssignments.Contains(nodeValueDirPair) ? Up :
                            (LeftAssignments.Contains(nodeValueDirPair) ? Left :
                            (DownAssignments.Contains(nodeValueDirPair) ? Down : dir)));

                        // Close path
                        var allXyPairs = MinusOneYs.Concat(MinusOneXs.Concat(PlusOneYs.Concat(PlusOneXs))).ToList();
                        var isCompletedPath = !allXyPairs.Contains(nodeValueDirPair);
                        var canClosePath = (px - 1 == thisPath[0].X) && (py - 1 == thisPath[0].Y);
                        pathFinished = isCompletedPath || canClosePath;

                        // Discarding 'hole' type paths and paths shorter than pathOmit
                        var isHoleOrShortPath = holePath || (thisPath.Count < pathOmit);
                        if (isCompletedPath || (canClosePath && isHoleOrShortPath))
                        {
                            paths.Remove(thisPath);
                        }
                    }
                }
            }

            return paths;
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

        public static IEnumerable<Segment> Trace(IReadOnlyList<InterpolationPoint> path, Tracing tracingOptions)
        {
            var sequences = Sequencing.Create(path.Select(p => p.Direction).ToList());
            // Fit the sequences into segments, and return them.
            return sequences.Select(s => Segmentation.Fit(path, tracingOptions, s)).SelectMany(s => s);
        }
    }
}
