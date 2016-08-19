using System;
using System.IO;
using ImageTracerNet;
using Options = System.Collections.Generic.Dictionary<string, float>; // HashMap<String, Float>()

namespace ImageTracerTester
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //SaveTracedImage(args);
            SaveTracedImage(new [] { @"..\..\Images\Chrono Trigger2.png", "outfilename", @"Chrono Trigger2-traced.svg" });
        }

        private static void SaveTracedImage(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    Console.WriteLine("ERROR: there's no input filename. Basic usage: \r\n\r\njava -jar ImageTracer.jar <filename>" +
                            "\r\n\r\nor\r\n\r\njava -jar ImageTracer.jar help");
                }
                else if (arraycontains(args, "help") > -1)
                {
                    Console.WriteLine("Example usage:\r\n\r\njava -jar ImageTracer.jar <filename> outfilename test.svg " +
                            "ltres 1 qtres 1 pathomit 8 colorsampling 1 numberofcolors 16 mincolorratio 0.02 colorquantcycles 3 " +
                            "scale 1 simplifytolerance 0 roundcoords 1 lcpr 0 qcpr 0 desc 1 viewbox 0 blurradius 0 blurdelta 20 \r\n" +
                            "\r\nOnly <filename> is mandatory, if some of the other optional parameters are missing, they will be set to these defaults. " +
                            "\r\nWarning: if outfilename is not specified, then <filename>.svg will be overwritten." +
                            "\r\nSee https://github.com/jankovicsandras/imagetracerjava for details. \r\nThis is version " + ImageTracer.versionnumber);
                }
                else
                {

                    // Parameter parsing
                    String outfilename = args[0] + ".svg";
                    Options options = new Options();
                    String[] parameternames = { "ltres", "qtres", "pathomit", "colorsampling", "numberofcolors", "mincolorratio", "colorquantcycles", "scale", "simplifytolerance", "roundcoords", "lcpr", "qcpr", "desc", "viewbox", "blurradius", "blurdelta", "outfilename" };
                    int j = -1; float f = -1;
                    foreach (var parametername in parameternames)
                    {
                        j = arraycontains(args, parametername);
                        if (j > -1)
                        {
                            if (parametername == "outfilename")
                            {
                                if (j < (args.Length - 1)) { outfilename = args[j + 1]; }
                            }
                            else
                            {
                                f = parsenext(args, j); if (f > -1) { options[parametername] = f; }
                            }
                        }
                    }// End of parameternames loop

                    var imageTracer = new ImageTracer();
                    // Loading image, tracing, rendering SVG, saving SVG file
                    File.WriteAllText(outfilename, ImageTracer.imageToSVG(args[0], options, null));

                }// End of parameter parsing and processing

            }
            catch (Exception e) { Console.WriteLine(e.StackTrace); }
        }

        public static int arraycontains(String[] arr, String str)
        {
            for (int j = 0; j < arr.Length; j++) { if (arr[j].ToLower().Equals(str)) { return j; } }
            return -1;
        }

        public static float parsenext(String[] arr, int i)
        {
            if (i < (arr.Length - 1)) { try { return (float)Convert.ToDouble(arr[i + 1]); } catch (Exception) { } }
            return -1;
        }
    }
}
