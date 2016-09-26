using System;
using System.Collections.Generic;
using ImageTracerNet.Vectorization.Points;
using LinePointCalculation = System.Func<double, double, double, double>;

namespace ImageTracerNet.Vectorization.Segments
{
    internal class LineSegment : Segment
    {
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
        public static Segment Fit(IReadOnlyList<InterpolationPoint> path, double threshold, SequenceIndices sequence, int sequenceLength, out int errorIndex)
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

            return isLine ? new LineSegment { Start = startPoint, End = endPoint } : null;
        }
    }
}
