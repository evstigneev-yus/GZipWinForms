using System;

namespace WinFormsGzip
{
    /// <summary>
    /// Считает размер одного блока в байтах,
    ///  основываясь на колличестве ядер и доступной оперативной памяти
    /// "размер блока"=(доступная оперативка)/(колличество ядер)
    /// </summary>
    internal class MyBytesCounter
    {
        /// <summary>
        /// Доступно только после вызова функции GetBytesPerBlock()
        /// </summary>
        public int CPUCores { get; private set; }

        ///
        /// <returns>0-ошибка</returns>
        public int GetBytesPerBlock()
        {
           int rez = 0;
                try
                {
                    long ram = 0;
                    foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_OperatingSystem").Get())
                    {
                        //+1k т.к. тут размер в кб, а надо в б
                        //80% от всей доступной оперативки, не будем жадничать
                        ram = (long.Parse(item["FreePhysicalMemory"].ToString()) * 800);
                    }
                    CPUCores = Environment.ProcessorCount; 
                    var bytesPerBlock = (int)(ram / CPUCores);
                    //Чтобы не было переполнения
                    var max = 1_000_000_000 / CPUCores;
                    if (bytesPerBlock> max)
                    {
                        bytesPerBlock = max;
                    }
                    rez = bytesPerBlock;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                return rez;
        }
    }
}
