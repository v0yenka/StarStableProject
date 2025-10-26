using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace SwapperGUI
{
    public partial class Form1 : Form
    {
        string backupPath = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "CSA files|*.csa|All files|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtTargetFile.Text = dlg.FileName;
                }
            }
        }

        private void btnSwap_Click(object sender, EventArgs e)
        {
            string target = txtTargetFile.Text;
            string originalText = txtOriginal.Text;
            string replacementText = txtReplacement.Text;

            if (!File.Exists(target))
            {
                MessageBox.Show("Target file not found!");
                return;
            }

            try
            {
                // Backup
                string backupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StarshineBackups");
                Directory.CreateDirectory(backupFolder);
                backupPath = Path.Combine(backupFolder, Path.GetFileName(target) + ".bak");
                File.Copy(target, backupPath, true);

                // Text replacement (simplified)
                string data = File.ReadAllText(target, Encoding.UTF8);
                if (!data.Contains(originalText))
                {
                    MessageBox.Show("Original text not found!");
                    return;
                }
                data = data.Replace(originalText, replacementText);
                File.WriteAllText(target, data, Encoding.UTF8);

                MessageBox.Show("Swap done!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            string gameExe = txtGameExe.Text;
            if (!File.Exists(gameExe))
            {
                MessageBox.Show("Game exe not found!");
                return;
            }
            Process.Start(gameExe);
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(backupPath) && File.Exists(backupPath))
            {
                File.Copy(backupPath, txtTargetFile.Text, true);
                MessageBox.Show("Backup restored!");
            }
        }

        private void btnSelectGame_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Exe files|*.exe|All files|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtGameExe.Text = dlg.FileName;
                }
            }
        }
    }
}