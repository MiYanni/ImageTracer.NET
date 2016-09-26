using System;
using System.Linq;
using ImageTracerNet.Extensions;
using ImageTracerNet.Vectorization.Points;

namespace ImageTracerNet.Vectorization.Segments
{
    internal abstract class Segment
    {
        public Point<double> Start { get; set; }
        public Point<double> End { get; set; }

        protected static bool Fit(Func<int, Point<double>> interpPointMethod, Func<int, Point<double>> calcPointMethod, double threshold, int initialPathIndex, 
            Func<int, bool> pathCondition, Func<int, int> pathStep, ref int errorIndex)
        {
            var pathIndices = EnumerableExtensions.ForAsRange(initialPathIndex, pathCondition, pathStep);
            // TODO: Parallelization is very slow here.
            var distancesAndIndices = pathIndices.Select(i =>
            {
                var interpolatedPoint = interpPointMethod(i);
                var calculatedPoint = calcPointMethod(i);
                return new { Index = i, Distance = Math.Pow(interpolatedPoint.X - calculatedPoint.X, 2) + Math.Pow(interpolatedPoint.Y - calculatedPoint.Y, 2) };
            }).ToList();

            // If this is true, the segment is not this segment type.
            if (distancesAndIndices.Any(di => di.Distance > threshold))
            {
                // Finds the point index with the biggest error.
                errorIndex = distancesAndIndices.Aggregate(new { Index = errorIndex, Distance = (double)0 },
                    (errorDi, nextDi) => nextDi.Distance > errorDi.Distance ? nextDi : errorDi).Index;
                return false;
            }

            return true;
        }
    }
}
