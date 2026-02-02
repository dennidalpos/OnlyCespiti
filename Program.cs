using System;
using System.Windows.Forms;
using GestioneCespiti.Services;

namespace GestioneCespiti
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                Logger.LogError("Fatal error in Main", ex);
                MessageBox.Show(
                    $"Errore critico: {ex.Message}\n\nL'applicazione verrà chiusa.",
                    "Errore Fatale",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Logger.LogError("Unhandled thread exception", e.Exception);
            MessageBox.Show(
                $"Errore non gestito: {e.Exception.Message}",
                "Errore",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.LogError("Unhandled domain exception", ex);
                MessageBox.Show(
                    $"Errore critico: {ex.Message}\n\nL'applicazione verrà chiusa.",
                    "Errore Fatale",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
