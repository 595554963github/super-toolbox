using System;
using System.Collections.Generic;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace supertoolbox.Extractor
{
    public class Fsb5Extractor : BaseExtractor
    {
        private static readonly byte[] FSB5_MAGIC_NUMBER = { 0x46, 0x53, 0x42, 0x35 };

        public event EventHandler<List<string>>? FilesExtracted;

        public class Fsb5Header
        {
            public int TotalSubsongs;
            public int Version;
            public int Codec;
            public int Flags;

            public int Channels;
            public int Layers;
            public int SampleRate;
            public int NumSamples;
            public int LoopStart;
            public int LoopEnd;
            public int LoopFlag;

            public uint SampleHeaderSize;
            public uint NameTableSize;
            public uint SampleDataSize;
            public uint BaseHeaderSize;

            public uint ExtradataOffset;
            public uint ExtradataSize;

            public uint StreamOffset;
            public uint StreamSize;
            public uint NameOffset;
        }

        private static IEnumerable<byte[]> ExtractFsb5Data(byte[] fileContent)
        {
            int startIndex = 0;
            Fsb5Header header = new Fsb5Header(); // Move the declaration of 'header' outside the loop
            while ((startIndex = IndexOf(fileContent, FSB5_MAGIC_NUMBER, startIndex)) != -1)
            {
                using (MemoryStream ms = new MemoryStream(fileContent, startIndex, fileContent.Length - startIndex))
                {
                    using (System.IO.BinaryReader br = new System.IO.BinaryReader(ms))
                    {
                        br.ReadBytes(4);
                        header.Version = br.ReadInt32();
                        header.TotalSubsongs = br.ReadInt32();
                        header.SampleHeaderSize = br.ReadUInt32();
                        header.NameTableSize = br.ReadUInt32();
                        header.SampleDataSize = br.ReadUInt32();
                        header.Codec = br.ReadInt32();
                        br.ReadBytes(4);

                        if (header.Version == 0x01)
                        {
                            header.Flags = br.ReadInt32();
                            br.ReadBytes(24);
                            header.BaseHeaderSize = 0x3c;
                        }
                        else
                        {
                            br.ReadBytes(4);
                            br.ReadBytes(4);
                            br.ReadBytes(16);
                            br.ReadBytes(8);
                            header.BaseHeaderSize = 0x40;
                        }

                        int endIndex = startIndex + (int)(header.BaseHeaderSize + header.SampleHeaderSize + header.NameTableSize + header.SampleDataSize);
                        if (endIndex > fileContent.Length)
                        {
                            endIndex = fileContent.Length;
                        }

                        int length = endIndex - startIndex;
                        byte[] fsb5Data = new byte[length];
                        Array.Copy(fileContent, startIndex, fsb5Data, 0, length);
                        yield return fsb5Data;
                    }
                }
                startIndex += (int)(header.BaseHeaderSize + header.SampleHeaderSize + header.NameTableSize + header.SampleDataSize);
            }
        }

        private static int IndexOf(byte[] source, byte[] pattern, int startIndex)
        {
            for (int i = startIndex; i <= source.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (source[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }

        private static List<string> ExtractFsb5sFromFile(string filePath)
        {
            List<string> extractedFileNames = new List<string>();
            byte[] fileContent = File.ReadAllBytes(filePath);

            int count = 0;
            foreach (byte[] fsb5Data in ExtractFsb5Data(fileContent))
            {
                string baseFilename = Path.GetFileNameWithoutExtension(filePath);
                string extractedFilename = $"{baseFilename}{count}.fsb";
                string? dirName = Path.GetDirectoryName(filePath);
                string extractedPath;
                if (dirName != null)
                {
                    extractedPath = Path.Combine(dirName, extractedFilename);
                }
                else
                {
                    extractedPath = extractedFilename;
                }

                string? dirToCreate = Path.GetDirectoryName(extractedPath);
                if (dirToCreate != null)
                {
                    Directory.CreateDirectory(dirToCreate);
                }
                File.WriteAllBytes(extractedPath, fsb5Data);
                Console.WriteLine($"提取内容另存为: {extractedPath}");

                extractedFileNames.Add(extractedPath);
                count++;
            }
            return extractedFileNames;
        }

        private static bool AreArraysEqual(byte[] arr1, byte[] arr2)
        {
            if (arr1.Length != arr2.Length)
                return false;
            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i])
                    return false;
            }
            return true;
        }

        public override void Extract(string directoryPath)
        {
            List<string> allExtractedFileNames = new List<string>();
            foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(filePath).Equals(".fsb", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                List<string> fileNames = ExtractFsb5sFromFile(filePath);
                allExtractedFileNames.AddRange(fileNames);
            }

            FilesExtracted?.Invoke(this, allExtractedFileNames);
            Console.WriteLine($"总共提取了 {allExtractedFileNames.Count} 个文件。");
        }
    }
}
