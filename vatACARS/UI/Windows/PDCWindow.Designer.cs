using vatsys;
using System.Windows.Forms;
using VATSYSControls;

namespace vatACARS.UI
{
    partial class PDCWindow
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.strips_aircraft = new vatsys.ListViewEx();
            this.vScrollBar1 = new VATSYSControls.ScrollBar();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // strips_aircraft
            // 
            this.strips_aircraft.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.strips_aircraft.BackColor = System.Drawing.SystemColors.ScrollBar;
            this.strips_aircraft.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.strips_aircraft.HideSelection = false;
            this.strips_aircraft.Location = new System.Drawing.Point(1, 2);
            this.strips_aircraft.MultiSelect = false;
            this.strips_aircraft.Name = "strips_aircraft";
            this.strips_aircraft.Size = new System.Drawing.Size(464, 357);
            this.strips_aircraft.TabIndex = 1;
            this.strips_aircraft.UseCompatibleStateImageBehavior = false;
            // 
            // vScrollBar1
            // 
            this.vScrollBar1.ActualHeight = 10;
            this.vScrollBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.vScrollBar1.Change = 1;
            this.vScrollBar1.Location = new System.Drawing.Point(471, 2);
            this.vScrollBar1.Name = "vScrollBar1";
            this.vScrollBar1.Orientation = System.Windows.Forms.ScrollOrientation.VerticalScroll;
            this.vScrollBar1.PercentageValue = 0F;
            this.vScrollBar1.PreferredHeight = 10;
            this.vScrollBar1.Size = new System.Drawing.Size(20, 357);
            this.vScrollBar1.TabIndex = 2;
            this.vScrollBar1.Value = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 362);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "label1";
            // 
            // PDCWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(493, 388);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.vScrollBar1);
            this.Controls.Add(this.strips_aircraft);
            this.MaximumSize = new System.Drawing.Size(497, 900);
            this.MiddleClickClose = false;
            this.MinimumSize = new System.Drawing.Size(497, 137);
            this.Name = "PDCWindow";
            this.Text = "Pre-Departure Clearance";
            this.Resize += new System.EventHandler(this.PDCWindow_Resize);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private vatsys.ListViewEx strips_aircraft;
        private VATSYSControls.ScrollBar vScrollBar1;
        private Label label1;
    }
}