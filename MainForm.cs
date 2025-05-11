using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace supertoolbox.Extractor
{
    public partial class MainForm : Form
    {
        private int totalFileCount;
        private Dictionary<string, TreeNode> formatNodes = new Dictionary<string, TreeNode>();
        private readonly List<string>[] messageBuffers = new List<string>[2];
        private readonly object[] bufferLocks = { new object(), new object() };
        private int activeBufferIndex;
        private bool isUpdatingUI;
        private System.Windows.Forms.Timer updateTimer;
        private CancellationTokenSource extractionCancellationTokenSource;
        private const int UpdateInterval = 200;
        private const int MaxMessagesPerUpdate = 1000;
        private bool isExtracting;
        private StatusStrip? statusStrip1;
        private ToolStripStatusLabel? lblStatus;
        private ToolStripStatusLabel? lblFileCount;

        public MainForm()
        {
            InitializeComponent();
            InitializeTreeView();

            messageBuffers[0] = new List<string>(MaxMessagesPerUpdate);
            messageBuffers[1] = new List<string>(MaxMessagesPerUpdate);

            updateTimer = new System.Windows.Forms.Timer { Interval = UpdateInterval };
            updateTimer.Tick += UpdateUITimerTick;
            updateTimer.Start();

            extractionCancellationTokenSource = new CancellationTokenSource();

            InitializeUIComponents();
        }

        private void InitializeUIComponents()
        {
            richTextBox1.HideSelection = false;
            richTextBox1.ReadOnly = true;

            statusStrip1 = new StatusStrip();
            lblStatus = new ToolStripStatusLabel { Text = "就绪" };
            lblFileCount = new ToolStripStatusLabel { Text = "已提取: 0 个文件" };
            statusStrip1.Items.Add(lblStatus);
            statusStrip1.Items.Add(lblFileCount);
            this.Controls.Add(statusStrip1);
        }

        private void InitializeTreeView()
        {
            TreeNode audioNode = treeView1.Nodes.Add("音频");
            TreeNode imageNode = treeView1.Nodes.Add("图片");
            TreeNode otherNode = treeView1.Nodes.Add("其他档案");

            var extractorTypes = new Dictionary<string, TreeNode>
            {
                { "RIFF - wave系列", audioNode },
                { "RIFF - Fmod - bank", audioNode },
                { "RIFF - wmav2 - xwma", audioNode },
                { "RIFX - BigEndian - wem", audioNode },
                { "RIFF - cdxa - xa", audioNode },
                { "CRI - adpcm_adx - adx", audioNode },
                { "CRI - adpcm_adx - ahx", audioNode },
                { "Fmod - fsb5", audioNode },
                { "Xiph.Org - Ogg", audioNode },
                { "CRI - HCA - hca", audioNode },
                { "RIFF - Google - webp", imageNode },
                { "JPEG/JPG", imageNode },
                { "PNG", imageNode },
                { "ENDILTLE - APK -apk", otherNode },
            };

            var sortedExtractorTypes = extractorTypes.Keys.OrderBy(name => name).ToList();

            foreach (string formatName in sortedExtractorTypes)
            {
                TreeNode parentNode = extractorTypes[formatName];
                TreeNode node = parentNode.Nodes.Add(formatName);
                formatNodes[formatName] = node;
            }

            treeView1.ExpandAll();
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "选择要提取的文件夹";
                folderBrowserDialog.ShowNewFolderButton = false;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFolderPath.Text = folderBrowserDialog.SelectedPath;
                    EnqueueMessage($"已选择文件夹: {folderBrowserDialog.SelectedPath}");
                }
            }
        }

        private async void btnExtract_Click(object sender, EventArgs e)
        {
            if (isExtracting)
            {
                EnqueueMessage("正在进行提取操作，请等待...");
                return;
            }

            string dirPath = txtFolderPath.Text;
            if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath))
            {
                EnqueueMessage($"错误: {dirPath} 不是一个有效的目录。");
                return;
            }

            TreeNode? selectedNode = treeView1.SelectedNode;
            if (selectedNode == null || selectedNode.Parent == null)
            {
                EnqueueMessage("请选择一个具体的文件格式。");
                return;
            }
            string formatName = selectedNode.Text;

            totalFileCount = 0;
            isExtracting = true;
            UpdateUIState(true);

            try
            {
                var extractor = CreateExtractor(formatName);
                if (extractor == null)
                {
                    EnqueueMessage($"错误: 不支持的格式 {formatName}");
                    isExtracting = false;
                    UpdateUIState(false);
                    return;
                }

                EnqueueMessage($"开始提取 {formatName} 格式的文件...");

                var fileExtractedEventInfo = extractor.GetType().GetEvent("FileExtracted");
                var extractionProgressEventInfo = extractor.GetType().GetEvent("ExtractionProgress");
                var filesExtractedEventInfo = extractor.GetType().GetEvent("FilesExtracted");

                if (fileExtractedEventInfo != null)
                {
                    fileExtractedEventInfo.AddEventHandler(extractor, new EventHandler<string>((s, fileName) =>
                    {
                        Interlocked.Increment(ref totalFileCount);
                        EnqueueMessage($"已提取: {Path.GetFileName(fileName)}");
                    }));
                }

                if (extractionProgressEventInfo != null)
                {
                    extractionProgressEventInfo.AddEventHandler(extractor, new EventHandler<string>((s, message) =>
                    {
                        EnqueueMessage(message);
                    }));
                }

                if (filesExtractedEventInfo != null)
                {
                    filesExtractedEventInfo.AddEventHandler(extractor, new EventHandler<List<string>>((s, fileNames) =>
                    {
                        foreach (var fileName in fileNames)
                        {
                            Interlocked.Increment(ref totalFileCount);
                            EnqueueMessage($"已提取: {Path.GetFileName(fileName)}");
                        }
                    }));
                }

                await Task.Run(async () =>
                {
                    try
                    {
                        await extractor.ExtractAsync(dirPath, CancellationToken.None);

                        this.Invoke(new Action(() =>
                        {
                            UpdateFileCountDisplay();
                            EnqueueMessage($"提取操作完成，总共提取了 {totalFileCount} 个文件");
                        }));
                    }
                    catch (Exception ex)
                    {
                        this.Invoke(new Action(() =>
                        {
                            EnqueueMessage($"提取过程中出现错误: {ex.Message}");
                        }));
                    }
                    finally
                    {
                        this.Invoke(new Action(() =>
                        {
                            isExtracting = false;
                            UpdateUIState(false);
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                EnqueueMessage($"提取过程中出现错误: {ex.Message}");
                isExtracting = false;
                UpdateUIState(false);
            }
        }

        private BaseExtractor CreateExtractor(string formatName)
        {
            switch (formatName)
            {
                case "RIFF - wave系列": return new WaveExtractor();
                case "RIFF - Fmod - bank": return new BankExtractor();
                case "RIFF - Google - webp": return new WebpExtractor();
                case "RIFF - wmav2 - xwma": return new XwmaExtractor();
                case "RIFX - BigEndian - wem": return new RifxExtractor();
                case "RIFF - cdxa - xa": return new CdxaExtractor();
                case "CRI - adpcm_adx - adx": return new AdxExtractor();
                case "CRI - adpcm_adx - ahx": return new AhxExtractor();
                case "Fmod - fsb5": return new Fsb5Extractor();
                case "Xiph.Org - Ogg": return new OggExtractor();
                case "JPEG/JPG": return new JpgExtractor();
                case "PNG": return new PngExtractor();
                case "CRI - HCA - hca": return new HcaExtractor();
                case "ENDILTLE - APK -apk": return new ApkExtractor();
                default: throw new NotSupportedException($"不支持的格式: {formatName}");
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            lock (bufferLocks[0])
            {
                messageBuffers[0].Clear();
            }

            lock (bufferLocks[1])
            {
                messageBuffers[1].Clear();
                richTextBox1.Clear();
            }
            totalFileCount = 0;
            UpdateFileCountDisplay();
        }

        private void EnqueueMessage(string message)
        {
            int bufferIndex = activeBufferIndex;

            lock (bufferLocks[bufferIndex])
            {
                if (messageBuffers[bufferIndex].Count >= MaxMessagesPerUpdate && !isUpdatingUI)
                {
                    activeBufferIndex = (activeBufferIndex + 1) % 2;
                    bufferIndex = activeBufferIndex;
                }

                messageBuffers[bufferIndex].Add(message);
            }
        }

        private void UpdateUITimerTick(object? sender, EventArgs e)
        {
            if (isUpdatingUI) return;

            int inactiveBufferIndex = (activeBufferIndex + 1) % 2;
            object bufferLock = bufferLocks[inactiveBufferIndex];
            List<string>? messagesToUpdate = null;

            lock (bufferLock)
            {
                if (messageBuffers[inactiveBufferIndex].Count > 0)
                {
                    isUpdatingUI = true;
                    messagesToUpdate = new List<string>(messageBuffers[inactiveBufferIndex]);
                    messageBuffers[inactiveBufferIndex].Clear();
                }
            }

            if (messagesToUpdate != null && messagesToUpdate.Count > 0)
            {
                UpdateRichTextBox(messagesToUpdate);
            }
            else
            {
                isUpdatingUI = false;
            }
        }

        private void UpdateRichTextBox(List<string> messages)
        {
            if (richTextBox1.IsDisposed || richTextBox1.Disposing)
            {
                isUpdatingUI = false;
                return;
            }

            if (richTextBox1.InvokeRequired)
            {
                try
                {
                    richTextBox1.Invoke(new Action(() => UpdateRichTextBoxInternal(messages)));
                }
                catch (ObjectDisposedException)
                {
                    isUpdatingUI = false;
                    return;
                }
            }
            else
            {
                UpdateRichTextBoxInternal(messages);
            }
        }

        private void UpdateRichTextBoxInternal(List<string> messages)
        {
            if (statusStrip1 == null || lblFileCount == null) return;

            try
            {
                richTextBox1.SuspendLayout();

                StringBuilder sb = new StringBuilder();
                foreach (string message in messages)
                {
                    sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
                }

                int scrollPosition = richTextBox1.SelectionStart;
                bool isAtBottom = scrollPosition >= richTextBox1.TextLength - 10;

                richTextBox1.AppendText(sb.ToString());

                if (isAtBottom)
                {
                    richTextBox1.ScrollToCaret();
                }
                else
                {
                    richTextBox1.SelectionStart = scrollPosition;
                    richTextBox1.SelectionLength = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新UI时出错: {ex.Message}");
            }
            finally
            {
                richTextBox1.ResumeLayout();
                isUpdatingUI = false;
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null && e.Node.Parent != null && lblStatus != null)
            {
                lblStatus.Text = $"已选择: {e.Node.Text}";
            }
        }

        private void treeViewContextMenu_Opening(object sender, CancelEventArgs e)
        {
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.Parent == null)
            {
                e.Cancel = true;
                return;
            }
        }

        private void moveToAudioMenuItem_Click(object sender, EventArgs e)
        {
            MoveSelectedNodeToCategory("音频");
        }

        private void moveToImageMenuItem_Click(object sender, EventArgs e)
        {
            MoveSelectedNodeToCategory("图片");
        }

        private void moveToOtherMenuItem_Click(object sender, EventArgs e)
        {
            MoveSelectedNodeToCategory("其他档案");
        }

        private void MoveSelectedNodeToCategory(string category)
        {
            TreeNode? selectedNode = treeView1.SelectedNode;
            if (selectedNode == null || selectedNode.Parent == null)
                return;

            TreeNode? targetCategory = treeView1.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == category);
            if (targetCategory == null || selectedNode.Parent == targetCategory)
                return;

            TreeNode oldParent = selectedNode.Parent;
            targetCategory.Nodes.Add((TreeNode)selectedNode.Clone());
            oldParent.Nodes.Remove(selectedNode);
            treeView1.SelectedNode = targetCategory.LastNode;

            EnqueueMessage($"已将 {selectedNode.Text} 移动到 {category} 类别");
        }

        private void UpdateUIState(bool isExtracting)
        {
            btnExtract.Enabled = !isExtracting;
            btnSelectFolder.Enabled = !isExtracting;
            treeView1.Enabled = !isExtracting;

            if (lblStatus != null)
            {
                lblStatus.Text = isExtracting ? "正在提取..." : "就绪";
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            updateTimer?.Stop();
            updateTimer?.Dispose();
        }

        private void UpdateFileCountDisplay()
        {
            if (lblFileCount != null)
            {
                lblFileCount.Text = $"已提取: {totalFileCount} 个文件";
            }
        }
    }
}