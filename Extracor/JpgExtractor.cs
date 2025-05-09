using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace supertoolbox.Extractor
{
    public class JpgExtractor
    {
        private RichTextBox outputTextBox;
        private int extractedFileCount = 0;

        public event EventHandler<string[]>? FilesExtracted;

        public JpgExtractor(RichTextBox textBox)
        {
            outputTextBox = textBox;
        }

        public void Extract(string dirPath)
        {
            ExtractJpegsFromDirectory(dirPath);
        }

        public void ExtractJpegsFromDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                outputTextBox.AppendText($"错误: {dirPath} 不是一个有效的目录。\n");
                return;
            }

            byte[] startSequence = { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG 开始标记
            byte[] jfifMarker = System.Text.Encoding.ASCII.GetBytes("JFIF"); // JFIF 数据块标记
            byte[] endSequence = { 0xFF, 0xD9 }; // JPEG 结束标记

            List<string> extractedFileNames = new List<string>();
            foreach (string file in Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories))
            {
                outputTextBox.AppendText($"正在处理文件: {file}\n");
                byte[] content = File.ReadAllBytes(file);
                List<byte[]> extractedJpegs = FindValidJpegs(content, startSequence, jfifMarker, endSequence);
                SaveExtractedJpegs(file, dirPath, extractedJpegs, extractedFileNames);
            }
            outputTextBox.AppendText($"已提取 {extractedFileCount} 个文件。\n");

            // 触发 FilesExtracted 事件，通知外部提取的文件名列表
            if (FilesExtracted != null)
            {
                FilesExtracted(this, extractedFileNames.ToArray());
            }
        }

        static List<byte[]> FindValidJpegs(byte[] content, byte[] startSequence, byte[] jfifMarker, byte[] endSequence)
        {
            List<byte[]> validJpegs = new List<byte[]>();
            int startIndex = 0;
            while (startIndex < content.Length - startSequence.Length)
            {
                int startOfJpeg = IndexOfSequence(content, startSequence, startIndex);
                if (startOfJpeg == -1)
                {
                    break;
                }

                int endOfJpeg = IndexOfSequence(content, endSequence, startOfJpeg);
                if (endOfJpeg == -1)
                {
                    startIndex = startOfJpeg + 1;
                    continue;
                }

                byte[] potentialJpeg = content.Skip(startOfJpeg).Take(endOfJpeg - startOfJpeg + endSequence.Length).ToArray();
                if (IsValidJpeg(potentialJpeg, jfifMarker))
                {
                    validJpegs.Add(potentialJpeg);
                }

                startIndex = endOfJpeg + 1;
            }

            return validJpegs;
        }

        static bool IsValidJpeg(byte[] data, byte[] jfifMarker)
        {
            int jfifIndex = IndexOfSequence(data, jfifMarker, 0);
            return jfifIndex != -1;
        }

        static int IndexOfSequence(byte[] content, byte[] sequence, int startIndex)
        {
            for (int i = startIndex; i <= content.Length - sequence.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < sequence.Length; j++)
                {
                    if (content[i + j] != sequence[j])
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

        private void SaveExtractedJpegs(string sourceFilePath, string directoryPath, List<byte[]> extractedJpegs, List<string> extractedFileNames)
        {
            string baseName = Path.GetFileNameWithoutExtension(sourceFilePath);
            for (int i = 0; i < extractedJpegs.Count; i++)
            {
                string newFileName = $"{baseName}_{i}.jpg";
                string newFilePath = Path.Combine(directoryPath, newFileName);
                File.WriteAllBytes(newFilePath, extractedJpegs[i]);
                outputTextBox.AppendText($"已提取并保存 {newFileName}\n");
                extractedFileCount++;
                extractedFileNames.Add(newFileName);
            }
        }
    }
}
