namespace MediaBrowserConfig {
    partial class MainForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.RenameFolder = new System.Windows.Forms.Button();
            this.RemoveFolder = new System.Windows.Forms.Button();
            this.folderList = new System.Windows.Forms.ListBox();
            this.infoPanel = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.internalFolder = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.changeImage = new System.Windows.Forms.Button();
            this.folderImage = new System.Windows.Forms.PictureBox();
            this.RemoveSubFolder = new System.Windows.Forms.Button();
            this.AddSubFolder = new System.Windows.Forms.Button();
            this.addFolder = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.lblTitle1 = new System.Windows.Forms.Label();
            this.lblTitle2 = new System.Windows.Forms.Label();
            this.lblTitle3 = new System.Windows.Forms.Label();
            this.lnkHelp = new System.Windows.Forms.LinkLabel();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.infoPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.folderImage)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl1.Location = new System.Drawing.Point(12, 39);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(583, 463);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.lnkHelp);
            this.tabPage1.Controls.Add(this.RenameFolder);
            this.tabPage1.Controls.Add(this.RemoveFolder);
            this.tabPage1.Controls.Add(this.folderList);
            this.tabPage1.Controls.Add(this.infoPanel);
            this.tabPage1.Controls.Add(this.addFolder);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(575, 434);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Media Location";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // RenameFolder
            // 
            this.RenameFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RenameFolder.Location = new System.Drawing.Point(101, 390);
            this.RenameFolder.Name = "RenameFolder";
            this.RenameFolder.Size = new System.Drawing.Size(75, 32);
            this.RenameFolder.TabIndex = 7;
            this.RenameFolder.Text = "Rename";
            this.RenameFolder.UseVisualStyleBackColor = true;
            this.RenameFolder.Click += new System.EventHandler(this.RenameFolder_Click);
            // 
            // RemoveFolder
            // 
            this.RemoveFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemoveFolder.Location = new System.Drawing.Point(182, 390);
            this.RemoveFolder.Name = "RemoveFolder";
            this.RemoveFolder.Size = new System.Drawing.Size(75, 32);
            this.RemoveFolder.TabIndex = 6;
            this.RemoveFolder.Text = "Remove";
            this.RemoveFolder.UseVisualStyleBackColor = true;
            this.RemoveFolder.Click += new System.EventHandler(this.RemoveFolder_Click);
            // 
            // folderList
            // 
            this.folderList.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.folderList.FormattingEnabled = true;
            this.folderList.ItemHeight = 16;
            this.folderList.Location = new System.Drawing.Point(20, 60);
            this.folderList.Name = "folderList";
            this.folderList.Size = new System.Drawing.Size(237, 324);
            this.folderList.TabIndex = 5;
            this.folderList.SelectedIndexChanged += new System.EventHandler(this.folderList_SelectedIndexChanged);
            // 
            // infoPanel
            // 
            this.infoPanel.Controls.Add(this.label3);
            this.infoPanel.Controls.Add(this.internalFolder);
            this.infoPanel.Controls.Add(this.label2);
            this.infoPanel.Controls.Add(this.changeImage);
            this.infoPanel.Controls.Add(this.folderImage);
            this.infoPanel.Controls.Add(this.RemoveSubFolder);
            this.infoPanel.Controls.Add(this.AddSubFolder);
            this.infoPanel.Location = new System.Drawing.Point(277, 60);
            this.infoPanel.Name = "infoPanel";
            this.infoPanel.Size = new System.Drawing.Size(284, 324);
            this.infoPanel.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(3, 3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(31, 21);
            this.label3.TabIndex = 7;
            this.label3.Text = "❷";
            // 
            // internalFolder
            // 
            this.internalFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.internalFolder.FormattingEnabled = true;
            this.internalFolder.ItemHeight = 16;
            this.internalFolder.Location = new System.Drawing.Point(7, 165);
            this.internalFolder.Name = "internalFolder";
            this.internalFolder.Size = new System.Drawing.Size(271, 116);
            this.internalFolder.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(7, 145);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(271, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "Your folder can span across multiple folders.";
            // 
            // changeImage
            // 
            this.changeImage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.changeImage.Location = new System.Drawing.Point(170, 95);
            this.changeImage.Name = "changeImage";
            this.changeImage.Size = new System.Drawing.Size(108, 32);
            this.changeImage.TabIndex = 4;
            this.changeImage.Text = "Change Image";
            this.changeImage.UseVisualStyleBackColor = true;
            this.changeImage.Click += new System.EventHandler(this.changeImage_Click);
            // 
            // folderImage
            // 
            this.folderImage.Location = new System.Drawing.Point(40, 3);
            this.folderImage.Name = "folderImage";
            this.folderImage.Size = new System.Drawing.Size(122, 124);
            this.folderImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.folderImage.TabIndex = 3;
            this.folderImage.TabStop = false;
            // 
            // RemoveSubFolder
            // 
            this.RemoveSubFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemoveSubFolder.Location = new System.Drawing.Point(203, 287);
            this.RemoveSubFolder.Name = "RemoveSubFolder";
            this.RemoveSubFolder.Size = new System.Drawing.Size(75, 32);
            this.RemoveSubFolder.TabIndex = 2;
            this.RemoveSubFolder.Text = "Remove";
            this.RemoveSubFolder.UseVisualStyleBackColor = true;
            this.RemoveSubFolder.Click += new System.EventHandler(this.RemoveSubFolder_Click);
            // 
            // AddSubFolder
            // 
            this.AddSubFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddSubFolder.Location = new System.Drawing.Point(7, 287);
            this.AddSubFolder.Name = "AddSubFolder";
            this.AddSubFolder.Size = new System.Drawing.Size(75, 32);
            this.AddSubFolder.TabIndex = 1;
            this.AddSubFolder.Text = "Add...";
            this.AddSubFolder.UseVisualStyleBackColor = true;
            this.AddSubFolder.Click += new System.EventHandler(this.btnAddSubFolder_Click);
            // 
            // addFolder
            // 
            this.addFolder.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.addFolder.Location = new System.Drawing.Point(20, 390);
            this.addFolder.Name = "addFolder";
            this.addFolder.Size = new System.Drawing.Size(75, 32);
            this.addFolder.TabIndex = 2;
            this.addFolder.Text = "Add...";
            this.addFolder.UseVisualStyleBackColor = true;
            this.addFolder.Click += new System.EventHandler(this.addFolder_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(163, 21);
            this.label1.TabIndex = 0;
            this.label1.Text = "❶ Locate your media";
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(575, 434);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Extenders";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // lblTitle1
            // 
            this.lblTitle1.AutoSize = true;
            this.lblTitle1.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle1.Font = new System.Drawing.Font("Segoe UI", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle1.Location = new System.Drawing.Point(404, -1);
            this.lblTitle1.Name = "lblTitle1";
            this.lblTitle1.Size = new System.Drawing.Size(91, 37);
            this.lblTitle1.TabIndex = 1;
            this.lblTitle1.Text = "media";
            this.lblTitle1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblTitle2
            // 
            this.lblTitle2.AutoSize = true;
            this.lblTitle2.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle2.Font = new System.Drawing.Font("Segoe UI", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle2.ForeColor = System.Drawing.Color.Blue;
            this.lblTitle2.Location = new System.Drawing.Point(486, -1);
            this.lblTitle2.Name = "lblTitle2";
            this.lblTitle2.Size = new System.Drawing.Size(112, 37);
            this.lblTitle2.TabIndex = 2;
            this.lblTitle2.Text = "browser";
            // 
            // lblTitle3
            // 
            this.lblTitle3.AutoSize = true;
            this.lblTitle3.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle3.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle3.Location = new System.Drawing.Point(426, 30);
            this.lblTitle3.Name = "lblTitle3";
            this.lblTitle3.Size = new System.Drawing.Size(151, 21);
            this.lblTitle3.TabIndex = 3;
            this.lblTitle3.Text = "c o n f i g u r a t i o n";
            this.lblTitle3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lnkHelp
            // 
            this.lnkHelp.AutoSize = true;
            this.lnkHelp.Location = new System.Drawing.Point(524, 3);
            this.lnkHelp.Name = "lnkHelp";
            this.lnkHelp.Size = new System.Drawing.Size(37, 16);
            this.lnkHelp.TabIndex = 8;
            this.lnkHelp.TabStop = true;
            this.lnkHelp.Text = "Help";
            this.lnkHelp.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkHelp_LinkClicked);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(607, 514);
            this.Controls.Add(this.lblTitle3);
            this.Controls.Add(this.lblTitle2);
            this.Controls.Add(this.lblTitle1);
            this.Controls.Add(this.tabControl1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(623, 550);
            this.MinimumSize = new System.Drawing.Size(623, 550);
            this.Name = "MainForm";
            this.Text = "Media Browser Configuration Wizard";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.infoPanel.ResumeLayout(false);
            this.infoPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.folderImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button addFolder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Panel infoPanel;
        private System.Windows.Forms.Button RemoveSubFolder;
        private System.Windows.Forms.Button AddSubFolder;
        private System.Windows.Forms.Button changeImage;
        private System.Windows.Forms.PictureBox folderImage;
        private System.Windows.Forms.ListBox folderList;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox internalFolder;
        private System.Windows.Forms.Button RemoveFolder;
        private System.Windows.Forms.Button RenameFolder;
        private System.Windows.Forms.Label lblTitle1;
        private System.Windows.Forms.Label lblTitle2;
        private System.Windows.Forms.Label lblTitle3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.LinkLabel lnkHelp;
    }
}

