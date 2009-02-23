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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.RenameFolder = new System.Windows.Forms.Button();
            this.RemoveFolder = new System.Windows.Forms.Button();
            this.folderList = new System.Windows.Forms.ListBox();
            this.infoPanel = new System.Windows.Forms.Panel();
            this.internalFolder = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.changeImage = new System.Windows.Forms.Button();
            this.folderImage = new System.Windows.Forms.PictureBox();
            this.RemoveSubFolder = new System.Windows.Forms.Button();
            this.AddSubFolder = new System.Windows.Forms.Button();
            this.addFolder = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
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
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(568, 493);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.RenameFolder);
            this.tabPage1.Controls.Add(this.RemoveFolder);
            this.tabPage1.Controls.Add(this.folderList);
            this.tabPage1.Controls.Add(this.infoPanel);
            this.tabPage1.Controls.Add(this.addFolder);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(560, 467);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Media Location";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // RenameFolder
            // 
            this.RenameFolder.Location = new System.Drawing.Point(88, 399);
            this.RenameFolder.Name = "RenameFolder";
            this.RenameFolder.Size = new System.Drawing.Size(75, 23);
            this.RenameFolder.TabIndex = 7;
            this.RenameFolder.Text = "Rename";
            this.RenameFolder.UseVisualStyleBackColor = true;
            this.RenameFolder.Click += new System.EventHandler(this.RenameFolder_Click);
            // 
            // RemoveFolder
            // 
            this.RemoveFolder.Location = new System.Drawing.Point(169, 399);
            this.RemoveFolder.Name = "RemoveFolder";
            this.RemoveFolder.Size = new System.Drawing.Size(75, 23);
            this.RemoveFolder.TabIndex = 6;
            this.RemoveFolder.Text = "Remove";
            this.RemoveFolder.UseVisualStyleBackColor = true;
            this.RemoveFolder.Click += new System.EventHandler(this.RemoveFolder_Click);
            // 
            // folderList
            // 
            this.folderList.FormattingEnabled = true;
            this.folderList.Location = new System.Drawing.Point(20, 60);
            this.folderList.Name = "folderList";
            this.folderList.Size = new System.Drawing.Size(224, 329);
            this.folderList.TabIndex = 5;
            this.folderList.SelectedIndexChanged += new System.EventHandler(this.folderList_SelectedIndexChanged);
            // 
            // infoPanel
            // 
            this.infoPanel.Controls.Add(this.internalFolder);
            this.infoPanel.Controls.Add(this.label2);
            this.infoPanel.Controls.Add(this.changeImage);
            this.infoPanel.Controls.Add(this.folderImage);
            this.infoPanel.Controls.Add(this.RemoveSubFolder);
            this.infoPanel.Controls.Add(this.AddSubFolder);
            this.infoPanel.Location = new System.Drawing.Point(263, 60);
            this.infoPanel.Name = "infoPanel";
            this.infoPanel.Size = new System.Drawing.Size(272, 329);
            this.infoPanel.TabIndex = 4;
            // 
            // internalFolder
            // 
            this.internalFolder.FormattingEnabled = true;
            this.internalFolder.Location = new System.Drawing.Point(7, 191);
            this.internalFolder.Name = "internalFolder";
            this.internalFolder.Size = new System.Drawing.Size(250, 95);
            this.internalFolder.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 161);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(234, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "You can span your folder across multiple folders.";
            // 
            // changeImage
            // 
            this.changeImage.Location = new System.Drawing.Point(158, 113);
            this.changeImage.Name = "changeImage";
            this.changeImage.Size = new System.Drawing.Size(99, 23);
            this.changeImage.TabIndex = 4;
            this.changeImage.Text = "Change Image...";
            this.changeImage.UseVisualStyleBackColor = true;
            this.changeImage.Click += new System.EventHandler(this.changeImage_Click);
            // 
            // folderImage
            // 
            this.folderImage.Location = new System.Drawing.Point(3, 10);
            this.folderImage.Name = "folderImage";
            this.folderImage.Size = new System.Drawing.Size(139, 127);
            this.folderImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.folderImage.TabIndex = 3;
            this.folderImage.TabStop = false;
            // 
            // RemoveSubFolder
            // 
            this.RemoveSubFolder.Location = new System.Drawing.Point(96, 306);
            this.RemoveSubFolder.Name = "RemoveSubFolder";
            this.RemoveSubFolder.Size = new System.Drawing.Size(75, 23);
            this.RemoveSubFolder.TabIndex = 2;
            this.RemoveSubFolder.Text = "Remove";
            this.RemoveSubFolder.UseVisualStyleBackColor = true;
            this.RemoveSubFolder.Click += new System.EventHandler(this.RemoveSubFolder_Click);
            // 
            // AddSubFolder
            // 
            this.AddSubFolder.Location = new System.Drawing.Point(3, 306);
            this.AddSubFolder.Name = "AddSubFolder";
            this.AddSubFolder.Size = new System.Drawing.Size(75, 23);
            this.AddSubFolder.TabIndex = 1;
            this.AddSubFolder.Text = "Add...";
            this.AddSubFolder.UseVisualStyleBackColor = true;
            this.AddSubFolder.Click += new System.EventHandler(this.btnAddSubFolder_Click);
            // 
            // addFolder
            // 
            this.addFolder.Location = new System.Drawing.Point(20, 399);
            this.addFolder.Name = "addFolder";
            this.addFolder.Size = new System.Drawing.Size(60, 23);
            this.addFolder.TabIndex = 2;
            this.addFolder.Text = "Add...";
            this.addFolder.UseVisualStyleBackColor = true;
            this.addFolder.Click += new System.EventHandler(this.addFolder_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(16, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(161, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Where is your media?";
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(560, 467);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Extenders";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(607, 514);
            this.Controls.Add(this.tabControl1);
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
    }
}

