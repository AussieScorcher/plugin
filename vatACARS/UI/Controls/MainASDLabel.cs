using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace vatACARS.UI
{
    internal class MainASDLabel
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

        private static bool isDragging = false;
        private static Point dragStartPoint;

        public static Label Hook(Form form)
        {
            if (form.InvokeRequired)
            {
                return (Label)form.Invoke(new Func<Form, Label>(Hook), form);
            }
            else
            {
                Control tb_test = new Label();
                tb_test.Text = $"vatACARS v{vatACARS.AppData.CurrentVersion.ToString()}";
                form.Controls.Add(tb_test);
                tb_test.Location = new Point(15, 40);
                tb_test.BackColor = Color.Transparent;
                tb_test.ForeColor = Color.LightGray;
                tb_test.Size = new Size(300, 20);
                tb_test.Font = vatsys.MMI.eurofont_winsml;
                tb_test.Enabled = true;
                tb_test.BringToFront();
                IntegrateControl(tb_test);

                tb_test.MouseDown += Label_MouseDown;
                tb_test.MouseMove += Label_MouseMove;

                return (Label)tb_test;
            }
        }

        private static void IntegrateControl(Control control)
        {
            IntPtr hwnd = control.Handle;
            IntPtr exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, new IntPtr(exStyle.ToInt32() | WS_EX_LAYERED | WS_EX_TRANSPARENT));
            SetLayeredWindowAttributes(hwnd, 0, 0, 0);
        }

        private static void Label_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is Label label)
            {
                if (e.Button == MouseButtons.Right)
                {
                    // Toggle dragging state
                    isDragging = !isDragging;

                    if (isDragging)
                    {
                        dragStartPoint = e.Location;
                        label.ForeColor = Color.Yellow;
                    }
                    else
                    {
                        label.ForeColor = Color.LightGray;
                    }
                }
            }
        }

        private static void Label_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && sender is Label label)
            {
                var newLocation = new Point(
                    label.Left + e.X - dragStartPoint.X,
                    label.Top + e.Y - dragStartPoint.Y
                );
                label.Location = newLocation;
            }
        }
    }
}
