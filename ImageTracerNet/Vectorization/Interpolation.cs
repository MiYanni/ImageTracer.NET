using System;
using System.Collections.Generic;
using ImageTracerNet.Vectorization.Points;

namespace ImageTracerNet.Vectorization
{
    internal static class Interpolation
    {
        private static InterpolationPoint CreatePoint(IReadOnlyList<PathPoint> path, int index)
        {
            var pp1 = path[index];

            // interpolate between two path points
            var pp2 = path[(index + 1) % path.Count];
            var pp3 = path[(index + 2) % path.Count];
            var other = new InterpolationPoint(pp2, pp3);

            // line segment direction to the next point
            return new InterpolationPoint(pp1, pp2, other);
        }

        // 4. interpolating between path points for nodes with 8 directions ( East, SouthEast, S, SW, W, NW, N, NE )
        public static List<List<InterpolationPoint>> Convert(IEnumerable<IReadOnlyList<PathPoint>> paths)
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
                    //var pp1 = path[pointIndex];
                    //// interpolate between two path points
                    //var pp2 = path[(pointIndex + 1) % pathLength];
                    //var pp3 = path[(pointIndex + 2) % pathLength];

                    //var thisPoint = new InterpolationPoint
                    //{
                    //    X = (pp1.X + pp2.X) / 2.0,
                    //    Y = (pp1.Y + pp2.Y) / 2.0
                    //};
                    thisInp.Add(CreatePoint(path, pointIndex));

                    //var nextPoint = new InterpolationPoint
                    //{
                    //    X = (pp2.X + pp3.X) / 2.0,
                    //    Y = (pp2.Y + pp3.Y) / 2.0
                    //};

                    //// line segment direction to the next point
                    //var pointComparison = new Tuple<int, int>(thisPoint.X.ToDirectionKey(nextPoint.X), thisPoint.Y.ToDirectionKey(nextPoint.Y));
                    //thisPoint.Direction = Directions[pointComparison];
                }
            }

            return ins;
        }
    }
}
