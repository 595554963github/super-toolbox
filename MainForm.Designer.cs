using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace supertoolbox.Extractor
{
    partial class MainForm : Form
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
            btnSelectFolder = new Button();
            txtFolderPath = new TextBox();
            btnExtract = new Button();
            radioWave = new RadioButton();
            radioBank = new RadioButton();
            radioWebp = new RadioButton();
            radioXwma = new RadioButton();
            radioWem = new RadioButton();
            radioXa = new RadioButton();
            radioAdx = new RadioButton();
            richTextBox1 = new RichTextBox();
            btnClear = new Button();
            radioAhx = new RadioButton();
            radioFsb5 = new RadioButton();
            label1 = new Label();
            label2 = new Label();
            radioOgg = new RadioButton();
            radioJpg = new RadioButton();
            radioPng = new RadioButton();
            radioHca = new RadioButton();
            SuspendLayout();
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.Location = new Point(14, 16);
            btnSelectFolder.Margin = new Padding(4);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(116, 26);
            btnSelectFolder.TabIndex = 0;
            btnSelectFolder.Text = "选择文件夹";
            btnSelectFolder.UseVisualStyleBackColor = true;
            btnSelectFolder.Click += btnSelectFolder_Click;
            // 
            // txtFolderPath
            // 
            txtFolderPath.Location = new Point(138, 16);
            txtFolderPath.Margin = new Padding(4);
            txtFolderPath.Name = "txtFolderPath";
            txtFolderPath.Size = new Size(490, 23);
            txtFolderPath.TabIndex = 1;
            // 
            // btnExtract
            // 
            btnExtract.Location = new Point(429, 408);
            btnExtract.Margin = new Padding(4);
            btnExtract.Name = "btnExtract";
            btnExtract.Size = new Size(85, 30);
            btnExtract.TabIndex = 2;
            btnExtract.Text = "提取";
            btnExtract.UseVisualStyleBackColor = true;
            btnExtract.Click += btnExtract_Click;
            // 
            // radioWave
            // 
            radioWave.AutoSize = true;
            radioWave.Location = new Point(14, 68);
            radioWave.Margin = new Padding(4);
            radioWave.Name = "radioWave";
            radioWave.Size = new Size(116, 21);
            radioWave.TabIndex = 3;
            radioWave.TabStop = true;
            radioWave.Text = "RIFF - wave系列";
            radioWave.UseVisualStyleBackColor = true;
            // 
            // radioBank
            // 
            radioBank.AutoSize = true;
            radioBank.Location = new Point(14, 114);
            radioBank.Margin = new Padding(4);
            radioBank.Name = "radioBank";
            radioBank.Size = new Size(138, 21);
            radioBank.TabIndex = 4;
            radioBank.TabStop = true;
            radioBank.Text = "RIFF - Fmod - bank";
            radioBank.UseVisualStyleBackColor = true;
            // 
            // radioWebp
            // 
            radioWebp.AutoSize = true;
            radioWebp.Location = new Point(14, 140);
            radioWebp.Margin = new Padding(4);
            radioWebp.Name = "radioWebp";
            radioWebp.Size = new Size(151, 21);
            radioWebp.TabIndex = 5;
            radioWebp.TabStop = true;
            radioWebp.Text = "RIFF - Google - webp";
            radioWebp.UseVisualStyleBackColor = true;
            // 
            // radioXwma
            // 
            radioXwma.AutoSize = true;
            radioXwma.Location = new Point(14, 166);
            radioXwma.Margin = new Padding(4);
            radioXwma.Name = "radioXwma";
            radioXwma.Size = new Size(149, 21);
            radioXwma.TabIndex = 6;
            radioXwma.TabStop = true;
            radioXwma.Text = "RIFF - wmav2 - xwma";
            radioXwma.UseVisualStyleBackColor = true;
            // 
            // radioWem
            // 
            radioWem.AutoSize = true;
            radioWem.Location = new Point(14, 192);
            radioWem.Margin = new Padding(4);
            radioWem.Name = "radioWem";
            radioWem.Size = new Size(163, 21);
            radioWem.TabIndex = 7;
            radioWem.TabStop = true;
            radioWem.Text = "RIFX - BigEndian - wem";
            radioWem.UseVisualStyleBackColor = true;
            // 
            // radioXa
            // 
            radioXa.AutoSize = true;
            radioXa.Location = new Point(14, 218);
            radioXa.Margin = new Padding(4);
            radioXa.Name = "radioXa";
            radioXa.Size = new Size(116, 21);
            radioXa.TabIndex = 8;
            radioXa.TabStop = true;
            radioXa.Text = "RIFF - cdxa - xa";
            radioXa.UseVisualStyleBackColor = true;
            // 
            // radioAdx
            // 
            radioAdx.AutoSize = true;
            radioAdx.Location = new Point(14, 245);
            radioAdx.Margin = new Padding(4);
            radioAdx.Name = "radioAdx";
            radioAdx.Size = new Size(159, 21);
            radioAdx.TabIndex = 9;
            radioAdx.TabStop = true;
            radioAdx.Text = "CRI - adpcm_adx - adx";
            radioAdx.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(636, 13);
            richTextBox1.Margin = new Padding(4);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(349, 425);
            richTextBox1.TabIndex = 10;
            richTextBox1.Text = "";
            // 
            // btnClear
            // 
            btnClear.Location = new Point(540, 408);
            btnClear.Margin = new Padding(4);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(88, 30);
            btnClear.TabIndex = 11;
            btnClear.Text = "清除";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;
            // 
            // radioAhx
            // 
            radioAhx.AutoSize = true;
            radioAhx.Location = new Point(14, 271);
            radioAhx.Margin = new Padding(4);
            radioAhx.Name = "radioAhx";
            radioAhx.Size = new Size(158, 21);
            radioAhx.TabIndex = 12;
            radioAhx.TabStop = true;
            radioAhx.Text = "CRI - adpcm_adx - ahx";
            radioAhx.UseVisualStyleBackColor = true;
            // 
            // radioFsb5
            // 
            radioFsb5.AutoSize = true;
            radioFsb5.Location = new Point(14, 297);
            radioFsb5.Margin = new Padding(4);
            radioFsb5.Name = "radioFsb5";
            radioFsb5.Size = new Size(97, 21);
            radioFsb5.TabIndex = 13;
            radioFsb5.TabStop = true;
            radioFsb5.Text = "Fmod - fsb5";
            radioFsb5.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(14, 350);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(0, 17);
            label1.TabIndex = 14;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.ForeColor = SystemColors.GrayText;
            label2.Location = new Point(12, 93);
            label2.Name = "label2";
            label2.Size = new Size(145, 17);
            label2.TabIndex = 15;
            label2.Text = "(wav/at3/at9/xma/wem)";
            // 
            // radioOgg
            // 
            radioOgg.AutoSize = true;
            radioOgg.Location = new Point(14, 323);
            radioOgg.Margin = new Padding(4);
            radioOgg.Name = "radioOgg";
            radioOgg.Size = new Size(117, 21);
            radioOgg.TabIndex = 16;
            radioOgg.TabStop = true;
            radioOgg.Text = "Xiph.Org - Ogg";
            radioOgg.UseVisualStyleBackColor = true;
            // 
            // radioJpg
            // 
            radioJpg.AutoSize = true;
            radioJpg.Location = new Point(14, 345);
            radioJpg.Margin = new Padding(4);
            radioJpg.Name = "radioJpg";
            radioJpg.Size = new Size(80, 21);
            radioJpg.TabIndex = 18;
            radioJpg.TabStop = true;
            radioJpg.Text = "JPEG/JPG";
            radioJpg.UseVisualStyleBackColor = true;
            // 
            // radioPng
            // 
            radioPng.AutoSize = true;
            radioPng.Location = new Point(14, 367);
            radioPng.Margin = new Padding(4);
            radioPng.Name = "radioPng";
            radioPng.Size = new Size(52, 21);
            radioPng.TabIndex = 19;
            radioPng.TabStop = true;
            radioPng.Text = "PNG";
            radioPng.UseVisualStyleBackColor = true;
            //
            //radioHca
            //
            radioHca.AutoSize = true;
            radioHca.Location = new Point(14, 389);
            radioHca.Margin = new Padding(4);
            radioHca.Name = "radioHca";
            radioHca.Size = new Size(24, 21);
            radioHca.TabIndex = 19;
            radioHca.TabStop = true;
            radioHca.Text = "CRI - HCA - hca";
            radioHca.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(994, 441);
            Controls.Add(radioHca);
            Controls.Add(radioPng);
            Controls.Add(radioJpg);
            Controls.Add(radioOgg);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnClear);
            Controls.Add(richTextBox1);
            Controls.Add(radioFsb5);
            Controls.Add(radioAhx);
            Controls.Add(radioAdx);
            Controls.Add(radioXa);
            Controls.Add(radioWem);
            Controls.Add(radioXwma);
            Controls.Add(radioWebp);
            Controls.Add(radioBank);
            Controls.Add(radioWave);
            Controls.Add(btnExtract);
            Controls.Add(txtFolderPath);
            Controls.Add(btnSelectFolder);
            ForeColor = Color.Blue;
            Margin = new Padding(4);
            Name = "MainForm";
            Text = "超级工具箱";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnSelectFolder;
        private System.Windows.Forms.TextBox txtFolderPath;
        private System.Windows.Forms.Button btnExtract;
        private System.Windows.Forms.RadioButton radioWave;
        private System.Windows.Forms.RadioButton radioBank;
        private System.Windows.Forms.RadioButton radioWebp;
        private System.Windows.Forms.RadioButton radioXwma;
        private System.Windows.Forms.RadioButton radioWem;
        private System.Windows.Forms.RadioButton radioXa;
        private System.Windows.Forms.RadioButton radioAdx;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.RadioButton radioAhx;
        private System.Windows.Forms.RadioButton radioFsb5;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RadioButton radioOgg;
        private System.Windows.Forms.RadioButton radioJpg;
        private System.Windows.Forms.RadioButton radioPng;
        private System.Windows.Forms.RadioButton radioHca;
        }
    }
namespace supertoolbox.Extractor
{
    partial class MainForm : Form
    {
    }
}
