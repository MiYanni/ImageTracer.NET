using System.Collections.Generic;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Vectorization.Points;
using ImageTracerNet.Vectorization.Segments;

namespace ImageTracerNet.Vectorization
{
    internal static class Segmentation
    {
        // 5.2. - 5.6. recursively fitting a straight or quadratic line segment on this sequence of path nodes,
        // called from tracepath()
        // Returns a segment (a list of those doubles is a segment).
        public static IEnumerable<Segment> Fit(IReadOnlyList<InterpolationPoint> path, Tracing tracingOptions, SequenceIndices sequence)
        {
            var pathLength = path.Count;
            // return if invalid sequence.End
            // TODO: When would this ever happen?
            if ((sequence.End > pathLength) || (sequence.End < 0))
            {
                yield break;
            }

            var sequenceLength = sequence.End - sequence.Start;
            sequenceLength += sequenceLength < 0 ? pathLength : 0;

            int errorIndex;
            var lineResult = LineSegment.Fit(path, tracingOptions.LTres, sequence, sequenceLength, out errorIndex);
            if (lineResult != null)
            {
                yield return lineResult;
                yield break;
            }

            // 5.3. If the straight line fails (an error>ltreshold), find the point with the biggest error
            var fitIndex = errorIndex;
            var splineResult = SplineSegment.Fit(path, tracingOptions.QTres, sequence, sequenceLength, ref errorIndex);
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
