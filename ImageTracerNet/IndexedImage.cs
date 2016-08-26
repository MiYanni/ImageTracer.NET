using TriListDoubleArray = System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;

namespace ImageTracerNet
{
    // Container for the color-indexed image before and tracedata after vectorizing
    public class IndexedImage
    {
        public int Width { get; }
        public int Height { get; }
        // array[x][y] of palette colors
        public int[][] Array { get; }
        // array[palettelength][4] RGBA color palette
        public byte[][] Palette { get;  }
        // tracedata
        public TriListDoubleArray Layers { set; get; }

        public IndexedImage(int[][] array, byte[][] palette)
        {
            Array = array; Palette = palette;
            // Color quantization adds +2 to the original width and height
            Width = array[0].Length - 2;
            Height = array.Length - 2;
        }
    }
}
