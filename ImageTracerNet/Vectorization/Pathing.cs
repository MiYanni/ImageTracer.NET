using System;
using System.Collections.Generic;
using System.Linq;
using ImageTracerNet.Vectorization.Points;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Vectorization.Segments;
using DirectedEdgeList = System.Collections.Generic.List<ImageTracerNet.Vectorization.DirectedEdge>;
using static ImageTracerNet.Vectorization.EdgeNode;
using static ImageTracerNet.Vectorization.WalkDirection;

namespace ImageTracerNet.Vectorization
{
    internal static class Pathing
    {
        private static readonly EdgeNode[] InitializeUp = { DDDL, LLLD };
        private static readonly EdgeNode[] InitializeDown = { DLDD, DLDL, LDLD, DLLD, LDLL };
        private static readonly Func<EdgeNode, WalkDirection> InitializeDirection = n => 
            InitializeUp.Contains(n) ? Up : 
            (InitializeDown.Contains(n) ? Down : Right);

        private static readonly EdgeNode[] HoleNodes = { LLDL, LLLD, LDLL, DLLL };
        private const EdgeNode NonHoleNode = DDDL;

        private static readonly Dictionary<EdgeNode, EdgeNode[]> NonZeroNodes = new Dictionary<EdgeNode, EdgeNode[]>
        {
                          // 0 > ; 1 ^ ; 2 < ; 3 v
            [LDDL] = new[] { LDLL, LDLL, LLDL, LLDL },
            [DLLD] = new[] { LLLD, DLLL, DLLL, LLLD }
        };

        private static readonly DirectedEdgeList MinusOneYs = new DirectedEdgeList
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
        private static readonly DirectedEdgeList PlusOneYs = new DirectedEdgeList
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

        private static readonly DirectedEdgeList MinusOneXs = new DirectedEdgeList
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
        private static readonly DirectedEdgeList PlusOneXs = new DirectedEdgeList
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

        private static readonly DirectedEdgeList AcceptedPaths = MinusOneYs.Concat(MinusOneXs.Concat(PlusOneYs.Concat(PlusOneXs))).ToList();

        private static readonly DirectedEdgeList RightAssignments = new DirectedEdgeList
        {
            {DLDD, Down},
            {DDDL, Up},
            {LDDL, Down},
            {DLLD, Up},
            {LLLD, Up},
            {LDLL, Down}
        };
        private static readonly DirectedEdgeList UpAssignments = new DirectedEdgeList
        {
            {LDDD, Right},
            {DLDD, Left},
            {LDDL, Left},
            {DLLD, Right},
            {LDLL, Left},
            {DLLL, Right}
        };
        private static readonly DirectedEdgeList LeftAssignments = new DirectedEdgeList
        {
            {LDDD, Down},
            {LDDL, Up},
            {LLDL, Up},
            {DDLD, Up},
            {DLLD, Down},
            {DLLL, Down}
        };
        private static readonly DirectedEdgeList DownAssignments = new DirectedEdgeList
        {
            {DDDL, Left},
            {LDDL, Right},
            {LLDL, Right},
            {DDLD, Right},
            {DLLD, Left},
            {LLLD, Left}
        };

        private static readonly Func<DirectedEdge, int> CalcNextX = ndp => MinusOneXs.Contains(ndp) ? -1 : (PlusOneXs.Contains(ndp) ? 1 : 0);
        private static readonly Func<DirectedEdge, int> CalcNextY = ndp => MinusOneYs.Contains(ndp) ? -1 : (PlusOneYs.Contains(ndp) ? 1 : 0);
        private static readonly Func<DirectedEdge, WalkDirection> CalcDirection = ndp => 
            RightAssignments.Contains(ndp) ? Right :
            (UpAssignments.Contains(ndp) ? Up :
            (LeftAssignments.Contains(ndp) ? Left :
            (DownAssignments.Contains(ndp) ? Down : ndp.Direction)));
        private static readonly Func<DirectedEdge, EdgeNode> CalcNode = ndp => NonZeroNodes.ContainsKey(ndp.Node) ? NonZeroNodes[ndp.Node][(int)ndp.Direction] : DDDD;

        private static IEnumerable<PathPoint> CreatePath(IReadOnlyList<EdgeNode[]> nodes, int x, int y, WalkDirection dir, bool holePath, int pathOmit)
        {
            //var initialPoint = new PathPoint {X = px - 1, Y = py - 1, EdgeNode = nodes[py][px]};
            var path = new List<PathPoint>();
            bool isIncorrectPath;
            bool canClosePath;

            // Path points loop
            do
            {
                var directedEdge = new DirectedEdge {Node = nodes[y][x], Direction = dir};
                // New path point
                path.Add(new PathPoint {X = x - 1, Y = y - 1, EdgeNode = directedEdge.Node});

                nodes[y][x] = CalcNode(directedEdge);

                y += CalcNextY(directedEdge);
                x += CalcNextX(directedEdge);
                dir = CalcDirection(directedEdge);

                // Close path
                isIncorrectPath = !AcceptedPaths.Contains(directedEdge);
                canClosePath = (x - 1 == path[0].X) && (y - 1 == path[0].Y);
            } while (!(isIncorrectPath || canClosePath));

            // Discarding 'hole' type paths and paths shorter than pathOmit
            var isHoleOrShortPath = holePath || (path.Count < pathOmit);
            return isIncorrectPath || isHoleOrShortPath ? null : path;
        }

        // 3. Walking through an edge node array, discarding edge node types 0 and 15 and creating paths from the rest.
        // Walk directions (dir): 0 > ; 1 ^ ; 2 < ; 3 v

        // Edge node types ( ▓:light or 1; ░:dark or 0 )

        // ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓
        // ░░  ░░  ░░  ░░  ░▓  ░▓  ░▓  ░▓  ▓░  ▓░  ▓░  ▓░  ▓▓  ▓▓  ▓▓  ▓▓
        // 0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
        public static IEnumerable<IEnumerable<PathPoint>> Scan(EdgeNode[][] nodes, int pathOmit)
        {
            var width = nodes[0].Length;
            var height = nodes.Length;
            var holePath = false;

            //var filteredNodes = nodes.Select(r => r.Where(c => c != DDDD && c != LLLL));

            for (var row = 0; row < height; row++)
            {
                for (var column = 0; column < width; column++)
                {
                    var node = nodes[row][column];

                    // Follow path
                    if ((node == DDDD) || (node == LLLL)) continue;

                    // fill paths will be drawn, but hole paths are also required to remove unnecessary edge nodes
                    var dir = InitializeDirection(node);
                    holePath = HoleNodes.Contains(node) || (NonHoleNode != node && holePath);

                    // The values in nodes are updated in CreatePath.
                    var path = CreatePath(nodes, column, row, dir, holePath, pathOmit);
                    if (path != null)
                    {
                        yield return path;
                    }
                }
            }
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
