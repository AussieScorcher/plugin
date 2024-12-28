using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using vatACARS.Services.Authority;
using vatACARS.UI;
using vatsys;
using vatsys.Plugin;
using System;

namespace vatACARS
{
    [Export(typeof(IPlugin))]
    public class vatACARS : IPlugin
    {
        public string Name { get => "vatACARSNext"; }

        private CustomToolStripMenuItem acarsWindowMenu;

        public vatACARS()
        {
            VatACARSAuthority authority = new VatACARSAuthority("ws://vatacars.com:3000/gateway");
            _ = authority.ConnectAsync("test");

            return;
        }

        public void OnFDRUpdate(FDP2.FDR updated) { }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated) { }
    }
}
