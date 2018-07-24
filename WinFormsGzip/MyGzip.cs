using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinFormsGzip
{
    class MyGzip
    {
        /// <summary>
        /// Throw exception if file does not exist
        /// </summary>
        /// <param name="srcFileName"></param>
        public MyGzip(string srcFileName)
        {
            SrcFileName = srcFileName;
            FunctionFlg = srcFileName.EndsWith(".gz");
            DestFileName = FunctionFlg ? SrcFileName.Remove(SrcFileName.Length - 3) : SrcFileName + ".gz";
            sourceFi = new FileInfo(SrcFileName);
            if (!sourceFi.Exists)
            {
                throw new Exception("File not found");
            }
        }

        private FileInfo sourceFi { get; set; }
        private string SrcFileName { get; set; }
        private string DestFileName { get; set; }
        public bool FunctionFlg { get; set; }//false - архивировать true-разархивировать
        public Action<long> progressAction { get; set; }
        public Action<long> RezFileLength { get; set; }
        public CancellationToken cancellationToken { get; set; }

        public async void Compress()
        {
            try
            {
                RezFileLength.Invoke(sourceFi.Length);
                using (var src = sourceFi.OpenRead())
                {
                    using (var dest = new FileStream(DestFileName, FileMode.Create))
                    {
                        using (var gz = new GZipStream(dest, CompressionMode.Compress))
                        {
                            await src.CopyToWithProgressAsync(gz, 1024 * 1024 * 8, progressAction, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception)
            {
                File.Delete(DestFileName);
            }
        }

        public async void Decompress()
        {
            try
            {
                if (sourceFi.Length < int.MaxValue)
                {//works only with less than 2Gb files
                    using (var v = sourceFi.OpenRead())
                    {
                        var buff = new byte[4];
                        v.Seek(-4, SeekOrigin.End);
                        v.Read(buff, 0, 4);
                        RezFileLength.Invoke(BitConverter.ToInt32(buff, 0));
                    }
                }
                using (var originalFileStream = sourceFi.OpenRead())
                {
                    using (var dest = new FileStream(DestFileName, FileMode.Create))
                    {
                        using (var gz = new GZipStream(originalFileStream, CompressionMode.Decompress))
                        {
                            await gz.CopyToWithProgressAsync(dest, 1024 * 1024 * 8,
                                progressAction, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception)
            {
                File.Delete(DestFileName);
            }
            
        }
    }
}
