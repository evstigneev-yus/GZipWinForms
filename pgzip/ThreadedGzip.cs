using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pgzip
{
    public class ThreadedGzip : IDisposable
    {
        #region private fields
        private static readonly int _cpuNum = Environment.ProcessorCount;
        private byte[][] _uncompressedDataArray = new byte[_cpuNum][];
        private byte[][] _compressedDataArray = new byte[_cpuNum][];
        private static FileStream _inFileStream;
        private static FileStream _outFileStream;
        private readonly string _inFileName;
        private static Action<long> _progressAction;
        #endregion
        #region public fields
        public string OutFileName { get; set; }
        public int BlockSize { get; set; }
        #endregion
        #region constructors
        public ThreadedGzip(string inFileName, Action<long> progressAction)
        {
            OutFileName = inFileName + ".gz";
            _inFileName = inFileName;
            OutFileName = inFileName + ".gz";
            BlockSize = 104857600;
            _progressAction = progressAction;
        }
        #endregion
        #region public functions
        public void Compress(CancellationToken token)
        {
            var fi = new FileInfo(OutFileName);
            if (fi.Exists)
            {
                fi.Delete();
            }
            _inFileStream = new FileStream(_inFileName, FileMode.Open);
            _outFileStream = new FileStream(OutFileName, FileMode.Append);
            var tPool = new Thread[_cpuNum];
            for (var i = 0; i < _cpuNum; i++)
            {
                if (token.IsCancellationRequested) break;
                StartNewCompressThread(i, BlockSize, ref _inFileStream, ref tPool);
            }
            for (var i = 0; (i < _cpuNum && tPool[i] != null) && !token.IsCancellationRequested;)
            {
                if (token.IsCancellationRequested)
                {
                    foreach (var thread in tPool)
                    {
                        thread.Join();
                    }
                    break;
                }
                tPool[i].Join();
                //Колдунство: дописываем длинну сжатых данных в MTIME блок заголовка файла, чтобы потом разжать можно было
                var v = BitConverter.GetBytes(_compressedDataArray[i].Length);
                v.CopyTo(_compressedDataArray[i], 4);
                ////////////////////////////////////////////////////////////
                //пишем сжатый блок в файл
                _outFileStream.Write(_compressedDataArray[i], 0, _compressedDataArray[i].Length);
                tPool[i] = null;
                //Костыль от переполнения
                GC.Collect();
                if (_inFileStream.Position < _inFileStream.Length)
                {
                    StartNewCompressThread(i, BlockSize, ref _inFileStream, ref tPool);
                }
                i++;
                if (i == _cpuNum)
                {
                    i = 0;
                }
            }
            _outFileStream.Close();
            _inFileStream.Close();
            if (token.IsCancellationRequested)
            {
                var f = new FileInfo(OutFileName);
                if (f.Exists)
                {
                    f.Delete();
                }
            }
        }
        public void Decompress(CancellationToken token)
        {
            var fi = new FileInfo(OutFileName);
            if (fi.Exists)
            {
                fi.Delete();
            }
            _inFileStream = new FileStream(_inFileName, FileMode.Open);
            _outFileStream = new FileStream(OutFileName, FileMode.Append);
            var tPool = new Thread[_cpuNum];
            for (var i = 0; i < _cpuNum; i++)
            {
                if (token.IsCancellationRequested) break;
                StartNewDecompressThread(i, ref _inFileStream, ref tPool);
            }
            for (var i = 0; i < _cpuNum && tPool[i] != null && !token.IsCancellationRequested;)
            {
                tPool[i].Join();

                _outFileStream.Write(_uncompressedDataArray[i], 0, _uncompressedDataArray[i].Length);
                tPool[i] = null;
                //Костыль от переполнения
                GC.Collect();
                if (_inFileStream.Position < _inFileStream.Length)
                {
                    StartNewDecompressThread(i, ref _inFileStream, ref tPool);
                }
                i++;
                if (i == _cpuNum)
                {
                    i = 0;
                }
            }
            _outFileStream.Close();
            _inFileStream.Close();
            if (token.IsCancellationRequested)
            {
                var f = new FileInfo(OutFileName);
                if (f.Exists)
                {
                    f.Delete();
                }
            }
        }
        #endregion
        #region private functions
        private void StartNewCompressThread(int i, int initialBlockSize, ref FileStream inFileStream, ref Thread[] tPool)
        {
            int blockSize;
            if (inFileStream.Length - inFileStream.Position <= initialBlockSize)
            {
                blockSize = (int)(inFileStream.Length - inFileStream.Position);
            }
            else
            {
                blockSize = initialBlockSize;
            }
            _uncompressedDataArray[i] = new byte[blockSize];
            inFileStream.Read(_uncompressedDataArray[i], 0, blockSize);
            _progressAction.Invoke(inFileStream.Position);
            tPool[i] = new Thread(CompressBlock);
            tPool[i].Start(i);
        }
        private void CompressBlock(object i)
        {
            using (var output = new MemoryStream(_uncompressedDataArray[(int)i].Length))
            {
                using (var gZipStream = new GZipStream(output, CompressionMode.Compress))
                {
                    gZipStream.Write(_uncompressedDataArray[(int)i], 0, _uncompressedDataArray[(int)i].Length);
                }
                _compressedDataArray[(int)i] = output.ToArray();
            }
        }
        private void StartNewDecompressThread(int i, ref FileStream inFileStream, ref Thread[] tPool)
        {
            var buffer = new byte[8];
            //читаем заголовок файла
            inFileStream.Read(buffer, 0, 8);
            //выбираем из прочитанного размер блока
            var compressedBlockLength = BitConverter.ToInt32(buffer, 4);
            _compressedDataArray[i] = new byte[compressedBlockLength + 1];
            buffer.CopyTo(_compressedDataArray[i], 0);
            inFileStream.Read(_compressedDataArray[i], 8, compressedBlockLength - 8);
            _progressAction.Invoke(inFileStream.Position);
            //читаем размер блока после разархивации из футера файла
            var blockSize = BitConverter.ToInt32(_compressedDataArray[i], compressedBlockLength - 4);
            _uncompressedDataArray[i] = new byte[blockSize];
            tPool[i] = new Thread(DecompressBlock);
            tPool[i].Start(i);
        }
        private void DecompressBlock(object i)
        {
            using (var input = new MemoryStream(_compressedDataArray[(int)i]))
            {
                using (var gZipStream = new GZipStream(input, CompressionMode.Decompress))
                {
                    gZipStream.Read(_uncompressedDataArray[(int)i], 0, _uncompressedDataArray[(int)i].Length);
                }
            }
        }


        #endregion
        #region IDisposable

        public void Dispose()
        {
            _outFileStream.Close();
            _inFileStream.Close();
        }

        #endregion
    }
}
