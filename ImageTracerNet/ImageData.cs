namespace ImageTracerNet
{
    // https://developer.mozilla.org/en-US/docs/Web/API/ImageData
    public class ImageData
    {
        public int width, height;
        public byte[] data; // raw byte data: R G B A R G B A ...
        public ImageData(int mwidth, int mheight, byte[] mdata)
        {
            width = mwidth; height = mheight; data = mdata;
        }
    }
}
