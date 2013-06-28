namespace WcfBinaryViewer
{
    partial class XmlBinaryViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dictionariesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.useWCFStaticDictionaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.useCustomStaticDictionaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadDynamicDictionaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unloadStaticDictionaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unloadDynamicDictionaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tvBinaryFile = new System.Windows.Forms.TreeView();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.lblStaticDictionary = new System.Windows.Forms.Label();
            this.lblDynamicDictionary = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.dictionariesToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(393, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.openToolStripMenuItem.Text = "&Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // dictionariesToolStripMenuItem
            // 
            this.dictionariesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.useWCFStaticDictionaryToolStripMenuItem,
            this.useCustomStaticDictionaryToolStripMenuItem,
            this.loadDynamicDictionaryToolStripMenuItem,
            this.unloadStaticDictionaryToolStripMenuItem,
            this.unloadDynamicDictionaryToolStripMenuItem});
            this.dictionariesToolStripMenuItem.Name = "dictionariesToolStripMenuItem";
            this.dictionariesToolStripMenuItem.Size = new System.Drawing.Size(74, 20);
            this.dictionariesToolStripMenuItem.Text = "&Dictionaries";
            // 
            // useWCFStaticDictionaryToolStripMenuItem
            // 
            this.useWCFStaticDictionaryToolStripMenuItem.Name = "useWCFStaticDictionaryToolStripMenuItem";
            this.useWCFStaticDictionaryToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
            this.useWCFStaticDictionaryToolStripMenuItem.Text = "Use &WCF Static Dictionary";
            this.useWCFStaticDictionaryToolStripMenuItem.Click += new System.EventHandler(this.useWCFStaticDictionaryToolStripMenuItem_Click);
            // 
            // useCustomStaticDictionaryToolStripMenuItem
            // 
            this.useCustomStaticDictionaryToolStripMenuItem.Name = "useCustomStaticDictionaryToolStripMenuItem";
            this.useCustomStaticDictionaryToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
            this.useCustomStaticDictionaryToolStripMenuItem.Text = "Use Custom &Static Dictionary...";
            this.useCustomStaticDictionaryToolStripMenuItem.Click += new System.EventHandler(this.useCustomStaticDictionaryToolStripMenuItem_Click);
            // 
            // loadDynamicDictionaryToolStripMenuItem
            // 
            this.loadDynamicDictionaryToolStripMenuItem.Name = "loadDynamicDictionaryToolStripMenuItem";
            this.loadDynamicDictionaryToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
            this.loadDynamicDictionaryToolStripMenuItem.Text = "Load &Dynamic Dictionary...";
            this.loadDynamicDictionaryToolStripMenuItem.Click += new System.EventHandler(this.loadDynamicDictionaryToolStripMenuItem_Click);
            // 
            // unloadStaticDictionaryToolStripMenuItem
            // 
            this.unloadStaticDictionaryToolStripMenuItem.Name = "unloadStaticDictionaryToolStripMenuItem";
            this.unloadStaticDictionaryToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
            this.unloadStaticDictionaryToolStripMenuItem.Text = "Unload Static Dictionary";
            this.unloadStaticDictionaryToolStripMenuItem.Click += new System.EventHandler(this.unloadStaticDictionaryToolStripMenuItem_Click);
            // 
            // unloadDynamicDictionaryToolStripMenuItem
            // 
            this.unloadDynamicDictionaryToolStripMenuItem.Name = "unloadDynamicDictionaryToolStripMenuItem";
            this.unloadDynamicDictionaryToolStripMenuItem.Size = new System.Drawing.Size(224, 22);
            this.unloadDynamicDictionaryToolStripMenuItem.Text = "Unload Dynamic Dictionary";
            this.unloadDynamicDictionaryToolStripMenuItem.Click += new System.EventHandler(this.unloadDynamicDictionaryToolStripMenuItem_Click);
            // 
            // tvBinaryFile
            // 
            this.tvBinaryFile.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tvBinaryFile.Location = new System.Drawing.Point(12, 80);
            this.tvBinaryFile.Name = "tvBinaryFile";
            this.tvBinaryFile.Size = new System.Drawing.Size(366, 262);
            this.tvBinaryFile.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(87, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Static Dictionary:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(101, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Dynamic Dictionary:";
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // lblStaticDictionary
            // 
            this.lblStaticDictionary.AutoSize = true;
            this.lblStaticDictionary.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStaticDictionary.Location = new System.Drawing.Point(133, 28);
            this.lblStaticDictionary.Name = "lblStaticDictionary";
            this.lblStaticDictionary.Size = new System.Drawing.Size(34, 13);
            this.lblStaticDictionary.TabIndex = 2;
            this.lblStaticDictionary.Text = "WCF";
            // 
            // lblDynamicDictionary
            // 
            this.lblDynamicDictionary.AutoSize = true;
            this.lblDynamicDictionary.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDynamicDictionary.Location = new System.Drawing.Point(133, 51);
            this.lblDynamicDictionary.Name = "lblDynamicDictionary";
            this.lblDynamicDictionary.Size = new System.Drawing.Size(37, 13);
            this.lblDynamicDictionary.TabIndex = 2;
            this.lblDynamicDictionary.Text = "None";
            // 
            // XmlBinaryViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(393, 359);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblDynamicDictionary);
            this.Controls.Add(this.lblStaticDictionary);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tvBinaryFile);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "XmlBinaryViewer";
            this.Text = "WCF XML Binary Viewer";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dictionariesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem useWCFStaticDictionaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem useCustomStaticDictionaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadDynamicDictionaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem unloadStaticDictionaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem unloadDynamicDictionaryToolStripMenuItem;
        private System.Windows.Forms.TreeView tvBinaryFile;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label lblStaticDictionary;
        private System.Windows.Forms.Label lblDynamicDictionary;
    }
}

