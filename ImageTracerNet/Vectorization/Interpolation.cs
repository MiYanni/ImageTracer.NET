using System;
using System.Collections.Generic;
using ImageTracerNet.Extensions;
using ImageTracerNet.Vectorization.Points;

namespace ImageTracerNet.Vectorization
{
    internal static class Interpolation
    {
        private static readonly Dictionary<Tuple<int, int>, Heading> Directions = new Dictionary<Tuple<int, int>, Heading>
        {
            {-1, -1, Heading.SouthEast},
            {-1,  1, Heading.NorthEast},
            {-1,  0, Heading.East},
            {1,  -1, Heading.SouthWest},
            {1,   1, Heading.NorthWest},
            {1,   0, Heading.West},
            {0,  -1, Heading.South},
            {0,   1, Heading.North},
            {0,   0, Heading.Center}
        };

        private static int ToDirectionKey(this double value1, double value2)
        {
            //https://msdn.microsoft.com/en-us/library/fyxd1d26(v=vs.110).aspx
            return value1.AreEqual(value2) ? 0 : value1.CompareTo(value2);
        }

        // 4. interpolating between path points for nodes with 8 directions ( East, SouthEast, S, SW, W, NW, N, NE )
        public static List<List<InterpolationPoint>> Convert(IEnumerable<List<PathPoint>> paths)
        {
            var ins = new List<List<InterpolationPoint>>();

            // paths loop
            foreach (var path in paths)
            {
                var thisInp = new List<InterpolationPoint>();
                ins.Add(thisInp);
                var pathLength = path.Count;
                // pathpoints loop
                for (var pointIndex = 0; pointIndex < pathLength; pointIndex++)
                {
                    var pp1 = path[pointIndex];
                    // interpolate between two path points
                    var pp2 = path[(pointIndex + 1) % pathLength];
                    var pp3 = path[(pointIndex + 2) % pathLength];

                    var thisPoint = new InterpolationPoint
                    {
                        X = (pp1.X + pp2.X) / 2.0,
                        Y = (pp1.Y + pp2.Y) / 2.0
                    };
                    thisInp.Add(thisPoint);

                    var nextPoint = new InterpolationPoint
                    {
                        X = (pp2.X + pp3.X) / 2.0,
                        Y = (pp2.Y + pp3.Y) / 2.0
                    };

                    // line segment direction to the next point
                    var pointComparison = new Tuple<int, int>(thisPoint.X.ToDirectionKey(nextPoint.X), thisPoint.Y.ToDirectionKey(nextPoint.Y));
                    thisPoint.Direction = Directions[pointComparison];
                }
            }

            return ins;
        }
    }
}
