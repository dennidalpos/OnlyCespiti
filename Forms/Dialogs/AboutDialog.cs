using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace GestioneCespiti.Forms.Dialogs
{
    public class AboutDialog : Form
    {
        public AboutDialog()
        {
            const string defaultGithubUsername = "dennidalpos";
            const string defaultProjectUrl = "https://github.com/dennidalpos/OnlyCespiti";

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var productName = "GestioneCespiti - Dismissioni";
            var githubUsername = Environment.GetEnvironmentVariable("GITHUB_USERNAME");
            var projectUrl = Environment.GetEnvironmentVariable("GITHUB_PROJECT_URL");

            if (string.IsNullOrWhiteSpace(githubUsername))
            {
                githubUsername = defaultGithubUsername;
            }

            if (string.IsNullOrWhiteSpace(projectUrl))
            {
                projectUrl = githubUsername != defaultGithubUsername
                    ? $"https://github.com/{githubUsername}/OnlyCespiti"
                    : defaultProjectUrl;
            }
            var company = $"Sviluppato da {githubUsername}";
            var copyright = $"© {DateTime.Now.Year} - Tutti i diritti riservati";

            Text = "Informazioni su";
            ClientSize = new Size(500, 380);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var logoLabel = new Label
            {
                Text = "📊",
                Font = new Font("Segoe UI", 48, FontStyle.Regular),
                Location = new Point((ClientSize.Width - 100) / 2, 20),
                Size = new Size(100, 80),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var titleLabel = new Label
            {
                Text = productName,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 110),
                Size = new Size(ClientSize.Width - 40, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var versionLabel = new Label
            {
                Text = $"Versione {version?.Major}.{version?.Minor}.{version?.Build}",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Location = new Point(20, 145),
                Size = new Size(ClientSize.Width - 40, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkGray
            };

            var companyLabel = new Label
            {
                Text = company,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(20, 175),
                Size = new Size(ClientSize.Width - 40, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var githubLink = new LinkLabel
            {
                Text = projectUrl,
                Font = new Font("Segoe UI", 9, FontStyle.Underline),
                Location = new Point(20, 200),
                Size = new Size(ClientSize.Width - 40, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                LinkBehavior = LinkBehavior.HoverUnderline
            };

            githubLink.Links.Add(0, projectUrl.Length, projectUrl);
            githubLink.LinkClicked += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = projectUrl,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    Clipboard.SetText(projectUrl);
                    MessageBox.Show("Link copiato negli appunti", "Informazioni", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var copyrightLabel = new Label
            {
                Text = copyright,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Location = new Point(20, 225),
                Size = new Size(ClientSize.Width - 40, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray
            };

            var descriptionLabel = new Label
            {
                Text = "Sistema di gestione cespiti e dismissioni\ncon supporto archiviazione ed export Excel",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                Location = new Point(20, 250),
                Size = new Size(ClientSize.Width - 40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
            };

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(120, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };

            btnOk.Location = new Point((ClientSize.Width - btnOk.Width) / 2, ClientSize.Height - btnOk.Height - 20);

            Controls.AddRange(new Control[] {
                logoLabel,
                titleLabel,
                versionLabel,
                companyLabel,
                githubLink,
                copyrightLabel,
                descriptionLabel,
                btnOk
            });

            AcceptButton = btnOk;
        }
    }
}
