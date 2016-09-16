using System;
using System.Collections.Generic;
using System.Text;
using ImageTracerNet.Extensions;
using ImageTracerNet.OptionTypes;

namespace ImageTracerNet
{
    internal static class SvgGeneration
    {
        // Converting tracedata to an SVG string, paths are drawn according to a Z-index
        // the optional lcpr and qcpr are linear and quadratic control point radiuses
        public static string ToSvgString(this IndexedImage ii, SvgRendering options)
        {
            // SVG start
            var width = (int)(ii.ImageWidth * options.Scale);
            var height = (int)(ii.ImageHeight * options.Scale);

            var viewBoxOrViewPort = options.Viewbox.IsNotZero() ?
                $"viewBox=\"0 0 {width} {height}\"" :
                $"width=\"{width}\" height=\"{height}\"";
            var svgStringBuilder = new StringBuilder($"<svg {viewBoxOrViewPort} version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" ");
            if (options.Desc.IsNotZero())
            {
                svgStringBuilder.Append($"desc=\"Created with ImageTracer.NET version {ImageTracer.VersionNumber}\" ");
            }
            svgStringBuilder.Append(">");

            // creating Z-index
            var zIndex = new SortedDictionary<double, ZPosition>();
            // Layer loop
            for (var layerIndex = 0; layerIndex < ii.Layers.Count; layerIndex++)
            {
                // Path loop
                for (var pathIndex = 0; pathIndex < ii.Layers[layerIndex].Count; pathIndex++)
                {
                    // Label (Z-index key) is the startpoint of the path, linearized
                    var label = ii.Layers[layerIndex][pathIndex][0][2] * width + ii.Layers[layerIndex][pathIndex][0][1];
                    zIndex[label] = new ZPosition { Layer = layerIndex, Path = pathIndex };
                }
            }

            // Sorting Z-index is not required, TreeMap is sorted automatically

            // Drawing
            // Z-index loop
            foreach (var zPosition in zIndex)
            {
                var zValue = zPosition.Value;
                var description = String.Empty;
                if (options.Desc.IsNotZero())
                {
                    description = $"desc=\"l {zValue.Layer} p {zValue.Path}\" ";
                }

                AppendPathString(svgStringBuilder, description, ii.Layers[zValue.Layer][zValue.Path],
                    ii.Palette[zValue.Layer].ToSvgColorString(), options);
            }

            // SVG End
            svgStringBuilder.Append("</svg>");

            return svgStringBuilder.ToString();
        }

        // Getting SVG path element string from a traced path
        private static void AppendPathString(StringBuilder stringBuilder, string description, IReadOnlyList<double[]> segments, string colorString, SvgRendering options)
        {
            var scale = options.Scale;
            var linearControlPointRadius = options.LCpr;
            var quadraticControlPointRadius = options.LCpr;
            var roundCoords = options.RoundCoords;
            // Path
            stringBuilder.Append($"<path {description}{colorString}d=\"M {segments[0][1] * scale} {segments[0][2] * scale} ");
            foreach (var segment in segments)
            {
                string segmentAsString;
                if (roundCoords == -1)
                {
                    segmentAsString = segment[0].AreEqual(1.0)
                        ? $"L {segment[3] * scale} {segment[4] * scale} "
                        : $"Q {segment[3] * scale} {segment[4] * scale} {segment[5] * scale} {segment[6] * scale} ";
                }
                else
                {
                    segmentAsString = segment[0].AreEqual(1.0)
                        ? $"L {Math.Round(segment[3] * scale, roundCoords)} {Math.Round(segment[4] * scale, roundCoords)} "
                        : $"Q {Math.Round(segment[3] * scale, roundCoords)} {Math.Round(segment[4] * scale, roundCoords)} {Math.Round(segment[5] * scale, roundCoords)} {Math.Round(segment[6] * scale, roundCoords)} ";
                }

                stringBuilder.Append(segmentAsString);
            }

            stringBuilder.Append("Z\" />");

            // Rendering control points
            foreach (var segment in segments)
            {
                if ((linearControlPointRadius > 0) && segment[0].AreEqual(1.0))
                {
                    stringBuilder.Append($"<circle cx=\"{segment[3] * scale}\" cy=\"{segment[4] * scale}\" r=\"{linearControlPointRadius}\" fill=\"white\" stroke-width=\"{linearControlPointRadius * 0.2}\" stroke=\"black\" />");
                }
                if ((quadraticControlPointRadius > 0) && segment[0].AreEqual(2.0))
                {
                    stringBuilder.Append($"<circle cx=\"{segment[3] * scale}\" cy=\"{segment[4] * scale}\" r=\"{quadraticControlPointRadius}\" fill=\"cyan\" stroke-width=\"{quadraticControlPointRadius * 0.2}\" stroke=\"black\" />");
                    stringBuilder.Append($"<circle cx=\"{segment[5] * scale}\" cy=\"{segment[6] * scale}\" r=\"{quadraticControlPointRadius}\" fill=\"white\" stroke-width=\"{quadraticControlPointRadius * 0.2}\" stroke=\"black\" />");
                    stringBuilder.Append($"<line x1=\"{segment[1] * scale}\" y1=\"{segment[2] * scale}\" x2=\"{segment[3] * scale}\" y2=\"{segment[4] * scale}\" stroke-width=\"{quadraticControlPointRadius * 0.2}\" stroke=\"cyan\" />");
                    stringBuilder.Append($"<line x1=\"{segment[3] * scale}\" y1=\"{segment[4] * scale}\" x2=\"{segment[5] * scale}\" y2=\"{segment[6] * scale}\" stroke-width=\"{quadraticControlPointRadius * 0.2}\" stroke=\"cyan\" />");
                }
            }
        }

        private static string ToSvgColorString(this IReadOnlyList<byte> c)
        {
            return $"fill=\"rgb({c[0]},{c[1]},{c[2]})\" stroke=\"rgb({c[0]},{c[1]},{c[2]})\" stroke-width=\"1\" opacity=\"{c[3] / 255.0}\" ";
        }
    }
}
