// ReSharper disable InconsistentNaming

using System.Linq;
using static ImageTracerNet.Vectorization.EdgeNode;

namespace ImageTracerNet.Vectorization
{

    // Edge node types ( ▓:light or 1; ░:dark or 0 )

    // ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓  ░░  ▓░  ░▓  ▓▓

    // ░░  ░░  ░░  ░░  ░▓  ░▓  ░▓  ░▓  ▓░  ▓░  ▓░  ▓░  ▓▓  ▓▓  ▓▓  ▓▓
    // 0   1   2   3   4   5   6   7   8   9   10  11  12  13  14  15
    internal enum EdgeNode
    {
        DDDD = 0,
        LDDD = 1,
        DLDD = 2,
        LLDD = 3,
        DDDL = 4,
        LDDL = 5,
        DLDL = 6,
        LLDL = 7,
        DDLD = 8,
        LDLD = 9,
        DLLD = 10,
        LLLD = 11,
        DDLL = 12,
        LDLL = 13,
        DLLL = 14,
        LLLL = 15
    }

    internal static class EdgeNodeExtensions
    {
        // Dark nodes are 0-3 and 8-11. This is 2 groups of 4.
        private static readonly EdgeNode[] _darkNodes =
        {
            DDDD, LDDD, DLDD, LLDD,
            DDLD, LDLD, DLLD, LLLD
        };
        // Even nodes are dark (no color) when based on the top-left pixel.
        // However, nodes are based on the bottom right pixel, as this is always set during conversion.
        public static bool IsDark(this EdgeNode node)
        {
            return _darkNodes.Contains(node);
        }
        // Odd nodeas are light (colored) when based on the top-left pixel.
        // However, nodes are based on the bottom right pixel, as this is always set during conversion.
        public static bool IsLight(this EdgeNode node)
        {
            return !node.IsDark();
        }
    }
}
