using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using vatsys;

namespace vatACARS.Helpers
{
    public static class VatSys
    {
        public static async Task<string> GetStationAsync(CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<string>();

            async Task CheckStationAsync()
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    string station = GetStation();
                    if (!string.IsNullOrEmpty(station))
                    {
                        tcs.TrySetResult(station);
                        return;
                    }

                    await Task.Delay(500, cancellationToken);
                }

                tcs.TrySetCanceled(cancellationToken);
            }

            _ = Task.Run(CheckStationAsync, cancellationToken);
            return await tcs.Task;
        }

        private static string GetStation()
        {
            if (Network.Me.IsRealATC && Network.IsConnected)
            {
                if (new[] { "_APP", "_TWR", "_GND", "_DEL" }.Any(suffix => Network.Me.Callsign.EndsWith(suffix)))
                {
                    return MMI.PrimePosition.ArrivalListAirports.ToList().FirstOrDefault();
                }

                return "Y" + Network.Me.Callsign.Split('-')[1].Split('_')[0];
            }
            else
            {
                return "";
            }
        }
    }
}