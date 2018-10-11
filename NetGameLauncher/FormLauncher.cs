using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using NetGameShared;

namespace NetGameLauncher
{
    public partial class FormLauncher : Form
    {
        public static string RootDirectory = new FileInfo(Application.ExecutablePath).DirectoryName;

        List<FileStateInfo> corruptFiles = new List<FileStateInfo>();
        const string UPDATE_URL = "http://ohj35.github.io/games/NetGame/";
        const string CHANGELOG_URL = "http://ohj35.github.io/games/NetGame/changelog.html";
        const string CONTENT_FILE = "http://ohj35.github.io/games/NetGame/content.xml";
        const string INFO_FILE = "http://ohj35.github.io/games/NetGame/content.info.txt";
        const string SOURCE_PATH = "../../../NetGameClient/bin/Debug/";
        const bool GENERATE_MODE = false;

        WebClient webClient;
        ContentFile contentFile;
        BackgroundWorker contentFileDownloadWorker, fileCheckWorker, fileUpdateWorker;

        public FormLauncher()
        {
            InitializeComponent();
            RefreshChangelog();
            MaximizeBox = false;
            MinimizeBox = false;
            playButton.Enabled = false;
            updateButton.Enabled = false;

            if (GENERATE_MODE)
            {
                contentFile = new ContentFile();
                contentFile.GenerateContentFiles(SOURCE_PATH);
                statusLabel.Text = "Content files generated.";
            }
            else
            {
                webClient = new WebClient();
                launcherVersionLabel.Text = string.Format(launcherVersionLabel.Text, Universal.LAUNCHER_VERSION);
                contentFileDownloadWorker = new BackgroundWorker();
                contentFileDownloadWorker.DoWork += new DoWorkEventHandler(contentFileDownloadWorker_DoWork);
                contentFileDownloadWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.contentFileDownloadWorker_RunWorkerCompleted);
                CheckUpdate();
            }
        }

        private void RefreshChangelog()
        {
            changelogBrowser.Navigate(CHANGELOG_URL);
            changelogBrowser.Refresh(WebBrowserRefreshOption.Completely);
        }

        private void contentFileDownloadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (File.Exists("content.xml"))
                File.Delete("content.xml");
            if (File.Exists("content.info.txt"))
                File.Delete("content.info.txt");
            try
            {
                webClient.DownloadFile(CONTENT_FILE, "content.xml");
            }
            catch
            {
                Logger.Log("Failed to download \"content.xml\"");
            }
            try
            {
                webClient.DownloadFile(INFO_FILE, "content.info.txt");
            }
            catch
            {
                Logger.Log("Failed to download \"content.info.txt\"");
            }
            contentFile = ContentFile.FromFile(RootDirectory.CombinePath("content.xml"));
            if (contentFile != null)
                return;
            e.Cancel = true;
        }

        private void contentFileDownloadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                statusLabel.Text = "Error: Update info could not be downloaded.";
                playButton.Enabled = true;
                updateButton.Enabled = true;
            }
            else
            {
                statusLabel.Text = "Checking files...";
                fileCheckWorker = new BackgroundWorker();
                fileCheckWorker.DoWork += new DoWorkEventHandler(this.fileCheckWorker_DoWork);
                fileCheckWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.fileCheckWorker_RunWorkerCompleted);
                fileCheckWorker.RunWorkerAsync();
            }
        }

        private void fileCheckWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            corruptFiles.Clear();
            if (contentFile == null)
                return;
            foreach (FileStateInfo file in contentFile.Files)
            {
                if (!file.CheckLocal())
                    corruptFiles.Add(file);
            }
        }

        private void fileCheckWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (corruptFiles.Count > 0)
            {
                statusLabel.Text = String.Format("New version available! ({0}) Press Update to download.", contentFile.GameVersion);
                playButton.Enabled = true;
                updateButton.Enabled = true;
            }
            else
                UpdateComplete();
        }

        private void fileUpdateWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (FileStateInfo corruptFile in corruptFiles)
                RequestFile(corruptFile);
        }

        private void fileUpdateWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateComplete();
        }

        private void CheckUpdate()
        {
            statusLabel.Text = "Checking for updates...";
            playButton.Enabled = false;
            updateButton.Enabled = false;
            contentFileDownloadWorker.RunWorkerAsync();
        }

        private void UpdateComplete()
        {
            statusLabel.Text = String.Format("Game is up to date. ({0})", contentFile.GameVersion);
            corruptFiles.Clear();
            updateProgressBar.Value = updateProgressBar.Maximum;
            playButton.Enabled = true;
            updateButton.Enabled = true;
        }

        private void RequestFile(FileStateInfo file)
        {
            Invoke(new CrossAppDomainDelegate(updateProgressBar.PerformStep));
            string uriString = UPDATE_URL + file.FileName.Replace('\\', '/');
            string fileName = RootDirectory.CombinePath(file.FileName);
            FileInfo fileInfo = new FileInfo(fileName);
            if (!Directory.Exists(fileInfo.DirectoryName))
                Directory.CreateDirectory(fileInfo.DirectoryName);
            try
            {
                Uri address = new Uri(uriString);
                webClient.DownloadFile(address, fileName);
            }
            catch
            {
                Logger.Log("Failed to download \"" + file.FileName + "\"");
            }
        }

        private void playButton_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(Universal.EXECUTABLE_PATH);
                Application.Exit();
            }
            catch
            {
                int num = (int)MessageBox.Show("Game could not be started! \nPlease try updating again or restarting the launcher.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void updateButton_Click(object sender, EventArgs e)
        {
            if (corruptFiles.Count > 0)
            {
                playButton.Enabled = false;
                updateButton.Enabled = false;
                statusLabel.Text = String.Format("Downloading {0}...", contentFile.GameVersion);
                updateProgressBar.Value = 0;
                updateProgressBar.Maximum = corruptFiles.Count;
                fileUpdateWorker = new BackgroundWorker();
                fileUpdateWorker.DoWork += new DoWorkEventHandler(this.fileUpdateWorker_DoWork);
                fileUpdateWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.fileUpdateWorker_RunWorkerCompleted);
                fileUpdateWorker.RunWorkerAsync();
            }
            else
                CheckUpdate();
        }
    }
}
