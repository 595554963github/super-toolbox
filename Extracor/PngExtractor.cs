using System;
using System.Collections.Generic;
using System.IO;

namespace supertoolbox.Extractor
{
    public class PngExtractor : BaseExtractor
    {
        private static readonly byte[] PNG_START_HEADER = { 0x89, 0x50, 0x4E, 0x47 };
        private static readonly byte[] PNG_END_HEADER = { 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };

        public event EventHandler<List<string>>? FilesExtracted;

        private static IEnumerable<byte[]> ExtractPngData(byte[] fileContent)
        {
            int startIndex = 0;
            while ((startIndex = IndexOf(fileContent, PNG_START_HEADER, startIndex)) != -1)
            {
                int endIndex = IndexOf(fileContent, PNG_END_HEADER, startIndex + PNG_START_HEADER.Length);
                if (endIndex != -1)
                {
                    endIndex += PNG_END_HEADER.Length;
                    int length = endIndex - startIndex;
                    byte[] pngData = new byte[length];
                    Array.Copy(fileContent, startIndex, pngData, 0, length);
                    yield return pngData;
                    startIndex = endIndex;
                }
                else
                {
                    break;
                }
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

        private static List<string> ExtractPngsFromFile(string filePath)
        {
            List<string> extractedFileNames = new List<string>();
            byte[] fileContent = File.ReadAllBytes(filePath);

            int count = 0;
            foreach (byte[] pngData in ExtractPngData(fileContent))
            {
                string baseFilename = Path.GetFileNameWithoutExtension(filePath);
                string extractedFilename = $"{baseFilename}{count}.png";
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
                File.WriteAllBytes(extractedPath, pngData);
                Console.WriteLine($"提取内容另存为: {extractedPath}");
                extractedFileNames.Add(extractedPath);
                count++;
            }
            return extractedFileNames;
        }

        public override void Extract(string directoryPath)
        {
            List<string> allExtractedFileNames = new List<string>();
            foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(filePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                List<string> fileNames = ExtractPngsFromFile(filePath);
                allExtractedFileNames.AddRange(fileNames);
            }

            FilesExtracted?.Invoke(this, allExtractedFileNames);
            Console.WriteLine($"总共提取了 {allExtractedFileNames.Count} 个文件。");
        }
    }
}
