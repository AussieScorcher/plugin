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
using Newtonsoft.Json;
using RossCarlson.Vatsim.Network;

namespace vatACARS
{
    public static class AppData
    {
        public static Version CurrentVersion { get; } = new Version(2, 0, 0);
    }

    [Export(typeof(IPlugin))]
    public class VatACARS : IPlugin
    {
        public string Name => "vatACARSNext";
        private readonly Logger _logger = new Logger("vatACARS");
        private readonly VatACARSAuthority _vatACARSAuthority;
        private string _authToken;
        private Label _asdLabel;
        private CancellationTokenSource _networkCancellationTokenSource = new CancellationTokenSource();
        private CancellationTokenSource _logonCancellationTokenSource = new CancellationTokenSource();

        public bool Connected { get; private set; }

        private CustomToolStripMenuItem dispatchWindowMenu;
        private CustomToolStripMenuItem pdcWindowMenu;
        private CustomToolStripMenuItem settingsWindowMenu;
        private static DispatchWindow dispatchWindow;
        private static PDCWindow pdcWindow;
        private static SettingsWindow settingsWindow;

        public VatACARS()
        {
            _vatACARSAuthority = new VatACARSAuthority("ws://api.vatacars.com/gateway"); //ws://localhost:3000/gateway
            InitializeDirectories();

            _logger.Log($"vatACARS v{AppData.CurrentVersion} on {RegHelper.FriendlyName()}");
            StartAsync().ConfigureAwait(false);
        }

        private void InitializeDirectories()
        {
            string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "vatACARS");
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(Path.Combine(dataPath, "data"));
            Directory.CreateDirectory(Path.Combine(dataPath, "profiles"));
        }

        private async Task StartAsync()
        {
            _logger.Log($"Starting...");

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

            _ = AuthenticateAsync();
            AttachToVatSysForms();
            _logger.Log("Ready to rock and roll");
        }

        private async void AttachToVatSysForms()
        {
            await Task.Delay(3000);
            foreach (Form form in Application.OpenForms)
            {
                if (form.Text.StartsWith("vatSys"))
                {
                    _asdLabel = MainASDLabel.Hook(form);
                }
            }
        }

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

        private async Task AuthenticateAsync()
        {
            string authFilePath = GetAuthFilePath();
            TryLoadAuthToken(authFilePath);
            await _vatACARSAuthority.ConnectAsync(_authToken);

            _asdLabel?.UpdateVatACARSText("Ready");
            SetupNetworkEvents();
        }

        private string GetAuthFilePath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "vatACARS", "_auth.json");
        }

        private bool TryLoadAuthToken(string authFilePath)
        {
            try
            {
                string authContent = File.ReadAllText(authFilePath);
                dynamic authObject = JsonConvert.DeserializeObject(authContent);
                _authToken = authObject?.token?.ToString();
                return !string.IsNullOrEmpty(_authToken);
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to load auth token: {ex.Message}");
                return false;
            }
        }

        private void SetupNetworkEvents()
        {
            Network.Connected += async (_, e) => await HandleNetworkConnectedAsync();
            Network.Disconnected += async (_, e) => await HandleNetworkDisconnectedAsync();
        }

        private async Task HandleNetworkConnectedAsync()
        {
            if (Connected) return;

            try
            {
                CancellationToken token = _logonCancellationTokenSource.Token;
                await Task.Delay(3000, token);

                string station = await VatSys.GetStationAsync(token);

                _asdLabel?.SetVatACARSPrefix($"vatACARS [{station}]");
                _asdLabel?.UpdateVatACARSText($"Connecting...");

                if (!await _vatACARSAuthority.StationLogon(station))
                {
                    _asdLabel?.UpdateVatACARSText("Failed to connect to station");
                    return;
                }

                Connected = true;
                _asdLabel?.UpdateVatACARSText("No new messages");
            }
            catch (OperationCanceledException)
            {
                _logger.Log("Network connection cancelled.");
                _asdLabel?.UpdateVatACARSText("Ready");
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to connect to station: {ex.Message}");
                _asdLabel?.UpdateVatACARSText($"Error occurred: {ex.Message}");
            }
        }

        private async Task HandleNetworkDisconnectedAsync()
        {
            try
            {
                _logonCancellationTokenSource.Cancel();
                _logonCancellationTokenSource = new CancellationTokenSource();

                _asdLabel?.SetVatACARSPrefix($"vatACARS v{AppData.CurrentVersion}");
                _asdLabel?.UpdateVatACARSText("Logging off...");
                await _vatACARSAuthority.StationLogoff();

                Connected = false;
                _asdLabel?.UpdateVatACARSText("Ready");
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to disconnect from station: {ex.Message}");
            }
        }

        public void OnFDRUpdate(FDP2.FDR updated) { }
        public void OnRadarTrackUpdate(RDP.RadarTrack updated) { }

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
            try {
                if (!Network.IsConnected)
                {
                    _logger.Log("Disconnected");
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
                _logger.Log($"Connected to {station}");

                if (station != null)
                {
                    MMI.InvokeOnGUI(() =>
                    {
                        pdcWindowMenu.Item.Enabled = Network.Rating >= NetworkRating.S1;
                        dispatchWindowMenu.Item.Enabled = Network.Rating >= NetworkRating.C1;
                    });
                }
            } catch (Exception ex)
            {
                _logger.Log($"Error: {ex.Message}");
            }
        }
    }
}