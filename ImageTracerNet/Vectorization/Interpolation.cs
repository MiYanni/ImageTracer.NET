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
            var next = new InterpolationPoint(pp2, pp3);

            // line segment direction to the next point
            return new InterpolationPoint(pp1, pp2, next);
        }

        // 4. interpolating between path points for nodes with 8 directions ( East, SouthEast, S, SW, W, NW, N, NE )
        public static IEnumerable<IEnumerable<InterpolationPoint>> Convert(IEnumerable<IReadOnlyList<PathPoint>> paths)
        {
            // TODO: It was less efficient to parallelize these calls. Investigate later.
            return paths.Select(path => path.Select((p, i) => CreatePoint(path, i)));
        }
    }
}
