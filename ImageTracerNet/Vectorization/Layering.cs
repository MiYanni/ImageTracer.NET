using System.Collections.Generic;
using System.Linq;
using ImageTracerNet.Extensions;
using ImageTracerNet.Vectorization.TraceTypes;
using EdgeNodeCalc = System.Func<ImageTracerNet.Vectorization.ColorGroup, ImageTracerNet.Vectorization.EdgeNode>;
using static System.Convert;

namespace ImageTracerNet.Vectorization
{
    internal static class Layering
    {
        // 1, 3, 5, 7, 9, 11, 13, 15
        private static readonly EdgeNodeCalc BottomRight = 
            cg => (EdgeNode)(1 + ToInt32(cg.MidRight == cg.Mid) * 2 + ToInt32(cg.BottomRight == cg.Mid) * 4 + ToInt32(cg.BottomMid == cg.Mid) * 8);
        // 2, 6, 10, 14
        private static readonly EdgeNodeCalc BottomMid =
            cg => (EdgeNode)(2 + ToInt32(cg.BottomMid == cg.Mid) * 4 + ToInt32(cg.BottomLeft == cg.Mid) * 8);
        // 8, 10, 12, 14
        private static readonly EdgeNodeCalc MidRight =
            cg => (EdgeNode)(8 + ToInt32(cg.TopRight == cg.Mid) * 2 + ToInt32(cg.MidRight == cg.Mid) * 4);
        // 4, 6, 12, 14
        private static readonly EdgeNodeCalc Mid =
            cg => (EdgeNode)(4 + ToInt32(cg.TopMid == cg.Mid) * 2 + ToInt32(cg.MidLeft == cg.Mid) * 8);

        // 2. Layer separation and edge detection

        // Edge node types ( ▓:light or 1; ░:dark or 0 )

        // 12  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓

        // 48  ░░  ░░  ░░  ░░  ░▓  ░▓  ░▓  ░▓  ▓░  ▓░  ▓░  ▓░  ▓▓  ▓▓  ▓▓  ▓▓
        //     0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
        public static Dictionary<ColorReference, RawLayer> Convert(IEnumerable<ColorGroup> groups, int width, int height, IEnumerable<ColorReference> palette)
        {
            // Creating layers for each indexed color in arr
            // Adding 1 to height and width because EdgeNodes represent in-between pixels. So, half of a pixel is added to all sides of the image (in a logical sense).
            var layers = palette.ToDictionary(p => p, p => new RawLayer { Nodes = new EdgeNode[height + 1][].InitInner(width + 1) });
            // Looping through all pixels and calculating edge node type
            foreach (var cg in groups)
            {
                // Are neighbor pixel colors the same? Set this pixel's type and look back on previous pixels.
                layers[cg.Mid].Nodes[cg.Y][cg.X] = BottomRight(cg);
                if (cg.MidLeft != cg.Mid)
                {
                    layers[cg.Mid].Nodes[cg.Y][cg.X - 1] = BottomMid(cg);
                }
                if (cg.TopMid != cg.Mid)
                {
                    layers[cg.Mid].Nodes[cg.Y - 1][cg.X] = MidRight(cg);
                }
                if (cg.TopLeft != cg.Mid)
                {
                    layers[cg.Mid].Nodes[cg.Y - 1][cg.X - 1] = Mid(cg);
                }
            }
            // After the process is complete, we remove any layers that do not contain allocated nodes.
            return layers.Where(cl => cl.Value.Nodes.Any(r => r.Any(n => n.IsLight()))).ToDictionary(cl => cl.Key, cl => cl.Value);
        }
    }
}
