using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace super_toolbox
{
    public class Kvs_Kns_Extractor : BaseExtractor
    {
        public event EventHandler<List<string>>? FilesExtracted;
        public event EventHandler<string>? ExtractionStarted;
        public event EventHandler<string>? ExtractionProgress;
        public event EventHandler<string>? ExtractionError;
        public new event EventHandler<string>? ExtractionCompleted;

        private static readonly byte[] KVS_SIG_BYTES = { 0x4B, 0x4F, 0x56, 0x53 }; // Steam平台
        private static readonly byte[] KNS_SIG_BYTES = { 0x4B, 0x54, 0x53, 0x53 }; // Switch平台
        private static readonly byte[] AT3_SIG_BYTES = { 0x52, 0x49, 0x46, 0x46 }; // PS4平台(AT3)
        private static readonly byte[] KTAC_SIG_BYTES = { 0x4B, 0x54, 0x41, 0x43 }; // PS4平台(KTAC)

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

        public override void Extract(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                ExtractionError?.Invoke(this, $"源文件夹 {directoryPath} 不存在");
                return;
            }

            string extractedDir = Path.Combine(directoryPath, "Extracted");
            Directory.CreateDirectory(extractedDir);

            ExtractionStarted?.Invoke(this, $"开始处理目录: {directoryPath}");

            var filePaths = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).ToList();
            TotalFilesToExtract = filePaths.Count;

            var extractedFiles = ProcessFiles(filePaths, extractedDir);

            FilesExtracted?.Invoke(this, extractedFiles);
            ExtractionCompleted?.Invoke(this, $"处理完成，共提取出 {extractedFiles.Count} 个文件");
            OnExtractionCompleted();
        }

        public override async Task ExtractAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(directoryPath))
            {
                ExtractionError?.Invoke(this, $"源文件夹 {directoryPath} 不存在");
                OnExtractionFailed($"源文件夹 {directoryPath} 不存在");
                return;
            }

            string extractedDir = Path.Combine(directoryPath, "Extracted");
            Directory.CreateDirectory(extractedDir);

            ExtractionStarted?.Invoke(this, $"开始处理目录: {directoryPath}");
            OnFileExtracted($"开始处理目录: {directoryPath}");

            var filePaths = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).ToList();
            TotalFilesToExtract = filePaths.Count;

            var extractedFiles = await ProcessFilesAsync(filePaths, extractedDir, cancellationToken);

            FilesExtracted?.Invoke(this, extractedFiles);
            ExtractionCompleted?.Invoke(this, $"处理完成，共提取出 {extractedFiles.Count} 个文件");
            OnExtractionCompleted();
        }

        private List<string> ProcessFiles(List<string> filePaths, string extractedDir)
        {
            var extractedFiles = new List<string>();
            int processed = 0;

            foreach (var filePath in filePaths)
            {
                processed++;
                string fileName = Path.GetFileName(filePath);
                ExtractionProgress?.Invoke(this, $"处理中 [{processed}/{filePaths.Count}]: {fileName}");
                OnFileExtracted(filePath);

                try
                {
                    byte[] content = File.ReadAllBytes(filePath);
                    extractedFiles.AddRange(ProcessFileContent(content, filePath, extractedDir));
                }
                catch (Exception ex)
                {
                    ExtractionError?.Invoke(this, $"处理 {fileName} 失败: {ex.Message}");
                }
            }

            return extractedFiles;
        }

        private async Task<List<string>> ProcessFilesAsync(List<string> filePaths, string extractedDir, CancellationToken cancellationToken)
        {
            var extractedFiles = new List<string>();
            int processed = 0;

            foreach (var filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processed++;
                string fileName = Path.GetFileName(filePath);
                ExtractionProgress?.Invoke(this, $"处理中 [{processed}/{filePaths.Count}]: {fileName}");
                OnFileExtracted(filePath);

                try
                {
                    byte[] content = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    extractedFiles.AddRange(ProcessFileContent(content, filePath, extractedDir));
                }
                catch (OperationCanceledException)
                {
                    ExtractionError?.Invoke(this, "提取操作已取消");
                    OnExtractionFailed("提取操作已取消");
                    throw;
                }
                catch (Exception ex)
                {
                    ExtractionError?.Invoke(this, $"处理 {fileName} 失败: {ex.Message}");
                    OnExtractionFailed($"处理 {fileName} 失败: {ex.Message}");
                }
            }

            return extractedFiles;
        }

        private IEnumerable<string> ProcessFileContent(byte[] content, string sourcePath, string outputDir)
        {
            string baseName = Path.GetFileNameWithoutExtension(sourcePath);
            var allExtracted = new List<string>();

            allExtracted.AddRange(ExtractFormat(content, baseName, outputDir, KVS_SIG_BYTES, ".kvs"));
            allExtracted.AddRange(ExtractFormat(content, baseName, outputDir, KNS_SIG_BYTES, ".kns"));
            allExtracted.AddRange(ExtractFormat(content, baseName, outputDir, AT3_SIG_BYTES, ".at3"));
            allExtracted.AddRange(ExtractFormat(content, baseName, outputDir, KTAC_SIG_BYTES, ".ktac"));

            return allExtracted;
        }

        private IEnumerable<string> ExtractFormat(byte[] content, string baseName, string outputDir, byte[] sigBytes, string ext)
        {
            var extracted = new List<string>();
            int index = 0;
            int count = 0;

            while (index < content.Length)
            {
                int start = IndexOf(content, sigBytes, index);
                if (start == -1) break;

                int end = IndexOf(content, sigBytes, start + 1);
                if (end == -1) end = content.Length;

                count++;
                string fileName = count == 1 ? $"{baseName}{ext}" : $"{baseName}_{count}{ext}";
                string outputPath = Path.Combine(outputDir, fileName);

                try
                {
                    File.WriteAllBytes(outputPath, content[start..end]);
                    extracted.Add(outputPath);

                }
                catch (Exception ex)
                {
                    ExtractionError?.Invoke(this, $"保存 {fileName} 失败: {ex.Message}");
                }

                index = end;
            }

            return extracted;
        }
    }
}