using System;
using System.Collections.Generic;
using System.IO;

namespace supertoolbox.Extractor
{
    public class BankExtractor : BaseExtractor
    {
        private static readonly byte[] riffHeader = { 0x52, 0x49, 0x46, 0x46 };
        private static readonly byte[] bankBlock = { 0x46, 0x45, 0x56, 0x20, 0x46, 0x4D, 0x54 };

        public event EventHandler<List<string>>? FilesExtracted;

        private static List<byte[]> ExtractbankData(byte[] fileContent)
        {
            List<byte[]> bankDataList = new List<byte[]>();
            int bankDataStart = 0;
            while ((bankDataStart = IndexOf(fileContent, riffHeader, bankDataStart)) != -1)
            {
                try
                {
                    int fileSize = BitConverter.ToInt32(fileContent, bankDataStart + 4);
                    fileSize = (fileSize + 1) & ~1;

                    int blockStart = bankDataStart + 8;
                    bool hasbankBlock = IndexOf(fileContent, bankBlock, blockStart) != -1;

                    if (hasbankBlock)
                    {
                        byte[] bankData = new byte[fileSize + 8];
                        Array.Copy(fileContent, bankDataStart, bankData, 0, fileSize + 8);
                        bankDataList.Add(bankData);
                    }

                    bankDataStart += fileSize + 8;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"提取bank数据时出错: {ex.Message}");
                }
            }
            return bankDataList;
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

        private static List<string> ExtractbanksFromFile(string filePath, ref int fileCount)
        {
            List<string> extractedFileNames = new List<string>();
            try
            {
                byte[] fileContent = File.ReadAllBytes(filePath);
                List<byte[]> bankDataList = ExtractbankData(fileContent);
                int count = 0;
                foreach (byte[] bankData in bankDataList)
                {
                    string baseFilename = Path.GetFileNameWithoutExtension(filePath);
                    string extractedFilename = $"{baseFilename}{count}.bank";
                    string? dirName = Path.GetDirectoryName(filePath);
                    string extractedPath;
                    if (dirName != null)
                    {
                        extractedPath = Path.Combine(dirName, extractedFilename);
                    }
                    else
                    {
                        extractedPath = Path.Combine(Directory.GetCurrentDirectory(), extractedFilename);
                    }

                    string? dirToCreate = Path.GetDirectoryName(extractedPath);
                    if (dirToCreate != null)
                    {
                        Directory.CreateDirectory(dirToCreate);
                    }
                    File.WriteAllBytes(extractedPath, bankData);
                    Console.WriteLine($"提取内容另存为: {extractedPath}");
                    extractedFileNames.Add(extractedPath);
                    fileCount++;
                    count++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误处理文件{filePath}: {ex.Message}");
            }
            return extractedFileNames;
        }

        public override void Extract(string directoryPath)
        {
            List<string> allExtractedFileNames = new List<string>();
            int fileCount = 0;
            try
            {
                foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
                {
                    if (Path.GetExtension(filePath).Equals(".bank", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    List<string> fileNames = ExtractbanksFromFile(filePath, ref fileCount);
                    allExtractedFileNames.AddRange(fileNames);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误处理目录{directoryPath}: {ex.Message}");
            }

            FilesExtracted?.Invoke(this, allExtractedFileNames);
            Console.WriteLine($"总共提取了 {fileCount} 个文件。");
        }
    }
}
