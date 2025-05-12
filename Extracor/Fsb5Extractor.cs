using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace supertoolbox.Extractor
{
    public class Fsb5Extractor : BaseExtractor
    {
        private static readonly byte[] FSB5_MAGIC = { 0x46, 0x53, 0x42, 0x35 }; // "FSB5"
        private static readonly object consoleLock = new object();

        public event EventHandler<List<string>>? FilesExtracted;

        public override async Task ExtractAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            await Task.Run(() => Extract(directoryPath), cancellationToken);
        }

        public override void Extract(string directoryPath)
        {
            List<string> allExtractedFiles = new List<string>();
            var outputQueue = new BlockingCollection<string>();

            var outputThread = new Thread(() =>
            {
                foreach (var message in outputQueue.GetConsumingEnumerable())
                {
                    lock (consoleLock)
                    {
                        Console.WriteLine(message);
                    }
                }
            })
            { IsBackground = true };
            outputThread.Start();

            try
            {
                var filesToProcess = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".fsb", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                outputQueue.Add($"在目录 {directoryPath} 中找到 {filesToProcess.Count} 个待处理文件");

                Parallel.ForEach(filesToProcess, filePath =>
                {
                    try
                    {
                        var extractedCount = 0;
                        var fileData = File.ReadAllBytes(filePath);
                        var position = 0;

                        while ((position = FindFsb5Position(fileData, position)) >= 0)
                        {
                            var nextPosition = FindFsb5Position(fileData, position + 4);
                            var fsbLength = (nextPosition > 0 ? nextPosition : fileData.Length) - position;

                            var fsbData = new byte[fsbLength];
                            Array.Copy(fileData, position, fsbData, 0, fsbLength);

                            var outputPath = Path.Combine(
                                Path.GetDirectoryName(filePath) ?? directoryPath,
                                $"{Path.GetFileNameWithoutExtension(filePath)}_{extractedCount}.fsb");

                            File.WriteAllBytes(outputPath, fsbData);

                            outputQueue.Add($"已提取: {outputPath}");
                            lock (allExtractedFiles)
                            {
                                allExtractedFiles.Add(outputPath);
                            }

                            extractedCount++;
                            position += fsbLength;
                        }

                        if (extractedCount > 0)
                        {
                            outputQueue.Add($"从 {Path.GetFileName(filePath)} 中提取了 {extractedCount} 个FSB文件");
                        }
                    }
                    catch (Exception ex)
                    {
                        outputQueue.Add($"处理 {filePath} 时出错: {ex.Message}");
                    }
                });
            }
            finally
            {
                outputQueue.CompleteAdding();
                outputThread.Join(1000);
                FilesExtracted?.Invoke(this, allExtractedFiles);
            }
        }

        private static int FindFsb5Position(byte[] data, int startIndex)
        {
            for (int i = startIndex; i <= data.Length - 4; i++)
            {
                if (data[i] == FSB5_MAGIC[0] &&
                    data[i + 1] == FSB5_MAGIC[1] &&
                    data[i + 2] == FSB5_MAGIC[2] &&
                    data[i + 3] == FSB5_MAGIC[3])
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
