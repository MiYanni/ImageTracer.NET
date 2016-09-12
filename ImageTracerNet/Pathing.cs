using System;
using System.Collections.Generic;
using System.Linq;
using ImageTracerNet.Extensions;
using NodeDirList = System.Collections.Generic.List<System.Tuple<int, int>>;

namespace ImageTracerNet
{
    internal static class Pathing
    {
        private static readonly int[] InitialOneNodes = { 4, 11 };
        private static readonly int[] InitialThreeNodes = { 2, 6, 9, 10, 13 };

        private static readonly int[] HoleNodes = { 7, 11, 13, 14 };
        private const int NonHoleNode = 4;

        private static readonly Dictionary<int, int[]> NonZeroNodes = new Dictionary<int, int[]>
        {
            [5] = new[] { 13, 13, 7, 7 },
            [10] = new[] { 11, 14, 14, 11 }
        };

        private static readonly NodeDirList MinusOneYs = new NodeDirList
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
        private static readonly NodeDirList PlusOneYs = new NodeDirList
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

        private static readonly NodeDirList MinusOneXs = new NodeDirList
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
        private static readonly NodeDirList PlusOneXs = new NodeDirList
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

        private static readonly NodeDirList DirZeroAssignments = new NodeDirList
        {
            {2, 3},
            {4, 1},
            {5, 3},
            {10, 1},
            {11, 1},
            {13, 3}
        };
        private static readonly NodeDirList DirOneAssignments = new NodeDirList
        {
            {1, 0},
            {2, 2},
            {5, 2},
            {10, 0},
            {13, 2},
            {14, 0}
        };
        private static readonly NodeDirList DirTwoAssignments = new NodeDirList
        {
            {1, 3},
            {5, 1},
            {7, 1},
            {8, 1},
            {10, 3},
            {14, 3}
        };
        private static readonly NodeDirList DirThreeAssignments = new NodeDirList
        {
            {4, 2},
            {5, 0},
            {7, 0},
            {8, 0},
            {10, 2},
            {11, 2}
        };

        // 3. Walking through an edge node array, discarding edge node types 0 and 15 and creating paths from the rest.
        // Walk directions (dir): 0 > ; 1 ^ ; 2 < ; 3 v

        // Edge node types ( ▓:light or 1; ░:dark or 0 )

        // ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓
        // ░░  ░░  ░░  ░░  ░▓  ░▓  ░▓  ░▓  ▓░  ▓░  ▓░  ▓░  ▓▓  ▓▓  ▓▓  ▓▓
        // 0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
        public static List<List<PathPoint>> Scan(int[][] arr, int pathOmit)
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
                    // MJY: Logically, arr[j][i] cannot equal 0
                    if ((initialNodeValue == 0) || (initialNodeValue == 15)) continue;

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
                        arr[py][px] = NonZeroNodes.ContainsKey(nodeValue) ? NonZeroNodes[nodeValue][dir] : 0;

                        var nodeValueDirPair = new Tuple<int, int>(nodeValue, dir);
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
    }
}
