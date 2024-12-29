using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using vatsys;

namespace vatACARS.Helpers
{
    public static class VatSys
    {
        public static string GetStation()
        {
            if (Network.Me.IsRealATC && Network.IsConnected)
            {
                if(new[] { "_APP", "_TWR", "_GND", "_DEL" }.Any(suffix => Network.Me.Callsign.EndsWith(suffix)))
                {
                    return MMI.PrimePosition.ArrivalListAirports.ToList().FirstOrDefault();
                }

                return "Y" + Network.Me.Callsign.Split('-')[1].Split('_')[0];
            } else
            {
                return "";
            }
        }
    }
}
