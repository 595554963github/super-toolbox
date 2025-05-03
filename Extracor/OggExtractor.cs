using System;
using System.Collections.Generic;
using System.IO;

namespace Extractor.Extractor
{
    public class OggExtractor : BaseExtractor
    {
        private int totalFileCount = 0;
        public event EventHandler<List<string>>? FilesExtracted;

        public override void Extract(string inputPath)
        {
            if (!Directory.Exists(inputPath))
            {
                List<string> errorMessage = new List<string> { $"输入的路径 {inputPath} 不是一个有效的文件夹。" };
                FilesExtracted?.Invoke(this, errorMessage);
                return;
            }

            List<string> extractedFiles = new List<string>();
            bool stopParsingOnFormatError = true;

            foreach (string filePath in Directory.EnumerateFiles(inputPath, "*", SearchOption.AllDirectories))
            {
                ProcessFile(filePath, stopParsingOnFormatError, extractedFiles);
            }

            extractedFiles.Add($"提取操作完成，总共提取了 {totalFileCount} 个文件。");
            FilesExtracted?.Invoke(this, extractedFiles);
        }

        private void ProcessFile(string filePath, bool stopParsingOnFormatError, List<string> extractedFiles)
        {
            long offset = 0;
            byte pageType;
            long pageSize;
            uint bitstreamSerialNumber;
            byte segmentCount;
            uint sizeOfAllSegments;
            byte i;
            string outputPath;
            string outputFileName;
            byte[] rawPageBytes;
            bool pageWrittenToFile = false;

            Dictionary<uint, FileStream> outputStreams = new Dictionary<uint, FileStream>();
            int globalIndex = 0;

            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    outputPath = Path.GetDirectoryName(filePath) ?? string.Empty;

                    while ((offset = ParseFile.GetNextOffset(fs, offset, XiphOrgOggContainer.MAGIC_BYTES)) > -1)
                    {
                        pageWrittenToFile = false;

                        pageType = ParseFile.ParseSimpleOffset(fs, offset + 5, 1)[0];
                        bitstreamSerialNumber = BitConverter.ToUInt32(ParseFile.ParseSimpleOffset(fs, offset + 0xE, 4), 0);
                        segmentCount = ParseFile.ParseSimpleOffset(fs, offset + 0x1A, 1)[0];

                        sizeOfAllSegments = 0;
                        for (i = 0; i < segmentCount; i++)
                        {
                            sizeOfAllSegments += ParseFile.ParseSimpleOffset(fs, offset + 0x1B + i, 1)[0];
                        }

                        pageSize = 0x1B + segmentCount + sizeOfAllSegments;

                        rawPageBytes = ParseFile.ParseSimpleOffset(fs, offset, (int)pageSize);

                        if ((pageType & XiphOrgOggContainer.PAGE_TYPE_BEGIN_STREAM) == XiphOrgOggContainer.PAGE_TYPE_BEGIN_STREAM)
                        {
                            if (outputStreams.ContainsKey(bitstreamSerialNumber))
                            {
                                if (stopParsingOnFormatError)
                                {
                                    throw new FormatException($"多次找到流开始页面，但没有流结束页面，用于序列号: {bitstreamSerialNumber:X8}，文件: {filePath}");
                                }
                                else
                                {
                                    List<string> warningMessage = new List<string> { $"警告：对于文件 <{filePath}>，多次找到流开始页面但没有流结束页面，序列号为: {bitstreamSerialNumber:X8}。" };
                                    FilesExtracted?.Invoke(this, warningMessage);
                                }
                            }
                            else
                            {
                                string fileNamePrefix = Path.GetFileNameWithoutExtension(filePath);
                                outputFileName = Path.Combine(outputPath, $"{fileNamePrefix}_{globalIndex}.ogg");
                                outputFileName = GetNonDuplicateFileName(outputFileName);

                                outputStreams[bitstreamSerialNumber] = File.Open(outputFileName, FileMode.CreateNew, FileAccess.Write);
                                outputStreams[bitstreamSerialNumber].Write(rawPageBytes, 0, rawPageBytes.Length);
                                pageWrittenToFile = true;

                                extractedFiles.Add($"正在生成文件: {outputFileName}");
                                totalFileCount++;
                                List<string> generateMessage = new List<string> { $"正在生成文件: {outputFileName}" };
                                FilesExtracted?.Invoke(this, generateMessage);
                                globalIndex++;
                            }
                        }

                        if (outputStreams.ContainsKey(bitstreamSerialNumber))
                        {
                            if (!pageWrittenToFile)
                            {
                                outputStreams[bitstreamSerialNumber].Write(rawPageBytes, 0, rawPageBytes.Length);
                                pageWrittenToFile = true;
                            }
                        }
                        else
                        {
                            if (stopParsingOnFormatError)
                            {
                                throw new FormatException($"找到没有流开始页的流数据页，用于序列号: {bitstreamSerialNumber:X8}，文件: {filePath}");
                            }
                            else
                            {
                                List<string> warningMessage = new List<string> { $"警告：对于文件 <{filePath}>，找到没有流开始页的流数据页，序列号为: {bitstreamSerialNumber:X8}。" };
                                FilesExtracted?.Invoke(this, warningMessage);
                            }
                        }

                        if ((pageType & XiphOrgOggContainer.PAGE_TYPE_END_STREAM) == XiphOrgOggContainer.PAGE_TYPE_END_STREAM)
                        {
                            if (outputStreams.ContainsKey(bitstreamSerialNumber))
                            {
                                if (!pageWrittenToFile)
                                {
                                    outputStreams[bitstreamSerialNumber].Write(rawPageBytes, 0, rawPageBytes.Length);
                                    pageWrittenToFile = true;
                                }

                                outputStreams[bitstreamSerialNumber].Close();
                                outputStreams[bitstreamSerialNumber].Dispose();
                                outputStreams.Remove(bitstreamSerialNumber);
                            }
                            else
                            {
                                if (stopParsingOnFormatError)
                                {
                                    throw new FormatException($"找到没有流开始页面的流结束页面，用于序列号: {bitstreamSerialNumber:X8}，文件: {filePath}");
                                }
                                else
                                {
                                    List<string> warningMessage = new List<string> { $"警告：对于文件 <{filePath}>，找到没有流开始页面的流结束页面，序列号为: {bitstreamSerialNumber:X8}。" };
                                    FilesExtracted?.Invoke(this, warningMessage);
                                }
                            }
                        }

                        offset += pageSize;
                    }
                }
                catch (Exception ex)
                {
                    List<string> errorMessage = new List<string> { $"处理文件 {filePath} 时出现错误: {ex.Message}" };
                    FilesExtracted?.Invoke(this, errorMessage);
                }
                finally
                {
                    foreach (uint k in outputStreams.Keys)
                    {
                        outputStreams[k].Close();
                        outputStreams[k].Dispose();
                    }
                }
            }
        }

        private string GetNonDuplicateFileName(string fileName)
        {
            string directory = Path.GetDirectoryName(fileName) ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            int counter = 1;
            string newFileName = fileName;
            while (File.Exists(newFileName))
            {
                newFileName = Path.Combine(directory, $"{name}_{counter}{extension}");
                counter++;
            }
            return newFileName;
        }

        private static class ParseFile
        {
            public static byte[] ParseSimpleOffset(FileStream fs, long offset, int length)
            {
                byte[] buffer = new byte[length];
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Read(buffer, 0, length);
                return buffer;
            }

            public static long GetNextOffset(FileStream fs, long offset, byte[] magicBytes)
            {
                byte[] buffer = new byte[magicBytes.Length];
                while (offset + magicBytes.Length <= fs.Length)
                {
                    fs.Seek(offset, SeekOrigin.Begin);
                    fs.Read(buffer, 0, magicBytes.Length);
                    if (AreByteArraysEqual(buffer, magicBytes))
                    {
                        return offset;
                    }
                    offset++;
                }
                return -1;
            }

            private static bool AreByteArraysEqual(byte[] a, byte[] b)
            {
                if (a.Length != b.Length) return false;
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i]) return false;
                }
                return true;
            }
        }

        private static class XiphOrgOggContainer
        {
            public static readonly byte[] MAGIC_BYTES = { 0x4F, 0x67, 0x67, 0x53 };
            public const byte PAGE_TYPE_BEGIN_STREAM = 0x02;
            public const byte PAGE_TYPE_END_STREAM = 0x04;
        }
    }
}