using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace super_toolbox
{
    public class LopusExtractor : BaseExtractor
    {
        public event EventHandler<List<string>>? FilesExtracted;
        public event EventHandler<string>? ExtractionStarted;
        public event EventHandler<string>? ExtractionProgress;
        public event EventHandler<string>? ExtractionError;
        public new event EventHandler<string>? ExtractionCompleted;

        private static readonly byte[] OPUS_HEADER = { 0x4F, 0x50, 0x55, 0x53, 0x00, 0x00, 0x00, 0x00 };
        private static readonly byte[] LOPUS_HEADER = { 0x01, 0x00, 0x00, 0x80, 0x18, 0x00, 0x00, 0x00 };

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

        private static List<int> FindOpusPositions(byte[] content)
        {
            List<int> positions = new List<int>();
            int offset = 0;
            while (true)
            {
                offset = IndexOf(content, OPUS_HEADER, offset);
                if (offset == -1)
                {
                    break;
                }
                positions.Add(offset);
                offset++;
            }
            return positions;
        }

        private static int FindLopusHeader(byte[] content)
        {
            return IndexOf(content, LOPUS_HEADER, 0);
        }

        private bool DecryptOpusFile(string file_path)
        {
            try
            {
                byte[] content = File.ReadAllBytes(file_path);
                int lopus_pos = FindLopusHeader(content);
                if (lopus_pos == -1)
                {
                    ExtractionError?.Invoke(this, $"警告: {file_path} 中未找到lopus头");
                    return false;
                }

                byte[] newContent = new byte[content.Length - lopus_pos];
                Array.Copy(content, lopus_pos, newContent, 0, newContent.Length);
                File.WriteAllBytes(file_path, newContent);

                string lopus_file = Path.ChangeExtension(file_path, ".lopus");
                File.Move(file_path, lopus_file);
                ExtractionProgress?.Invoke(this, $"已处理并保存为: {lopus_file}");

                return true;
            }
            catch (Exception e)
            {
                ExtractionError?.Invoke(this, $"处理文件 {file_path} 时出错: {e.Message}");
                return false;
            }
        }

        private void ExtractOpusFiles(string file_path, string output_dir)
        {
            try
            {
                byte[] content = File.ReadAllBytes(file_path);
                List<int> opus_positions = FindOpusPositions(content);
                if (opus_positions.Count == 0)
                {
                    ExtractionError?.Invoke(this, $"{file_path} 中未找到OPUS标记");
                    return;
                }

                string base_name = Path.GetFileNameWithoutExtension(file_path);

                for (int i = 0; i < opus_positions.Count; i++)
                {
                    int pos = opus_positions[i];
                    int end_pos = content.Length;
                    if (i < opus_positions.Count - 1)
                    {
                        end_pos = opus_positions[i + 1];
                    }

                    byte[] extracted_data = new byte[end_pos - pos];
                    Array.Copy(content, pos, extracted_data, 0, extracted_data.Length);
                    string output_file = Path.Combine(output_dir, $"{base_name}_{i}.opus");

                    try
                    {
                        File.WriteAllBytes(output_file, extracted_data);
                        ExtractionProgress?.Invoke(this, $"已提取: {output_file}");

                        if (DecryptOpusFile(output_file))
                        {
                            // Do something if needed
                        }
                    }
                    catch (IOException e)
                    {
                        ExtractionError?.Invoke(this, $"无法写入文件 {output_file}: {e.Message}");
                    }
                }
            }
            catch (IOException e)
            {
                ExtractionError?.Invoke(this, $"无法读取文件 {file_path}: {e.Message}");
            }
        }

        private void ProcessPureOpusDirectory(string directory_path)
        {
            ExtractionProgress?.Invoke(this, $"检测到纯OPUS目录，直接处理所有OPUS文件...");
            string[] opusFiles = Directory.GetFiles(directory_path, "*.opus", SearchOption.AllDirectories);
            foreach (string file in opusFiles)
            {
                ExtractionProgress?.Invoke(this, $"\n处理OPUS文件: {file}");
                DecryptOpusFile(file);
            }
        }

        public override void Extract(string directoryPath)
        {
            List<string> extractedFiles = new List<string>();
            int fileCount = 0;

            if (!Directory.Exists(directoryPath))
            {
                ExtractionError?.Invoke(this, $"源文件夹 {directoryPath} 不存在");
                return;
            }

            ExtractionStarted?.Invoke(this, $"开始处理目录: {directoryPath}");

            var filePaths = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories);
            int totalFiles = 0;
            int processedFiles = 0;

            foreach (var _ in filePaths) totalFiles++;

            TotalFilesToExtract = totalFiles;

            int opusFiles = 0;
            int nonOpusFiles = 0;
            foreach (var filePath in filePaths)
            {
                if (Path.GetExtension(filePath).Equals(".opus", StringComparison.OrdinalIgnoreCase))
                {
                    opusFiles++;
                }
                else if (!Path.GetFileName(filePath).Equals("Extracted"))
                {
                    nonOpusFiles++;
                }
            }

            if (nonOpusFiles == 0 && opusFiles > 0)
            {
                ProcessPureOpusDirectory(directoryPath);
            }
            else
            {
                string output_dir = Path.Combine(directoryPath, "Extracted");
                Directory.CreateDirectory(output_dir);
                ExtractionProgress?.Invoke(this, $"输出目录: {output_dir}");

                foreach (var filePath in filePaths)
                {
                    if (!Path.GetFileName(filePath).Equals("Extracted"))
                    {
                        processedFiles++;
                        ExtractionProgress?.Invoke(this, $"正在处理文件 {processedFiles}/{totalFiles}: {Path.GetFileName(filePath)}");
                        OnFileExtracted(filePath);

                        ExtractOpusFiles(filePath, output_dir);
                    }
                }
            }

            FilesExtracted?.Invoke(this, extractedFiles);
            ExtractionCompleted?.Invoke(this, $"处理完成，共提取出 {fileCount} 个符合条件的文件片段");
            OnExtractionCompleted();
        }

        public override async Task ExtractAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                List<string> extractedFiles = new List<string>();
                int fileCount = 0;

                if (!Directory.Exists(directoryPath))
                {
                    ExtractionError?.Invoke(this, $"源文件夹 {directoryPath} 不存在");
                    OnExtractionFailed($"源文件夹 {directoryPath} 不存在");
                    return;
                }

                ExtractionStarted?.Invoke(this, $"开始处理目录: {directoryPath}");
                OnFileExtracted($"开始处理目录: {directoryPath}");

                var filePaths = Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories);
                int totalFiles = 0;
                int processedFiles = 0;

                foreach (var _ in filePaths) totalFiles++;

                TotalFilesToExtract = totalFiles;

                int opusFiles = 0;
                int nonOpusFiles = 0;
                foreach (var filePath in filePaths)
                {
                    if (Path.GetExtension(filePath).Equals(".opus", StringComparison.OrdinalIgnoreCase))
                    {
                        opusFiles++;
                    }
                    else if (!Path.GetFileName(filePath).Equals("Extracted"))
                    {
                        nonOpusFiles++;
                    }
                }

                if (nonOpusFiles == 0 && opusFiles > 0)
                {
                    ProcessPureOpusDirectory(directoryPath);
                }
                else
                {
                    string output_dir = Path.Combine(directoryPath, "Extracted");
                    Directory.CreateDirectory(output_dir);
                    ExtractionProgress?.Invoke(this, $"输出目录: {output_dir}");

                    foreach (var filePath in filePaths)
                    {
                        ThrowIfCancellationRequested(cancellationToken);

                        if (!Path.GetFileName(filePath).Equals("Extracted"))
                        {
                            processedFiles++;
                            ExtractionProgress?.Invoke(this, $"正在处理文件 {processedFiles}/{totalFiles}: {Path.GetFileName(filePath)}");
                            OnFileExtracted(filePath);

                            try
                            {
                                ExtractOpusFiles(filePath, output_dir);
                            }
                            catch (OperationCanceledException)
                            {
                                ExtractionError?.Invoke(this, "提取操作已取消");
                                OnExtractionFailed("提取操作已取消");
                                throw;
                            }
                            catch (Exception e)
                            {
                                ExtractionError?.Invoke(this, $"处理文件 {filePath} 时出错: {e.Message}");
                                OnExtractionFailed($"处理文件 {filePath} 时出错: {e.Message}");
                            }
                        }
                    }
                }

                FilesExtracted?.Invoke(this, extractedFiles);
                ExtractionCompleted?.Invoke(this, $"处理完成，共提取出 {fileCount} 个符合条件的文件片段");
                OnExtractionCompleted();
            }, cancellationToken);
        }
    }
}