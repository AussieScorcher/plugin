using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using vatsys;
using vatACARS.Lib;
using static vatACARS.Util.ExtendedUI;
using vatACARS.Util;
using static vatsys.CPDLC;

namespace vatACARS.Components
{
    public partial class ProfileSelector : BaseForm
    {
        private static Logger logger = new Logger("ProfileSelector");

        public ProfileSelector()
        {
            InitializeComponent();
            StyleComponent();

            foreach (Lib.Profile profile in ProfileManager.Profiles)
            {
                AddProfileLabel(profile);
            }
        }

        public void AddProfileLabel(Lib.Profile profile)
        {
            try
            {
                GenericButton profileLabel = new GenericButton()
                {
                    Text = profile.Name.ToUpper(),
                    Font = MMI.eurofont_winsml,
                    ForeColor = Colours.GetColour(Colours.Identities.InteractiveText),
                    Size = new Size(176, 26),
                    Tag = profile
                };

                profileLabel.Click += ProfileLabel_Click;

                pnl_profiles.Controls.Add(profileLabel);
            }
            catch (Exception ex)
            {
                logger.Log($"Something went wrong:\n{ex.ToString()}");
            }
        }

        private void ProfileLabel_Click(object sender, EventArgs e)
        {
            GenericButton profileLabel = (GenericButton)sender;
            Lib.Profile profile = (Lib.Profile)profileLabel.Tag;
            logger.Log($"Selected profile: {profile.Name}");
            ErrorHandler.GetInstance().AddError($"Selected profile: {profile.Name}");
            ErrorHandler.GetInstance().AddError($"Restart vatSys to apply changes.");
            //TODO: Implement profile selection
        }

        private void StyleComponent()
        {
            this.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
        }
    }
}
