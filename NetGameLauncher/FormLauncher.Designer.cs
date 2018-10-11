namespace NetGameLauncher
{
    partial class FormLauncher
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
            this.playButton = new System.Windows.Forms.Button();
            this.changelogBrowser = new System.Windows.Forms.WebBrowser();
            this.updateProgressBar = new System.Windows.Forms.ProgressBar();
            this.statusLabel = new System.Windows.Forms.Label();
            this.updateButton = new System.Windows.Forms.Button();
            this.launcherVersionLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // playButton
            // 
            this.playButton.Location = new System.Drawing.Point(496, 258);
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(83, 30);
            this.playButton.TabIndex = 0;
            this.playButton.Text = "Play";
            this.playButton.UseVisualStyleBackColor = true;
            this.playButton.Click += new System.EventHandler(this.playButton_Click);
            // 
            // changelogBrowser
            // 
            this.changelogBrowser.Location = new System.Drawing.Point(12, 23);
            this.changelogBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.changelogBrowser.Name = "changelogBrowser";
            this.changelogBrowser.Size = new System.Drawing.Size(567, 229);
            this.changelogBrowser.TabIndex = 1;
            this.changelogBrowser.Url = new System.Uri("", System.UriKind.Relative);
            // 
            // updateProgressBar
            // 
            this.updateProgressBar.Location = new System.Drawing.Point(12, 274);
            this.updateProgressBar.Name = "updateProgressBar";
            this.updateProgressBar.Size = new System.Drawing.Size(389, 14);
            this.updateProgressBar.TabIndex = 2;
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(9, 258);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(117, 13);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "Checking for updates...";
            // 
            // updateButton
            // 
            this.updateButton.Location = new System.Drawing.Point(407, 259);
            this.updateButton.Name = "updateButton";
            this.updateButton.Size = new System.Drawing.Size(83, 29);
            this.updateButton.TabIndex = 4;
            this.updateButton.Text = "Update";
            this.updateButton.UseVisualStyleBackColor = true;
            this.updateButton.Click += new System.EventHandler(this.updateButton_Click);
            // 
            // launcherVersionLabel
            // 
            this.launcherVersionLabel.AutoSize = true;
            this.launcherVersionLabel.Location = new System.Drawing.Point(9, 7);
            this.launcherVersionLabel.Name = "launcherVersionLabel";
            this.launcherVersionLabel.Size = new System.Drawing.Size(110, 13);
            this.launcherVersionLabel.TabIndex = 6;
            this.launcherVersionLabel.Text = "Launcher Version: {0}";
            // 
            // FormLauncher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(591, 300);
            this.Controls.Add(this.launcherVersionLabel);
            this.Controls.Add(this.updateButton);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.updateProgressBar);
            this.Controls.Add(this.changelogBrowser);
            this.Controls.Add(this.playButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "FormLauncher";
            this.Text = "NetGame Launcher";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button playButton;
        private System.Windows.Forms.WebBrowser changelogBrowser;
        private System.Windows.Forms.ProgressBar updateProgressBar;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Button updateButton;
        private System.Windows.Forms.Label launcherVersionLabel;
    }
}

