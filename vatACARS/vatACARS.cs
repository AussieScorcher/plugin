using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using vatACARS.UI;
using vatsys;
using vatsys.Plugin;
using System;
using vatACARS.Services.Authority;
using vatACARS.Util;
using vatACARS.Helpers;
using System.IO;
using Newtonsoft.Json;

namespace vatACARS
{
    [Export(typeof(IPlugin))]
    public class vatACARS : IPlugin
    {
        public string Name { get => "vatACARSNext"; }
        private readonly Logger logger = new Logger("vatACARS");
        private CustomToolStripMenuItem acarsWindowMenu;
        public bool Connected { get; set; }
        public Label ASDLabel { get; set; }
        private string _authToken;
        private VatACARSAuthority VatACARSAuthority;

        public static class AppData
        {
            public static Version CurrentVersion { get; } = new Version(2, 0, 0);
        }

        public vatACARS()
        {
            VatACARSAuthority = new VatACARSAuthority("ws://vatacars.com:3000/gateway");
            string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "vatACARS");
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(Path.Combine(dataPath, "data"));
            Directory.CreateDirectory(Path.Combine(dataPath, "profiles"));

            logger.Log($"vatACARS v{AppData.CurrentVersion} on {RegHelper.FriendlyName()}");

            Thread startThread = new Thread(Start);
            startThread.Start();

            return;
        }

        public async void Start()
        {
            await Task.Delay(3000);


            foreach (Form form in Application.OpenForms)
            {
                if (form.Text.StartsWith("vatSys"))
                {
                    ASDLabel = MainASDLabel.Hook(form);
                }
            }

            Authenticate();
        }

        public async void Authenticate()
        {
            MouseEventHandler clickHandler = null;
            ASDLabel.UpdateVatACARSText("Authenticating...");
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(localAppDataPath, "vatACARS", "_auth.json");

            if (!File.Exists(filePath))
            {
                ASDLabel.UpdateVatACARSText("Please login to the hub and then click here.");
                clickHandler = (sender, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        Authenticate();
                        ASDLabel.MouseClick -= clickHandler;
                    }
                };
                ASDLabel.MouseClick += clickHandler;

                return;
            }

            try
            {
                string authContent = File.ReadAllText(filePath);
                dynamic authObject = JsonConvert.DeserializeObject(authContent);
                _authToken = authObject?.token?.ToString();
                if(string.IsNullOrEmpty(_authToken))
                {
                    throw new Exception("Token not found");
                }
            } catch (Exception ex)
            {
                ASDLabel.UpdateVatACARSText("Failed to authenticate. Log out and back into the hub and then click here.");
                logger.Log($"Failed to authenticate: {ex.Message}");
                clickHandler = (sender, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        Authenticate();
                        ASDLabel.MouseClick -= clickHandler;
                    }
                };
                ASDLabel.MouseClick += clickHandler;
                return;
            }

            bool connection = await VatACARSAuthority.ConnectAsync(_authToken);
            if (!connection)
            {
                ASDLabel.UpdateVatACARSText("Failed to authenticate. Log out and back into the hub and then click here.");
                logger.Log("Failed to connect to vatACARS, bad token?");
                clickHandler = (sender, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        Authenticate();
                        ASDLabel.MouseClick -= clickHandler;
                    }
                };
                ASDLabel.MouseClick += clickHandler;
                return;
            }

            ASDLabel.UpdateVatACARSText("Ready");

            Network.Connected += async (_, e) =>
            {
                if(Connected) return;

                VatACARSAuthority._cancellationTokenSource.Cancel();
                VatACARSAuthority._cancellationTokenSource = new CancellationTokenSource();

                try
                {
                    ASDLabel.UpdateVatACARSText("Please wait...");
                    await Task.Delay(3000, VatACARSAuthority._cancellationTokenSource.Token);
                    string station = VatSys.GetStation();
                    ASDLabel.SetVatACARSPrefix($"vatACARS [{station}]");
                    ASDLabel.UpdateVatACARSText($"Connecting to {station}...");
                    bool stationConnection = await VatACARSAuthority.StationLogon(station);
                    if (!stationConnection)
                    {
                        ASDLabel.UpdateVatACARSText("Failed to connect to station");
                        await Task.Delay(2000, VatACARSAuthority._cancellationTokenSource.Token);
                        ASDLabel.UpdateVatACARSText("Ready");
                        return;
                    }
                    ASDLabel.UpdateVatACARSText($"Connected to {station}");
                    Connected = true;
                    await Task.Delay(2000, VatACARSAuthority._cancellationTokenSource.Token);
                    ASDLabel.UpdateVatACARSText("No new messages");
                } catch (Exception ex)
                {
                    logger.Log($"Failed to connect to station: {ex.Message}");
                }
            };

            Network.Disconnected += async (_, e) =>
            {
                VatACARSAuthority._cancellationTokenSource.Cancel();
                VatACARSAuthority._cancellationTokenSource = new CancellationTokenSource();

                try
                {
                    ASDLabel.SetVatACARSPrefix($"vatACARS v{vatACARS.AppData.CurrentVersion}");
                    ASDLabel.UpdateVatACARSText("Logging off...");
                    await VatACARSAuthority.StationLogoff();
                    ASDLabel.UpdateVatACARSText("Disconnected from network.");
                    Connected = false;
                    await Task.Delay(2000, VatACARSAuthority._cancellationTokenSource.Token);
                    ASDLabel.UpdateVatACARSText("Ready");
                } catch (Exception ex)
                {
                    logger.Log($"Failed to disconnect from station: {ex.Message}");
                }
            };
        }

        public void OnFDRUpdate(FDP2.FDR updated) { }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated) { }
    }
}
