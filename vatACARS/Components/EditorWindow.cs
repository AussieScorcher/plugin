﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using vatACARS.Helpers;
using vatACARS.Lib;
using vatACARS.Util;
using vatsys;
using static vatACARS.Components.QuickFillWindow;
using static vatACARS.Helpers.Transceiver;

namespace vatACARS.Components
{
    public partial class EditorWindow : BaseForm
    {
        public IMessageData selectedMsg;

        private static readonly Dictionary<string, List<string>> keywordGroupMapping = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "14", new List<string> { "EMERG", "EMERGENCY", "MAYDAY", "PAN PAN" } } ,
            { "2", new List<string> { "ROUTE", "DIRECT", "HEADING", "TRACK", "DIVERTING" } },
            { "1", new List<string> { "LEVEL", "ALTITUDE", "FL", "DECENT", "CLIMB", "CLIMBING", "DESCENDING", "LEAVING" } },
            { "3", new List<string> { "TRANSFR", "HANDOFF", "TRANSFER" } },
            { "4", new List<string> { "CROSS", "OVERFLY", "PASS" } },
            { "5", new List<string> { "ENQ", "INQUIRE", "QUESTION", "TXT", "TEXT" } },
            { "6", new List<string> { "SURV", "SURVEILLANCE", "MONITOR" } },
            { "7", new List<string> { "EXPECT", "ANTICIPATE", "WAIT" } },
            { "8", new List<string> { "CONDITION" } },
            { "10", new List<string> { "COMM", "CONTACT", "MESSAGE", "VOICE" } },
            { "12", new List<string> { "CONFIRM", "REPORT" } },
            { "13", new List<string> { "MISC", "OTHER" } },
            { "11", new List<string> { "M", "K", "SPEED" } },
            { "9", new List<string> { "WX", "WEATHER", } }
        };

        private static readonly Regex placeholderParse = new Regex(@"\((.*?)\)");
        private static Logger logger = new Logger("EditorWindow");
        private static ResponseItem[] response = new ResponseItem[5];
        private int currentresponseindex;
        private TextLabel currentresponselabel;
        private GenericButton currentScrollerButton;
        private int responseIndex = 0;
        private List<TextLabel> responselabels = new List<TextLabel>();
        private List<GenericButton> scrollerButtons = new List<GenericButton>();

        public EditorWindow()
        {
            InitializeComponent();
            StyleComponent();
            selectedMsg = DispatchWindow.SelectedMessage;
            currentScrollerButton = btn_messageScroller_0;
            currentresponselabel = lbl_response_0;
            InitializeScrollerButtons();
            InitializeResponselabels();
            foreach (TextLabel label in responselabels)
            {
                label.Invalidate();
            }

            if (selectedMsg is TelexMessage)
            {
                var msg = (TelexMessage)selectedMsg;

                this.Text = $"Replying to {msg.Station}";
                string[] msgSplit = CutString(msg.Content.Replace("\n", ""));
                ListViewItem lvMsg = new ListViewItem(msg.TimeReceived.ToString("HH:mm"));
                lvMsg.SubItems.Add($"{msgSplit[0]}");
                lvMsg.Font = MMI.eurofont_winsml;

                lvw_messages.Items.Add(lvMsg);

                foreach (string msgPart in msgSplit.Skip(1))
                {
                    ListViewItem lvMsgPart = new ListViewItem("");
                    lvMsgPart.SubItems.Add($"{msgPart}");
                    lvMsgPart.Font = MMI.eurofont_winsml;
                    lvw_messages.Items.Add(lvMsgPart);
                }

                if (msg.Content == "(no message received)")
                {
                    Text = $"Sending to {msg.Station}";
                    btn_editor_Click(null, null);
                    return;
                }

                if (msg.State == MessageState.Uplink || msg.State == MessageState.Finished || msg.State == MessageState.ADSC)
                {
                    Text = $"Viewing Message from {msg.Station}";
                    foreach (Control ctl in Controls)
                    {
                        if (ctl is Button) ctl.Enabled = false;
                    }
                    btn_air.Enabled = false;
                    btn_tfc.Enabled = false;
                    btn_standby.Enabled = false;
                    btn_editor.Enabled = false;
                    btn_defer.Enabled = false;
                    ClearResponses();
                    return;
                }

                ShowGroupBasedOnMessageContent(msg.Content);
            }
            else if (selectedMsg is CPDLCMessage msg)
            {
                Text = $"Replying to {msg.Station}";
                string[] msgSplit = CutString(msg.Content.Replace("\n", ""));
                ListViewItem lvMsg = new ListViewItem(msg.TimeReceived.ToString("HH:mm"));
                lvMsg.SubItems.Add($"{msgSplit[0]}");
                lvMsg.Font = MMI.eurofont_winsml;

                lvw_messages.Items.Add(lvMsg);

                foreach (string msgPart in msgSplit.Skip(1))
                {
                    ListViewItem lvMsgPart = new ListViewItem("");
                    lvMsgPart.SubItems.Add($"{msgPart}");
                    lvMsgPart.Font = MMI.eurofont_winsml;
                    lvw_messages.Items.Add(lvMsgPart);
                }

                if (msg.State == MessageState.Uplink || msg.State == MessageState.Finished || msg.State == MessageState.ADSC)
                {
                    Text = $"Viewing Message from {msg.Station}";
                    foreach (Control ctl in Controls)
                    {
                        if (ctl is Button) ctl.Enabled = false;
                    }
                    btn_air.Enabled = false;
                    btn_tfc.Enabled = false;
                    btn_standby.Enabled = false;
                    btn_editor.Enabled = false;
                    btn_defer.Enabled = false;
                    ClearResponses();
                    return;
                }

                ShowGroupBasedOnMessageContent(msg.Content);
            }

            response = new ResponseItem[5];
            currentresponselabel.Invalidate();
        }

        public static string FormatSpeed(string s)
        {
            if (s.EndsWith("KT", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(0, s.Length - 2);
                return s;
            }
            else if (s.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(0, s.Length - 1);
                s = s.Replace(".", string.Empty);
                return "M" + s;
            }
            return s;
        }

        public void lbl_response_Paint(object sender, PaintEventArgs e)
        {
            if (response[responseIndex] == null || response[responseIndex].Placeholders == null) return;

            SolidBrush highlight = new SolidBrush(Colours.GetColour(Colours.Identities.CPDLCDownlink));
            SolidBrush highlightText = new SolidBrush(Colours.GetColour(Colours.Identities.WindowBackground));

            StringFormat format = new StringFormat
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Near
            };

            e.Graphics.FillRectangle(new SolidBrush(Colours.GetColour(Colours.Identities.WindowBackground)), currentresponselabel.ClientRectangle);
            e.Graphics.DrawString(currentresponselabel.Text, currentresponselabel.Font, new SolidBrush(Colours.GetColour(Colours.Identities.InteractiveText)), currentresponselabel.ClientRectangle, format);

            foreach (ResponseItemPlaceholderData item in response[responseIndex].Placeholders)
            {
                e.Graphics.FillRectangle(highlight, new Rectangle(item.TopLeftLoc, item.Size));
                format.Alignment = StringAlignment.Center;

                if (item.UserValue != "")
                {
                    SizeF strSpace = e.Graphics.MeasureString(item.UserValue, currentresponselabel.Font);
                    if (strSpace.Width > (float)item.Size.Width)
                    {
                        int place = (int)Math.Floor((float)item.Size.Width / (strSpace.Width / (float)item.UserValue.Length) - 1);
                        if (place > 0) e.Graphics.DrawString(item.UserValue.Substring(0, place) + "*", currentresponselabel.Font, highlightText, new PointF(item.TopLeftLoc.X + (item.Size.Width / 2), item.TopLeftLoc.Y + (item.Size.Height / 2)), format);
                    }
                    else
                    {
                        e.Graphics.DrawString(item.UserValue, currentresponselabel.Font, highlightText, new PointF(item.TopLeftLoc.X + (item.Size.Width / 2), item.TopLeftLoc.Y + (item.Size.Height / 2)), format);
                    }
                }
                else
                {
                    e.Graphics.DrawString(item.Placeholder, currentresponselabel.Font, highlightText, new PointF(item.TopLeftLoc.X + (item.Size.Width / 2), item.TopLeftLoc.Y + (item.Size.Height / 2)), format);
                }
            }
        }

        private static Intent DetectIntent(string message)
        {
            Intent intent = new Intent();

            var detectionStrings = new Dictionary<string, string>
            {
                { "CLIMB TO AND MAINTAIN BLOCK", "BLOCK" },
                { "DESCEND TO AND MAINTAIN BLOCK", "BLOCK" },
                { "CRUISE CLIMB TO", "LEVEL" },
                { "REACH", "LEVEL" },
                { "CRUISE CLIMB ABOVE", "LEVEL" },
                { "STOP CLIMB AT", "LEVEL" },
                { "STOP DESCENT AT", "LEVEL" },
                { "IMMEDIATELY CLIMB TO", "LEVEL" },
                { "IMMEDIATELY DESCEND TO", "LEVEL" },
                { "EXPEDITE CLIMB TO", "LEVEL" },
                { "EXPEDITE DESCENT TO", "LEVEL" },
                { "MAINTAIN BLOCK", "BLOCK" },
                { "CLIMB TO", "LEVEL" },
                { "DESCEND TO", "LEVEL" },
                { "MAINTAIN PRESENT SPEED", "SPEED" },
                { "MAINTAIN", "LEVEL" },
                { "PROCEED DIRECT TO", "DIRECT" },
                { "WHEN ABLE PROCEED DIRECT TO", "DIRECT" },
                { "INCREASE SPEED TO", "LEVEL" }, // dont wanna duplicate working code so set to LEVEL
                { "REDUCE SPEED TO", "LEVEL" }, // same story
                { "DO NOT EXCEED", "LEVEL" }, // same again
                { "ADJUST SPEED TO", "LEVEL" }, // same :)
                { "RESUME NORMAL SPEED", "SPEED" }, // finally
                { "NO SPEED RESTRICTION", "SPEED" },
            };

            foreach (var detectionString in detectionStrings)
            {
                if (message.StartsWith(detectionString.Key, StringComparison.OrdinalIgnoreCase))
                {
                    intent.Type = detectionString.Value;
                    var startIndex = detectionString.Key.Length;
                    var value = message.Substring(startIndex).TrimStart().Split(' ')[0];
                    value = value.Replace(",", string.Empty);
                    if (intent.Type == "BLOCK")
                    {
                        var valueParts = message.Substring(startIndex).TrimStart().Split(new[] { '(', ')', 'T', 'O' }, StringSplitOptions.RemoveEmptyEntries);
                        intent.Value = string.Join(",", valueParts);
                    }
                    else if (intent.Type == "LEVEL")
                    {
                        if (value.StartsWith("A", StringComparison.OrdinalIgnoreCase) || value.StartsWith("FL", StringComparison.OrdinalIgnoreCase))
                        {
                            intent.Value = value;
                        }
                        else if (message.EndsWith("OR GREATER", StringComparison.OrdinalIgnoreCase))
                        {
                            if (value.EndsWith("KT", StringComparison.OrdinalIgnoreCase) || value.EndsWith("M", StringComparison.OrdinalIgnoreCase))
                            {
                                intent.Type = "SPEED";
                                intent.Value = FormatSpeed(value) + "G";
                            }
                        }
                        else if (message.EndsWith("OR LESS", StringComparison.OrdinalIgnoreCase))
                        {
                            if (value.EndsWith("KT", StringComparison.OrdinalIgnoreCase) || value.EndsWith("M", StringComparison.OrdinalIgnoreCase))
                            {
                                intent.Type = "SPEED";
                                intent.Value = FormatSpeed(value) + "L";
                            }
                        }
                        else if (message.StartsWith("DO NOT EXCEED"))
                        {
                            if (value.EndsWith("KT", StringComparison.OrdinalIgnoreCase) || value.EndsWith("M", StringComparison.OrdinalIgnoreCase))
                            {
                                intent.Type = "SPEED";
                                intent.Value = FormatSpeed(value) + "L";
                            }
                        }
                        else if (value.EndsWith("KT", StringComparison.OrdinalIgnoreCase) || value.EndsWith("M", StringComparison.OrdinalIgnoreCase))
                        {
                            intent.Type = "SPEED";
                            intent.Value = "S" + FormatSpeed(value);
                        }
                        else
                        {
                            intent.Type = "UNKNOWN";
                            intent.Value = "Unknown";
                        }
                    }
                    else if (intent.Type == "SPEED")
                    {
                        if (message.StartsWith("NO SPEED RESTRICTION"))
                        {
                            intent.Value = "CSR";
                        }
                        else if (message.StartsWith("RESUME NORMAL SPEED"))
                        {
                            intent.Value = "CLRSPD";
                        }
                        else if (message.StartsWith("MAINTAIN PRESENT SPEED"))
                        {
                            intent.Value = "PRSSPD";
                        }
                        else
                        {
                            intent.Value = value;
                        }
                    }
                    else if (intent.Type == "DIRECT")
                    {
                        intent.Value = value;
                    }
                    else
                    {
                        intent.Value = value;
                    }
                    return intent;
                }
            }

            intent.Type = "UNKNOWN";
            intent.Value = "Unknown";
            return intent;
        }

        private void btn_air_Click(object sender, EventArgs e)
        {
            var unable = (UplinkEntry)XMLReader.uplinks.Entries.Where(entry => entry.Code == "0").ToList().FirstOrDefault().Clone();
            responseIndex = 0;
            HandleResponse(unable);

            var air = (UplinkEntry)XMLReader.uplinks.Entries.Where(entry => entry.Code == "167").ToList().FirstOrDefault().Clone();
            responseIndex = 1;
            HandleResponse(air);
        }

        private void btn_category_Click(object sender, EventArgs e)
        {
            try
            {
                GenericButton clicked = (GenericButton)sender;
                switch (clicked.Text)
                {
                    case "LEVEL": ShowGroup("1"); break;
                    case "ROUTE": ShowGroup("2"); break;
                    case "TRANSFR": ShowGroup("3"); break;
                    case "CROSS": ShowGroup("4"); break;
                    case "ENQ/TXT": ShowGroup("5"); break;
                    case "SURV": ShowGroup("6"); break;
                    case "EXPECT": ShowGroup("7"); break;
                    case "BLK/CND": ShowGroup("8"); break;
                    case "WX/OFF": ShowGroup("9"); break;
                    case "COMM": ShowGroup("10"); break;
                    case "SPEED": ShowGroup("11"); break;
                    case "CFM/RPT": ShowGroup("12"); break;
                    case "MISC": ShowGroup("13"); break;
                    case "EMERG": ShowGroup("14"); break;
                    default: ShowGroup("1"); break;
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Something went wrong!\n{ex.ToString()}");
            }
        }

        private void btn_defer_Click(object sender, EventArgs e)
        {
            var defer = (UplinkEntry)XMLReader.uplinks.Entries.Where(entry => entry.Code == "2").ToList().FirstOrDefault().Clone();
            responseIndex = 0;
            ClearResponses();
            HandleResponse(defer);
        }

        private void btn_editor_Click(object sender, EventArgs e)
        {
            pnl_categories.Visible = true;
            ClearResponses();
            ShowGroup("1");
        }

        private void btn_escape_Click(object sender, EventArgs e)
        {
            ClearResponses();
        }

        private void btn_messageScroller_MouseDown(object sender, MouseEventArgs e)
        {
            UpdateResponseText();
            if (e.Button == MouseButtons.Left && responseIndex < 4)
            {
                currentresponseindex = responseIndex;
                responseIndex++;
                UpdateScrollerButton();
            }
            else if (e.Button == MouseButtons.Right && responseIndex > 0)
            {
                if (currentresponselabel.Text == "")
                {
                    response[responseIndex] = null;
                }
                currentresponseindex = responseIndex;
                responseIndex--;
                UpdateScrollerButton();
            }

            if (response[responseIndex] == null)
            {
                response[responseIndex] = new ResponseItem();
            }

            if (response[responseIndex].Entry == null)
            {
                response[responseIndex].Entry = new UplinkEntry();
            }

            UpdateScroll();
            currentresponselabel.Text = response[responseIndex].Entry.Element ?? string.Empty;
        }

        private void btn_messageScrollerSecondary_MouseDown(object sender, MouseEventArgs e)
        {
            UpdateResponseText();
            Button button = (Button)sender;
            int index = int.Parse(button.Name.Substring(button.Name.Length - 1));
            if (e.Button == MouseButtons.Left)
            {
                responseIndex = index;
                UpdateScrollerButton();
                foreach (TextLabel label in responselabels)
                {
                    label.Invalidate();
                }
            }
        }

        private void btn_restore_Click(object sender, EventArgs e)
        {
            currentresponselabel.Refresh();
            response = new ResponseItem[5];
            responseIndex = 0;
            currentresponselabel.Text = string.Empty;

            if (selectedMsg != null)
            {
                var message = selectedMsg as dynamic;
                if (message != null && message.SuspendedResponses != null)
                {
                    foreach (ResponseItem item in message.SuspendedResponses)
                    {
                        var responsecode = (UplinkEntry)XMLReader.uplinks.Entries.Where(entry => entry.Code == item.Entry.Code).ToList().FirstOrDefault().Clone();
                        HandleResponse(responsecode);

                        if (responseIndex < message.SuspendedResponses.Count - 1)
                        {
                            responseIndex++;
                        }
                    }
                }
            }
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            try
            {
                // TODO: replace placeholder content
                foreach (ResponseItem item in response.Where(obj => obj != null && obj.Entry.Element != ""))
                {
                    if (item.Placeholders != null)
                    {
                        foreach (ResponseItemPlaceholderData placeholder in item.Placeholders)
                        {
                            item.Entry.Element = item.Entry.Element.Replace(placeholder.Placeholder, $"@{placeholder.UserValue}@");
                        }
                    }
                    else
                    {
                        //idk
                    }
                }

                if (selectedMsg is TelexMessage)
                {
                    TelexMessage message = (TelexMessage)selectedMsg;
                    string resp = string.Join("\n", response.Where(obj => obj != null && obj.Entry.Element != "").Select(obj => obj.Entry.Element)).Replace("@", "");
                    FormUrlEncodedContent req = HoppiesInterface.ConstructMessage(selectedMsg.Station, "telex", resp);

                    if (selectedMsg.Content == "(no message received)")
                    {
                        addTelexMessage(new TelexMessage()
                        {
                            State = MessageState.Uplink,
                            Station = selectedMsg.Station,
                            Content = resp.Replace("\n", ", "),
                            TimeReceived = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        selectedMsg.Content = resp;
                        selectedMsg.setMessageState(MessageState.Finished); // Done
                    }
                    _ = HoppiesInterface.SendMessage(req);
                }
                else if (selectedMsg is CPDLCMessage message1)
                {
                    var responseCode = "N";
                    if (response.Any(obj => obj != null && obj.Entry != null && obj.Entry.Response == "R")) responseCode = "R"; // TODO: Fix priorities here
                    if (response.Any(obj => obj != null && obj.Entry != null && obj.Entry.Response == "Y")) responseCode = "Y";
                    if (response.Any(obj => obj != null && obj.Entry != null && obj.Entry.Response == "W/U")) responseCode = "WU";
                    CPDLCMessage message = message1;
                    string encodedMessage = string.Join("\n", response.Where(obj => obj != null && obj.Entry != null && obj.Entry.Element != "").Select(obj => obj.Entry.Element));
                    string resp = $"/data2/{SentMessages}/{message.MessageId}/{responseCode}/{encodedMessage}";
                    if (resp.EndsWith("@")) resp = resp.Substring(0, resp.Length - 1);
                    FormUrlEncodedContent req = HoppiesInterface.ConstructMessage(selectedMsg.Station, "CPDLC", resp);

                    if (selectedMsg.Content == "(no message received)")
                    {
                        addSentCPDLCMessage(new SentCPDLCMessage()
                        {
                            Station = selectedMsg.Station,
                            MessageId = SentMessages,
                            ReplyMessageId = SentMessages,
                            Intent = DetectIntent(encodedMessage.Replace("@", ""))
                        });

                        addCPDLCMessage(new CPDLCMessage()
                        {
                            State = responseCode == "N" ? MessageState.Finished : MessageState.Uplink,
                            Station = selectedMsg.Station,
                            Content = encodedMessage.Replace("@", "").Replace("\n", ", "),
                            TimeReceived = DateTime.UtcNow,
                            MessageId = SentMessages,
                            ReplyMessageId = -1
                        });
                    }
                    else
                    {
                        addSentCPDLCMessage(new SentCPDLCMessage()
                        {
                            Station = selectedMsg.Station,
                            MessageId = SentMessages,
                            ReplyMessageId = message.MessageId,
                            Intent = DetectIntent(encodedMessage.Replace("@", ""))
                        });

                        selectedMsg.Content = encodedMessage.Replace("@", "");
                        selectedMsg.setMessageState(responseCode == "N" ? MessageState.Finished : MessageState.Uplink);
                    }

                    _ = HoppiesInterface.SendMessage(req);
                }

                logger.Log("Message sent successfully");
                Close();
            }
            catch (Exception ex)
            {
                logger.Log($"Oops: {ex.ToString()}");
            }
        }

        private void btn_standby_Click(object sender, EventArgs e)
        {
            var standby = (UplinkEntry)XMLReader.uplinks.Entries.Where(entry => entry.Code == "1").ToList().FirstOrDefault().Clone();
            responseIndex = 0;
            ClearResponses();
            HandleResponse(standby);
        }

        private void btn_suspend_Click(object sender, EventArgs e)
        {
            var message = selectedMsg as dynamic;
            if (message != null)
            {
                message.SuspendedResponses.Clear();
                foreach (ResponseItem item in response.Where(obj => obj != null && obj.Entry.Element != ""))
                {
                    message.SuspendedResponses.Add(item);
                }
            }
            Close();
        }

        private void btn_tfc_Click(object sender, EventArgs e)
        {
            var unable = (UplinkEntry)XMLReader.uplinks.Entries.Where(entry => entry.Code == "0").ToList().FirstOrDefault().Clone();
            responseIndex = 0;
            HandleResponse(unable);

            var tfc = (UplinkEntry)XMLReader.uplinks.Entries.Where(entry => entry.Code == "166").ToList().FirstOrDefault().Clone();
            responseIndex = 1;
            HandleResponse(tfc);
        }

        private void ClearResponses()
        {
            response = new ResponseItem[5];
            responseIndex = 0;
            UpdateScrollerButton();
            UpdateScroll();
            response[responseIndex] = null;
            currentresponselabel.Text = "";
            currentresponselabel.Refresh();
        }

        private string[] CutString(string input, int maxLength = 58)
        {
            if (input.Length <= maxLength) return new string[] { input };

            string[] words = input.Split(' ');
            List<string> segments = new List<string>();
            string currentSegment = string.Empty;

            foreach (string word in words)
            {
                if ((currentSegment + " " + word).Trim().Length > maxLength)
                {
                    segments.Add(currentSegment.Trim());
                    currentSegment = word;
                }
                else
                {
                    if (currentSegment.Length > 0) currentSegment += " ";
                    currentSegment += word;
                }
            }

            if (currentSegment.Length > 0)
            {
                segments.Add(currentSegment.Trim());
            }

            return segments.ToArray();
        }

        private void HandleResponse(UplinkEntry selected)
        {
            var placeholders = placeholderParse.Matches(selected.Element);

            response[responseIndex] = new ResponseItem()
            {
                Entry = selected,
                Placeholders = null,
            };

            if (placeholders.Count > 0)
            {
                response[responseIndex].Placeholders = new ResponseItemPlaceholderData[placeholders.Count];
                Graphics graphics = currentresponselabel.CreateGraphics();
                StringFormat format = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Near
                };

                for (int i = 0; i < placeholders.Count; i++)
                {
                    CharacterRange[] ranges = { new CharacterRange(placeholders[i].Index, placeholders[i].Length) };
                    format.SetMeasurableCharacterRanges(ranges);

                    Region region = graphics.MeasureCharacterRanges(response[responseIndex].Entry.Element, currentresponselabel.Font, currentresponselabel.Bounds, format)[0];
                    Rectangle bounds = Rectangle.Round(region.GetBounds(graphics));

                    response[responseIndex].Placeholders[i] = new ResponseItemPlaceholderData()
                    {
                        Placeholder = placeholders[i].Value,
                        UserValue = "",
                        TopLeftLoc = new Point(bounds.X - 4, bounds.Y - 2),
                        Size = new Size(bounds.Width + 4, bounds.Height + 2)
                    };
                }
            }
            else
            {
                response[responseIndex].Placeholders = new ResponseItemPlaceholderData[placeholders.Count];
            }
            UpdateScrollerButton();
            UpdateScroll();
            currentresponselabel.Text = selected.Element;
            currentresponselabel.Refresh();
        }

        private void InitializeResponselabels()
        {
            for (int i = 0; i <= 4; i++)
            {
                TextLabel label = (TextLabel)this.Controls.Find("lbl_response_" + i, true).FirstOrDefault();
                if (label != null)
                {
                    responselabels.Add(label);
                    if (i == 0)
                    {
                        currentresponselabel = label;
                        currentresponselabel.MouseDown += lbl_response_MouseDown;
                        currentresponselabel.MouseUp += lbl_response_MouseUp;
                        currentresponselabel.Paint += lbl_response_Paint;
                        currentresponselabel.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
                    }
                }
            }
        }

        private void InitializeScrollerButtons()
        {
            for (int i = 0; i <= 4; i++)
            {
                GenericButton button = (GenericButton)this.Controls.Find("btn_messageScroller_" + i, true).FirstOrDefault();
                if (button != null)
                {
                    scrollerButtons.Add(button);
                    if (i == 0)
                    {
                        currentScrollerButton = button;
                        currentScrollerButton.MouseDown += btn_messageScroller_MouseDown;
                    }
                }
            }
        }

        private void lbl_response_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                response[responseIndex] = null;

                currentresponselabel.Text = "";
                currentresponselabel.Refresh();
            }
        }

        private void lbl_response_MouseUp(object sender, MouseEventArgs e)
        {
            if (response[responseIndex] == null || response[responseIndex].Placeholders == null) return;

            try
            {
                for (var i = 0; i < response[responseIndex].Placeholders.Count(); i++)
                {
                    ResponseItemPlaceholderData item = response[responseIndex].Placeholders[i];
                    if (new Rectangle(item.TopLeftLoc, item.Size).Contains(e.Location))
                    {
                        QuickFillWindow fillWindow = new QuickFillWindow(item.Placeholder.Substring(1, item.Placeholder.Length - 2).ToUpper(), selectedMsg, item.UserValue);
                        fillWindow.QuickFillDataChanged += (object s, QuickFillData data) =>
                        {
                            var placesub = (item.Placeholder.Substring(1, item.Placeholder.Length - 2).ToUpper());
                            string setting = Regex.Replace(data.Setting, @"\s", string.Empty);
                            if (placesub == "UNIT NAME")
                            {
                                item.UserValue = Regex.Replace(setting, @"[\d\.]", string.Empty);
                            }
                            else if (placesub == "FREQUENCY")
                            {
                                item.UserValue = Regex.Replace(setting, @"[^\d\.]", string.Empty);
                            }
                            else
                            {
                                item.UserValue = setting;
                            }
                            currentresponselabel.Refresh();
                        };

                        fillWindow.ShowDialog(ActiveForm);

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Oops: {ex.ToString()}");
            }
        }

        private void lvw_messages_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            Font font = MMI.eurofont_winsml;
            SolidBrush bg = new SolidBrush(e.Item.BackColor);
            SolidBrush fg = new SolidBrush(e.Item.ForeColor);
            e.Graphics.FillRectangle(bg, e.Bounds);
            int n = 0;
            foreach (ListViewItem.ListViewSubItem subItem in e.Item.SubItems)
            {
                StringFormat format = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Near
                };
                int offset = lvw_messages.ClientSize.Width - n;
                SizeF strSpace = e.Graphics.MeasureString(subItem.Text, font);
                if (strSpace.Width > (float)offset)
                {
                    int place = (int)Math.Floor((float)offset / (strSpace.Width / (float)subItem.Text.Length));
                    if (place > 0) e.Graphics.DrawString(subItem.Text.Substring(0, place) + "...", font, fg, subItem.Bounds, format);
                }
                else e.Graphics.DrawString(subItem.Text, font, fg, subItem.Bounds, format);
                n++;
            }
        }

        private void lvw_messages_SelectedIndexChanged(object sender, EventArgs e)
        { }

        private void lvw_messageSelector_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            Font font = MMI.eurofont_winsml;
            SolidBrush bg = new SolidBrush(e.Item.BackColor);
            SolidBrush fg = new SolidBrush(e.Item.ForeColor);
            e.Graphics.FillRectangle(bg, e.Bounds);
            int n = 0;
            foreach (ListViewItem.ListViewSubItem subItem in e.Item.SubItems)
            {
                StringFormat format = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Near
                };
                int offset = lvw_messageSelector.ClientSize.Width - n;
                SizeF strSpace = e.Graphics.MeasureString(subItem.Text, font);
                if (strSpace.Width > (float)offset)
                {
                    int place = (int)Math.Floor((float)offset / (strSpace.Width / (float)subItem.Text.Length));
                    if (place > 0) e.Graphics.DrawString(subItem.Text.Substring(0, place) + "...", font, fg, subItem.Bounds, format);
                }
                else e.Graphics.DrawString(subItem.Text, font, fg, subItem.Bounds, format);
                n++;
            }
        }

        private void lvw_messageSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvw_messageSelector.SelectedItems.Count > 0)
            {
                UplinkEntry selected = (UplinkEntry)XMLReader.uplinks.Entries.Where(entry => entry.Element == lvw_messageSelector.SelectedItems[0].Text).ToList().FirstOrDefault().Clone();
                HandleResponse(selected);
            }

            lvw_messageSelector.SelectedItems.Clear();
        }

        private void MessageScroll_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                this.scr_messageSelector.Value -= scr_messageSelector.Change;
            }
            else
            {
                if (e.Delta >= 0)
                    return;
                this.scr_messageSelector.Value += scr_messageSelector.Change;
            }
        }

        private void scr_messageSelector_Scroll(object sender, EventArgs e)
        {
            lvw_messageSelector.SetScrollPosVert(scr_messageSelector.PercentageValue);
        }

        private void ShowGroup(string group_id)
        {
            lvw_messageSelector.Items.Clear();
            List<UplinkEntry> filteredUplinks = XMLReader.uplinks.Entries.Where(entry => entry.Group == group_id).ToList();

            int visibleCount = 0;
            int startIndex = lvw_messageSelector.TopItem != null ? lvw_messageSelector.TopItem.Index : 0;
            for (int i = startIndex; i < lvw_messageSelector.Items.Count; i++)
            {
                ListViewItem item = lvw_messageSelector.Items[i];
                Rectangle itemRect = lvw_messageSelector.GetItemRect(i);
                if (lvw_messageSelector.ClientRectangle.IntersectsWith(itemRect)) visibleCount++;
            }

            int tileHeight = lvw_messageSelector.TileSize.Height;
            if (filteredUplinks.Count > 0)
            {
                scr_messageSelector.PreferredHeight = (filteredUplinks.Count * tileHeight) / 10;
                scr_messageSelector.ActualHeight = ((filteredUplinks.Count * tileHeight) / 10) - (filteredUplinks.Count - 8);
                scr_messageSelector.Enabled = true;
            }
            else
            {
                // Disable the scrollbar
                scr_messageSelector.PreferredHeight = 1;
                scr_messageSelector.ActualHeight = 1;
                scr_messageSelector.Enabled = false;
            }

            for (int i = startIndex; i < lvw_messageSelector.Items.Count; i++)
            {
                ListViewItem item = lvw_messageSelector.Items[i];
                Rectangle itemRect = lvw_messageSelector.GetItemRect(i);
                if (lvw_messageSelector.ClientRectangle.IntersectsWith(itemRect)) visibleCount++;
            }

            scr_messageSelector.Value = 0;
            foreach (var uplink in filteredUplinks)
            {
                lvw_messageSelector.Items.Add(uplink.Element);
            }
        }

        private void ShowGroupBasedOnMessageContent(string content)
        {
            foreach (var entry in keywordGroupMapping)
            {
                foreach (var keyword in entry.Value)
                {
                    if (content.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (entry.Key == "11")
                        {
                            if (keyword.Equals("M", StringComparison.OrdinalIgnoreCase))
                            {
                                if (Regex.IsMatch(content, @"M\s*\d+", RegexOptions.IgnoreCase))
                                {
                                    ShowGroup(entry.Key);
                                    return;
                                }
                            }
                            else if (keyword.Equals("K", StringComparison.OrdinalIgnoreCase))
                            {
                                if (Regex.IsMatch(content, @"\d+\s*K", RegexOptions.IgnoreCase))
                                {
                                    ShowGroup(entry.Key);
                                    return;
                                }
                            }
                            else
                            {
                                ShowGroup(entry.Key);
                                return;
                            }
                        }
                        else
                        {
                            ShowGroup(entry.Key);
                            return;
                        }
                    }
                }
            }
            ShowGroup("1");
        }

        private void StyleComponent()
        {
            lbl_receivedMsgs.ForeColor = Colours.GetColour(Colours.Identities.NonInteractiveText);

            lvw_messages.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            lvw_messages.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);
            lvw_messageSelector.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            lvw_messageSelector.ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);

            btn_send.BackColor = Colours.GetColour(Colours.Identities.CPDLCSendButton);
            btn_send.ForeColor = Colours.GetColour(Colours.Identities.NonJurisdictionIQL);
            btn_standby.BackColor = Colours.GetColour(Colours.Identities.CPDLCSendButton);
            btn_defer.BackColor = Colours.GetColour(Colours.Identities.CPDLCSendButton);
            btn_tfc.BackColor = Colours.GetColour(Colours.Identities.CPDLCSendButton);
            btn_air.BackColor = Colours.GetColour(Colours.Identities.CPDLCSendButton);
            btn_standby.ForeColor = Colours.GetColour(Colours.Identities.NonJurisdictionIQL);
            btn_defer.ForeColor = Colours.GetColour(Colours.Identities.NonJurisdictionIQL);
            btn_tfc.ForeColor = Colours.GetColour(Colours.Identities.NonJurisdictionIQL);
            btn_air.ForeColor = Colours.GetColour(Colours.Identities.NonJurisdictionIQL);

            DelayLabel.ForeColor = Colours.GetColour(Colours.Identities.NonInteractiveText);
            ToEditLabel.ForeColor = Colours.GetColour(Colours.Identities.NonInteractiveText);
            UnableLabel.ForeColor = Colours.GetColour(Colours.Identities.NonInteractiveText);

            scr_messageSelector.ForeColor = Colours.GetColour(Colours.Identities.WindowBackground);
            scr_messageSelector.BackColor = Colours.GetColour(Colours.Identities.WindowButtonSelected);

            this.Size = new Size(590, 497);
        }

        private void UpdateResponselabels()
        {
            if (currentresponselabel != null)
            {
                currentresponselabel.MouseDown -= lbl_response_MouseDown;
                currentresponselabel.MouseUp -= lbl_response_MouseUp;
                currentresponselabel.Paint -= lbl_response_Paint;
                currentresponselabel.Name = "lbl_response_" + responselabels.IndexOf(currentresponselabel);
            }

            if (responseIndex >= 0 && responseIndex < responselabels.Count)
            {
                currentresponselabel = responselabels[responseIndex];
                currentresponselabel.Name = "lbl_response";
                currentresponselabel.MouseDown += lbl_response_MouseDown;
                currentresponselabel.MouseUp += lbl_response_MouseUp;
                currentresponselabel.Paint += lbl_response_Paint;
            }
        }

        private void UpdateResponseText()
        {
            if (response == null || responseIndex < 0 || responseIndex >= response.Length)
            {
                return;
            }

            ResponseItem currentItem = response[responseIndex];
            if (currentItem == null || currentItem.Placeholders == null)
            {
                return;
            }

            string responseText = currentresponselabel.Text ?? currentresponselabel.Text;
            foreach (ResponseItemPlaceholderData item in currentItem.Placeholders)
            {
                if (item != null && !string.IsNullOrEmpty(item.Placeholder) && !string.IsNullOrEmpty(item.UserValue))
                {
                    responseText = responseText.Replace(item.Placeholder, item.UserValue);
                }
            }
            currentresponselabel.Text = responseText;
            currentresponselabel.Refresh();
        }

        private void UpdateScroll()
        {
            int visibleIndex = 0;
            for (int i = 0; i < response.Length; i++)
            {
                if (response[i] != null)
                {
                    visibleIndex = i;
                }
            }

            insetPanel_1.Visible = visibleIndex >= 1;
            insetPanel_2.Visible = visibleIndex >= 2;
            insetPanel_3.Visible = visibleIndex >= 3;
            insetPanel_4.Visible = visibleIndex >= 4;

            scrollerButtons[1].Visible = visibleIndex >= 1;
            scrollerButtons[2].Visible = visibleIndex >= 2;
            scrollerButtons[3].Visible = visibleIndex >= 3;
            scrollerButtons[4].Visible = visibleIndex >= 4;

            this.Size = new Size(590, 497 + visibleIndex * 38);

            foreach (TextLabel label in responselabels)
            {
                label.Invalidate();
            }
        }

        private void UpdateScrollerButton()
        {
            currentScrollerButton.MouseDown -= btn_messageScroller_MouseDown;
            currentScrollerButton.MouseDown += btn_messageScrollerSecondary_MouseDown;
            currentScrollerButton.Name = "btn_messageScroller_" + scrollerButtons.IndexOf(currentScrollerButton);

            currentScrollerButton = scrollerButtons[responseIndex];
            currentScrollerButton.Name = "btn_messageScroller";
            currentScrollerButton.MouseDown += btn_messageScroller_MouseDown;
            currentScrollerButton.MouseDown -= btn_messageScrollerSecondary_MouseDown;
            currentScrollerButton.Text = (responseIndex + 1).ToString();

            UpdateResponselabels();
        }
    }

    public class ResponseItem
    {
        public UplinkEntry Entry;
        public ResponseItemPlaceholderData[] Placeholders;
    }

    public class ResponseItemPlaceholderData
    {
        public string Placeholder;
        public Size Size;
        public Point TopLeftLoc;
        public string UserValue;
    }
}