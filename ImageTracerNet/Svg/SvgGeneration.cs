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
            var scaledSegments = segments.Select(s => s.Scale(options.Scale)).ToList();

            // Path
            stringBuilder.Append($"<path {description}{colorString}d=\"M {scaledSegments[0].Start.X} {scaledSegments[0].Start.Y} ");
            //foreach (var segment in scaledSegments)
            //{
            //    stringBuilder.Append(segment.ToPathString(options.RoundCoords));
            //}
            //http://stackoverflow.com/a/217814/294804
            scaledSegments.Aggregate(stringBuilder,
                (current, next) => current.Append(next.ToPathString(options.RoundCoords)));
            stringBuilder.Append("Z\" />");

            // Rendering control points
            var filteredSegments = scaledSegments.Where(s => (s is LineSegment && options.LCpr > 0) || (s is SplineSegment && options.QCpr > 0));
            foreach (var segment in filteredSegments)
            {
                //var quadraticSegment = segment as SplineSegment;
                //var segmentAsString = quadraticSegment != null
                //    ? $"<circle cx=\"{quadraticSegment.Mid.X}\" cy=\"{quadraticSegment.Mid.Y}\" r=\"{quadraticControlPointRadius}\" fill=\"cyan\" stroke-width=\"{quadraticControlPointRadius*0.2}\" stroke=\"black\" />" +
                //      $"<circle cx=\"{quadraticSegment.End.X}\" cy=\"{quadraticSegment.End.Y}\" r=\"{quadraticControlPointRadius}\" fill=\"white\" stroke-width=\"{quadraticControlPointRadius*0.2}\" stroke=\"black\" />" +
                //      $"<line x1=\"{quadraticSegment.Start.X}\" y1=\"{quadraticSegment.Start.Y}\" x2=\"{quadraticSegment.Mid.X}\" y2=\"{quadraticSegment.Mid.Y}\" stroke-width=\"{quadraticControlPointRadius*0.2}\" stroke=\"cyan\" />" +
                //      $"<line x1=\"{quadraticSegment.Mid.X}\" y1=\"{quadraticSegment.Mid.Y}\" x2=\"{quadraticSegment.End.X}\" y2=\"{quadraticSegment.End.Y}\" stroke-width=\"{quadraticControlPointRadius*0.2}\" stroke=\"cyan\" />"
                //    : $"<circle cx=\"{segment.End.X}\" cy=\"{segment.End.Y}\" r=\"{linearControlPointRadius}\" fill=\"white\" stroke-width=\"{linearControlPointRadius*0.2}\" stroke=\"black\" />";
                var radius = segment is SplineSegment ? options.QCpr : options.LCpr;
                stringBuilder.Append(segment.ToControlPointString(radius));
            }
        }

        internal static string ToSvgColorString(this ColorReference c)
        {
            return $"fill=\"rgb({c.R},{c.G},{c.B})\" stroke=\"rgb({c.R},{c.G},{c.B})\" stroke-width=\"1\" opacity=\"{c.A / 255.0}\" ";
        }
    }
}
