using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace supertoolbox.Extractor
{
    public class ApkExtractor : BaseExtractor
    {
        public event EventHandler<List<string>>? FilesExtracted;
        public event EventHandler<string>? ExtractionStarted;
        public event EventHandler<string>? ExtractionProgress;
        public event EventHandler<string>? ExtractionError;
        public new event EventHandler<string>? ExtractionCompleted;
        public new event EventHandler<string>? FileExtracted;

        private string _existsMode = "skip";
        private bool _isDebug = false;

        public ApkExtractor(string existsMode = "skip", bool isDebug = false)
        {
            _existsMode = existsMode;
            _isDebug = isDebug;

            FilesExtracted = delegate { };
            ExtractionStarted = delegate { };
            ExtractionProgress = delegate { };
            ExtractionError = delegate { };
            ExtractionCompleted = delegate { };
            FileExtracted = delegate { };
        }

        public override async Task ExtractAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(directoryPath))
            {
                OnExtractionError("目录不存在");
                return;
            }

            try
            {
                OnExtractionStarted($"处理目录: {directoryPath}");

                await Task.Run(() => ProcessDirectory(directoryPath), cancellationToken);

                OnExtractionCompleted($"处理完成");
            }
            catch (OperationCanceledException)
            {
                OnExtractionError("提取操作已取消");
            }
            catch (Exception ex)
            {
                OnExtractionError($"提取时出错: {ex.Message}");
            }
        }

        private void ProcessDirectory(string inputDir)
        {
            string outputDir = Path.Combine(inputDir, "ExtractedAPKs");
            Directory.CreateDirectory(outputDir);

            var apkFiles = Directory.EnumerateFiles(inputDir, "*.apk", SearchOption.AllDirectories);
            int total = 0, processed = 0;

            foreach (string apkFile in apkFiles)
            {
                total++;
                if (IsEndiltleApk(apkFile))
                {
                    processed++;
                    OnExtractionProgress($"正在处理APK ({processed}/{total}): {Path.GetFileName(apkFile)}");

                    try
                    {
                        string apkName = Path.GetFileNameWithoutExtension(apkFile);
                        string currentOutputDir = Path.Combine(outputDir, apkName);
                        Directory.CreateDirectory(currentOutputDir);

                        var unpacker = new UnpackApk(apkFile, currentOutputDir, _existsMode, _isDebug);

                        unpacker.FileExtracted += (sender, filePath) =>
                        {
                            FileExtracted?.Invoke(this, filePath);
                        };

                        unpacker.ProcessCompletedWithCount += (sender, count) =>
                        {
                            OnExtractionProgress($"已完成: {apkName} ({count}个文件)");
                        };

                        unpacker.Extract();
                    }
                    catch (Exception ex)
                    {
                        OnExtractionError($"处理失败{Path.GetFileName(apkFile)}: {ex.Message}");
                    }
                }
            }
        }

        private bool IsEndiltleApk(string filePath)
        {
            try
            {
                byte[] header = new byte[8];
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, 8);
                }
                return Encoding.ASCII.GetString(header) == "ENDILTLE";
            }
            catch
            {
                return false;
            }
        }

        protected virtual void OnFilesExtracted(List<string> files)
        {
            FilesExtracted?.Invoke(this, files);
        }

        protected virtual void OnExtractionStarted(string message)
        {
            ExtractionStarted?.Invoke(this, message);
        }

        protected virtual void OnExtractionProgress(string message)
        {
            ExtractionProgress?.Invoke(this, message);
        }

        protected virtual void OnExtractionError(string message)
        {
            ExtractionError?.Invoke(this, message);
        }

        protected virtual void OnExtractionCompleted(string message)
        {
            ExtractionCompleted?.Invoke(this, message);
        }
    }

    public class UnpackApk
    {
        private string InputApkPath { get; }
        private string OutputDirPath { get; }
        private string FileExists { get; }
        private bool IsDebug { get; }
        private int _fileCount = 0;

        public event EventHandler<string>? FileExtracted;
        public event EventHandler<string>? ProcessStarted;
        public event EventHandler<string>? ProcessCompleted;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler<int>? ProcessCompletedWithCount;

        public UnpackApk(string inputPath, string outputPath, string fileExists, bool debug)
        {
            InputApkPath = inputPath;
            OutputDirPath = outputPath;
            FileExists = fileExists;
            IsDebug = debug;

            FileExtracted = delegate { };
            ProcessStarted = delegate { };
            ProcessCompleted = delegate { };
            ErrorOccurred = delegate { };
            ProcessCompletedWithCount = delegate { };
        }

        public void Extract()
        {
            _fileCount = 0;
            ProcessStarted?.Invoke(this, InputApkPath);
            Directory.CreateDirectory(OutputDirPath);

            BinaryReader reader;
            try
            {
                byte[] fileData = File.ReadAllBytes(InputApkPath);
                reader = new BinaryReader(fileData);
                reader.Seek(0);
            }
            catch (Exception e)
            {
                ErrorOccurred?.Invoke(this, $"文件读取出错: {e.Message}");
                return;
            }

            var dump = new Dictionary<string, object>
            {
                ["PACKTOC"] = new Dictionary<string, object>(),
                ["PACKFSLS"] = new Dictionary<string, object>(),
                ["GENESTRT"] = new Dictionary<string, object>(),
                ["FILE_AREA"] = new Dictionary<string, object>()
            };

            var fileList = new Dictionary<string, List<Dictionary<string, object>>>();

            try
            {
                string endianness = reader.ReadStringBytes(8);
                byte[] zero = reader.GetBuffer(8);

                string packhedr = reader.ReadStringBytes(8);
                ulong headerSize = reader.ReadU64();
                byte[] unknown1 = reader.GetBuffer(8);
                uint fileListOffset = reader.ReadU32();
                byte[] unknown2 = reader.GetBuffer(4);
                byte[] unknown3 = reader.GetBuffer(16);

                string packtoc = reader.ReadStringBytes(8);
                headerSize = reader.ReadU64();
                int packtocStartOffset = reader.GetPosition();
                uint tocSegSize = reader.ReadU32();
                uint tocSegCount = reader.ReadU32();
                byte[] unknown4 = reader.GetBuffer(4);
                zero = reader.GetBuffer(4);

                var tocSegmentList = new List<Dictionary<string, ByteSegment>>();
                ((Dictionary<string, object>)dump["PACKTOC"])["TOC_SEGMENT_LIST"] = tocSegmentList;

                for (int i = 0; i < tocSegCount; i++)
                {
                    uint identifier = reader.ReadU32();
                    uint nameIdx = reader.ReadU32();
                    byte[] unknown5 = reader.GetBuffer(8);
                    ulong fileOffset = reader.ReadU64();
                    ulong size = reader.ReadU64();
                    ulong zsize = reader.ReadU64();

                    tocSegmentList.Add(new Dictionary<string, ByteSegment>
                    {
                        ["IDENTIFIER"] = new ByteSegment("int", identifier),
                        ["NAME_IDX"] = new ByteSegment("int", nameIdx),
                        ["FILE_OFFSET"] = new ByteSegment("offset", fileOffset),
                        ["SIZE"] = new ByteSegment("int", size),
                        ["ZSIZE"] = new ByteSegment("int", zsize)
                    });
                }

                int padCnt = (int)(packtocStartOffset + (int)headerSize) - reader.GetPosition();
                byte[] padding = reader.GetBuffer(padCnt);

                string packfsls = reader.ReadStringBytes(8);
                headerSize = reader.ReadU64();
                int packfslsStartOffset = reader.GetPosition();
                uint archiveCount = reader.ReadU32();
                uint archiveSegSize = reader.ReadU32();
                byte[] unknown6 = reader.GetBuffer(4);
                byte[] unknown7 = reader.GetBuffer(4);

                var archiveSegmentList = new List<Dictionary<string, ByteSegment>>();
                ((Dictionary<string, object>)dump["PACKFSLS"])["ARCHIVE_SEGMENT_LIST"] = archiveSegmentList;

                for (int i = 0; i < archiveCount; i++)
                {
                    uint nameIdx = reader.ReadU32();
                    byte[] unknown8 = reader.GetBuffer(4);
                    ulong archiveOffset = reader.ReadU64();
                    ulong size = reader.ReadU64();
                    byte[] dummy = reader.GetBuffer(16);

                    archiveSegmentList.Add(new Dictionary<string, ByteSegment>
                    {
                        ["NAME_IDX"] = new ByteSegment("int", nameIdx),
                        ["ARCHIVE_OFFSET"] = new ByteSegment("offset", archiveOffset),
                        ["SIZE"] = new ByteSegment("int", size)
                    });
                }

                padCnt = (int)(packfslsStartOffset + (int)headerSize) - reader.GetPosition();
                padding = reader.GetBuffer(padCnt);

                string genestrt = reader.ReadStringBytes(8);
                ulong genestrtSize = reader.ReadU64();
                int genestrtStartOffset = reader.GetPosition();
                uint strOffsetCount = reader.ReadU32();
                byte[] unknown9 = reader.GetBuffer(4);
                uint genestrtSize2 = reader.ReadU32();
                ((Dictionary<string, object>)dump["GENESTRT"])["HEADER_SIZE+STR_OFFSET_LIST_SIZE"] = new ByteSegment("int", genestrtSize2);
                genestrtSize2 = reader.ReadU32();

                var strOffsetList = new List<ByteSegment>();
                ((Dictionary<string, object>)dump["GENESTRT"])["STR_OFFSET_LIST"] = strOffsetList;

                for (int i = 0; i < strOffsetCount; i++)
                {
                    strOffsetList.Add(new ByteSegment("int", reader.ReadU32()));
                }

                padCnt = (int)(genestrtStartOffset + (int)((ByteSegment)((Dictionary<string, object>)dump["GENESTRT"])["HEADER_SIZE+STR_OFFSET_LIST_SIZE"]).GetInt()) - reader.GetPosition();
                ((Dictionary<string, object>)dump["GENESTRT"])["PAD"] = new ByteSegment("raw", reader.GetBuffer(padCnt));

                var stringList = new List<ByteSegment>();
                ((Dictionary<string, object>)dump["GENESTRT"])["STRING_LIST"] = stringList;

                for (int i = 0; i < strOffsetCount; i++)
                {
                    try
                    {
                        string str = reader.ReadStringUtf8();
                        stringList.Add(new ByteSegment("str", str));
                    }
                    catch (Exception e)
                    {
                        ErrorOccurred?.Invoke(this, $"字符串解码错误: {e.Message}");
                        stringList.Add(new ByteSegment("str", ""));
                    }
                }

                padCnt = (int)(genestrtStartOffset + (int)genestrtSize) - reader.GetPosition();
                ((Dictionary<string, object>)dump["GENESTRT"])["TABLE_PADDING"] = new ByteSegment("raw", reader.GetBuffer(padCnt));

                string geneeof = reader.ReadStringBytes(8);
                byte[] zeroPadding = reader.GetBuffer(8);
                byte[] tablePadding = reader.GetBuffer((int)fileListOffset - reader.GetPosition());

                ((Dictionary<string, object>)dump["FILE_AREA"])["ROOT_ARCHIVE"] = new Dictionary<string, object>();

                foreach (var tocSeg in tocSegmentList)
                {
                    string fname = stringList[(int)tocSeg["NAME_IDX"].GetInt()].StringValue.TrimEnd('\0');
                    uint identifier = (uint)tocSeg["IDENTIFIER"].GetInt();
                    ulong fileOffset = tocSeg["FILE_OFFSET"].GetInt();
                    ulong size = tocSeg["SIZE"].GetInt();
                    ulong zsize = tocSeg["ZSIZE"].GetInt();

                    reader.Seek((int)fileOffset);
                    ulong realSize = zsize == 0 ? size : zsize;
                    if (identifier == 1 || realSize == 0)
                    {
                        continue;
                    }

                    byte[] file = reader.GetBuffer((int)realSize);
                    string outPath = Path.Combine(OutputDirPath, fname);

                    if (!fileList.ContainsKey(fname))
                    {
                        fileList[fname] = new List<Dictionary<string, object>>();
                    }

                    fileList[fname].Add(new Dictionary<string, object>
                    {
                        ["out_path"] = outPath,
                        ["file"] = file,
                        ["offset"] = fileOffset,
                        ["zsize"] = zsize,
                        ["fname"] = fname
                    });
                }

                for (int i = 0; i < archiveCount; i++)
                {
                    string key = $"ARCHIVE #{i}";
                    var archiveDict = new Dictionary<string, object>
                    {
                        ["PACKFSHD"] = new Dictionary<string, object>(),
                        ["GENESTRT"] = new Dictionary<string, object>()
                    };
                    ((Dictionary<string, object>)dump["FILE_AREA"])[key] = archiveDict;

                    uint nameIdx = (uint)archiveSegmentList[i]["NAME_IDX"].GetInt();
                    ulong archiveOffset = archiveSegmentList[i]["ARCHIVE_OFFSET"].GetInt();
                    ulong size = archiveSegmentList[i]["SIZE"].GetInt();
                    string archiveName = stringList[(int)nameIdx].StringValue.TrimEnd('\0');

                    reader.Seek((int)archiveOffset);
                    string endiannessArchive = reader.ReadStringBytes(8);
                    byte[] zeroArchive = reader.GetBuffer(8);

                    string packfshd = reader.ReadStringBytes(8);
                    ulong headerSizeArchive = reader.ReadU64();
                    byte[] dummy1 = reader.GetBuffer(4);
                    uint fileSegSize = reader.ReadU32();
                    uint fileSegCount = reader.ReadU32();
                    uint segCount = reader.ReadU32();
                    byte[] dummy2 = reader.GetBuffer(16);

                    var fileSegList = new List<Dictionary<string, ByteSegment>>();
                    ((Dictionary<string, object>)archiveDict["PACKFSHD"])["FILE_SEG_LIST"] = fileSegList;

                    for (int j = 0; j < fileSegCount; j++)
                    {
                        uint segNameIdx = reader.ReadU32();
                        uint zip = reader.ReadU32();
                        ulong offset = reader.ReadU64();
                        ulong segSize = reader.ReadU64();
                        ulong segZsize = reader.ReadU64();

                        fileSegList.Add(new Dictionary<string, ByteSegment>
                        {
                            ["NAME_IDX"] = new ByteSegment("int", segNameIdx),
                            ["ZIP"] = new ByteSegment("int", zip),
                            ["OFFSET"] = new ByteSegment("offset", offset),
                            ["SIZE"] = new ByteSegment("int", segSize),
                            ["ZSIZE"] = new ByteSegment("int", segZsize)
                        });
                    }

                    string genestrtArchive = reader.ReadStringBytes(8);
                    ulong genestrtSizeArchive = reader.ReadU64();
                    int genestrtStartOffsetArchive = reader.GetPosition();
                    uint strOffsetCountArchive = reader.ReadU32();
                    byte[] unknown10 = reader.GetBuffer(4);
                    uint genestrtSize2Archive = reader.ReadU32();
                    ((Dictionary<string, object>)archiveDict["GENESTRT"])["HEADER_SIZE+STR_OFFSET_LIST_SIZE"] = new ByteSegment("int", genestrtSize2Archive);
                    genestrtSize2Archive = reader.ReadU32();
                    ((Dictionary<string, object>)archiveDict["GENESTRT"])["GENESTRT_SIZE_2"] = new ByteSegment("int", genestrtSize2Archive);

                    var archiveStrOffsetList = new List<ByteSegment>();
                    ((Dictionary<string, object>)archiveDict["GENESTRT"])["STR_OFFSET_LIST"] = archiveStrOffsetList;

                    for (int j = 0; j < strOffsetCountArchive; j++)
                    {
                        archiveStrOffsetList.Add(new ByteSegment("int", reader.ReadU32()));
                    }

                    padCnt = (int)(genestrtStartOffsetArchive + (int)((ByteSegment)((Dictionary<string, object>)archiveDict["GENESTRT"])["HEADER_SIZE+STR_OFFSET_LIST_SIZE"]).GetInt()) - reader.GetPosition();
                    byte[] archivePadding = reader.GetBuffer(padCnt);

                    var archiveStringList = new List<ByteSegment>();
                    ((Dictionary<string, object>)archiveDict["GENESTRT"])["STRING_LIST"] = archiveStringList;

                    for (int j = 0; j < strOffsetCountArchive; j++)
                    {
                        try
                        {
                            string str = reader.ReadStringUtf8();
                            archiveStringList.Add(new ByteSegment("str", str));
                        }
                        catch (Exception e)
                        {
                            ErrorOccurred?.Invoke(this, $"字符串解码错误: {e.Message}");
                            archiveStringList.Add(new ByteSegment("str", ""));
                        }
                    }

                    padCnt = (int)(genestrtStartOffsetArchive + (int)genestrtSizeArchive) - reader.GetPosition();
                    ((Dictionary<string, object>)archiveDict["GENESTRT"])["TABLE_PADDING"] = new ByteSegment("raw", reader.GetBuffer(padCnt));

                    ((Dictionary<string, object>)archiveDict)["FILE_AREA"] = new Dictionary<string, object>();

                    foreach (var fileSeg in fileSegList)
                    {
                        ulong offset = fileSeg["OFFSET"].GetInt();
                        ulong zsize = fileSeg["ZSIZE"].GetInt();
                        ulong segSize = fileSeg["SIZE"].GetInt();
                        uint segNameIdx = (uint)fileSeg["NAME_IDX"].GetInt();
                        string fname = archiveStringList[(int)segNameIdx].StringValue.TrimEnd('\0');

                        reader.Seek((int)(archiveOffset + offset));
                        ulong realSize = zsize == 0 ? segSize : zsize;
                        if (realSize == 0)
                        {
                            continue;
                        }

                        byte[] file = reader.GetBuffer((int)realSize);
                        string outPath = Path.Combine(OutputDirPath, archiveName, fname);
                        string fullName = $"{archiveName}/{fname}";

                        if (!fileList.ContainsKey(fullName))
                        {
                            fileList[fullName] = new List<Dictionary<string, object>>();
                        }

                        fileList[fullName].Add(new Dictionary<string, object>
                        {
                            ["out_path"] = outPath,
                            ["file"] = file,
                            ["offset"] = archiveOffset + offset,
                            ["zsize"] = zsize,
                            ["fname"] = fullName
                        });
                    }
                }

                foreach (var kvp in fileList)
                {
                    bool isSameName = kvp.Value.Count > 1;
                    foreach (var obj in kvp.Value)
                    {
                        string outPath = (string)obj["out_path"];
                        byte[] file = (byte[])obj["file"];
                        ulong offset = (ulong)obj["offset"];
                        string fname = (string)obj["fname"];

                        if (ExtractFile(outPath, file, offset, isSameName, (ulong)obj["zsize"] != 0))
                        {
                            _fileCount++;
                        }
                    }
                }

                ProcessCompleted?.Invoke(this, InputApkPath);
                ProcessCompletedWithCount?.Invoke(this, _fileCount);
            }
            catch (Exception e)
            {
                ErrorOccurred?.Invoke(this, $"解析错误: {e.Message}");
            }
        }

        private bool ExtractFile(string outPath, byte[] file, ulong offset, bool isSameName, bool isZip)
        {
            if (isZip)
            {
                try
                {
                    using (var compressedStream = new MemoryStream(file, 2, file.Length - 2))
                    using (var decompressedStream = new MemoryStream())
                    using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
                    {
                        deflateStream.CopyTo(decompressedStream);
                        file = decompressedStream.ToArray();
                    }
                }
                catch (Exception e)
                {
                    ErrorOccurred?.Invoke(this, $"解压错误: {e.Message}");
                    return false;
                }
            }

            string? directory = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (isSameName)
            {
                string basename = Path.GetFileNameWithoutExtension(outPath);
                string extension = Path.GetExtension(outPath);
                directory = directory ?? Directory.GetCurrentDirectory();

                outPath = Path.Combine(directory, $"{basename}__OFS_{offset}{extension}");
            }

            if (File.Exists(outPath))
            {
                if (FileExists == "skip")
                {
                    return false;
                }
            }

            try
            {
                File.WriteAllBytes(outPath, file);
                FileExtracted?.Invoke(this, outPath);
                return true;
            }
            catch (Exception e)
            {
                ErrorOccurred?.Invoke(this, $"写入错误: {e.Message}");
                return false;
            }
        }
    }

    public class BinaryReader : IDisposable
    {
        private byte[] _raw;
        private int _offset;

        public BinaryReader(byte[] data)
        {
            _raw = data;
            _offset = 0;
        }

        public int Size()
        {
            return _raw.Length;
        }

        public byte[] GetBuffer(int length)
        {
            if (_offset + length <= _raw.Length)
            {
                byte[] result = new byte[length];
                Buffer.BlockCopy(_raw, _offset, result, 0, length);
                _offset += length;
                return result;
            }
            return new byte[0];
        }

        public void Seek(int offset)
        {
            _offset = offset;
        }

        public int GetPosition()
        {
            return _offset;
        }

        public uint ReadU32()
        {
            if (_offset + 4 > _raw.Length)
                return 0;

            uint result = BitConverter.ToUInt32(_raw, _offset);
            _offset += 4;
            return result;
        }

        public ulong ReadU64()
        {
            if (_offset + 8 > _raw.Length)
                return 0;

            ulong result = BitConverter.ToUInt64(_raw, _offset);
            _offset += 8;
            return result;
        }

        public string ReadStringUtf8()
        {
            int start = _offset;
            while (_offset < _raw.Length && _raw[_offset] != 0)
                _offset++;

            string result = Encoding.UTF8.GetString(_raw, start, _offset - start);
            _offset++;
            return result;
        }

        public string ReadStringBytes(int n)
        {
            string result = Encoding.ASCII.GetString(_raw, _offset, n);
            _offset += n;
            return result;
        }

        public void Dispose()
        {
            _raw = Array.Empty<byte>();
        }
    }

    public class ByteSegment
    {
        public string Type { get; set; }
        public byte[] Raw { get; set; }
        public string StringValue { get; set; }
        public ulong IntValue { get; set; }
        public string OffsetValue { get; set; }

        public ByteSegment(string type, object value)
        {
            Type = type;
            StringValue = string.Empty;
            OffsetValue = string.Empty;
            Raw = Array.Empty<byte>();

            switch (type)
            {
                case "raw":
                    Raw = (byte[])value;
                    break;
                case "str":
                    StringValue = (string)value;
                    Raw = Encoding.UTF8.GetBytes(StringValue);
                    break;
                case "int":
                    IntValue = Convert.ToUInt64(value);
                    Raw = BitConverter.GetBytes(IntValue);
                    break;
                case "offset":
                    IntValue = Convert.ToUInt64(value);
                    OffsetValue = "0x" + IntValue.ToString("X8");
                    Raw = BitConverter.GetBytes(IntValue);
                    break;
            }
        }

        public ulong GetInt()
        {
            return IntValue;
        }
    }
}