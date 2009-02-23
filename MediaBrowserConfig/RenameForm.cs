using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MediaBrowserConfig {
    public partial class RenameForm : Form {
        public RenameForm(string name) {
            InitializeComponent();
            folderName.Text = name;
        }

        public string FolderName { get { return folderName.Text; } }

        private void cancelButton_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void okButton_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
