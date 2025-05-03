using System;
using System.Collections.Generic;
using System.IO;

namespace Extractor.Extractor
{
    public class RifxExtractor : BaseExtractor
    {
        private static readonly byte[] RIFXHeader = { 0x52, 0x49, 0x46, 0x58 };
        private static readonly byte[] wemBlock = { 0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74 };

        public event EventHandler<List<string>>? FilesExtracted;

        private static IEnumerable<byte[]> ExtractwemData(byte[] fileContent)
        {
            int wemDataStart = 0;
            while ((wemDataStart = IndexOf(fileContent, RIFXHeader, wemDataStart)) != -1)
            {
                int nextRifxIndex = IndexOf(fileContent, RIFXHeader, wemDataStart + 1);
                int endIndex;
                if (nextRifxIndex != -1)
                {
                    endIndex = nextRifxIndex;
                }
                else
                {
                    endIndex = fileContent.Length;
                }

                int blockStart = wemDataStart + 8;
                bool haswemBlock = IndexOf(fileContent, wemBlock, blockStart) != -1;

                if (haswemBlock)
                {
                    int length = endIndex - wemDataStart;
                    byte[] wemData = new byte[length];
                    Array.Copy(fileContent, wemDataStart, wemData, 0, length);
                    yield return wemData;
                }

                wemDataStart = endIndex;
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

        private static List<string> ExtractwemsFromFile(string filePath)
        {
            List<string> extractedFileNames = new List<string>();
            byte[] fileContent = File.ReadAllBytes(filePath);

            int count = 0;
            foreach (byte[] wemData in ExtractwemData(fileContent))
            {
                string baseFilename = Path.GetFileNameWithoutExtension(filePath);
                string extractedFilename = $"{baseFilename}{count}.wem";
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
                File.WriteAllBytes(extractedPath, wemData);
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
                if (Path.GetExtension(filePath).Equals(".wem", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                List<string> fileNames = ExtractwemsFromFile(filePath);
                allExtractedFileNames.AddRange(fileNames);
            }

            FilesExtracted?.Invoke(this, allExtractedFileNames);
            Console.WriteLine($"总共提取了 {allExtractedFileNames.Count} 个文件。");
        }
    }
}