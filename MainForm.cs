// MainForm.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace supertoolbox.Extractor
{
    public partial class MainForm : Form
    {
        private int totalFileCount = 0;
        private Dictionary<string, TreeNode> formatNodes = new Dictionary<string, TreeNode>();

        public MainForm()
        {
            InitializeComponent();
            InitializeTreeView();
        }

        private void InitializeTreeView()
        {
            // 创建根节点
            TreeNode audioNode = treeView1.Nodes.Add("音频");
            TreeNode imageNode = treeView1.Nodes.Add("图片");
            TreeNode otherNode = treeView1.Nodes.Add("其他档案");

            // 定义提取器类型及其分类
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

        { "其他格式1", otherNode },
        { "其他格式2", otherNode }
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
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFolderPath.Text = folderBrowserDialog.SelectedPath;
                    AppendMessageToRichTextBox($"已选择文件夹: {folderBrowserDialog.SelectedPath}");
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

            TreeNode selectedNode = treeView1.SelectedNode;
            if (selectedNode == null)
            {
                AppendMessageToRichTextBox("请选择一个具体的文件格式。");
                return;
            }
            string formatName = selectedNode.Text;

            totalFileCount = 0;
            try
            {
                var extractor = CreateExtractor(formatName);
                if (extractor == null)
                {
                    AppendMessageToRichTextBox($"错误: 不支持的格式 {formatName}");
                    return;
                }

                // 内联 ExtractorAdapter 功能
                Type extractorType = extractor.GetType();
                EventInfo? eventInfo = extractorType.GetEvent("FilesExtracted");

                if (eventInfo == null)
                {
                    throw new InvalidOperationException($"提取器类型 {extractorType.Name} 不包含 FilesExtracted 事件");
                }

                // 确保 EventHandlerType 不为 null
                Type eventHandlerType = eventInfo.EventHandlerType
                    ?? throw new InvalidOperationException($"事件 {eventInfo.Name} 的处理程序类型未知");

                MethodInfo? invokeMethod = eventHandlerType.GetMethod("Invoke");
                if (invokeMethod == null || invokeMethod.GetParameters().Length != 2)
                {
                    throw new InvalidOperationException("事件处理程序格式不正确");
                }

                Type eventArgsType = invokeMethod.GetParameters()[1].ParameterType;

                // 根据事件参数类型创建适当的处理程序
                if (eventArgsType == typeof(List<string>))
                {
                    EventHandler<List<string>> handler = (s, fileNames) =>
                    {
                        foreach (string fileName in fileNames)
                        {
                            AppendMessageToRichTextBox(fileName);
                            totalFileCount++;
                        }
                    };

                    Delegate handlerDelegate = Delegate.CreateDelegate(
                        eventHandlerType,
                        handler.Target,
                        handler.Method);

                    eventInfo.AddEventHandler(extractor, handlerDelegate);
                }
                else if (eventArgsType == typeof(string[]))
                {
                    EventHandler<string[]> handler = (s, fileNames) =>
                    {
                        foreach (string fileName in fileNames)
                        {
                            AppendMessageToRichTextBox(fileName);
                            totalFileCount++;
                        }
                    };

                    Delegate handlerDelegate = Delegate.CreateDelegate(
                        eventHandlerType,
                        handler.Target,
                        handler.Method);

                    eventInfo.AddEventHandler(extractor, handlerDelegate);
                }
                else
                {
                    throw new NotSupportedException($"不支持的事件参数类型: {eventArgsType.Name}");
                }

                AppendMessageToRichTextBox($"开始提取 {formatName} 格式的文件...");
                extractor.Extract(dirPath);
                AppendMessageToRichTextBox($"提取操作完成，总共提取了 {totalFileCount} 个文件。");
            }
            catch (Exception ex)
            {
                AppendMessageToRichTextBox($"提取过程中出现错误: {ex.Message}");
            }
        }

        private BaseExtractor? CreateExtractor(string formatName)
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
                default: return null;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void AppendMessageToRichTextBox(string message)
        {
            richTextBox1.AppendText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            richTextBox1.ScrollToCaret();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                labelStatus.Text = $"已选择: {e.Node.Text}";
            }
        }

        private void treeViewContextMenu_Opening(object sender, CancelEventArgs e)
        {
            // 根据节点类型启用/禁用菜单项
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.Parent == null)
            {
                e.Cancel = true; // 根节点不显示菜单
                return;
            }

            // 可以在这里根据节点类型禁用某些菜单项
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
            TreeNode selectedNode = treeView1.SelectedNode;
            if (selectedNode == null || selectedNode.Parent == null)
                return;

            TreeNode? targetCategory = treeView1.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == category);
            if (targetCategory == null || selectedNode.Parent == targetCategory)
                return;

            TreeNode oldParent = selectedNode.Parent;
            targetCategory.Nodes.Add((TreeNode)selectedNode.Clone());
            oldParent.Nodes.Remove(selectedNode);
            treeView1.SelectedNode = targetCategory.LastNode;

            AppendMessageToRichTextBox($"已将 {selectedNode.Text} 移动到 {category} 类别");
        }
    }
}
