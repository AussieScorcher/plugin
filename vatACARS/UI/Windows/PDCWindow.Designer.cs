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
            this.scl_aircraft = new VATSYSControls.ScrollBar();
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
            this.strips_aircraft.Size = new System.Drawing.Size(464, 374);
            this.strips_aircraft.TabIndex = 1;
            this.strips_aircraft.UseCompatibleStateImageBehavior = false;
            this.strips_aircraft.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.strips_aircraft_MouseWheel);
            // 
            // scl_aircraft
            // 
            this.scl_aircraft.ActualHeight = 10;
            this.scl_aircraft.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.scl_aircraft.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.scl_aircraft.Change = 1;
            this.scl_aircraft.Location = new System.Drawing.Point(467, 2);
            this.scl_aircraft.Name = "scl_aircraft";
            this.scl_aircraft.Orientation = System.Windows.Forms.ScrollOrientation.VerticalScroll;
            this.scl_aircraft.PercentageValue = 0F;
            this.scl_aircraft.PreferredHeight = 10;
            this.scl_aircraft.Size = new System.Drawing.Size(20, 374);
            this.scl_aircraft.TabIndex = 2;
            this.scl_aircraft.Value = 0;
            this.scl_aircraft.Scroll += new System.EventHandler(this.scl_aircraft_Scroll);
            this.scl_aircraft.Scrolling += new System.EventHandler(this.scl_aircraft_Scroll);
            this.scl_aircraft.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.strips_aircraft_MouseWheel);
            // 
            // PDCWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(489, 388);
            this.Controls.Add(this.scl_aircraft);
            this.Controls.Add(this.strips_aircraft);
            this.MaximumSize = new System.Drawing.Size(493, 900);
            this.MiddleClickClose = false;
            this.MinimumSize = new System.Drawing.Size(493, 137);
            this.Name = "PDCWindow";
            this.Text = "Pre-Departure Clearance";
            this.Resize += new System.EventHandler(this.PDCWindow_Resize);
            this.ResumeLayout(false);

        }

        #endregion

        private vatsys.ListViewEx strips_aircraft;
        private VATSYSControls.ScrollBar scl_aircraft;
    }
}