// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Volti {
  // Overview: a summary header and one card per device.
  sealed class DevicesPage : Control {
    readonly MonitorConnection connection;
    readonly Label title = new Label { AutoSize = false, AutoEllipsis = true };
    readonly Label summary = new Label { AutoSize = false, AutoEllipsis = true };
    readonly FlatButton sort, detect;
    readonly ContextMenuStrip sortMenu = new ContextMenuStrip();
    readonly ScrollPanel list = new ScrollPanel();
    readonly Dictionary<string, DeviceRow> rows = new Dictionary<string, DeviceRow>();
    readonly Label emptyTitle = new Label { TextAlign = ContentAlignment.MiddleCenter };
    readonly Label emptyText = new Label { TextAlign = ContentAlignment.TopCenter };
    readonly FlatButton emptyAction;
    readonly Font titleFont = new Font("Segoe UI", 15f, FontStyle.Bold);
    readonly Font emptyFont = new Font("Segoe UI", 11f, FontStyle.Bold);
    readonly Timer spinner = new Timer { Interval = 40 };
    List<DeviceView> devices = new List<DeviceView>();
    bool online;
    DateTime scanDeadline;
    internal event EventHandler PreferencesChanged;

    internal DevicesPage(MonitorConnection connection) {
      this.connection = connection;
      SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
      title.Text = L.T("Devices");
      title.Font = titleFont;
      summary.ForeColor = Theme.Muted;
      sort = new FlatButton("", Glyph.Sort, ButtonStyle.Subtle) { AccessibleName = L.T("Sort devices") };
      sort.Click += (s, e) => ShowSortMenu();
      detect = new FlatButton(L.T("Detect devices"), Glyph.Refresh, ButtonStyle.Primary);
      detect.Click += (s, e) => DetectOrStart();
      emptyTitle.Font = emptyFont;
      emptyAction = new FlatButton(L.T("Detect devices"), Glyph.Refresh, ButtonStyle.Secondary);
      emptyAction.Click += (s, e) => DetectOrStart();
      list.Content.Controls.AddRange(new Control[] { emptyTitle, emptyText, emptyAction });
      Controls.AddRange(new Control[] { title, summary, sort, detect, list });
      foreach (var option in new[] { new[] { "default", "Default order" }, new[] { "level", "Lowest battery first" }, new[] { "name", "Name" } }) {
        string key = option[0];
        var item = new ToolStripMenuItem(L.T(option[1])) { Tag = key };
        item.Click += (s, e) => { Settings.DeviceSort = key; Arrange(); };
        sortMenu.Items.Add(item);
      }
      spinner.Tick += (s, e) => Spin();
    }

    void ShowSortMenu() {
      sortMenu.Renderer = new MenuRenderer();
      sortMenu.BackColor = Theme.Surface;
      string current = Settings.DeviceSort;
      foreach (ToolStripMenuItem item in sortMenu.Items) { item.Checked = Equals(item.Tag, current); item.ForeColor = Theme.Ink; }
      sortMenu.Show(sort, new Point(0, sort.Height));
    }

    internal void DetectOrStart() {
      if (!online) { connection.StartMonitor(); return; }
      connection.Detect();
      scanDeadline = DateTime.UtcNow.AddSeconds(15);
      spinner.Start();
    }

    void Spin() {
      if (connection.ScanFinished() || DateTime.UtcNow >= scanDeadline) {
        spinner.Stop();
        detect.GlyphAngle = emptyAction.GlyphAngle = 0;
      } else detect.GlyphAngle = emptyAction.GlyphAngle = (detect.GlyphAngle + 24) % 360;
      detect.Invalidate();
      emptyAction.Invalidate();
    }

    internal bool Scanning { get { return spinner.Enabled; } }

    internal void SetDevices(List<DeviceView> next, bool monitorOnline) {
      devices = next;
      online = monitorOnline;
      detect.Text = L.T(online ? "Detect devices" : "Start monitor");
      detect.AccessibleName = detect.Text;
      emptyAction.Text = detect.Text;
      var seen = new HashSet<string>();
      foreach (var device in devices) {
        seen.Add(device.Id);
        DeviceRow row;
        if (!rows.TryGetValue(device.Id, out row)) {
          row = new DeviceRow(device);
          row.LayoutChanged += (s, e) => { Arrange(); list.Reveal((Control)s); };
          row.PreferencesChanged += (s, e) => { if (PreferencesChanged != null) PreferencesChanged(this, EventArgs.Empty); };
          row.GotFocus += (s, e) => list.Reveal((Control)s);
          rows.Add(device.Id, row);
          list.Content.Controls.Add(row);
          row.ApplyTheme();
        } else row.SetDevice(device);
      }
      foreach (string id in rows.Keys.Where(id => !seen.Contains(id)).ToList()) {
        rows[id].Dispose();
        rows.Remove(id);
      }
      UpdateSummary();
      Arrange();
    }

    void UpdateSummary() {
      if (!online) { summary.Text = L.T("Monitor offline"); return; }
      string count = devices.Count == 1 ? L.T("1 device") : L.F("{0} devices", devices.Count);
      var levels = devices.Where(d => d.HasPercent).Select(d => d.Percent).ToList();
      summary.Text = levels.Count == 0 ? count : count + "  ·  " + L.F("lowest {0}%", levels.Min());
    }

    IEnumerable<DeviceView> Ordered() {
      switch (Settings.DeviceSort) {
        case "level": return devices.OrderBy(d => d.HasPercent ? d.Percent : 101).ThenBy(d => d.DisplayName, StringComparer.CurrentCultureIgnoreCase);
        case "name": return devices.OrderBy(d => d.DisplayName, StringComparer.CurrentCultureIgnoreCase);
        default: return devices;
      }
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); Arrange(); }

    void Arrange() {
      if (detect == null) return;
      int pad = Dpi.S(20);
      int width = ClientSize.Width;
      bool sortable = rows.Count > 1;
      sort.Visible = sortable;
      detect.Width = detect.PreferredWidth;
      detect.Location = new Point(width - pad - detect.Width, Dpi.S(12));
      sort.SetBounds(detect.Left - Dpi.S(42), Dpi.S(12), Dpi.S(34), Dpi.S(34));
      title.SetBounds(pad, Dpi.S(12), Math.Max(Dpi.S(60), (sortable ? sort.Left : detect.Left) - pad - Dpi.S(8)), Dpi.S(34));
      summary.SetBounds(pad + 1, Dpi.S(48), Math.Max(Dpi.S(60), width - pad * 2), Dpi.S(20));
      list.SetBounds(Dpi.S(14), Dpi.S(76), width - Dpi.S(14), Math.Max(0, ClientSize.Height - Dpi.S(76)));

      int y = Dpi.S(2), rowWidth = Math.Max(Dpi.S(200), width - pad * 2);
      bool empty = rows.Count == 0;
      emptyTitle.Visible = emptyText.Visible = emptyAction.Visible = empty;
      if (empty) {
        emptyTitle.Text = L.T(online ? "No battery readings yet" : "Monitor offline");
        emptyText.Text = L.T(online
          ? "Connect a supported receiver or Bluetooth device, then select Detect devices."
          : "The monitor is not running, so readings are paused.");
        emptyTitle.SetBounds(Dpi.S(6), Dpi.S(60), rowWidth, Dpi.S(28));
        emptyText.SetBounds(Dpi.S(26), Dpi.S(92), rowWidth - Dpi.S(40), Dpi.S(44));
        emptyAction.Width = emptyAction.PreferredWidth;
        emptyAction.Location = new Point(Dpi.S(6) + (rowWidth - emptyAction.Width) / 2, Dpi.S(144));
        y = emptyAction.Bottom + Dpi.S(12);
      }
      foreach (var device in Ordered()) {
        var row = rows[device.Id];
        row.SetBounds(Dpi.S(6), y, rowWidth, row.Height);
        y += row.Height + Dpi.S(10);
      }
      list.ContentHeight = y;
    }

    internal void ApplyTheme() {
      BackColor = list.BackColor = list.Content.BackColor = Theme.Surface;
      foreach (Control c in new Control[] { title, emptyTitle }) { c.BackColor = Theme.Surface; c.ForeColor = Theme.Ink; }
      foreach (Control c in new Control[] { summary, emptyText }) { c.BackColor = Theme.Surface; c.ForeColor = Theme.Muted; }
      foreach (var row in rows.Values) row.ApplyTheme();
      Invalidate(true);
    }

    protected override void Dispose(bool disposing) {
      if (disposing) { spinner.Dispose(); sortMenu.Dispose(); titleFont.Dispose(); emptyFont.Dispose(); }
      base.Dispose(disposing);
    }
  }
}
