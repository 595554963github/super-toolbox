using System;
using System.Collections.Generic;
using System.IO;

namespace supertoolbox.Extractor
{
    public class XwmaExtractor : BaseExtractor
    {
        private static readonly byte[] riffHeader = { 0x52, 0x49, 0x46, 0x46 };
        private static readonly byte[] xwmaBlock = { 0x58, 0x57, 0x4D, 0x41, 0x66, 0x6D, 0x74 };

        public event EventHandler<List<string>>? FilesExtracted;

        private static IEnumerable<byte[]> ExtractxwmaData(byte[] fileContent)
        {
            int xwmaDataStart = 0;
            while ((xwmaDataStart = IndexOf(fileContent, riffHeader, xwmaDataStart)) != -1)
            {
                int fileSize = BitConverter.ToInt32(fileContent, xwmaDataStart + 4);
                fileSize = (fileSize + 1) & ~1;

                int blockStart = xwmaDataStart + 8;
                bool hasxwmaBlock = IndexOf(fileContent, xwmaBlock, blockStart) != -1;

                if (hasxwmaBlock)
                {
                    byte[] xwmaData = new byte[fileSize + 8];
                    Array.Copy(fileContent, xwmaDataStart, xwmaData, 0, fileSize + 8);
                    yield return xwmaData;
                }

                xwmaDataStart += fileSize + 8;
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

        private static List<string> ExtractxwmasFromFile(string filePath, ref int fileCount)
        {
            List<string> extractedFileNames = new List<string>();
            byte[] fileContent = File.ReadAllBytes(filePath);
            int count = 0;
            foreach (byte[] xwmaData in ExtractxwmaData(fileContent))
            {
                string baseFilename = Path.GetFileNameWithoutExtension(filePath);
                string extractedFilename = $"{baseFilename}{count}.xwma";
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
                File.WriteAllBytes(extractedPath, xwmaData);
                Console.WriteLine($"提取的文件: {extractedPath}");
                extractedFileNames.Add(extractedPath);
                fileCount++;
                count++;
            }
            return extractedFileNames;
        }

        public override void Extract(string directoryPath)
        {
            List<string> allExtractedFileNames = new List<string>();
            int fileCount = 0;
            foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(filePath).Equals(".xwma", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                List<string> fileNames = ExtractxwmasFromFile(filePath, ref fileCount);
                allExtractedFileNames.AddRange(fileNames);
            }

            FilesExtracted?.Invoke(this, allExtractedFileNames);
            Console.WriteLine($"总共提取了 {fileCount} 个文件。");
        }
    }
}
