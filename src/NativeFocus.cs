using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MiniScan
{
    internal static class NativeFocus
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);

        public static void Restore(Form form)
        {
            if (form == null || form.IsDisposed || !form.IsHandleCreated) return;
            if (!form.Visible || form.WindowState == FormWindowState.Minimized) return;
            IntPtr handle = form.Handle;
            IntPtr foreground = GetForegroundWindow();
            if (foreground == handle)
            {
                form.Activate();
                return;
            }

            uint foregroundProcess;
            uint foregroundThread = GetWindowThreadProcessId(foreground, out foregroundProcess);
            uint currentThread = GetCurrentThreadId();
            bool attached = false;
            try
            {
                if (foregroundThread != 0 && foregroundThread != currentThread)
                {
                    attached = AttachThreadInput(currentThread, foregroundThread, true);
                }
                ShowWindow(handle, 5);
                BringWindowToTop(handle);
                SetForegroundWindow(handle);
                form.Activate();
            }
            finally
            {
                if (attached) AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }
}
