using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace GestioneCespiti.Forms.Dialogs
{
    public class AboutDialog : Form
    {
        public AboutDialog()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var productName = "GestioneCespiti - Dismissioni";
            var company = "Developed by Claude AI Assistant";
            var copyright = $"© {DateTime.Now.Year} - Tutti i diritti riservati";

            Text = "Informazioni su";
            Size = new Size(450, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var logoLabel = new Label
            {
                Text = "📊",
                Font = new Font("Segoe UI", 48, FontStyle.Regular),
                Location = new Point(180, 20),
                Size = new Size(100, 80),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var titleLabel = new Label
            {
                Text = productName,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 110),
                Size = new Size(410, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var versionLabel = new Label
            {
                Text = $"Versione {version?.Major}.{version?.Minor}.{version?.Build}",
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Location = new Point(20, 145),
                Size = new Size(410, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkGray
            };

            var companyLabel = new Label
            {
                Text = company,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(20, 180),
                Size = new Size(410, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var copyrightLabel = new Label
            {
                Text = copyright,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Location = new Point(20, 205),
                Size = new Size(410, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray
            };

            var descriptionLabel = new Label
            {
                Text = "Sistema di gestione cespiti e dismissioni\ncon supporto archiviazione ed export Excel",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                Location = new Point(20, 230),
                Size = new Size(410, 35),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
            };

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(175, 240),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };

            Controls.AddRange(new Control[] {
                logoLabel,
                titleLabel,
                versionLabel,
                companyLabel,
                copyrightLabel,
                descriptionLabel,
                btnOk
            });

            AcceptButton = btnOk;
        }
    }
}
