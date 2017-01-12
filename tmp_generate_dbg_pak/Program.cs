using System;
using System.Collections.Generic;
using System.IO;

namespace tmp_generate_dbg_pak
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2) return 1;

            var tempFiles = new Dictionary<uint, string>();

            using (var sr = new StreamReader(args[0]))
            {
                while (!sr.EndOfStream)
                {
                    string[] line = sr.ReadLine().Split('\t');

                    string tempName = Path.GetTempFileName();
                    tempFiles.Add(Convert.ToUInt32(line[0], 16),tempName);

                    using (var sw = new StreamWriter(tempName))
                    {
                        sw.WriteLine("[Checksums]");
                        sw.Write("0x");
                        sw.Write(line[0]);
                        sw.Write(" ");
                        sw.WriteLine(line[1]);
                    }
                }
            }

            using (var f = File.Open(args[1], FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(f))
            {
                const uint dotDebug = 0xCC669555; // htonl(crc32(".dbg")  ^ 0xFFFFFFFF)
                const uint last     = 0xEBBBB183; // htonl(crc32("LAST")  ^ 0xFFFFFFFF)
                const uint dotLast  = 0x3BEFB32C; // htonl(crc32(".last") ^ 0xFFFFFFFF)

                uint headerSize = ((uint)tempFiles.Count + 1) * 32;
                uint headerPaddingSize = 0x1000 - headerSize % 0x1000;
                if (headerPaddingSize == 0x1000) headerPaddingSize = 0;

                uint currentOffset = headerSize + headerPaddingSize;

                // Write the PAK header
                foreach (var kv in tempFiles)
                {
                    string path = kv.Value;
                    FileInfo fi = new FileInfo(path);
                    uint length = (uint)fi.Length;
                    uint padLength = 32 - (length % 32);
                    if (padLength == 32) padLength = 0;

                    bw.Write(dotDebug);
                    bw.Write(ToBigEndian(currentOffset));
                    bw.Write(ToBigEndian(length));
                    bw.Write((uint)0);
                    bw.Write(ToBigEndian(kv.Key));
                    bw.Write(ToBigEndian(kv.Key));
                    bw.Write((uint)0);
                    bw.Write((uint)0);
                    
                    currentOffset += length + padLength - 32; // -32 because measured relative to header item
                }

                // Write the terminating header entry
                bw.Write(dotLast);
                bw.Write(ToBigEndian(currentOffset));
                bw.Write(ToBigEndian(4));
                bw.Write((uint)0);
                bw.Write(last);
                bw.Write(last);
                bw.Write((uint)0);
                bw.Write((uint)0);

                // Write the header padding
                for (int i = 0; i < headerPaddingSize; i++)
                {
                    bw.Write('\0');
                }
                
                // Write the PAK data
                foreach (var kv in tempFiles)
                {
                    string path = kv.Value;
                    FileInfo fi = new FileInfo(path);
                    uint length = (uint)fi.Length;
                    uint padLength = 32 - (length % 32);
                    if (padLength == 32) padLength = 0;

                    using (var tempf = File.OpenRead(path))
                    {
                        tempf.CopyTo(f);
                    }

                    for (int i = 0; i < padLength; i++)
                    {
                        bw.Write('\0');
                    }
                }

                // Write the terminating data entry
                bw.Write((uint)0);
            }

            return 0;
        }

        private static uint ToBigEndian(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }
    }
}
