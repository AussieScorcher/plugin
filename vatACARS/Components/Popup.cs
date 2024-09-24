using System;
using System.Windows.Forms;
using vatACARS.Util;
using vatsys;
using static vatACARS.Helpers.Transceiver;
using static vatsys.FDP2;

namespace vatACARS.Components
{
    public partial class PopupWindow : BaseForm
    {
        private string Content;
        private bool Direct = false;
        private bool down = false;
        private ErrorHandler errorHandler = ErrorHandler.GetInstance();
        private FDR FDR;
        private bool up = false;

        public PopupWindow(string content, bool direct, FDR fdr)
        {
            InitializeComponent();
            StyleComponent();
            Direct = direct;
            Content = content;
            FDR = fdr;

            if (direct)
            {
                ShowDirectPopup(content);
            }
            else
            {
                if (content.EndsWith("+"))
                {
                    up = true;
                    content = content.Substring(0, content.Length - 1);
                }
                else if (content.EndsWith("-"))
                {
                    down = true;
                    content = content.Substring(0, content.Length - 1);
                }
                else
                {
                }
                Content = content;
                ShowPopup(content);
            }
        }

        private void btn_1_Click(object sender, EventArgs e)
        {
            if (Direct)
            {
                MMI.OpenDirectToMenu(FDR, MousePosition);
                this.Close();
            }
            if (!Direct && Content.Contains("flight level"))
            {
                try
                {
                    string formattedCFLString = (FDR.CFLString != null && int.Parse(FDR.CFLString) < 110
                    ? "A"
                    : "FL")
                    + FDR.CFLString.PadLeft(3, '0');

                    CPDLCMessage msg = new CPDLCMessage()
                    {
                        State = 0,
                        Station = FDR.Callsign,
                        Content = $"({FDR.Callsign}'s Cleared Flight Level Changed: {formattedCFLString})",
                        TimeReceived = DateTime.UtcNow
                    };

                    DispatchWindow.SelectedMessage = msg;

                    if (up)
                    {
                        DispatchWindow.ShowEditorWindow(msg, "20");
                    }
                    else if (down)
                    {
                        DispatchWindow.ShowEditorWindow(msg, "23");
                    }
                    else
                    {
                        DispatchWindow.ShowEditorWindow(msg);
                    }

                    this.Hide();

                    Timer timer = new Timer(); // This is not the best way to do this.
                    timer.Interval = 300000;
                    timer.Tick += (timerSender, timerEventArgs) => this.Close();
                    timer.Start();
                }
                catch (Exception ex)
                {
                    errorHandler.AddError(ex.ToString());
                }
            }
        }

        private void btn_2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ShowDirectPopup(string content)
        {
            try
            {
                lbl_content.Text = content;
                btn_1.Text = "YES";
                btn_2.Text = "NO";
            }
            catch (Exception ex)
            {
                errorHandler.AddError(ex.ToString());
            }
        }

        private void ShowPopup(string content)
        {
            try
            {
                lbl_content.Text = content;
                btn_1.Text = "YES";
                btn_2.Text = "NO";
            }
            catch (Exception ex)
            {
                errorHandler.AddError(ex.ToString());
            }
        }

        private void StyleComponent()
        {
            try
            {
                lbl_content.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
                lbl_content.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);

                btn_1.BackColor = Colours.GetColour(Colours.Identities.CPDLCSendButton);
                btn_1.ForeColor = Colours.GetColour(Colours.Identities.NonJurisdictionIQL);

                btn_2.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
                btn_2.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }
            catch (Exception ex)
            {
                errorHandler.AddError(ex.ToString());
            }
        }
    }
}