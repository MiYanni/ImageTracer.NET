using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Vectorization.Segments;
using CoordMethod = System.Func<double, double>;

namespace ImageTracerNet.Svg
{
    internal static class SvgGeneration
    {
        // Converting tracedata to an SVG string, paths are drawn according to a Z-index
        // the optional lcpr and qcpr are linear and quadratic control point radiuses
        public static string ToSvgString(this TracedImage image, SvgRendering options)
        {
            // SVG start
            var width = (int)(image.Width * options.Scale);
            var height = (int)(image.Height * options.Scale);

            var viewBoxOrViewPort = options.Viewbox ?
                $"viewBox=\"0 0 {width} {height}\"" :
                $"width=\"{width}\" height=\"{height}\"";
            var svgStringBuilder = new StringBuilder($"<svg {viewBoxOrViewPort} version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" ");
            if (options.Desc)
            {
                svgStringBuilder.Append($"desc=\"Created with ImageTracer.NET version {ImageTracer.VersionNumber}\" ");
            }
            svgStringBuilder.Append(">");

            // creating Z-index
            // Only selecting the first segment of each path.
            var zSortedLayers = image.Layers
                .SelectMany(cs => cs.Value.Paths.Select(p =>
                {
                    var firstSegmentStart = p.Segments.First().Start;
                    var label = firstSegmentStart.Y * width + firstSegmentStart.X;
                    return new ZPosition { Label = label, Color = cs.Key, Path = p };
                })).OrderBy(z => z.Label);
            // Sorting Z-index is not required, TreeMap is sorted automatically

            // Drawing
            // Z-index loop
            foreach (var zPosition in zSortedLayers)
            {
                //var zValue = zPosition.Value;
                var description = String.Empty;
                //if (options.Desc)
                //{
                //    description = $"desc=\"l {zValue.Layer} p {zValue.Path}\" ";
                //}
                AppendPathString(svgStringBuilder, description, zPosition.Path.Segments, zPosition.Color.ToSvgColorString(), options);
            }

            // SVG End
            svgStringBuilder.Append("</svg>");

            return svgStringBuilder.ToString();
        }

        // Getting SVG path element string from a traced path
        internal static void AppendPathString(StringBuilder stringBuilder, string description, IReadOnlyList<Segment> segments, string colorString, SvgRendering options)
        {
            var scale = options.Scale;
            var linearControlPointRadius = options.LCpr;
            var quadraticControlPointRadius = options.LCpr;
            var coordMethod = options.RoundCoords == -1 ? (CoordMethod)(p => p) : p => Math.Round(p, options.RoundCoords);

            // Path
            stringBuilder.Append($"<path {description}{colorString}d=\"M {segments[0].Start.X * scale} {segments[0].Start.Y * scale} ");
            foreach (var segment in segments)
            {
                var quadraticSegment = segment as SplineSegment;
                var segmentAsString = quadraticSegment != null
                    ? $"Q {coordMethod(quadraticSegment.Mid.X * scale)} {coordMethod(quadraticSegment.Mid.Y * scale)} {coordMethod(quadraticSegment.End.X * scale)} {coordMethod(quadraticSegment.End.Y * scale)} "
                    : $"L {coordMethod(segment.End.X * scale)} {coordMethod(segment.End.Y * scale)} ";

                stringBuilder.Append(segmentAsString);
            }
            stringBuilder.Append("Z\" />");

            // Rendering control points
            var filteredSegments = segments.Where(s => (s is LineSegment && linearControlPointRadius > 0) || (s is SplineSegment && quadraticControlPointRadius > 0));
            foreach (var segment in filteredSegments)
            {
                var quadraticSegment = segment as SplineSegment;
                var segmentAsString = quadraticSegment != null
                    ? $"<circle cx=\"{quadraticSegment.Mid.X*scale}\" cy=\"{quadraticSegment.Mid.Y*scale}\" r=\"{quadraticControlPointRadius}\" fill=\"cyan\" stroke-width=\"{quadraticControlPointRadius*0.2}\" stroke=\"black\" />" +
                      $"<circle cx=\"{quadraticSegment.End.X*scale}\" cy=\"{quadraticSegment.End.Y*scale}\" r=\"{quadraticControlPointRadius}\" fill=\"white\" stroke-width=\"{quadraticControlPointRadius*0.2}\" stroke=\"black\" />" +
                      $"<line x1=\"{quadraticSegment.Start.X*scale}\" y1=\"{quadraticSegment.Start.Y*scale}\" x2=\"{quadraticSegment.Mid.X*scale}\" y2=\"{quadraticSegment.Mid.Y*scale}\" stroke-width=\"{quadraticControlPointRadius*0.2}\" stroke=\"cyan\" />" +
                      $"<line x1=\"{quadraticSegment.Mid.X*scale}\" y1=\"{quadraticSegment.Mid.Y*scale}\" x2=\"{quadraticSegment.End.X*scale}\" y2=\"{quadraticSegment.End.Y*scale}\" stroke-width=\"{quadraticControlPointRadius*0.2}\" stroke=\"cyan\" />"
                    : $"<circle cx=\"{segment.End.X*scale}\" cy=\"{segment.End.Y*scale}\" r=\"{linearControlPointRadius}\" fill=\"white\" stroke-width=\"{linearControlPointRadius*0.2}\" stroke=\"black\" />";

                stringBuilder.Append(segmentAsString);
            }
        }

        internal static string ToSvgColorString(this ColorReference c)
        {
            return $"fill=\"rgb({c.R},{c.G},{c.B})\" stroke=\"rgb({c.R},{c.G},{c.B})\" stroke-width=\"1\" opacity=\"{c.A / 255.0}\" ";
        }
    }
}
