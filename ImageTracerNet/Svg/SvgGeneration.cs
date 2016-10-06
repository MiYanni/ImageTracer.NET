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
            var scaledWidth = (int)(image.Width * options.Scale);
            var scaledHeight = (int)(image.Height * options.Scale);

            var viewBoxOrViewPort = options.Viewbox ?
                $"viewBox=\"0 0 {scaledWidth} {scaledHeight}\"" :
                $"width=\"{scaledWidth}\" height=\"{scaledHeight}\"";
            var stringBuilder = new StringBuilder($"<svg {viewBoxOrViewPort} version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" ");
            if (options.Desc)
            {
                stringBuilder.Append($"desc=\"Created with ImageTracer.NET version {ImageTracer.VersionNumber}\" ");
            }
            stringBuilder.Append(">");

            // creating Z-index
            // Only selecting the first segment of each path.
            var zSortedLayers = image.Layers
                .SelectMany(cs => cs.Value.Paths.Select(p =>
                {
                    var firstSegmentStart = p.Segments.First().Start;
                    var label = firstSegmentStart.Y * scaledWidth + firstSegmentStart.X;
                    return new ZPosition { Label = label, Color = cs.Key, Path = p };
                })).OrderBy(z => z.Label);
            // Sorting Z-index is not required, TreeMap is sorted automatically

            // Drawing
            // Z-index loop
            foreach (var zPosition in zSortedLayers)
            {
                var description = String.Empty;
                //if (options.Desc)
                //{
                //    description = $"desc=\"l {zValue.Layer} p {zValue.Path}\" ";
                //}
                var scaledSegments = zPosition.Path.Segments.Select(s => s.Scale(options.Scale)).ToList();
                AppendPathString(stringBuilder, description, scaledSegments, zPosition.Color.ToSvgColorString(), options);
            }

            // SVG End
            stringBuilder.Append("</svg>");

            return stringBuilder.ToString();
        }

        // Getting SVG path element string from a traced path
        internal static StringBuilder AppendPathString(StringBuilder stringBuilder, string description, IReadOnlyList<Segment> segments, string colorString, SvgRendering options)
        {
            // Path
            stringBuilder.Append($"<path {description}{colorString}d=\"M {segments[0].Start.X} {segments[0].Start.Y} ");
            //http://stackoverflow.com/a/217814/294804
            segments.Aggregate(stringBuilder, (sb, segment) => sb.Append(segment.ToPathString())).Append("Z\" />");

            // Rendering control points
            return segments.Where(s => s.Radius > 0).Aggregate(stringBuilder, (sb, segment) => sb.Append(segment.ToControlPointString()));
        }

        internal static string ToSvgColorString(this ColorReference c)
        {
            return $"fill=\"rgb({c.R},{c.G},{c.B})\" stroke=\"rgb({c.R},{c.G},{c.B})\" stroke-width=\"1\" opacity=\"{c.A / 255.0}\" ";
        }
    }
}
