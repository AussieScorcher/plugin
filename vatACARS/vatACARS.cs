using Newtonsoft.Json;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using vatACARS.Helpers;
using vatACARS.Services.Authority;
using vatACARS.UI;
using vatACARS.Util;
using vatsys;
using vatsys.Plugin;
using RossCarlson.Vatsim.Network;

namespace vatACARS
{
    [Export(typeof(IPlugin))]
    public class vatACARS : IPlugin
    {
        private static DispatchWindow dispatchWindow;

        // Toolstrip items
        private static PDCWindow pdcWindow;

        private static SettingsWindow settingsWindow;
        private readonly Logger logger = new Logger("vatACARS");
        private string _authToken;
        private CustomToolStripMenuItem dispatchWindowMenu;
        private CustomToolStripMenuItem pdcWindowMenu;
        private CustomToolStripMenuItem settingsWindowMenu;
        private VatACARSAuthority VatACARSAuthority;

        public vatACARS()
        {
            VatACARSAuthority = new VatACARSAuthority("ws://vatacars.com:3000/gateway");
            string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "vatACARS");
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(Path.Combine(dataPath, "data"));
            Directory.CreateDirectory(Path.Combine(dataPath, "profiles"));

            logger.Log($"vatACARS v{AppData.CurrentVersion} on {RegHelper.FriendlyName()}");

            Network.Connected += Vatsys_ConnectionChanged;
            Network.Disconnected += Vatsys_ConnectionChanged;

            Thread startThread = new Thread(Start);
            startThread.Start();

            return;
        }

        public Label ASDLabel { get; set; }
        public bool Connected { get; set; }
        public string Name { get => "vatACARSNext"; }

        public static void DoShowDispatchWindow()
        {
            if (pdcWindow == null || pdcWindow.IsDisposed)
            {
                pdcWindow = new PDCWindow();
            }
            else if (pdcWindow.Visible)
            {
                return;
            }
            pdcWindow.Show(Form.ActiveForm);
        }

        public static void DoShowPDCWindow()
        {
            if (pdcWindow == null || pdcWindow.IsDisposed)
            {
                pdcWindow = new PDCWindow();
            }
            else if (pdcWindow.Visible)
            {
                return;
            }
            pdcWindow.Show(Form.ActiveForm);
        }

        public static void DoShowSettingsWindow()
        {
            if (settingsWindow == null || settingsWindow.IsDisposed)
            {
                settingsWindow = new SettingsWindow();
            }
            else if (settingsWindow.Visible)
            {
                return;
            }
            settingsWindow.Show(Form.ActiveForm);
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
                if (string.IsNullOrEmpty(_authToken))
                {
                    throw new Exception("Token not found");
                }
            }
            catch (Exception ex)
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
                if (Connected) return;

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
                }
                catch (Exception ex)
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
                }
                catch (Exception ex)
                {
                    logger.Log($"Failed to disconnect from station: {ex.Message}");
                }
            };
        }

        public void OnFDRUpdate(FDP2.FDR updated)
        { }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        { }

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

            // Add buttons to vatSys toolstrip
            pdcWindowMenu = new CustomToolStripMenuItem(
                CustomToolStripMenuItemWindowType.Main,
                CustomToolStripMenuItemCategory.Windows,
            new ToolStripMenuItem("PDC - ACARS")
            );
            pdcWindowMenu.Item.Click += pdcWindowMenu_Click;
            pdcWindowMenu.Item.Enabled = false;

            dispatchWindowMenu = new CustomToolStripMenuItem(
                CustomToolStripMenuItemWindowType.Main,
                CustomToolStripMenuItemCategory.Windows,
                new ToolStripMenuItem("Dispatch - ACARS")
                );
            dispatchWindowMenu.Item.Click += DispatchWindowMenu_Click;
            dispatchWindowMenu.Item.Enabled = false;

            settingsWindowMenu = new CustomToolStripMenuItem(
                CustomToolStripMenuItemWindowType.Main,
                CustomToolStripMenuItemCategory.Windows,
                new ToolStripMenuItem("Settings - ACARS")
                );
            settingsWindowMenu.Item.Click += SettingsWindowMenu_Click;
            settingsWindowMenu.Item.Enabled = true;

            MMI.AddCustomMenuItem(pdcWindowMenu);
            MMI.AddCustomMenuItem(dispatchWindowMenu);
            MMI.AddCustomMenuItem(settingsWindowMenu);
        }

        private void DispatchWindowMenu_Click(object sender, EventArgs e)
        {
            MMI.InvokeOnGUI(() => DoShowDispatchWindow());
        }

        private void pdcWindowMenu_Click(object sender, EventArgs e)
        {
            MMI.InvokeOnGUI(() => DoShowPDCWindow());
        }

        private void SettingsWindowMenu_Click(object sender, EventArgs e)
        {
            MMI.InvokeOnGUI(() => DoShowSettingsWindow());
        }

        private void Vatsys_ConnectionChanged(object sender, EventArgs e)
        {
            if (!Network.IsConnected)
            {
                logger.Log("Disconnected");
                MMI.InvokeOnGUI(() =>
                {
                    pdcWindowMenu.Item.Enabled = false;
                    pdcWindow?.Close();
                    dispatchWindowMenu.Item.Enabled = false;
                    dispatchWindow?.Close();
                });
                return;
            }

            var station = MMI.PrimePosition.ArrivalListAirports.FirstOrDefault();
            logger.Log($"Connected to {station}");

            if (station != null)
            {
                MMI.InvokeOnGUI(() =>
                {
                    pdcWindowMenu.Item.Enabled = Network.Rating >= NetworkRating.S1;
                    dispatchWindowMenu.Item.Enabled = Network.Rating >= NetworkRating.C1;
                });
            }
        }

        public static class AppData
        {
            public static Version CurrentVersion { get; } = new Version(2, 0, 0);
        }
    }
}