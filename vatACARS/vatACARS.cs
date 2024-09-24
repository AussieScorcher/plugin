using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using vatACARS.Components;
using vatACARS.Helpers;
using vatACARS.Lib;
using vatACARS.Util;
using vatsys;
using vatsys.Plugin;
using static vatACARS.Helpers.Transceiver;
using static vatsys.FDP2;

namespace vatACARS
{
    public static class AppData
    {
        public static Version CurrentVersion { get; } = new Version(1, 1, 2);
    }

    [Export(typeof(IPlugin))]
    public class vatACARS : IPlugin
    {
        public static DebugWindow debugWindow;
        public static HistoryWindow historyWindow;
        public static SetupWindow setupWindow;
        public List<string> DebugNames = new List<string>();
        private static DispatchWindow dispatchWindow = new DispatchWindow();
        private static HandoffSelector HandoffSelector;
        private readonly Logger logger = new Logger("vatACARS");
        private CustomToolStripMenuItem debugWindowMenu;
        private CustomToolStripMenuItem dispatchWindowMenu;
        private CustomToolStripMenuItem historyWindowMenu;
        private Dictionary<FDR, string> lastCFLStrings = new Dictionary<FDR, string>();
        private CustomToolStripMenuItem setupWindowMenu;
        private System.Timers.Timer UpdateTimer;

        // The following function runs on vatSys startup. Init code should be contained here.
        public vatACARS()
        {
            string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "vatACARS");
            UpdateTimer = new System.Timers.Timer(10000.0);
            UpdateTimer.Elapsed += new ElapsedEventHandler(UpdateTimer_Elapsed);
            UpdateTimer.Start();
            // Create directories only if they don't exist
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(Path.Combine(dataPath, "audio"));
            Directory.CreateDirectory(Path.Combine(dataPath, "data"));
            Directory.CreateDirectory(Path.Combine(dataPath, "profiles"));
            if (File.Exists($"{dataPath}\\vatACARS.log")) File.Delete($"{dataPath}\\vatACARS.log");

            logger.Log("Starting...");
            _ = Start();
        }

        public string Name => "vatACARS";

        public static void DoShowSetupWindow()
        {
            if (setupWindow == null || setupWindow.IsDisposed)
            {
                setupWindow = new SetupWindow();
            }
            else if (setupWindow.Visible)
            {
                return;
            }

            setupWindow.Show(Form.ActiveForm);
        }
        public CustomLabelItem GetCustomLabelItem(string itemType, Track track, FDR flightDataRecord, RDP.RadarTrack radarTrack)
        {
            if (flightDataRecord == null || track == null || radarTrack == null) return null;
            try
            {
                Station[] stations = getAllStations() ?? Array.Empty<Station>();
                Station cStation = stations.FirstOrDefault(station => station.Callsign == flightDataRecord.Callsign);

                TelexMessage[] telexMessages = getAllTelexMessages() ?? Array.Empty<TelexMessage>();
                CPDLCMessage[] CPDLCMessages = getAllCPDLCMessages() ?? Array.Empty<CPDLCMessage>();
                RadioMessage[] radioRequests = GetRadioReq(flightDataRecord);

                IMessageData telexDownlink = telexMessages.Cast<IMessageData>().FirstOrDefault(message => message.State == 0 && message.Station == flightDataRecord.Callsign);
                IMessageData combinedDownlink = telexMessages.Cast<IMessageData>().Concat(CPDLCMessages.Cast<IMessageData>()).FirstOrDefault(message => message.State == 0 && message.Station == flightDataRecord.Callsign);

                switch (itemType)
                {

                    case "LABEL_ITEM_ACARS_ACID":
                        if (cStation == null)
                        {
                            return new CustomLabelItem()
                            {
                                Text = $"{flightDataRecord.Callsign}",
                            };
                        } 
                        else
                        {
                            if (Properties.Settings.Default.p_callsignbracket)
                            {
                                return new CustomLabelItem()
                                {
                                    Text = $"[{flightDataRecord.Callsign}]"
                                };
                            }
                            else
                            {
                                return new CustomLabelItem()
                                {
                                    Text = $"{flightDataRecord.Callsign}"
                                };
                            }
                        }
                    case "LABEL_ITEM_ACARS_CPDLC":
                        if (cStation == null)
                        {
                            if (flightDataRecord.ReceiveOnly)
                            {
                                return new CustomLabelItem()
                                {
                                    Text = "-",
                                    OnMouseClick = (e) =>
                                    {
                                        if (e.Button == CustomLabelItemMouseButton.Left)
                                        {
                                            if (radioRequests.Length != 0)
                                                MMI.OpenCPDLCMenu(((IEnumerable<RadioMessage>)radioRequests).Last<RadioMessage>());
                                            else
                                                MMI.OpenCPDLCWindow(flightDataRecord);
                                        }
                                        else if (e.Button == CustomLabelItemMouseButton.Right)
                                        {
                                            MMI.OpenCPDLCWindow(flightDataRecord);
                                        }
                                    }
                                };
                            }
                            else if (flightDataRecord.TextOnly)
                            {
                                return new CustomLabelItem()
                                {
                                    Text = "+",
                                    OnMouseClick = (e) =>
                                    {
                                        if (e.Button == CustomLabelItemMouseButton.Left)
                                        {
                                            if (radioRequests.Length != 0)
                                                MMI.OpenCPDLCMenu(((IEnumerable<RadioMessage>)radioRequests).Last<RadioMessage>());
                                            else
                                                MMI.OpenCPDLCWindow(flightDataRecord);
                                        }
                                        else if (e.Button == CustomLabelItemMouseButton.Right)
                                        {
                                            MMI.OpenCPDLCWindow(flightDataRecord);
                                        }
                                    }
                                };
                            }
                            else if (vatsys.Network.GetRadioMessages.Any<RadioMessage>((Func<RadioMessage, bool>)(r => r.Address == flightDataRecord.Callsign && r.Request && !r.History)))
                            {
                                return new CustomLabelItem()
                                {
                                    Text = "*",
                                    OnMouseClick = (e) =>
                                    {
                                        if (e.Button == CustomLabelItemMouseButton.Left)
                                        {
                                            if (radioRequests.Length != 0)
                                                MMI.OpenCPDLCMenu(((IEnumerable<RadioMessage>)radioRequests).Last<RadioMessage>());
                                            else
                                                MMI.OpenCPDLCWindow(flightDataRecord);
                                        }
                                        else if (e.Button == CustomLabelItemMouseButton.Right)
                                        {
                                            MMI.OpenCPDLCWindow(flightDataRecord);
                                        }
                                    }
                                };
                            }
                            else
                            {
                                return new CustomLabelItem()
                                {
                                    Text = " ",
                                    OnMouseClick = (e) =>
                                    {
                                        if (e.Button == CustomLabelItemMouseButton.Left)
                                        {
                                            if (radioRequests.Length != 0)
                                                MMI.OpenCPDLCMenu(((IEnumerable<RadioMessage>)radioRequests).Last<RadioMessage>());
                                            else
                                                MMI.OpenCPDLCWindow(flightDataRecord);
                                        }
                                        else if (e.Button == CustomLabelItemMouseButton.Right)
                                        {
                                            MMI.OpenCPDLCWindow(flightDataRecord);
                                        }
                                    }
                                };
                            }
                        }
                        else
                        {
                            int level = flightDataRecord.CoupledTrack.ActualAircraft.TrueAltitude;

                            if (combinedDownlink != null) return new CustomLabelItem()
                            {
                                Text = "@",
                                ForeColourIdentity = Colours.Identities.CPDLCDownlink,
                                OnMouseClick = (e) =>
                                {
                                    if (e.Button == CustomLabelItemMouseButton.Left)
                                    {
                                        CPDLCLabelClick(e);
                                    }
                                    else if (e.Button == CustomLabelItemMouseButton.Right)
                                    {
                                        CPDLCLabelClick(e);
                                    }
                                }
                            };
                            else if (!MMI.IsMySectorConcerned(flightDataRecord)) return new CustomLabelItem()
                            {
                                Text = "@",
                                ForeColourIdentity = Colours.Identities.Warning,
                                OnMouseClick = (e) =>
                                {
                                    if (e.Button == CustomLabelItemMouseButton.Left)
                                    {
                                        HandoffLabelClick(e);
                                    }
                                    else if (e.Button == CustomLabelItemMouseButton.Right)
                                    {
                                        HandoffLabelClick(e);
                                    }
                                }
                            };
                            else if (level < 24500)
                            {
                                return new CustomLabelItem()
                                {
                                    Text = "@",
                                    ForeColourIdentity = Colours.Identities.Warning,
                                    OnMouseClick = (e) =>
                                    {
                                        if (e.Button == CustomLabelItemMouseButton.Left)
                                        {
                                            HandoffLabelClick(e);
                                        }
                                        else if (e.Button == CustomLabelItemMouseButton.Right)
                                        {
                                            HandoffLabelClick(e);
                                        }
                                    }
                                };
                            }
                            else
                            {
                                return new CustomLabelItem()
                                {
                                    Text = "@",
                                    OnMouseClick = (e) =>
                                    {
                                        if (e.Button == CustomLabelItemMouseButton.Left)
                                        {
                                            CPDLCLabelClick(e);
                                        }
                                        else if (e.Button == CustomLabelItemMouseButton.Right)
                                        {
                                            CPDLCLabelClick(e);
                                        }
                                    }
                                };
                            }
                        }
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error in GetCustomLabelItem: {e.Message}");
                return null;
            }
        }
        public RadioMessage[] GetRadioReq(FDR fdr)
        {
            return vatsys.Network.GetRadioMessages.Where<RadioMessage>((Func<RadioMessage, bool>)(r => r.Address == fdr.Callsign && r.Request && !r.Acknowledged)).ToArray<RadioMessage>();
        }

        public void OnFDRUpdate(FDP2.FDR updated)
        { }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        { }

        public CustomColour SelectASDTrackColour(Track track)
        {
            // Something in this is broken.

            if (track == null) return null;
            if (track.Type != Track.TrackTypes.TRACK_TYPE_RADAR) return null;
            FDR fdr = ((RDP.RadarTrack)track.SourceData).CoupledFDR;
            if (fdr == null) return null;
            if (fdr.ControllingSector == null || fdr.HandoffSector == null) return null;

            if (!MMI.IsMySectorConcerned(fdr)) return null; // something here is fucked

            Station[] stations = getAllStations();
            Station cStation = stations.FirstOrDefault(station => station.Callsign == fdr.Callsign);
            if (cStation == null) return null;

            CPDLCMessage[] CPDLCMessages = getAllCPDLCMessages();
            TelexMessage[] telexMessages = getAllTelexMessages();
            IMessageData downlink = telexMessages.Cast<IMessageData>().Concat(CPDLCMessages.Cast<IMessageData>()).FirstOrDefault(message => message.State == 0 && message.Station == cStation.Callsign);
            if (downlink == null) return null;

            // We have an active downlink from this aircraft
            return new CustomColour(41, 178, 144);
        }

        public CustomColour SelectGroundTrackColour(Track track)
        {
            try
            {
                FDR fdr = track.GetFDR(true);
                if (!GetFDRs.Contains(fdr) || fdr == null) return null;

                TelexMessage[] telexMessages = getAllTelexMessages();
                TelexMessage downlink = telexMessages.FirstOrDefault(message => message.State == 0 && message.Station == fdr.Callsign);
                if (downlink == null) return null;

                // We have an active downlink for this aircraft
                return new CustomColour(41, 178, 144);
            }
            catch (Exception e)
            {
                logger.Log($"Error in SelectGroundTrackColour: {e.Message}");
                return null;
            }
        }

        private static void DoShowDebugWindow()
        {
            if (debugWindow == null || debugWindow.IsDisposed)
            {
                debugWindow = new DebugWindow();
            }
            else if (debugWindow.Visible)
            {
                return;
            }

            debugWindow.Show(Form.ActiveForm);
        }

        private static void DoShowDispatchWindow()
        {
            if (dispatchWindow == null || dispatchWindow.IsDisposed)
            {
                dispatchWindow = new DispatchWindow();
            }
            else if (dispatchWindow.Visible)
            {
                return;
            }

            dispatchWindow.Show(Form.ActiveForm);
        }

        private static void DoShowHistoryWindow()
        {
            try
            {
                if (historyWindow == null || historyWindow.IsDisposed)
                {
                    historyWindow = new HistoryWindow();
                }
                else if (historyWindow.Visible)
                {
                    return;
                }

                historyWindow.Show(Form.ActiveForm);
            }
            catch (Exception e)
            {
                ErrorHandler.GetInstance().AddError($"Error in DoShowHistoryWindow: {e}");
            }
        }

        private static void DoShowPopupWindow(string c, FDR fdr)
        {
            string formattedCFLString = (fdr.CFLString != null && int.Parse(fdr.CFLString) < 110
            ? "A"
            : "FL") + fdr.CFLString.PadLeft(3, '0');
            if (c.Equals("CFLUP"))
            {
                c = $"Do you want to send a CPDLC message to {fdr.Callsign} to clear their flight level to {formattedCFLString}?+";
            }
            else if (c.Equals("CFLDOWN"))
            {
                c = $"Do you want to send a CPDLC message to {fdr.Callsign} to clear their flight level to {formattedCFLString}?-";
            }
            else
            {
                c = $"Do you want to send a CPDLC message to {fdr.Callsign} to clear their flight level to {formattedCFLString}?";
            }

            PopupWindow newPopupWindow = new PopupWindow(c.Trim(), false, fdr);
            Form form = Form.ActiveForm;
            if (form != null)
            {
                if (form.InvokeRequired)
                {
                    form.Invoke((Action)(() => newPopupWindow.Show(form)));
                }
                else
                {
                    newPopupWindow.Show(form);
                }
            }
        }

        private void ActiveForm_KeyUp(object sender, KeyEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void CPDLCLabelClick(CustomLabelItemMouseClickEventArgs e)
        {
            FDR fdr = e.Track.GetFDR();
            if (fdr == null)
            {
                ErrorHandler.GetInstance().AddError($"Selected aircraft has not submitted a flight plan.");
                return;
            }

            CPDLCMessage msg1 = getAllCPDLCMessages().FirstOrDefault(message => message.State == 0 && message.Station == fdr.Callsign);

            if (msg1 != null) DispatchWindow.SelectedMessage = msg1;
            else DispatchWindow.SelectedMessage = new CPDLCMessage()
            {
                State = 0,
                Station = fdr.Callsign,
                Content = "(no message received)",
                TimeReceived = DateTime.UtcNow
            };

            EditorWindow window = new EditorWindow();
            window.Show(Form.ActiveForm);

            e.Handled = true;
        }

        private void DebugWindowMenu_Click(object sender, EventArgs e)
        {
            MMI.InvokeOnGUI(() => DoShowDebugWindow());
        }

        private void DispatchWindowMenu_Click(object sender, EventArgs e)
        {
            MMI.InvokeOnGUI(() => DoShowDispatchWindow());
        }

        private void HandoffLabelClick(CustomLabelItemMouseClickEventArgs e)
        {
            DispatchWindow.SelectedStation = getAllStations().FirstOrDefault(station => station.Callsign == e.Track.GetFDR().Callsign);
            HandoffSelector = new HandoffSelector();
            HandoffSelector.Show(Form.ActiveForm);

            e.Handled = true;
        }

        private void HistoryWindowMenu_Click(object sender, EventArgs e)
        {
            MMI.InvokeOnGUI(() => DoShowHistoryWindow());
        }

        private void PDCLabelClick(CustomLabelItemMouseClickEventArgs e)
        {
            FDR fdr = e.Track.GetFDR();
            if (fdr == null)
            {
                ErrorHandler.GetInstance().AddError($"Selected aircraft has not submitted a flight plan.");
                return;
            }
            TelexMessage telexMessage = getAllTelexMessages().FirstOrDefault(message => message.State == 0 && message.Station == fdr.Callsign && message.Content.StartsWith("REQUEST PREDEP"));
            DispatchWindow.SelectedMessage = telexMessage;

            PDCWindow window = new PDCWindow();
            window.Show(Form.ActiveForm);
        }

        private void SetupWindowMenu_Click(object sender, EventArgs e)
        {
            MMI.InvokeOnGUI(() => DoShowSetupWindow());
        }

        private async Task Start()
        {
            try
            {
                logger.Log("Running updater client...");
                HttpClientUtils.SetBaseUrl("https://api.vatacars.com");
                await UpdateClient.CheckDependencies();

                logger.Log("Populating vatSys toolstrip...");
                // Add our buttons to the vatSys toolstrip
                setupWindowMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Custom, new ToolStripMenuItem("Setup"));
                setupWindowMenu.CustomCategoryName = "ACARS";
                setupWindowMenu.Item.Click += SetupWindowMenu_Click;
                MMI.AddCustomMenuItem(setupWindowMenu);

                dispatchWindowMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Custom, new ToolStripMenuItem("Dispatch Interface"));
                dispatchWindowMenu.CustomCategoryName = "ACARS";
                dispatchWindowMenu.Item.Click += DispatchWindowMenu_Click;
                MMI.AddCustomMenuItem(dispatchWindowMenu);

                historyWindowMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Custom, new ToolStripMenuItem("History"));
                historyWindowMenu.CustomCategoryName = "ACARS";
                historyWindowMenu.Item.Click += HistoryWindowMenu_Click;
                MMI.AddCustomMenuItem(historyWindowMenu);

                ErrorHandler.Initialize(SynchronizationContext.Current); // Init error handler on ui thread

                DebugNames.Add("Joshua H");
                DebugNames.Add("Edward M");
                DebugNames.Add("Jamie K");
                if (!DebugNames.Contains(Network.Me.RealName))
                {
                    logger.Log($"{Network.Me.RealName} is not Authorized to Debug.");
                }
                else
                {
                    logger.Log($"{Network.Me.RealName} is Authorized to Debug.");
                    debugWindowMenu = new CustomToolStripMenuItem(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Settings, new ToolStripMenuItem("ACARS DEBUG"));
                    debugWindowMenu.Item.Click += DebugWindowMenu_Click;
                    MMI.AddCustomMenuItem(debugWindowMenu);
                }
                // Update Checking
                logger.Log("Starting version checker...");
                VersionChecker.StartListening();
                ProfileManager.LoadProfiles();
                XMLReader.MakeUplinks();
                JSONReader.MakeQuickFillItems();
                LabelsXMLPatcher.Patch();

                _ = Task.Run(() => CrashChecker.CheckForCrashes());

                logger.Log("Started successfully.");
            }
            catch (Exception e)
            {
                logger.Log($"Error in Start: {e.Message}");
            }
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<FDR> fdrs = GetFDRs.ToList();
            foreach (FDR fdr in fdrs)
            {
                if (!lastCFLStrings.TryGetValue(fdr, out string lastCFLString))
                {
                    lastCFLString = string.Empty;
                }
                string upordown = "CFL";
                if (fdr.CFLString != lastCFLString)
                {
                    if (float.TryParse(lastCFLString, out float lastCFL) && float.TryParse(fdr.CFLString, out float currentCFL))
                    {
                        if (currentCFL > lastCFL)
                        {
                            upordown = "CFLUP";
                        }
                        else if (currentCFL < lastCFL)
                        {
                            upordown = "CFLDOWN";
                        }
                        else
                        { 

                        }
                    }
                    else
                    {
                        logger.Log($"Station: {fdr.Callsign} CFL could not be compared. Invalid CFL values.");
                    }
                    lastCFLStrings[fdr] = fdr.CFLString;
                    Station station = getAllStations().FirstOrDefault(Station => Station.Callsign == fdr.Callsign);
                    if (station != null)
                    {
                        logger.Log("Station Connected Changed CFL?");
                        var recentLevelMessages = GetRecentSentCPDLCMessages()
                            .Where(m => m.Intent.Type == "LEVEL" && m.Station == station.Callsign);
                        if (!recentLevelMessages.Any())
                        {
                            logger.Log($"Station: {station.Callsign} Updated there CFL with no expectation.");
                            DoShowPopupWindow(upordown, fdr);
                        }
                    }
                }
            }
        }
    }
}