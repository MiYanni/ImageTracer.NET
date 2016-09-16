using System;
using System.Collections.Generic;
using System.Linq;
using ImageTracerNet.Extensions;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Vectorization.Points;
using LinePointCalculation = System.Func<double, double, double, double>;
using SplinePointCalculation = System.Func<double, double, double, double, double>;

namespace ImageTracerNet.Vectorization
{
    internal static class Segmentation
    {
        private static bool Fit(Func<int, Point<double>> interpPointMethod, Func<int, Point<double>> calcPointMethod, double threshold, int initialPathIndex, Func<int, bool> pathCondition, Func<int, int> pathStep, ref int errorIndex)
        {
            var pathIndices = EnumerableExtensions.ForAsRange(initialPathIndex, pathCondition, pathStep);
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

        private static Point<double> CreateLinePoint(double pseudoIndex, Point<double> first, Point<double> second, bool isPartialPoint = false)
        {
            // Static Point Calculations
            LinePointCalculation partialPointCalc = (i, start, end) => (end - start) / i;
            LinePointCalculation endPointCalc = (i, start, partial) => start + partial * i;

            Func<double, Point<double>, Point<double>, LinePointCalculation, Point<double>> createPoint =
                (i, p1, p2, func) => new Point<double>
                {
                    X = func(i, p1.X, p2.X),
                    Y = func(i, p1.Y, p2.Y)
                };

            return createPoint(pseudoIndex, first, second, isPartialPoint ? partialPointCalc : endPointCalc);
        }

        // 5.2. Fit a straight line on the sequence
        private static double[] FitLine(IReadOnlyList<InterpolationPoint> path, double threshold, SequenceIndices sequence, int sequenceLength, out int errorIndex)
        {
            var startPoint = path[sequence.Start];
            var endPoint = path[sequence.End];
            var partialPoint = CreateLinePoint(sequenceLength, startPoint, endPoint, true);

            var pathLength = path.Count;
            Func<int, double> pseudoIndexCalc = i =>
            {
                // I don't know what 'pl' as a variable name means. Is it related to path length?
                var pl = i - sequence.Start;
                pl += pl < 0 ? pathLength : 0;
                return pl;
            };

            errorIndex = sequence.Start;
            var isLine = Fit(i => path[i], i => CreateLinePoint(pseudoIndexCalc(i), startPoint, partialPoint), threshold, 
                (sequence.Start + 1) % pathLength, i => i != sequence.End, i => (i + 1) % pathLength, ref errorIndex);

            return isLine ? new[]
            {
                1.0,
                startPoint.X,
                startPoint.Y,
                endPoint.X,
                endPoint.Y,
                0.0,
                0.0
            } : null;
        }

        private static Point<double> CreateSplinePoint(double pseudoIndex, Point<double> first, Point<double> second, Point<double> third, bool isMidPoint = false)
        {
            // Static Term Calculations
            Func<double, double> t1Calc = t => (1.0 - t) * (1.0 - t);
            Func<double, double> t2Calc = t => 2.0 * (1.0 - t) * t;
            Func<double, double> t3Calc = t => Math.Pow(t, 2);

            // Static Point Calculations
            SplinePointCalculation midPointCalc =
                (i, start, end, fit) => (t1Calc(i) * start + t3Calc(i) * end - fit) / -t2Calc(i);

            SplinePointCalculation finalPointCalc =
                (i, start, mid, end) => t1Calc(i) * start + t2Calc(i) * mid + t3Calc(i) * end;

            Func<double, Point<double>, Point<double>, Point<double>, SplinePointCalculation, Point<double>> createPoint =
                (i, p1, p2, p3, func) => new Point<double>
                {
                    X = func(i, p1.X, p2.X, p3.X),
                    Y = func(i, p1.Y, p2.Y, p3.Y)
                };

            return createPoint(pseudoIndex, first, second, third, isMidPoint ? midPointCalc : finalPointCalc);
        }

        // 5.4. Fit a quadratic spline through this point, measure errors on every point in the sequence
        // helpers and projecting to get control point
        private static double[] FitSpline(IReadOnlyList<InterpolationPoint> path, double threshold, SequenceIndices sequence, int sequenceLength, ref int errorIndex)
        {
            var startPoint = path[sequence.Start];
            var endPoint = path[sequence.End];
            var fitPoint = path[errorIndex];

            Func<int, double> pseudoIndexCalc = i => (i - sequence.Start) / (double)sequenceLength;
            var midPoint = CreateSplinePoint(pseudoIndexCalc(errorIndex), startPoint, endPoint, fitPoint, true);

            // Check every point
            var isSpline = Fit(i => path[i], i => CreateSplinePoint(pseudoIndexCalc(i), startPoint, midPoint, endPoint), threshold,
                sequence.Start + 1, i => i != sequence.End, i => (i + 1) % path.Count, ref errorIndex);

            return isSpline ? new[]
            {
                2.0,
                startPoint.X,
                startPoint.Y,
                midPoint.X,
                midPoint.Y,
                endPoint.X,
                endPoint.Y
            } : null;
        }

        // 5.2. - 5.6. recursively fitting a straight or quadratic line segment on this sequence of path nodes,
        // called from tracepath()
        // Returns a segment (a list of those doubles is a segment).
        public static IEnumerable<double[]> Fit(List<InterpolationPoint> path, Tracing tracingOptions, SequenceIndices sequence)
        {
            var pathLength = path.Count;
            // return if invalid sequence.End
            if ((sequence.End > pathLength) || (sequence.End < 0))
            {
                yield break;
            }

            var sequenceLength = sequence.End - sequence.Start;
            sequenceLength += sequenceLength < 0 ? pathLength : 0;

            int errorIndex;
            var lineResult = FitLine(path, tracingOptions.LTres, sequence, sequenceLength, out errorIndex);
            if (lineResult != null)
            {
                yield return lineResult;
                yield break;
            }

            // 5.3. If the straight line fails (an error>ltreshold), find the point with the biggest error
            var fitIndex = errorIndex;
            var splineResult = FitSpline(path, tracingOptions.QTres, sequence, sequenceLength, ref errorIndex);
            if (splineResult != null)
            {
                yield return splineResult;
                yield break;
            }

            // 5.5. If the spline fails (an error>qtreshold), find the point with the biggest error,
            var splitIndex = (fitIndex + errorIndex) / 2;
            // 5.6. Split sequence and recursively apply 5.2. - 5.6. to startpoint-splitpoint and splitpoint-endpoint sequences
            foreach (var segmentPart in Fit(path, tracingOptions, new SequenceIndices { Start = sequence.Start, End = splitIndex }))
            {
                yield return segmentPart;
            }
            foreach (var segmentPart in Fit(path, tracingOptions, new SequenceIndices { Start = splitIndex, End = sequence.End }))
            {
                yield return segmentPart;
            }
        }
    }
}
