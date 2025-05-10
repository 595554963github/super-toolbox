// MainForm.Designer.cs
namespace supertoolbox.Extractor
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            btnSelectFolder = new Button();
            txtFolderPath = new TextBox();
            btnExtract = new Button();
            richTextBox1 = new RichTextBox();
            btnClear = new Button();
            treeView1 = new TreeView();
            treeViewContextMenu = new ContextMenuStrip(components);
            moveToAudioMenuItem = new ToolStripMenuItem();
            moveToImageMenuItem = new ToolStripMenuItem();
            moveToOtherMenuItem = new ToolStripMenuItem();
            labelStatus = new Label();
            treeViewContextMenu.SuspendLayout();
            SuspendLayout();
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.Location = new Point(14, 16);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(104, 26);
            btnSelectFolder.TabIndex = 0;
            btnSelectFolder.Text = "选择文件夹";
            btnSelectFolder.UseVisualStyleBackColor = true;
            btnSelectFolder.Click += btnSelectFolder_Click;
            // 
            // txtFolderPath
            // 
            txtFolderPath.BackColor = Color.White;
            txtFolderPath.Location = new Point(138, 16);
            txtFolderPath.Name = "txtFolderPath";
            txtFolderPath.Size = new Size(490, 25);
            txtFolderPath.TabIndex = 1;
            // 
            // btnExtract
            // 
            btnExtract.ForeColor = Color.Lime;
            btnExtract.Location = new Point(429, 408);
            btnExtract.Name = "btnExtract";
            btnExtract.Size = new Size(85, 30);
            btnExtract.TabIndex = 2;
            btnExtract.Text = "提取";
            btnExtract.UseVisualStyleBackColor = true;
            btnExtract.Click += btnExtract_Click;
            // 
            // richTextBox1
            // 
            richTextBox1.BackColor = Color.White;
            richTextBox1.Location = new Point(636, 13);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(349, 425);
            richTextBox1.TabIndex = 10;
            richTextBox1.Text = "";
            // 
            // btnClear
            // 
            btnClear.ForeColor = Color.OrangeRed;
            btnClear.Location = new Point(540, 408);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(88, 30);
            btnClear.TabIndex = 11;
            btnClear.Text = "清除";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;
            // 
            // treeView1
            // 
            treeView1.ContextMenuStrip = treeViewContextMenu;
            treeView1.Location = new Point(14, 48);
            treeView1.Name = "treeView1";
            treeView1.Size = new Size(614, 332);
            treeView1.TabIndex = 12;
            treeView1.AfterSelect += treeView1_AfterSelect;
            // 
            // treeViewContextMenu
            // 
            treeViewContextMenu.Items.AddRange(new ToolStripItem[] { moveToAudioMenuItem, moveToImageMenuItem, moveToOtherMenuItem });
            treeViewContextMenu.Name = "treeViewContextMenu";
            treeViewContextMenu.Size = new Size(161, 70);
            treeViewContextMenu.Opening += treeViewContextMenu_Opening;
            // 
            // moveToAudioMenuItem
            // 
            moveToAudioMenuItem.Name = "moveToAudioMenuItem";
            moveToAudioMenuItem.Size = new Size(160, 22);
            moveToAudioMenuItem.Text = "移动到音频";
            moveToAudioMenuItem.Click += moveToAudioMenuItem_Click;
            // 
            // moveToImageMenuItem
            // 
            moveToImageMenuItem.Name = "moveToImageMenuItem";
            moveToImageMenuItem.Size = new Size(160, 22);
            moveToImageMenuItem.Text = "移动到图片";
            moveToImageMenuItem.Click += moveToImageMenuItem_Click;
            // 
            // moveToOtherMenuItem
            // 
            moveToOtherMenuItem.Name = "moveToOtherMenuItem";
            moveToOtherMenuItem.Size = new Size(160, 22);
            moveToOtherMenuItem.Text = "移动到其他档案";
            moveToOtherMenuItem.Click += moveToOtherMenuItem_Click;
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.ForeColor = Color.Gray;
            labelStatus.Location = new Point(14, 383);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(104, 17);
            labelStatus.TabIndex = 16;
            labelStatus.Text = "请选择提取器";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(994, 450);
            Controls.Add(labelStatus);
            Controls.Add(treeView1);
            Controls.Add(btnClear);
            Controls.Add(richTextBox1);
            Controls.Add(btnExtract);
            Controls.Add(txtFolderPath);
            Controls.Add(btnSelectFolder);
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ForeColor = Color.Fuchsia;
            Margin = new Padding(4);
            Name = "MainForm";
            Text = "超级工具箱";
            treeViewContextMenu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSelectFolder;
        private System.Windows.Forms.TextBox txtFolderPath;
        private System.Windows.Forms.Button btnExtract;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.ContextMenuStrip treeViewContextMenu;
        private System.Windows.Forms.ToolStripMenuItem moveToAudioMenuItem;
        private System.Windows.Forms.ToolStripMenuItem moveToImageMenuItem;
        private System.Windows.Forms.ToolStripMenuItem moveToOtherMenuItem;
    }
}
