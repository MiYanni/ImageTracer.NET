using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageTracerNet.Extensions;
using ImageTracerNet.OptionTypes;

namespace ImageTracerNet
{
    internal static class Segmentation
    {
        private static bool Fit(Func<int, double> distanceFunction, double threshold, int initialPathIndex, Func<int, bool> pathCondition, Func<int, int> pathStep, ref int errorPoint)
        {
            var pathIndices = EnumerableExtensions.ForAsRange(initialPathIndex, pathCondition, pathStep);
            var distancesAndIndices = pathIndices.Select(i => new { Index = i, Distance = distanceFunction(i) }).ToList();

            // If this is true, the segment is not this line type.
            if (distancesAndIndices.Any(di => di.Distance > threshold))
            {
                errorPoint = distancesAndIndices.Aggregate(new { Index = errorPoint, Distance = (double)0 },
                    (errorDi, nextDi) => nextDi.Distance > errorDi.Distance ? nextDi : errorDi).Index;
                return false;
            }

            return true;
        }

        private static double[] FitLine(List<InterpolationPoint> path, Tracing tracingOptions, int seqStart, int seqEnd, int seqLength, out int errorPoint)
        {
            var pathLength = path.Count;
            var vx = (path[seqEnd].X - path[seqStart].X) / seqLength;
            var vy = (path[seqEnd].Y - path[seqStart].Y) / seqLength;
            Func<int, double> distanceFunction = i =>
            {
                var pl = i - seqStart;
                pl += pl < 0 ? pathLength : 0;
                var px = path[seqStart].X + vx*pl;
                var py = path[seqStart].Y + vy*pl;

                return (path[i].X - px)*(path[i].X - px) + (path[i].Y - py)*(path[i].Y - py);
            };

            errorPoint = seqStart;
            var isLine = Fit(distanceFunction, tracingOptions.LTres, (seqStart + 1) % pathLength, i => i != seqEnd, i => (i + 1) % pathLength, ref errorPoint);
            return isLine ? new[]
            {
                1.0,
                path[seqStart].X,
                path[seqStart].Y,
                path[seqEnd].X,
                path[seqEnd].Y,
                0.0,
                0.0
            } : null;
        }

        private static double[] FitSpline(List<InterpolationPoint> path, Tracing tracingOptions, int seqStart, int seqEnd, int seqLength, ref int errorPoint)
        {
            //var pathLength = path.Count;
            //// 5.3. If the straight line fails (an error>ltreshold), find the point with the biggest error
            //var fitpoint = errorPoint;
            //var curvePass = true;
            //double errorVal = 0;

            //// 5.4. Fit a quadratic spline through this point, measure errors on every point in the sequence
            //// helpers and projecting to get control point
            //var t = (fitpoint - seqStart) / (double)seqLength;
            //var t1 = (1.0 - t) * (1.0 - t);
            //var t2 = 2.0 * (1.0 - t) * t;
            //var t3 = t * t;
            //var cpx = (t1 * path[seqStart].X + t3 * path[seqEnd].X - path[fitpoint].X) / -t2;
            //var cpy = (t1 * path[seqStart].Y + t3 * path[seqEnd].Y - path[fitpoint].Y) / -t2;

            //// Check every point
            //var pcnt = seqStart + 1;
            //while (pcnt != seqEnd)
            //{
            //    t = (pcnt - seqStart) / (double)seqLength;
            //    t1 = (1.0 - t) * (1.0 - t);
            //    t2 = 2.0 * (1.0 - t) * t;
            //    t3 = t * t;
            //    var px = t1 * path[seqStart].X + t2 * cpx + t3 * path[seqEnd].X;
            //    var py = t1 * path[seqStart].Y + t2 * cpy + t3 * path[seqEnd].Y;

            //    var dist2 = (path[pcnt].X - px) * (path[pcnt].X - px) + (path[pcnt].Y - py) * (path[pcnt].Y - py);

            //    if (dist2 > tracingOptions.QTres)
            //    {
            //        curvePass = false;
            //    }
            //    if (dist2 > errorVal)
            //    {
            //        errorPoint = pcnt;
            //        errorVal = dist2;
            //    }

            //    pcnt = (pcnt + 1) % pathLength;
            //}

            //// return spline if fits
            //if (curvePass)
            //{
            //    segment.Add(new double[7]);
            //    var thisSegment = segment[segment.Count - 1];
            //    thisSegment[0] = 2.0;
            //    thisSegment[1] = path[seqStart].X;
            //    thisSegment[2] = path[seqStart].Y;
            //    thisSegment[3] = cpx;
            //    thisSegment[4] = cpy;
            //    thisSegment[5] = path[seqEnd].X;
            //    thisSegment[6] = path[seqEnd].Y;
            //    return segment;
            //}
            var pathLength = path.Count;
            var t = (errorPoint - seqStart) / (double)seqLength;
            var t1 = (1.0 - t) * (1.0 - t);
            var t2 = 2.0 * (1.0 - t) * t;
            var t3 = t * t;
            var cpx = (t1 * path[seqStart].X + t3 * path[seqEnd].X - path[errorPoint].X) / -t2;
            var cpy = (t1 * path[seqStart].Y + t3 * path[seqEnd].Y - path[errorPoint].Y) / -t2;

            Func<int, double> distanceFunction = i =>
            {
                t = (i - seqStart) / (double)seqLength;
                t1 = (1.0 - t) * (1.0 - t);
                t2 = 2.0 * (1.0 - t) * t;
                t3 = t * t;
                var px = t1 * path[seqStart].X + t2 * cpx + t3 * path[seqEnd].X;
                var py = t1 * path[seqStart].Y + t2 * cpy + t3 * path[seqEnd].Y;

                return (path[i].X - px) * (path[i].X - px) + (path[i].Y - py) * (path[i].Y - py);
            };

            var isSpline = Fit(distanceFunction, tracingOptions.LTres, (seqStart + 1) % pathLength, i => i != seqEnd, i => (i + 1) % pathLength, ref errorPoint);
            return isSpline ? new[]
            {
                2.0,
                path[seqStart].X,
                path[seqStart].Y,
                cpx,
                cpy,
                path[seqEnd].X,
                path[seqEnd].Y
            } : null;
        }

        // 5.2. - 5.6. recursively fitting a straight or quadratic line segment on this sequence of path nodes,
        // called from tracepath()
        public static List<double[]> Fit(List<InterpolationPoint> path, Tracing tracingOptions, int seqStart, int seqEnd)
        {
            var segment = new List<double[]>();
            var pathLength = path.Count;
            // return if invalid seqend
            if ((seqEnd > pathLength) || (seqEnd < 0))
            {
                return segment;
            }

            var tl = seqEnd - seqStart;
            tl += tl < 0 ? pathLength : 0;

            int errorPoint;
            var lineResult = FitLine(path, tracingOptions, seqStart, seqEnd, tl, out errorPoint);
            if (lineResult != null)
            {
                segment.Add(lineResult);
                return segment;
            }

            //var fitPoint = errorPoint;
            //var splineResult = FitSpline(path, tracingOptions, seqStart, seqEnd, tl, ref errorPoint);
            //if (splineResult != null)
            //{
            //    segment.Add(splineResult);
            //    return segment;
            //}

            // 5.3. If the straight line fails (an error>ltreshold), find the point with the biggest error
            var fitPoint = errorPoint;
            var curvePass = true;
            double errorVal = 0;

            // 5.4. Fit a quadratic spline through this point, measure errors on every point in the sequence
            // helpers and projecting to get control point
            var t = (fitPoint - seqStart) / (double)tl;
            var t1 = (1.0 - t) * (1.0 - t);
            var t2 = 2.0 * (1.0 - t) * t;
            var t3 = t * t;
            var cpx = (t1 * path[seqStart].X + t3 * path[seqEnd].X - path[fitPoint].X) / -t2;
            var cpy = (t1 * path[seqStart].Y + t3 * path[seqEnd].Y - path[fitPoint].Y) / -t2;

            // Check every point
            var pcnt = seqStart + 1;
            while (pcnt != seqEnd)
            {
                t = (pcnt - seqStart) / (double)tl;
                t1 = (1.0 - t) * (1.0 - t);
                t2 = 2.0 * (1.0 - t) * t;
                t3 = t * t;
                var px = t1 * path[seqStart].X + t2 * cpx + t3 * path[seqEnd].X;
                var py = t1 * path[seqStart].Y + t2 * cpy + t3 * path[seqEnd].Y;

                var dist2 = (path[pcnt].X - px) * (path[pcnt].X - px) + (path[pcnt].Y - py) * (path[pcnt].Y - py);

                if (dist2 > tracingOptions.QTres)
                {
                    curvePass = false;
                }
                if (dist2 > errorVal)
                {
                    errorPoint = pcnt;
                    errorVal = dist2;
                }

                pcnt = (pcnt + 1) % pathLength;
            }

            // return spline if fits
            if (curvePass)
            {
                segment.Add(new double[7]);
                var thisSegment = segment[segment.Count - 1];
                thisSegment[0] = 2.0;
                thisSegment[1] = path[seqStart].X;
                thisSegment[2] = path[seqStart].Y;
                thisSegment[3] = cpx;
                thisSegment[4] = cpy;
                thisSegment[5] = path[seqEnd].X;
                thisSegment[6] = path[seqEnd].Y;
                return segment;
            }

            // 5.5. If the spline fails (an error>qtreshold), find the point with the biggest error,
            var splitPoint = (fitPoint + errorPoint) / 2;

            // 5.6. Split sequence and recursively apply 5.2. - 5.6. to startpoint-splitpoint and splitpoint-endpoint sequences
            segment = Fit(path, tracingOptions, seqStart, splitPoint);
            segment.AddRange(Fit(path, tracingOptions, splitPoint, seqEnd));
            return segment;
        }
    }
}
