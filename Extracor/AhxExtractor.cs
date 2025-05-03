using System;
using System.Collections.Generic;
using System.IO;

namespace Extractor.Extractor
{
    public class AhxExtractor : BaseExtractor
    {
        private static readonly byte[] AHX_START_HEADER = { 0x80, 0x00, 0x00, 0x20 };
        private static readonly byte[] AHX_END_HEADER = { 0x80, 0x01, 0x00, 0x0C, 0x41, 0x48, 0x58, 0x45, 0x28, 0x63, 0x29, 0x43, 0x52, 0x49, 0x00, 0x00 };

        public event EventHandler<List<string>>? FilesExtracted;

        private static IEnumerable<byte[]> ExtractAhxData(byte[] fileContent)
        {
            int startIndex = 0;
            while ((startIndex = IndexOf(fileContent, AHX_START_HEADER, startIndex)) != -1)
            {
                int endIndex = IndexOf(fileContent, AHX_END_HEADER, startIndex + AHX_START_HEADER.Length);
                if (endIndex != -1)
                {
                    endIndex += AHX_END_HEADER.Length;
                    int length = endIndex - startIndex;
                    byte[] ahxData = new byte[length];
                    Array.Copy(fileContent, startIndex, ahxData, 0, length);
                    yield return ahxData;
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

        private static List<string> ExtractAhxsFromFile(string filePath)
        {
            List<string> extractedFileNames = new List<string>();
            byte[] fileContent = File.ReadAllBytes(filePath);

            int count = 0;
            foreach (byte[] ahxData in ExtractAhxData(fileContent))
            {
                string baseFilename = Path.GetFileNameWithoutExtension(filePath);
                string extractedFilename = $"{baseFilename}{count}.ahx";
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
                File.WriteAllBytes(extractedPath, ahxData);
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
                if (Path.GetExtension(filePath).Equals(".ahx", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                List<string> fileNames = ExtractAhxsFromFile(filePath);
                allExtractedFileNames.AddRange(fileNames);
            }

            FilesExtracted?.Invoke(this, allExtractedFileNames);
            Console.WriteLine($"总共提取了 {allExtractedFileNames.Count} 个文件。");
        }
    }
}
