using System;
using System.Collections.Generic;
using System.IO;

namespace Extractor.Extractor
{
    public class WebpExtractor : BaseExtractor
    {
        private static readonly byte[] riffHeader = { 0x52, 0x49, 0x46, 0x46 };
        private static readonly byte[] webpBlock = { 0x57, 0x45, 0x42, 0x50, 0x56, 0x50, 0x38 };

        public event EventHandler<List<string>>? FilesExtracted;

        private static IEnumerable<byte[]> ExtractWebpData(byte[] fileContent)
        {
            int webpDataStart = 0;
            while ((webpDataStart = IndexOf(fileContent, riffHeader, webpDataStart)) != -1)
            {
                int fileSize = BitConverter.ToInt32(fileContent, webpDataStart + 4);
                fileSize = (fileSize + 1) & ~1;

                int blockStart = webpDataStart + 8;
                bool hasWebpBlock = IndexOf(fileContent, webpBlock, blockStart) != -1;

                if (hasWebpBlock)
                {
                    byte[] webpData = new byte[fileSize + 8];
                    Array.Copy(fileContent, webpDataStart, webpData, 0, fileSize + 8);
                    yield return webpData;
                }

                webpDataStart += fileSize + 8;
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

        private static List<string> ExtractWebpsFromFile(string filePath, ref int fileCount)
        {
            List<string> extractedFileNames = new List<string>();
            byte[] fileContent = File.ReadAllBytes(filePath);
            int count = 0;
            foreach (byte[] webpData in ExtractWebpData(fileContent))
            {
                string baseFilename = Path.GetFileNameWithoutExtension(filePath);
                string extractedFilename = $"{baseFilename}{count}.webp";
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
                File.WriteAllBytes(extractedPath, webpData);
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
                if (Path.GetExtension(filePath).Equals(".webp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                List<string> fileNames = ExtractWebpsFromFile(filePath, ref fileCount);
                allExtractedFileNames.AddRange(fileNames);
            }

            FilesExtracted?.Invoke(this, allExtractedFileNames);
            Console.WriteLine($"总共提取了 {fileCount} 个文件。");
        }
    }
}