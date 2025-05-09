using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace supertoolbox.Extractor
{
    public class WaveExtractor : BaseExtractor
    {
        private static readonly byte[] riffHeader = { 0x52, 0x49, 0x46, 0x46 };
        private static readonly byte[] audioBlock = { 0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74 };

        public event EventHandler<List<string>>? FilesExtracted;

        public override void Extract(string directoryPath)
        {
            List<string> allExtractedFileNames = new List<string>();
            string targetExtension = "temp";
            int fileCount = 0;

            foreach (string filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(filePath).Equals($".{targetExtension}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                List<string> fileNames = ExtractFromFile(filePath, targetExtension, ref fileCount);
                allExtractedFileNames.AddRange(fileNames);
            }

            FilesExtracted?.Invoke(this, allExtractedFileNames);
            Console.WriteLine($"总共提取了 {fileCount} 个文件。");
        }       
        private static List<string> ExtractFromFile(string filePath, string targetExtension, ref int fileCount)
        {
            List<string> extractedFileNames = new List<string>();
            byte[] fileContent = File.ReadAllBytes(filePath);
            int count = 0;
            foreach (byte[] waveData in ExtractWaveData(fileContent))
            {
                string baseFilename = Path.GetFileNameWithoutExtension(filePath);
                string tempExtractedFilename = $"{baseFilename}{count}.{targetExtension}";
                string? dirName = Path.GetDirectoryName(filePath);
                string tempExtractedPath;
                if (dirName != null)
                {
                    tempExtractedPath = Path.Combine(dirName, tempExtractedFilename);
                }
                else
                {
                    tempExtractedPath = tempExtractedFilename;
                }

                string? dirToCreate = Path.GetDirectoryName(tempExtractedPath);
                if (dirToCreate != null)
                {
                    Directory.CreateDirectory(dirToCreate);
                }
                File.WriteAllBytes(tempExtractedPath, waveData);

                string detectedExtension = AnalyzeAudioFormat(tempExtractedPath);
                string finalExtractedFilename = $"{baseFilename}_{count}.{detectedExtension}";
                string finalExtractedPath;
                if (dirName != null)
                {
                    finalExtractedPath = Path.Combine(dirName, finalExtractedFilename);
                }
                else
                {
                    finalExtractedPath = finalExtractedFilename;
                }

                File.Move(tempExtractedPath, finalExtractedPath);
                extractedFileNames.Add(finalExtractedPath);
                Console.WriteLine($"提取的文件: {finalExtractedPath}");
                fileCount++;
                count++;
            }
            return extractedFileNames;
        }

        private static IEnumerable<byte[]> ExtractWaveData(byte[] fileContent)
        {
            int waveDataStart = 0;
            while ((waveDataStart = IndexOf(fileContent, riffHeader, waveDataStart)) != -1)
            {
                int fileSize = BitConverter.ToInt32(fileContent, waveDataStart + 4);
                fileSize = (fileSize + 1) & ~1;

                int blockStart = waveDataStart + 8;
                bool hasAudioBlock = IndexOf(fileContent, audioBlock, blockStart) != -1;

                if (hasAudioBlock)
                {
                    byte[] waveData = new byte[fileSize + 8];
                    Array.Copy(fileContent, waveDataStart, waveData, 0, fileSize + 8);
                    yield return waveData;
                }

                waveDataStart += fileSize + 8;
            }
        }

        private static int IndexOf(byte[] source, byte[] pattern, int startIndex)
        {
            for (int i = startIndex; i <= source.Length - pattern.Length; i++)
            {
                bool find = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (source[i + j] != pattern[j])
                    {
                        find = false;
                        break;
                    }
                }
                if (find)
                {
                    return i;
                }
            }
            return -1;
        }

        private static string AnalyzeAudioFormat(string filePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{filePath}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (output.Contains("atrac3", StringComparison.OrdinalIgnoreCase))
                {
                    return "at3";
                }
                else if (output.Contains("atrac9", StringComparison.OrdinalIgnoreCase))
                {
                    return "at9";
                }
                else if (output.Contains("xma2", StringComparison.OrdinalIgnoreCase))
                {
                    return "xma";
                }
                else if (output.Contains("none", StringComparison.OrdinalIgnoreCase))
                {
                    return "wem";
                }
                else if (output.Contains("pcm_s8", StringComparison.OrdinalIgnoreCase)
                         || output.Contains("pcm_s16le", StringComparison.OrdinalIgnoreCase)
                         || output.Contains("pcm_s16be", StringComparison.OrdinalIgnoreCase)
                         || output.Contains("pcm_s24le", StringComparison.OrdinalIgnoreCase)
                         || output.Contains("pcm_s24be", StringComparison.OrdinalIgnoreCase)
                         || output.Contains("pcm_s32le", StringComparison.OrdinalIgnoreCase)
                         || output.Contains("pcm_s32be", StringComparison.OrdinalIgnoreCase))
                {
                    return "wav";
                }
                return "wav";
            }
        }
    }
}
