using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace supertoolbox.Extractor
{
    public class PngExtractor : BaseExtractor
    {
        private readonly object _lockObject = new object();

        public new event EventHandler<string>? FileExtracted;
        public event EventHandler<string>? ExtractionProgress;
        public new event EventHandler<int>? ExtractionCompleted;

        private static readonly byte[] PNG_START_HEADER = { 0x89, 0x50, 0x4E, 0x47 };
        private static readonly byte[] PNG_END_HEADER = { 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82 };

        public override void Extract(string directoryPath)
        {
            ExtractAsync(directoryPath).Wait();
        }

        public override async Task ExtractAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(directoryPath))
            {
                ExtractionProgress?.Invoke(this, $"错误: {directoryPath} 不是有效的目录");
                OnExtractionFailed($"错误: {directoryPath} 不是有效的目录");
                return;
            }

            var files = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).ToList();
            TotalFilesToExtract = files.Count;
            ExtractionProgress?.Invoke(this, $"开始处理 {files.Count} 个文件...");
            OnFileExtracted($"开始处理 {files.Count} 个文件...");

            int processedFiles = 0;

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ProcessFileAsync(file, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    ExtractionProgress?.Invoke(this, "提取操作已取消");
                    OnExtractionFailed("提取操作已取消");
                    throw;
                }
                catch (Exception ex)
                {
                    ExtractionProgress?.Invoke(this, $"处理文件 {file} 时出错: {ex.Message}");
                    OnExtractionFailed($"处理文件 {file} 时出错: {ex.Message}");
                }
                finally
                {
                    processedFiles++;
                    OnFileExtracted(file);
                }
            }

            ExtractionCompleted?.Invoke(this, ExtractedFileCount);
            OnExtractionCompleted();
            ExtractionProgress?.Invoke(this, $"提取完成: 共找到 {ExtractedFileCount} 个PNG文件");
        }

        private async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken)
        {
            try
            {
                if (Path.GetExtension(filePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                byte[] fileContent = await File.ReadAllBytesAsync(filePath, cancellationToken);
                var extractedFiles = new List<string>();

                foreach (byte[] pngData in ExtractPngData(fileContent))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SavePngFile(pngData, filePath, extractedFiles);
                }

                if (extractedFiles.Count > 0)
                {
                    foreach (var extractedFile in extractedFiles)
                    {
                        FileExtracted?.Invoke(this, extractedFile);
                        base.OnFileExtracted(extractedFile);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ExtractionProgress?.Invoke(this, $"处理文件 {filePath} 时出错: {ex.Message}");
            }
        }

        private void SavePngFile(byte[] pngData, string sourceFilePath, List<string> extractedFiles)
        {
            string baseFilename = Path.GetFileNameWithoutExtension(sourceFilePath);
            string dirName = Path.GetDirectoryName(sourceFilePath) ?? Directory.GetCurrentDirectory();

            int currentCount = Interlocked.Increment(ref _extractedFileCount);
            string extractedFilename = $"{baseFilename}_{currentCount}.png";
            string extractedPath = Path.Combine(dirName, extractedFilename);

            try
            {
                string? dirToCreate = Path.GetDirectoryName(extractedPath);
                if (!string.IsNullOrEmpty(dirToCreate) && !Directory.Exists(dirToCreate))
                {
                    Directory.CreateDirectory(dirToCreate);
                }

                File.WriteAllBytes(extractedPath, pngData);
                extractedFiles.Add(extractedPath);

                Interlocked.Exchange(ref _extractedFileCount, _extractedFileCount);
            }
            catch (Exception ex)
            {
                ExtractionProgress?.Invoke(this, $"保存文件 {extractedFilename} 时出错: {ex.Message}");
            }
        }

        private new int _extractedFileCount = 0;

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
    }
}