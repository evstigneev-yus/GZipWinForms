using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WinFormsGzip
{
    public static class MyExtensions
    {
        public static async Task CopyToWithProgressAsync(this Stream source,
            Stream destination,
            int bufferSize = 1024 * 1024 * 8,
            Action<long> progress = null,
            CancellationToken ct = new CancellationToken())
        {
            var buffer = new byte[bufferSize];
            var total = 0L;
            int amtRead;
            do
            {
                amtRead = 0;
                while (amtRead < bufferSize)
                {
                    var numBytes = await source.ReadAsync(buffer,
                        amtRead,
                        bufferSize - amtRead, ct);
                    if (numBytes == 0)
                    {
                        break;
                    }
                    amtRead += numBytes;
                }
                total += amtRead;
                await destination.WriteAsync(buffer, 0, amtRead, ct);
                progress?.Invoke(total);
            } while (amtRead == bufferSize);
        }
    }
}
