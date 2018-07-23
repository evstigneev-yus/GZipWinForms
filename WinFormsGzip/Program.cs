using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            if (argc.Any())
            {
                var fi = new FileInfo(argc[0]);
                if (fi.Exists)
                {
                    var cts = new CancellationTokenSource();
                    var ct = cts.Token;
                    var zipper = new MyGzip(argc[0])
                    {
                        progressAction = (a) => { },
                        cancellationToken = ct,
                        RezFileLength = (a) => { }
                    };
                    if (zipper.FunctionFlg)
                    {
                        zipper.Decompress();
                    }
                    else
                    {
                        zipper.Compress();
                    }
                }
                
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
