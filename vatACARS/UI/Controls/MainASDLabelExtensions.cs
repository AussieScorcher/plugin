using System;
using System.Windows.Forms;

namespace vatACARS.UI
{
    public static class LabelExtensions
    {
        private static string _prefixText = $"vatACARS v{vatACARS.AppData.CurrentVersion}";

        public static void UpdateVatACARSText(this Label label, string newText)
        {
            if (label == null) return;

            if (label.InvokeRequired)
            {
                label.Invoke(new Action(() => UpdateVatACARSText(label, newText)));
            }
            else
            {
                string version = vatACARS.AppData.CurrentVersion.ToString();
                label.Text = $"{_prefixText} | {newText}";
                label.Size = TextRenderer.MeasureText(label.Text, label.Font);
                label.Invalidate();
            }
        }

        public static void SetVatACARSPrefix(this Label label, string newPrefix)
        {
            string currentText = label.Text.Substring(_prefixText.Length).Trim();
            _prefixText = newPrefix;
            label.UpdateVatACARSText(currentText);
        }
    }
}