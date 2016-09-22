// ReSharper disable InconsistentNaming

namespace ImageTracerNet
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
}
