using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace MiniScan
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            try { Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU"); } catch { }
            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                ReportError(e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                ReportError(e.ExceptionObject as Exception);
            };
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }

        private static void ReportError(Exception ex)
        {
            string message = ex != null ? ex.Message : "Неизвестная ошибка";
            string log = string.Empty;
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiniScan");
                Directory.CreateDirectory(dir);
                log = Path.Combine(dir, "error.log");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                if (ex != null) sb.AppendLine(ex.ToString());
                sb.AppendLine();
                File.AppendAllText(log, sb.ToString(), Encoding.UTF8);
            }
            catch { }
            string text = "Произошла ошибка: " + message;
            if (!string.IsNullOrEmpty(log)) text += "\r\n\r\nПодробности записаны в файл:\r\n" + log;
            try { MessageBox.Show(text, "Mini Scan", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch { }
        }
    }
}
