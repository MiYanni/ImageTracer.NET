using System;
using System.Collections.Generic;
using System.Linq;
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
        public static IEnumerable<IEnumerable<InterpolationPoint>> Convert(IEnumerable<IReadOnlyList<PathPoint>> paths)
        {
            //var ins = new List<List<InterpolationPoint>>();

            // paths loop
            foreach (var path in paths)
            {
                yield return path.Select((p, i) => CreatePoint(path, i));

                //var thisInp = new List<InterpolationPoint>();
                //ins.Add(thisInp);
                //var pathLength = path.Count;

                // pathpoints loop
                //for (var pointIndex = 0; pointIndex < pathLength; pointIndex++)
                //{
                //    thisInp.Add(CreatePoint(path, pointIndex));
                //}
            }

            //return ins;
        }
    }
}
