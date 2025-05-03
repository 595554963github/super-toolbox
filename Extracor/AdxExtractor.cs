using System;
using System.Collections.Generic;
using System.IO;

namespace Extractor.Extractor
{
    public class AdxExtractor : BaseExtractor
    {
        public event EventHandler<List<string>>? FilesExtracted;

        private static readonly byte[] ADX_SIG_BYTES = { 0x80, 0x00 };
        private static readonly byte[] CRI_COPYRIGHT_BYTES = { 0x28, 0x63, 0x29, 0x43, 0x52, 0x49 };
        private static readonly byte[][] FIXED_SEQUENCES =
        {
            new byte[] { 0x03, 0x12, 0x04, 0x01, 0x00, 0x00 },
            new byte[] { 0x03, 0x12, 0x04, 0x02, 0x00, 0x00 }
        };

        private static int IndexOf(byte[] data, byte[] pattern, int startIndex)
        {
            for (int i = startIndex; i <= data.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
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

        private static bool ContainsBytes(byte[] data, byte[] pattern)
        {
            return IndexOf(data, pattern, 0) != -1;
        }

        public override void Extract(string directoryPath)
        {
            List<string> extractedFiles = new List<string>();
            int fileCount = 0;

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"源文件夹 {directoryPath} 不存在");
                return;
            }

            foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                try
                {
                    byte[] content = File.ReadAllBytes(filePath);
                    int index = 0;
                    int? currentHeaderStart = null;
                    int innerCount = 1;

                    while (index < content.Length)
                    {
                        int headerStartIndex = IndexOf(content, ADX_SIG_BYTES, index);
                        if (headerStartIndex == -1)
                        {
                            if (currentHeaderStart.HasValue)
                            {
                                SaveExtractedFile(content, currentHeaderStart.Value, content.Length, filePath, innerCount, ref fileCount, extractedFiles);
                                innerCount++;
                            }
                            break;
                        }

                        int checkLength = Math.Min(10, content.Length - headerStartIndex);
                        var checkSegment = new byte[checkLength];
                        Array.Copy(content, headerStartIndex, checkSegment, 0, checkLength);

                        if (ContainsBytes(checkSegment, FIXED_SEQUENCES[0]) ||
                            ContainsBytes(checkSegment, FIXED_SEQUENCES[1]))
                        {
                            int nextHeaderIndex = IndexOf(content, ADX_SIG_BYTES, headerStartIndex + 1);
                            if (!currentHeaderStart.HasValue)
                            {
                                currentHeaderStart = headerStartIndex;
                            }
                            else
                            {
                                SaveExtractedFile(content, currentHeaderStart.Value, headerStartIndex, filePath, innerCount, ref fileCount, extractedFiles);
                                innerCount++;
                                currentHeaderStart = headerStartIndex;
                            }
                        }

                        index = headerStartIndex + 1;
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine($"读取文件 {filePath} 时出错: {e.Message}");
                }
            }

            FilesExtracted?.Invoke(this, extractedFiles);
            Console.WriteLine($"共提取出 {fileCount} 个符合条件的文件片段。");
        }

        private void SaveExtractedFile(byte[] content, int start, int end, string filePath, int innerCount, ref int fileCount, List<string> extractedFiles)
        {
            int length = end - start;
            byte[] searchRange = new byte[length];
            Array.Copy(content, start, searchRange, 0, length);

            if (ContainsBytes(searchRange, CRI_COPYRIGHT_BYTES))
            {
                string baseFileName = Path.GetFileNameWithoutExtension(filePath);
                string outputFileName;
                if (innerCount == 1)
                {
                    outputFileName = $"{baseFileName}.adx";
                }
                else
                {
                    outputFileName = $"{baseFileName}_{innerCount}.adx";
                }
                string outputFilePath = Path.Combine(Path.GetDirectoryName(filePath)!, outputFileName);

                try
                {
                    File.WriteAllBytes(outputFilePath, searchRange);
                    Console.WriteLine($"已提取文件: {outputFilePath}");
                    extractedFiles.Add(outputFilePath);
                    fileCount++;
                }
                catch (IOException e)
                {
                    Console.WriteLine($"写入文件 {outputFilePath} 时出错: {e.Message}");
                }
            }
        }
    }
}