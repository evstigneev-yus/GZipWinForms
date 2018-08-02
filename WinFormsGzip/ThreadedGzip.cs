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
    public class ThreadedGzip:IDisposable
    {
        private static int _cpuNum = Environment.ProcessorCount;
        private byte[][] _uncompressedDataArray = new byte[_cpuNum][];
        private byte[][] _compressedDataArray = new byte[_cpuNum][];
        private static bool _cancelToken;
        private FileStream _inFileStream;
        private FileStream _outFileStream;
        public bool CancellToken
        {
            get => _cancelToken;
            set => _cancelToken = value;
        }
        private readonly string _inFileName;
        private readonly string _outFileName;
        public ThreadedGzip(string inFileName, string outFileName,ref bool cancelToken)
        {
            var fi = new FileInfo(outFileName);
            if (fi.Exists)
            {
                fi.Delete();
            }
            _inFileName = inFileName;
            _outFileName = outFileName;
            _cancelToken = cancelToken;
        }

        public void Compress()
        {

            _inFileStream = new FileStream(_inFileName, FileMode.Open);
            _outFileStream = new FileStream(_outFileName, FileMode.Append);

            #region Считаем размер блока
            var bpb = new MyBytesCounter();
            var bytesPerBlock = bpb.GetBytesPerBlock();
            if ((_inFileStream.Length / bytesPerBlock) < bpb.CPUCores)
            {
                bytesPerBlock = (int)(_inFileStream.Length / bpb.CPUCores);
            }
            var initialBlockSize = bytesPerBlock;
            #endregion
            
            var tPool = new Thread[_cpuNum];
            for (var i = 0; i < _cpuNum; i++)
            {
                if (_cancelToken) break;
                StartNewCompressThread(i, initialBlockSize, ref _inFileStream, ref tPool);
            }
            for (var i = 0; i < _cpuNum && tPool[i] != null && !_cancelToken;)
            {
                if (_cancelToken)
                {
                    foreach (var thread in tPool)
                    {
                        thread.Join();
                        thread.Interrupt();
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
                if (_inFileStream.Position < _inFileStream.Length )
                {
                    StartNewCompressThread(i, initialBlockSize, ref _inFileStream, ref tPool);
                }
                i++;
                if (i == _cpuNum)
                {
                    i = 0;
                }
            }
            _outFileStream.Close();
            _inFileStream.Close();
        }
        private void StartNewCompressThread(int i, int initialBlockSize, ref FileStream inFileStream,ref Thread[] tPool)
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
        public void Decompress()
        {
            _inFileStream = new FileStream(_inFileName, FileMode.Open);
            _outFileStream = new FileStream(_outFileName, FileMode.Append);
            var tPool = new Thread[_cpuNum];
            for (var i = 0;i < _cpuNum;i++)
            {
                if (_cancelToken) break;
                StartNewDecompressThread(i, ref _inFileStream, ref tPool);
            }
            for (var i = 0; i < _cpuNum && tPool[i] != null && !_cancelToken;)
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

        public void Dispose()
        {
            if (_cancelToken)
            {
                var fi = new FileInfo(_outFileName);
                if (fi.Exists)
                {
                    fi.Delete();
                }
            }
            _outFileStream.Close();
            _inFileStream.Close();
        }
    }
}
