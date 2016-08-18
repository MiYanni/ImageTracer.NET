namespace ImageTracerTester
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            SaveTracedImage();
        }

        private static void SaveTracedImage()
        {
            //var originalImage = new Bitmap(@"..\..\Images\Chrono Trigger2.png");

            //const string fileName = "Image";
            //const string imageExtension = ".png";

            //originalImage.Save(fileName + "-orig" + imageExtension, ImageFormat.Png);

            //const int scaleSize = 3;
            //var scaledImage = new xBRZScaler().ScaleImage(originalImage, scaleSize);

            //scaledImage.Save(fileName + "-" + scaleSize + "xBRZ" + imageExtension, ImageFormat.Png);
        }

        //public static void main(String[] args)
        //{
        //    try
        //    {

        //        if (args.length < 1)
        //        {
        //            System.out.println("ERROR: there's no input filename. Basic usage: \r\n\r\njava -jar ImageTracer.jar <filename>" +
        //                    "\r\n\r\nor\r\n\r\njava -jar ImageTracer.jar help");
        //        }
        //        else if (arraycontains(args, "help") > -1)
        //        {
        //            System.out.println("Example usage:\r\n\r\njava -jar ImageTracer.jar <filename> outfilename test.svg " +
        //                    "ltres 1 qtres 1 pathomit 8 colorsampling 1 numberofcolors 16 mincolorratio 0.02 colorquantcycles 3 " +
        //                    "scale 1 simplifytolerance 0 roundcoords 1 lcpr 0 qcpr 0 desc 1 viewbox 0 blurradius 0 blurdelta 20 \r\n" +
        //                    "\r\nOnly <filename> is mandatory, if some of the other optional parameters are missing, they will be set to these defaults. " +
        //                    "\r\nWarning: if outfilename is not specified, then <filename>.svg will be overwritten." +
        //                    "\r\nSee https://github.com/jankovicsandras/imagetracerjava for details. \r\nThis is version " + versionnumber);
        //        }
        //        else
        //        {

        //            // Parameter parsing
        //            String outfilename = args[0] + ".svg";
        //            HashMap<String, Float> options = new HashMap<String, Float>();
        //            String[] parameternames = { "ltres", "qtres", "pathomit", "colorsampling", "numberofcolors", "mincolorratio", "colorquantcycles", "scale", "simplifytolerance", "roundcoords", "lcpr", "qcpr", "desc", "viewbox", "blurradius", "blurdelta", "outfilename" };
        //            int j = -1; float f = -1;
        //            for (String parametername : parameternames)
        //            {
        //                j = arraycontains(args, parametername);
        //                if (j > -1)
        //                {
        //                    if (parametername == "outfilename")
        //                    {
        //                        if (j < (args.length - 1)) { outfilename = args[j + 1]; }
        //                    }
        //                    else
        //                    {
        //                        f = parsenext(args, j); if (f > -1) { options.put(parametername, new Float(f)); }
        //                    }
        //                }
        //            }// End of parameternames loop

        //            // Loading image, tracing, rendering SVG, saving SVG file
        //            saveString(outfilename, imageToSVG(args[0], options, null));

        //        }// End of parameter parsing and processing

        //    }
        //    catch (Exception e) { e.printStackTrace(); }
        //}// End of main()

        //// Saving a String as a file
        //public static void saveString(String filename, String str) throws Exception
        //{
        //    File file = new File(filename);
        //    // if file doesnt exists, then create it
        //    if(!file.exists()){ file.createNewFile(); }
        //    FileWriter fw = new FileWriter(file.getAbsoluteFile());
        //    BufferedWriter bw = new BufferedWriter(fw);
        //    bw.write(str);
        //    bw.close();
        //}

        //public static int arraycontains(String[] arr, String str)
        //{
        //    for (int j = 0; j < arr.length; j++) { if (arr[j].toLowerCase().equals(str)) { return j; } }
        //    return -1;
        //}

        //public static float parsenext(String[] arr, int i)
        //{
        //    if (i < (arr.length - 1)) { try { return Float.parseFloat(arr[i + 1]); } catch (Exception e) { } }
        //    return -1;
        //}
    }
}
