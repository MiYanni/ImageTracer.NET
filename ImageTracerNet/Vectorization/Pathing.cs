using System;
using System.Collections.Generic;
using System.Linq;
using ImageTracerNet.Vectorization.Points;
using NodeDirList = System.Collections.Generic.List<System.Tuple<ImageTracerNet.Vectorization.EdgeNode, int>>;
using ImageTracerNet.Extensions;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Vectorization.Segments;
using static ImageTracerNet.Vectorization.EdgeNode;

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
            {LDDD, 0},
            {DLDD, 2},
            {LDDL, 2},
            {DLDL, 1},
            {LDLD, 1},
            {DLLD, 0},
            {LDLL, 2},
            {DLLL, 0}
        };
        private static readonly NodeDirList PlusOneYs = new NodeDirList
        {
            {DDDL, 2},
            {LDDL, 0},
            {DLDL, 3},
            {LLDL, 0},
            {DDLD, 0},
            {LDLD, 3},
            {DLLD, 2},
            {LLLD, 2}
        };

        private static readonly NodeDirList MinusOneXs = new NodeDirList
        {
            {LDDD, 3},
            {LLDD, 2},
            {LDDL, 1},
            {LLDL, 1},
            {DDLD, 1},
            {DLLD, 3},
            {DDLL, 2},
            {DLLL, 3}
        };
        private static readonly NodeDirList PlusOneXs = new NodeDirList
        {
            {DLDD, 3},
            {LLDD, 0},
            {DDDL, 1},
            {LDDL, 3},
            {DLLD, 1},
            {LLLD, 1},
            {DDLL, 0},
            {LDLL, 3}
        };

        // 0 >
        private static readonly NodeDirList DirZeroAssignments = new NodeDirList
        {
            {DLDD, 3},
            {DDDL, 1},
            {LDDL, 3},
            {DLLD, 1},
            {LLLD, 1},
            {LDLL, 3}
        };
        // 1 ^
        private static readonly NodeDirList DirOneAssignments = new NodeDirList
        {
            {LDDD, 0},
            {DLDD, 2},
            {LDDL, 2},
            {DLLD, 0},
            {LDLL, 2},
            {DLLL, 0}
        };
        // 2 <
        private static readonly NodeDirList DirTwoAssignments = new NodeDirList
        {
            {LDDD, 3},
            {LDDL, 1},
            {LLDL, 1},
            {DDLD, 1},
            {DLLD, 3},
            {DLLL, 3}
        };
        // 3 v
        private static readonly NodeDirList DirThreeAssignments = new NodeDirList
        {
            {DDDL, 2},
            {LDDL, 0},
            {LLDL, 0},
            {DDLD, 0},
            {DLLD, 2},
            {LLLD, 2}
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
                    var dir = InitialOneNodes.Contains(initialNodeValue) ? 1 : (InitialThreeNodes.Contains(initialNodeValue) ? 3 : 0);
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
                        arr[py][px] = NonZeroNodes.ContainsKey(nodeValue) ? NonZeroNodes[nodeValue][dir] : DDDD;

                        var nodeValueDirPair = new Tuple<EdgeNode, int>(nodeValue, dir);
                        py += MinusOneYs.Contains(nodeValueDirPair) ? -1 : (PlusOneYs.Contains(nodeValueDirPair) ? 1 : 0);
                        px += MinusOneXs.Contains(nodeValueDirPair) ? -1 : (PlusOneXs.Contains(nodeValueDirPair) ? 1 : 0);
                        dir = DirZeroAssignments.Contains(nodeValueDirPair) ? 0 :
                            (DirOneAssignments.Contains(nodeValueDirPair) ? 1 :
                            (DirTwoAssignments.Contains(nodeValueDirPair) ? 2 :
                            (DirThreeAssignments.Contains(nodeValueDirPair) ? 3 : dir)));

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
