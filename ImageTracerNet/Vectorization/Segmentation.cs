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
        public static IEnumerable<Segment> Fit(IReadOnlyList<InterpolationPoint> path, SequenceIndices sequence, Tracing tracing, SvgRendering rendering)
        {
            var pathLength = path.Count;
            // return if invalid sequence.End
            // TODO: When would this ever happen?
            if ((sequence.End > pathLength) || (sequence.End < 0))
            {
                yield break;
            }

            // TODO: This is actually the number of line segments in the sequence. Not the number of points in the sequence.
            var sequenceLength = sequence.End - sequence.Start;
            sequenceLength += sequenceLength < 0 ? pathLength : 0;

            int errorIndex;
            var lineResult = LineSegment.Fit(path, tracing.LTres, sequence, sequenceLength, out errorIndex, rendering);
            if (lineResult != null)
            {
                yield return lineResult;
                yield break;
            }

            // 5.3. If the straight line fails (an error>ltreshold), find the point with the biggest error
            var fitIndex = errorIndex;
            var splineResult = SplineSegment.Fit(path, tracing.QTres, sequence, sequenceLength, ref errorIndex, rendering);
            if (splineResult != null)
            {
                yield return splineResult;
                yield break;
            }

            // 5.5. If the spline fails (an error>qtreshold), find the point with the biggest error,
            var splitIndex = (fitIndex + errorIndex) / 2;
            // 5.6. Split sequence and recursively apply 5.2. - 5.6. to startpoint-splitpoint and splitpoint-endpoint sequences
            foreach (var segmentPart in Fit(path, new SequenceIndices { Start = sequence.Start, End = splitIndex }, tracing, rendering))
            {
                yield return segmentPart;
            }
            foreach (var segmentPart in Fit(path, new SequenceIndices { Start = splitIndex, End = sequence.End }, tracing, rendering))
            {
                yield return segmentPart;
            }
        }
    }
}
