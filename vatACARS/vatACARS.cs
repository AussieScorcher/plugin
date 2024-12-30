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
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        public bool Connected { get; private set; }

        private CustomToolStripMenuItem dispatchWindowMenu;
        private CustomToolStripMenuItem pdcWindowMenu;
        private CustomToolStripMenuItem settingsWindowMenu;
        private static DispatchWindow dispatchWindow;
        private static PDCWindow pdcWindow;
        private static SettingsWindow settingsWindow;

        public VatACARS()
        {
            _vatACARSAuthority = new VatACARSAuthority("ws://vatacars.com:3000/gateway");
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
            
            await Task.Delay(3000);
            AttachToVatSysForms();
            await AuthenticateAsync();
        }

        private void AttachToVatSysForms()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form.Text.StartsWith("vatSys"))
                {
                    _asdLabel = MainASDLabel.Hook(form);
                }
            }

            logger.Log($"vatACARS v{AppData.CurrentVersion} on {RegHelper.FriendlyName()}");

            Network.Connected += Vatsys_ConnectionChanged;
            Network.Disconnected += Vatsys_ConnectionChanged;

            Thread startThread = new Thread(Start);
            startThread.Start();

            return;
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
            _asdLabel?.UpdateVatACARSText("Authenticating...");
            string authFilePath = GetAuthFilePath();

            if (!File.Exists(authFilePath))
            {
                await PromptForLoginAsync("Please login to the hub and then click here.");
                return;
            }

            if (!TryLoadAuthToken(authFilePath))
            {
                await PromptForLoginAsync("Failed to authenticate. Log out and back into the hub and then click here.");
                return;
            }

            if (!await _vatACARSAuthority.ConnectAsync(_authToken))
            {
                await PromptForLoginAsync("Failed to authenticate. Log out and back into the hub and then click here.");
                return;
            }

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

        private async Task PromptForLoginAsync(string message)
        {
            _asdLabel?.UpdateVatACARSText(message);
            await HandleClickToRetryAsync(AuthenticateAsync);
        }

        private async Task HandleClickToRetryAsync(Func<Task> retryAction)
        {
            MouseEventHandler clickHandler = null;
            clickHandler = async (_, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _asdLabel.MouseClick -= clickHandler;
                    await retryAction();
                }
            };
            _asdLabel.MouseClick += clickHandler;
        }

        private void SetupNetworkEvents()
        {
            Network.Connected += async (_, e) => await HandleNetworkConnectedAsync();
            Network.Disconnected += async (_, e) => await HandleNetworkDisconnectedAsync();
        }

        private async Task HandleNetworkConnectedAsync()
        {
            if (Connected) return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                CancellationToken token = _cancellationTokenSource.Token;
                _asdLabel?.UpdateVatACARSText("Please wait...");
                await Task.Delay(3000, token);

                string station = VatSys.GetStation();
                _asdLabel?.SetVatACARSPrefix($"vatACARS [{station}]");
                _asdLabel?.UpdateVatACARSText($"Connecting to {station}...");

                if (await _vatACARSAuthority.StationLogon(station))
                {
                    Connected = true;
                    _asdLabel?.UpdateVatACARSText($"Connected to {station}");
                }
                else
                {
                    _asdLabel?.UpdateVatACARSText("Failed to connect to station");
                    await Task.Delay(2000, token);
                    _asdLabel?.UpdateVatACARSText("Ready");
                    return;
                }

                await Task.Delay(2000, token);
                _asdLabel?.UpdateVatACARSText("No new messages");
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to connect to station: {ex.Message}");
                if (ex is TaskCanceledException)
                {
                    _asdLabel?.UpdateVatACARSText("Ready");
                    return;
                }
            }
        }

        private async Task HandleNetworkDisconnectedAsync()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _asdLabel?.SetVatACARSPrefix($"vatACARS v{AppData.CurrentVersion}");
                _asdLabel?.UpdateVatACARSText("Logging off...");
                await _vatACARSAuthority.StationLogoff();

                Connected = false;
                _asdLabel?.UpdateVatACARSText("Disconnected from network.");
                await Task.Delay(2000, _cancellationTokenSource.Token);
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
            } catch (Exception ex)
            {
                logger.Log($"Error: {ex.Message}");
            }
        }
    }
}