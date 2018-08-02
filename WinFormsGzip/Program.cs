using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsGzip
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] argc)
        {

            #region ConsoleInputProcessing
            //if (argc.Length==3)
            //{
            //    var compressionMode = CompressionMode.Compress;
            //    switch (argc[0])
            //    {
            //        case "-c":
            //            compressionMode = CompressionMode.Compress;
            //            break;
            //        case "-d":
            //            compressionMode = CompressionMode.Decompress;
            //            break;
            //    }
            //    var srcFifi = new FileInfo(argc[1]);
            //    if (srcFifi.Exists)
            //    {
            //        var splitstr = argc[1].Split('\\');
            //        var dest = argc[2]+argc[1].Split('\\').Last();
            //        if (compressionMode == CompressionMode.Compress)
            //        {
            //            dest += ".gz";
            //        }
            //        else
            //        {
            //            dest = dest.Remove(dest.Length - 3);
            //        }
            //        var cts = new CancellationTokenSource();
            //        var ct = cts.Token;
            //        var zipper = new MyGzip(argc[1], dest, compressionMode)
            //        {
            //            //ProgressAction = (a) => { },
            //            CancellationToken = ct,
            //            //RezFileLength = (a) => { }
            //        };
            //        zipper.DoWork();
            //        Console.WriteLine("Done");
            //        return;
            //    }
            //}
            //else
            //{
            //    Console.Write("WinFormsGzip.exe -compression_mode [source file path] [result file path] \r\n compression mode should be -c for compression\r\n -d for decompression ");
            //}


            #endregion

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
