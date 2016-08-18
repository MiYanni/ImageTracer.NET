using System.Collections.Generic;
using TriListDoubleArray = System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;

namespace ImageTracerNet
{
    // Container for the color-indexed image before and tracedata after vectorizing
    public class IndexedImage
    {
        public int width, height;
        public int[][] array; // array[x][y] of palette colors
        public byte[][] palette;// array[palettelength][4] RGBA color palette
        public TriListDoubleArray layers;// tracedata

        public IndexedImage(int[][] marray, byte[][] mpalette)
        {
            array = marray; palette = mpalette;
            width = marray[0].Length - 2; height = marray.Length - 2;// Color quantization adds +2 to the original width and height
        }
    }
}
