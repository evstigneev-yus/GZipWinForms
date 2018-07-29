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
        /// <param name="destFileName"></param>
        /// <param name="compressionMode"></param>
        public MyGzip(string srcFileName, string destFileName, CompressionMode compressionMode)
        {
            _compressionMode = compressionMode;
            DestFileName = destFileName;
            sourceFi = new FileInfo(srcFileName);
            if (!sourceFi.Exists)
            {
                throw new Exception("File not found");
            }
        }
        private FileInfo sourceFi { get; set; }
        private string DestFileName { get; set; }
        private readonly CompressionMode _compressionMode;
        public Action<long> ProgressAction { get; set; }
        public Action<long> RezFileLength { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public async void Compress()
        {
            try
            {
                RezFileLength?.Invoke(sourceFi.Length);
                using (var src = sourceFi.OpenRead())
                {
                    using (var dest = new FileStream(DestFileName, FileMode.Create))
                    {
                        using (var gz = new GZipStream(dest, CompressionMode.Compress))
                        {
                            await src.CopyToWithProgressAsync(gz, 1024 * 1024 * 8, ProgressAction, CancellationToken);
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
                                ProgressAction, CancellationToken);
                        }
                    }
                }
            }
            catch (Exception)
            {
                File.Delete(DestFileName);
            }
            
        }

        public void DoWork()
        {
            switch (_compressionMode)
            {
                case CompressionMode.Compress:
                        Compress();
                    break;
                case CompressionMode.Decompress:
                        Decompress();
                    break;
            }
        }
    }
}
