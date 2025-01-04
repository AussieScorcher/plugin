using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using vatsys;

namespace vatACARS.UI
{
    public partial class PDCWindow : BaseForm
    {
        private List<Aircraft> aircraftList;
        private Point? hoverPoint = null;

        public PDCWindow()
        {
            InitializeComponent();
            StyleWindow();
            LoadSampleData();
            InitializeListView();
            LoadStrips();
        }

        private void LoadSampleData() // for testing
        {
            aircraftList = new List<Aircraft>
            {
                new Aircraft("QFA12", "B738", "YSSY", "NZAA", DateTime.Parse("21:29"), "Pending"),
                new Aircraft("VOZ823", "A320", "YBBN", "YMML", DateTime.Parse("22:29"), "Pending"),
                new Aircraft("JST401", "B789", "YSSY", "WSSS", DateTime.Parse("21:14"), "Pending"),
                new Aircraft("UAL863", "B777", "YSSY", "KLAX", DateTime.Parse("23:29"), "Pending"),
                new Aircraft("NZA118", "A321", "YSSY", "NZCH", DateTime.Parse("21:59"), "Pending"),
                new Aircraft("QFA12", "B738", "YSSY", "NZAA", DateTime.Parse("21:29"), "Pending"),
                new Aircraft("VOZ823", "A320", "YBBN", "YMML", DateTime.Parse("22:29"), "Pending"),
                new Aircraft("JST401", "B789", "YSSY", "WSSS", DateTime.Parse("21:14"), "Pending"),
                new Aircraft("UAL863", "B777", "YSSY", "KLAX", DateTime.Parse("23:29"), "Pending"),
                new Aircraft("NZA118", "A321", "YSSY", "NZCH", DateTime.Parse("21:59"), "Pending"),
                new Aircraft("QFA12", "B738", "YSSY", "NZAA", DateTime.Parse("21:29"), "Pending"),
                new Aircraft("VOZ823", "A320", "YBBN", "YMML", DateTime.Parse("22:29"), "Pending"),
                new Aircraft("JST401", "B789", "YSSY", "WSSS", DateTime.Parse("21:14"), "Pending"),
                new Aircraft("UAL863", "B777", "YSSY", "KLAX", DateTime.Parse("23:29"), "Pending"),
                new Aircraft("NZA118", "A321", "YSSY", "NZCH", DateTime.Parse("21:59"), "Pending"),
                new Aircraft("QFA12", "B738", "YSSY", "NZAA", DateTime.Parse("21:29"), "Pending"),
                new Aircraft("VOZ823", "A320", "YBBN", "YMML", DateTime.Parse("22:29"), "Pending"),
                new Aircraft("JST401", "B789", "YSSY", "WSSS", DateTime.Parse("21:14"), "Pending"),
                new Aircraft("UAL863", "B777", "YSSY", "KLAX", DateTime.Parse("23:29"), "Pending"),
                new Aircraft("NZA118", "A321", "YSSY", "NZCH", DateTime.Parse("21:59"), "Pending"),
                new Aircraft("QFA12", "B738", "YSSY", "NZAA", DateTime.Parse("21:29"), "Pending"),
                new Aircraft("VOZ823", "A320", "YBBN", "YMML", DateTime.Parse("22:29"), "Pending"),
                new Aircraft("JST401", "B789", "YSSY", "WSSS", DateTime.Parse("21:14"), "Pending"),
                new Aircraft("UAL863", "B777", "YSSY", "KLAX", DateTime.Parse("23:29"), "Pending"),
                new Aircraft("NZA118", "A321", "YSSY", "NZCH", DateTime.Parse("21:59"), "Pending"),
                new Aircraft("QFA12", "B738", "YSSY", "NZAA", DateTime.Parse("21:29"), "Pending"),
                new Aircraft("VOZ823", "A320", "YBBN", "YMML", DateTime.Parse("22:29"), "Pending"),
                new Aircraft("JST401", "B789", "YSSY", "WSSS", DateTime.Parse("21:14"), "Pending"),
            };
        }

        private void InitializeListView()
        {
            strips_aircraft.View = View.Details;
            strips_aircraft.FullRowSelect = true;
            strips_aircraft.GridLines = true;
            strips_aircraft.MultiSelect = false;
            strips_aircraft.HeaderStyle = ColumnHeaderStyle.None;
            strips_aircraft.Font = new Font(MMI.eurofont_sml.FontFamily, 14F, FontStyle.Regular, GraphicsUnit.Pixel);

            strips_aircraft.OwnerDraw = true;
            strips_aircraft.DrawItem += Strips_aircraft_DrawItem;
            strips_aircraft.DrawSubItem += Strips_aircraft_DrawSubItem;

            strips_aircraft.Columns.Add("", 85);  // CALLSIGN
            strips_aircraft.Columns.Add("", 65);  // TYPE
            strips_aircraft.Columns.Add("", 110); // DEP/ARR
            strips_aircraft.Columns.Add("", 60);  // ETD
            strips_aircraft.Columns.Add("", 80);  // STATUS
            strips_aircraft.Columns.Add("", 60);  // ACTION

            strips_aircraft.MouseMove += (s, e) => {
                var hit = strips_aircraft.HitTest(e.X, e.Y);
                if (hit.Item != null && hit.SubItem != null && hit.Item.SubItems.IndexOf(hit.SubItem) == 5)
                {
                    hoverPoint = new Point(e.X, e.Y);
                }
                else
                {
                    hoverPoint = null;
                }
                strips_aircraft.Invalidate();
            };

            strips_aircraft.MouseLeave += (s, e) => {
                hoverPoint = null;
                strips_aircraft.Invalidate();
            };
        }

        private void Strips_aircraft_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = false;
            e.DrawBackground();
        }

        private void Strips_aircraft_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = false;
            Rectangle bounds = e.Bounds;

            bool isHeader = e.Item == strips_aircraft.Items[0];
            bool isSendButton = e.ColumnIndex == 5 && !isHeader;

            Color backColor;
            Color foreColor;

            if (isHeader)
            {
                backColor = Colours.GetColour(Colours.Identities.WindowBackground);
                foreColor = Color.White;
            }
            else if (isSendButton)
            {
                bool isHovered = false;
                if (hoverPoint.HasValue)
                {
                    var hit = strips_aircraft.HitTest(hoverPoint.Value.X, hoverPoint.Value.Y);
                    isHovered = hit.Item == e.Item && hit.SubItem == e.SubItem;
                }

                backColor = isHovered
                    ? Color.FromArgb(100, 149, 237) 
                    : Colours.GetColour(Colours.Identities.InteractiveText);
                foreColor = Color.White;
            }
            else
            {
                backColor = Colours.GetColour(Colours.Identities.WindowBackground);
                foreColor = Colours.GetColour(Colours.Identities.InteractiveText);
            }

            using (SolidBrush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, bounds);
            }

            if (!string.IsNullOrEmpty(e.SubItem.Text))
            {
                Size textSize = TextRenderer.MeasureText(e.SubItem.Text, e.Item.Font);
                int x = bounds.X + (bounds.Width - textSize.Width) / 2 + 5;
                int y = bounds.Y + (bounds.Height - textSize.Height) / 2;

                if (isSendButton)
                {
                    TextRenderer.DrawText(
                        e.Graphics,
                        e.SubItem.Text,
                        e.Item.Font,
                        new Point(x, y),
                        foreColor,
                        Color.Transparent,
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix
                    );
                }
                else
                {
                    TextRenderer.DrawText(
                        e.Graphics,
                        e.SubItem.Text,
                        e.Item.Font,
                        new Point(x, y),
                        foreColor,
                        backColor,
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix
                    );
                }
            }
        }

        private void LoadStrips()
        {
            strips_aircraft.Items.Clear();
            ListViewItem headerItem = new ListViewItem(new[]
            {
                "CALLSIGN",
                "TYPE",
                "DEP/ARR",
                "ETD",
                "STATUS",
                "ACTION"
            });

            headerItem.Font = new Font(MMI.eurofont_sml.FontFamily, 14F, FontStyle.Bold, GraphicsUnit.Pixel);
            headerItem.UseItemStyleForSubItems = true;
            strips_aircraft.Items.Add(headerItem);
            int rowIndex = 0;
            foreach (Aircraft aircraft in aircraftList)
            {
                ListViewItem item = new ListViewItem(new[]
                {
                    aircraft.Callsign,
                    aircraft.Type,
                    $"{aircraft.DepartureAirport}/{aircraft.ArrivalAirport}",
                    aircraft.EstimatedDeparture.ToString("HHmm"),
                    aircraft.Status,
                    "SEND"
                });

                item.Tag = aircraft;

                item.UseItemStyleForSubItems = false;

                strips_aircraft.Items.Add(item);
                rowIndex++;
            }

            strips_aircraft.MouseClick += Strips_aircraft_MouseClick;
            UpdateScrollbar();
        }

        private void Strips_aircraft_MouseClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = strips_aircraft.HitTest(e.X, e.Y);
            if (hit.Item != null && strips_aircraft.Items.IndexOf(hit.Item) > 0) 
            {
                ListViewItem.ListViewSubItem subItem = hit.SubItem;
                if (subItem != null && hit.Item.SubItems.IndexOf(subItem) == 5)
                {
                    Aircraft aircraft = (Aircraft)hit.Item.Tag;
                    HandleSendClick(aircraft);
                }
            }
        }

        private void HandleSendClick(Aircraft aircraft)
        {
            MessageBox.Show($"Sending PDC to {aircraft.Callsign}");
        }

        private void scl_aircraft_Scroll(object sender, EventArgs e)
        {
            if (strips_aircraft.Items.Count > 0)
            {
                strips_aircraft.SetScrollPosVert(scl_aircraft.PercentageValue);
            }
        }

        private void PDCWindow_Resize(object sender, EventArgs e)
        {
            UpdateScrollbar();
        }

        private void UpdateScrollbar() // TODO: fix dead spot at bottom
        {
            int rowHeight = 0;
            if (strips_aircraft.Items.Count > 0)
            {
                Rectangle itemRect = strips_aircraft.GetItemRect(0);
                rowHeight = itemRect.Height;
            }
            else
            {
                rowHeight = TextRenderer.MeasureText("Tg", strips_aircraft.Font).Height + 4;
            }

            int totalContentHeight = strips_aircraft.Items.Count * rowHeight;
            int visibleHeight = strips_aircraft.ClientSize.Height;

            scl_aircraft.PreferredHeight = totalContentHeight;
            scl_aircraft.ActualHeight = visibleHeight;
            scl_aircraft.Change = rowHeight;

            scl_aircraft.Enabled = totalContentHeight > visibleHeight;
        }

        private void strips_aircraft_MouseWheel(object sender, MouseEventArgs e)
        {
            int scrollLines = SystemInformation.MouseWheelScrollLines;
            int delta = (e.Delta * scrollLines) / 120; // This fixes scroll jumping buncha lines

            if (delta > 0)
            {
                this.scl_aircraft.Value -= scl_aircraft.Change;
            }
            else if (delta < 0)
            {
                this.scl_aircraft.Value += scl_aircraft.Change;
            }
        }

        private void StyleWindow()
        {
            this.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            strips_aircraft.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            scl_aircraft.ForeColor = Colours.GetColour(Colours.Identities.WindowBackground);
            scl_aircraft.BackColor = Colours.GetColour(Colours.Identities.InteractiveText);
            scl_aircraft.Enabled = false;
        }

    }

    public class Aircraft // Move to a type.cs file once finalized
    {
        public string Callsign { get; set; }
        public string Type { get; set; }
        public string DepartureAirport { get; set; }
        public string ArrivalAirport { get; set; }
        public DateTime EstimatedDeparture { get; set; }
        public string Status { get; set; }

        public Aircraft(string callsign, string type, string dep, string arr, DateTime etd, string status)
        {
            Callsign = callsign;
            Type = type;
            DepartureAirport = dep;
            ArrivalAirport = arr;
            EstimatedDeparture = etd;
            Status = status;
        }
    }
}