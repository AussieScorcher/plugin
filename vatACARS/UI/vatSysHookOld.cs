using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace vatACARS.UI
{
    internal class vatSysHookOld
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;

        public static void TestHook(Form form)
        {
            if (form.InvokeRequired)
            {
                form.Invoke(new Action<Form>(TestHook), form);
            }
            else
            {
                Control tb_test = new Label();
                tb_test.Text = "Hello from vatACARS!";
                form.Controls.Add(tb_test);
                tb_test.Location = new Point(300, 300);
                tb_test.BackColor = Color.Transparent;
                tb_test.ForeColor = Color.LightGreen;
                tb_test.Size = new Size(300, 20);
                tb_test.Enabled = false;
                tb_test.BringToFront();
                IntegrateControl(tb_test);
            }
        }

        private static void IntegrateControl(Control control)
        {
            IntPtr hwnd = control.Handle;
            IntPtr exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, new IntPtr(exStyle.ToInt32() | WS_EX_LAYERED | WS_EX_TRANSPARENT));
            SetLayeredWindowAttributes(hwnd, 0, 0, 0);
        }
    }
}
