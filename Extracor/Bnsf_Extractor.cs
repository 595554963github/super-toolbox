using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace super_toolbox
{
    public class Bnsf_Extractor : BaseExtractor
    {
        private static readonly byte[] START_SEQ = { 0x42, 0x4E, 0x53, 0x46 };
        private const int BUFFER_SIZE = 4096;

        public override async Task ExtractAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            string extractedDir = Path.Combine(directoryPath, "extracted");
            Directory.CreateDirectory(extractedDir);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var files = Directory.EnumerateFiles(directoryPath, "*.TLDAT", SearchOption.AllDirectories)
               .Where(file => !file.StartsWith(extractedDir, StringComparison.OrdinalIgnoreCase))
               .ToList();

            var extractedFiles = new ConcurrentBag<string>();

            try
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, filePath =>
                    {
                        try
                        {
                            ExtractBNSFType(filePath, extractedDir, START_SEQ, extractedFiles);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"处理文件 {filePath} 时发生错误: {ex.Message}");
                        }
                    });
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("提取操作已取消。");
            }

            sw.Stop();

            int actualExtractedCount = Directory.EnumerateFiles(extractedDir, "*.bnsf", SearchOption.AllDirectories).Count();
            Console.WriteLine($"处理完成，耗时 {sw.Elapsed.TotalSeconds:F2} 秒");
            Console.WriteLine($"共提取出 {actualExtractedCount} 个BNSF文件，统计提取文件数量: {ExtractedFileCount}");
            if (ExtractedFileCount != actualExtractedCount)
            {
                Console.WriteLine("警告: 统计数量与实际数量不符，可能存在文件操作异常。");
            }
        }

        private void ExtractBNSFType(string filePath, string extractedDir,
            byte[] startSequence, ConcurrentBag<string> extractedFiles)
        {
            int count = 0;

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                long currentPosition = 0;
                byte[] buffer = new byte[BUFFER_SIZE];

                while (currentPosition < fileStream.Length)
                {
                    fileStream.Seek(currentPosition, SeekOrigin.Begin);
                    int bytesRead = fileStream.Read(buffer, 0, BUFFER_SIZE);

                    for (int i = 0; i < bytesRead; i++)
                    {
                        if (IsStartSequence(buffer, i, startSequence))
                        {
                            long start = currentPosition + i;
                            long end = FindNextStartSequence(fileStream, start, startSequence);

                            if (end - start >= 16)
                            {
                                SaveExtractedData(extractedDir, filePath, count, start, end, fileStream, extractedFiles);
                                count++;
                            }
                        }
                    }

                    currentPosition += bytesRead;
                }
            }
        }

        private bool IsStartSequence(byte[] buffer, int index, byte[] startSequence)
        {
            if (index + startSequence.Length > buffer.Length)
            {
                return false;
            }
            for (int i = 0; i < startSequence.Length; i++)
            {
                if (buffer[index + i] != startSequence[i])
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsValidBNSF(byte[] buffer, int index)
        {
            return true; 
        }

        private static bool StartsWith(byte[] data, byte[] pattern, int startIndex)
        {
            if (startIndex + pattern.Length > data.Length)
            {
                return false;
            }
            for (int i = 0; i < pattern.Length; i++)
            {
                if (data[startIndex + i] != pattern[i])
                {
                    return false;
                }
            }
            return true;
        }

        private long FindNextStartSequence(FileStream fileStream, long start, byte[] startSequence)
        {
            long currentPosition = start + startSequence.Length;
            fileStream.Seek(currentPosition, SeekOrigin.Begin);
            byte[] buffer = new byte[BUFFER_SIZE];

            while (currentPosition < fileStream.Length)
            {
                int bytesRead = fileStream.Read(buffer, 0, BUFFER_SIZE);

                for (int i = 0; i < bytesRead; i++)
                {
                    if (IsStartSequence(buffer, i, startSequence))
                    {
                        return currentPosition + i;
                    }
                }

                currentPosition += bytesRead;
            }

            return fileStream.Length;
        }

        private void SaveExtractedData(string extractedDir, string filePath, int count, long start, long end,
            FileStream fileStream, ConcurrentBag<string> extractedFiles)
        {
            string outputFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{count}.bnsf";
            string outputFilePath = Path.Combine(extractedDir, outputFileName);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);
                using (FileStream outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Seek(start, SeekOrigin.Begin);
                    long length = end - start;
                    byte[] tempBuffer = new byte[BUFFER_SIZE];

                    while (length > 0)
                    {
                        int bytesToRead = (int)Math.Min(length, BUFFER_SIZE);
                        int bytesRead = fileStream.Read(tempBuffer, 0, bytesToRead);
                        outputStream.Write(tempBuffer, 0, bytesRead);
                        length -= bytesRead;
                    }
                }

                extractedFiles.Add(outputFilePath);
                OnFileExtracted(outputFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法写入文件 {outputFilePath}，错误信息: {ex.Message}");
            }
        }
    }
}