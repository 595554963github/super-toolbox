using Extractor.Extractor;
using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace supertoolbox.Extractor
{
    public partial class MainForm : Form
    {
        private int totalFileCount = 0;

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFolderPath.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            string dirPath = txtFolderPath.Text;
            if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath))
            {
                AppendMessageToRichTextBox($"错误: {dirPath} 不是一个有效的目录。");
                return;
            }

            int choice = -1;
            if (radioWave.Checked)
            {
                choice = 1;
            }
            else if (radioBank.Checked)
            {
                choice = 2;
            }
            else if (radioWebp.Checked)
            {
                choice = 3;
            }
            else if (radioXwma.Checked)
            {
                choice = 4;
            }
            else if (radioWem.Checked)
            {
                choice = 5;
            }
            else if (radioXa.Checked)
            {
                choice = 6;
            }
            else if (radioAdx.Checked)
            {
                choice = 7;
            }
            else if (radioAhx.Checked)
            {
                choice = 8;
            }
            else if (radioFsb5.Checked)
            {
                choice = 9;
            }
            else if (radioOgg.Checked)
            {
                choice = 10;
            }
            else if (radioJpg.Checked)
            {
                choice = 11;
            }
            else if (radioPng.Checked)
            {
                choice = 12;
            }

            if (choice == -1)
            {
                AppendMessageToRichTextBox("无效的选择。");
                return;
            }

            totalFileCount = 0;
            try
            {
                switch (choice)
                {
                    case 1:
                        var waveExtractor = new WaveExtractor();
                        waveExtractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        waveExtractor.Extract(dirPath);
                        break;
                    case 2:
                        var bankExtractor = new BankExtractor();
                        bankExtractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        bankExtractor.Extract(dirPath);
                        break;
                    case 3:
                        var webpExtractor = new WebpExtractor();
                        webpExtractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        webpExtractor.Extract(dirPath);
                        break;
                    case 4:
                        var xwmaExtractor = new XwmaExtractor();
                        xwmaExtractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        xwmaExtractor.Extract(dirPath);
                        break;
                    case 5:
                        var rifxExtractor = new RifxExtractor();
                        rifxExtractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        rifxExtractor.Extract(dirPath);
                        break;
                    case 6:
                        var cdxaExtractor = new CdxaExtractor();
                        cdxaExtractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        cdxaExtractor.Extract(dirPath);
                        break;
                    case 7:
                        var adxExtractor = new AdxExtractor();
                        adxExtractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        adxExtractor.Extract(dirPath);
                        break;
                    case 8:
                        var ahxExtractor = new AhxExtractor();
                        ahxExtractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        ahxExtractor.Extract(dirPath);
                        break;
                    case 9:
                        var fsb5Extractor = new Fsb5Extractor();
                        fsb5Extractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        fsb5Extractor.Extract(dirPath);
                        break;
                    case 10:
                        var oggExtractor = new OggExtractor();
                        oggExtractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        oggExtractor.Extract(dirPath);
                        break;
                    case 11:
                        var jpgExtractor = new JpgExtractor(richTextBox1);
                        jpgExtractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        jpgExtractor.Extract(dirPath);
                        break;
                    case 12:
                        var pngExtractor = new PngExtractor();
                        pngExtractor.FilesExtracted += (senderObj, fileNames) =>
                        {
                            foreach (string fileName in fileNames)
                            {
                                AppendMessageToRichTextBox(fileName);
                                totalFileCount++;
                            }
                        };
                        pngExtractor.Extract(dirPath);
                        break;
                }
                AppendMessageToRichTextBox($"提取操作完成，总共提取了 {totalFileCount} 个文件。");
            }
            catch (Exception ex)
            {
                AppendMessageToRichTextBox($"提取过程中出现错误: {ex.Message}");
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void AppendMessageToRichTextBox(string message)
        {
            richTextBox1.AppendText($"{DateTime.Now:yyyy - MM - dd HH:mm:ss} - {message}{Environment.NewLine}");
        }
    }
}
