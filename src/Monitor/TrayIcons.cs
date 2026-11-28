// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Volti {
  // One tray icon per device with a reading, plus a Volti icon whenever no device icon is visible.
  sealed class TrayIcons : IDisposable {
    sealed class Entry {
      internal NotifyIcon Icon;
      internal Icon Image;
      internal string State;
      internal DeviceView Device;
    }

    readonly Icon appIcon;
    readonly Action openPanel;
    readonly ContextMenuStrip menu;
    readonly ToolStripMenuItem openItem, taskbarItem, exitItem;
    readonly ToolStripSeparator deviceSeparator = new ToolStripSeparator();
    readonly List<ToolStripMenuItem> deviceItems = new List<ToolStripMenuItem>();
    readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
    readonly NotifyIcon applicationIcon;
    readonly bool customPreview = !AppInfo.HasSwitch("--no-tray-preview");
    DateTime lastOpen;

    NotifyIcon hovered;
    Timer hoverTimer;
    TrayPreview preview;
    Point hoverAnchor;
    DateTime hoverSince;

    internal TrayIcons(Icon appIcon, Action openPanel, Action exit) {
      this.appIcon = appIcon;
      this.openPanel = openPanel;
      menu = new ContextMenuStrip { Renderer = new MenuRenderer(), ShowImageMargin = false, ShowCheckMargin = false };
      openItem = new ToolStripMenuItem();
      openItem.Click += (s, e) => openPanel();
      taskbarItem = new ToolStripMenuItem();
      taskbarItem.Click += (s, e) => OpenTaskbarSettings();
      exitItem = new ToolStripMenuItem();
      exitItem.Click += (s, e) => exit();
      menu.Items.AddRange(new ToolStripItem[] { deviceSeparator, openItem, taskbarItem, exitItem });
      applicationIcon = new NotifyIcon { Icon = appIcon, Text = AppInfo.Name, ContextMenuStrip = menu };
      Wire(applicationIcon, null);
      ApplyLanguage();
      ApplyTheme();
    }

    static void OpenTaskbarSettings() {
      try { using (Process.Start(new ProcessStartInfo("ms-settings:taskbar") { UseShellExecute = true })) {} }
      catch (System.ComponentModel.Win32Exception) { MessageBox.Show(L.T("Could not open Windows taskbar settings."), AppInfo.Name); }
    }

    internal void ApplyLanguage() {
      openItem.Text = L.T("Open Volti");
      taskbarItem.Text = L.T("Windows taskbar settings");
      exitItem.Text = L.T("Exit");
    }

    internal void ApplyTheme() {
      ClearPreview();
      menu.BackColor = Theme.Surface;
      menu.ForeColor = Theme.Ink;
      foreach (var entry in entries.Values) entry.State = null; // Recolour on the next update.
    }

    static bool ShowsInTray(DeviceView device) { return device.Slot >= 0 ? device.HasPercent : !device.Unavailable; }

    internal void Update(IList<DeviceView> devices) {
      var present = new HashSet<string>();
      bool anyVisible = false;
      foreach (var device in devices) {
        if (!ShowsInTray(device)) continue;
        present.Add(device.Id);
        Entry entry;
        if (!entries.TryGetValue(device.Id, out entry)) {
          entry = new Entry { Icon = new NotifyIcon { Icon = appIcon, ContextMenuStrip = menu } };
          string id = device.Id;
          Wire(entry.Icon, () => { Entry current; return entries.TryGetValue(id, out current) ? current.Device : null; });
          entries.Add(device.Id, entry);
        }
        entry.Device = device;
        string label = device.DisplayName + ": " + device.DisplayLevel;
        entry.Icon.Text = customPreview ? "" : (label.Length > 63 ? label.Substring(0, 63) : label);
        string state = device.Kind + "|" + device.Percent + "|" + Theme.Current.Name;
        if (entry.State != state) {
          var next = CreateIcon(device);
          entry.Icon.Icon = next;
          if (entry.Image != null) entry.Image.Dispose();
          entry.Image = next;
          entry.State = state;
        }
        entry.Icon.Visible = Settings.IsTrayVisible(device.Id);
        anyVisible |= entry.Icon.Visible;
      }
      foreach (string id in new List<string>(entries.Keys)) {
        if (present.Contains(id)) continue;
        var entry = entries[id];
        if (hovered == entry.Icon) ClearPreview();
        entry.Icon.Dispose();
        if (entry.Image != null) entry.Image.Dispose();
        entries.Remove(id);
      }
      applicationIcon.Visible = !anyVisible;
      UpdateMenu(devices);
    }

    void UpdateMenu(IList<DeviceView> devices) {
      foreach (var item in deviceItems) { menu.Items.Remove(item); item.Dispose(); }
      deviceItems.Clear();
      int index = 0;
      foreach (var device in devices) {
        if (!ShowsInTray(device)) continue;
        var item = new ToolStripMenuItem(device.DisplayName + ": " + device.DisplayLevel + (device.Charging ? "  " + Battery.Charging : "")) { Enabled = false };
        menu.Items.Insert(index++, item);
        deviceItems.Add(item);
      }
      deviceSeparator.Visible = index > 0;
    }

    // The icon that should carry a balloon or anchor a notice.
    internal NotifyIcon Visible() {
      foreach (var entry in entries.Values) if (entry.Icon.Visible) return entry.Icon;
      return applicationIcon;
    }

    static Icon CreateIcon(DeviceView device) {
      Color color = device.HasPercent ? Theme.Battery(device.Percent) : Color.FromArgb(130, 132, 143);
      using (var bitmap = new Bitmap(32, 32, PixelFormat.Format32bppArgb)) {
        using (var g = Graphics.FromImage(bitmap)) {
          g.Clear(Color.Transparent);
          DeviceArt.Draw(g, device.Kind, color, new Rectangle(0, 0, 32, 32));
        }
        IntPtr handle = bitmap.GetHicon();
        try { using (var source = Icon.FromHandle(handle)) return (Icon)source.Clone(); }
        finally { Win32.DestroyIcon(handle); }
      }
    }

    void Wire(NotifyIcon icon, Func<DeviceView> device) {
      icon.MouseClick += (s, e) => {
        if (e.Button != MouseButtons.Left) return;
        ClearPreview();
        // A double click arrives as two clicks; open the panel once.
        DateTime now = DateTime.UtcNow;
        if (now - lastOpen < TimeSpan.FromMilliseconds(SystemInformation.DoubleClickTime)) return;
        lastOpen = now;
        openPanel();
      };
      if (device == null || !customPreview) return;
      icon.MouseMove += (s, e) => {
        Point position = Cursor.Position;
        if (hovered == icon && (preview != null || (Math.Abs(position.X - hoverAnchor.X) <= 3 && Math.Abs(position.Y - hoverAnchor.Y) <= 3))) return;
        ClearPreview();
        hovered = icon;
        hoverAnchor = position;
        hoverSince = DateTime.UtcNow;
        hoverTimer = new Timer { Interval = 125 };
        hoverTimer.Tick += (s2, e2) => {
          Point now = Cursor.Position;
          if (!icon.Visible || Math.Abs(now.X - hoverAnchor.X) > 9 || Math.Abs(now.Y - hoverAnchor.Y) > 9) { ClearPreview(); return; }
          if (preview != null || DateTime.UtcNow - hoverSince < TimeSpan.FromMilliseconds(375)) return;
          var current = device();
          if (current == null) { ClearPreview(); return; }
          preview = new TrayPreview(hoverAnchor, current);
          preview.Show();
        };
        hoverTimer.Start();
      };
    }

    internal void ClearPreview() {
      if (hoverTimer != null) { hoverTimer.Dispose(); hoverTimer = null; }
      if (preview != null) { preview.Dispose(); preview = null; }
      hovered = null;
    }

    public void Dispose() {
      ClearPreview();
      foreach (var entry in entries.Values) { entry.Icon.Dispose(); if (entry.Image != null) entry.Image.Dispose(); }
      entries.Clear();
      applicationIcon.Dispose();
      menu.Dispose();
    }
  }
}
