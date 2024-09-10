namespace vatACARS.Components
{
    partial class ProfileSelector
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnl_profiles = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();
            // 
            // pnl_profiles
            // 
            this.pnl_profiles.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnl_profiles.Location = new System.Drawing.Point(1, 12);
            this.pnl_profiles.Name = "pnl_profiles";
            this.pnl_profiles.Size = new System.Drawing.Size(182, 323);
            this.pnl_profiles.TabIndex = 0;
            this.pnl_profiles.WrapContents = false;
            // 
            // ProfileSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(185, 336);
            this.Controls.Add(this.pnl_profiles);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Name = "ProfileSelector";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "vatACARS Profile Selector";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel pnl_profiles;
    }
}