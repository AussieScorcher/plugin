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

        public PDCWindow()
        {
            InitializeComponent();
            StyleWindow();
            this.Text = "Pre-Departure Clearance";
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
                new Aircraft("NZA118", "A321", "YSSY", "NZCH", DateTime.Parse("21:59"), "Pending")
            };
        }

        private void InitializeListView()
        {
            // Basic ListView properties
            strips_aircraft.View = View.Details;
            strips_aircraft.FullRowSelect = true;
            strips_aircraft.GridLines = true;
            strips_aircraft.MultiSelect = false;
            strips_aircraft.HeaderStyle = ColumnHeaderStyle.None;  // Remove built-in header
            strips_aircraft.Font = new Font(MMI.eurofont_sml.FontFamily, 14F, FontStyle.Regular, GraphicsUnit.Pixel);

            // Add columns with fixed widths (no headers)
            strips_aircraft.Columns.Add("", 85);  // CALLSIGN
            strips_aircraft.Columns.Add("", 65);  // TYPE
            strips_aircraft.Columns.Add("", 110); // DEP/ARR
            strips_aircraft.Columns.Add("", 60);  // ETD
            strips_aircraft.Columns.Add("", 80);  // STATUS
            strips_aircraft.Columns.Add("", 60);  // ACTION
        }

        private void LoadStrips()
        {
            strips_aircraft.Items.Clear();

            // Add header as first row
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

            // Add aircraft data
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
                item.ForeColor = Color.White;
                item.SubItems[1].ForeColor = Color.White;
                item.SubItems[2].ForeColor = Color.White;
                item.SubItems[3].ForeColor = Color.White;
                item.SubItems[4].ForeColor = Color.White;

                item.UseItemStyleForSubItems = false;

                // Style the SEND button
                item.SubItems[5].BackColor = Colours.GetColour(Colours.Identities.CPDLCSendButton);
                item.SubItems[5].ForeColor = Color.White;

                strips_aircraft.Items.Add(item);
            }

            strips_aircraft.MouseClick += Strips_aircraft_MouseClick;
        }

        private void Strips_aircraft_MouseClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = strips_aircraft.HitTest(e.X, e.Y);
            if (hit.Item != null && strips_aircraft.Items.IndexOf(hit.Item) > 0)  // Skip header row
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

        private void PDCWindow_Resize(object sender, EventArgs e)
        {
            label1.Text = $"Size: {this.Size.Width}x{this.Size.Height}";
        }

        private void StyleWindow()
        {
            this.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            strips_aircraft.BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
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