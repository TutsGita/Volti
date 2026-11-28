// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Volti {
  // Footer with the monitor state.
  sealed class StatusBar : Control {
    readonly Font font = new Font("Segoe UI", 9f);
    string text = "";
    bool online;

    internal StatusBar() {
      SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
      Height = Dpi.S(36);
    }

    internal void SetState(bool monitorOnline, string message) {
      online = monitorOnline;
      text = message;
      AccessibleName = message;
      Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e) {
      var g = e.Graphics;
      g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      g.Clear(Theme.Mix(Theme.Surface, Theme.Ink, 0.04f));
      using (var pen = new Pen(Theme.Line)) g.DrawLine(pen, 0, 0, Width, 0);
      int d = Dpi.S(8);
      using (var b = new SolidBrush(online ? Theme.Current.High : Theme.Current.Low)) g.FillEllipse(b, Dpi.S(18), (Height - d) / 2, d, d);
      int right = Width - Dpi.S(14);
      TextRenderer.DrawText(g, text, font, new Rectangle(Dpi.S(34), 0, right - Dpi.S(34), Height), Theme.Muted,
        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void Dispose(bool disposing) { if (disposing) font.Dispose(); base.Dispose(disposing); }
  }

  // Volti.UI.exe main window: title bar, tabs, the three pages and the status bar.
  sealed class PanelWindow : Form {
    enum Page { Devices, Settings, Themes }

    readonly MonitorConnection connection;
    readonly Action checkUpdates;
    readonly Icon appIcon;
    readonly Font titleFont = new Font("Segoe UI", 11f, FontStyle.Bold);
    readonly Font smallFont = new Font("Segoe UI", 8.5f);
    readonly List<IDisposable> resources = new List<IDisposable>();
    Panel titleBar, tabBar, content;
    FlatButton[] tabs;
    DevicesPage devices;
    SettingsPage settings;
    ThemesPage themes;
    StatusBar status;
    Page page = Page.Devices;

    internal event EventHandler PreferencesChanged;

    protected override CreateParams CreateParams {
      get {
        var p = base.CreateParams;
        p.Style |= 0x000A0000; // WS_MINIMIZEBOX | WS_SYSMENU, so the taskbar button can minimise and restore.
        return p;
      }
    }

    internal PanelWindow(MonitorConnection connection, Icon appIcon, Action checkUpdates) {
      this.connection = connection;
      this.appIcon = appIcon;
      this.checkUpdates = checkUpdates;
      Text = AppInfo.Name;
      Icon = appIcon;
      FormBorderStyle = FormBorderStyle.None;
      ShowInTaskbar = true;
      KeyPreview = true;
      Font = new Font("Segoe UI", 9.5f);
      resources.Add(Font);
      Padding = new Padding(Dpi.S(4)); // Room for the resize border.
      MinimumSize = Dpi.S(380, 480);
      RestoreSavedBounds();
      Build();
      ApplyTheme();
    }

    void RestoreSavedBounds() {
      StartPosition = FormStartPosition.CenterScreen;
      ClientSize = Dpi.S(460, 640);
      string[] saved = Settings.ReadString("PanelBounds", "").Split(',');
      int x, y, w, h;
      if (saved.Length != 4 || !int.TryParse(saved[0], out x) || !int.TryParse(saved[1], out y) ||
          !int.TryParse(saved[2], out w) || !int.TryParse(saved[3], out h)) return;
      var bounds = new Rectangle(x, y, Math.Max(MinimumSize.Width, w), Math.Max(MinimumSize.Height, h));
      // Only restore a position that is still visible on a connected screen.
      if (!Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(new Rectangle(bounds.X, bounds.Y, bounds.Width, Dpi.S(40))))) return;
      StartPosition = FormStartPosition.Manual;
      Bounds = bounds;
    }

    protected override void OnFormClosing(FormClosingEventArgs e) {
      var b = WindowState == FormWindowState.Normal ? Bounds : base.RestoreBounds;
      try { Settings.Write("PanelBounds", string.Join(",", new[] { b.X, b.Y, b.Width, b.Height }.Select(n => n.ToString(CultureInfo.InvariantCulture)))); }
      catch (Exception) {}
      base.OnFormClosing(e);
    }

    void Build() {
      SuspendLayout();
      content = new Panel { Dock = DockStyle.Fill };
      status = new StatusBar { Dock = DockStyle.Bottom };
      BuildTitleBar();
      BuildTabs();
      devices = new DevicesPage(connection) { Dock = DockStyle.Fill };
      devices.PreferencesChanged += (s, e) => Notify();
      settings = new SettingsPage { Dock = DockStyle.Fill };
      settings.PreferencesChanged += (s, e) => Notify();
      // Rebuild after the click that changed the language has finished, since it disposes the button.
      settings.LanguageChanged += (s, e) => BeginInvoke((Action)Rebuild);
      settings.TestNotice += (s, e) => connection.PreviewNotice();
      settings.Detect += (s, e) => { ShowPage(Page.Devices); devices.DetectOrStart(); };
      settings.CheckUpdates += (s, e) => checkUpdates();
      themes = new ThemesPage { Dock = DockStyle.Fill };
      themes.ThemeChanged += (s, e) => { ApplyTheme(); Notify(); };
      content.Controls.AddRange(new Control[] { devices, settings, themes });
      // Docking order: the last added top control sits highest.
      Controls.Add(content);
      Controls.Add(status);
      Controls.Add(tabBar);
      Controls.Add(titleBar);
      ShowPage(page);
      ResumeLayout(true);
    }

    // Rebuilds every control after a language change so all text is translated in one place.
    void Rebuild() {
      SuspendLayout();
      foreach (Control control in Controls.Cast<Control>().ToList()) control.Dispose();
      Controls.Clear();
      Build();
      ApplyTheme();
      ShowSnapshot(connection.Snapshot, connection.Online);
      ResumeLayout(true);
    }

    void Notify() { if (PreferencesChanged != null) PreferencesChanged(this, EventArgs.Empty); }

    void BuildTitleBar() {
      titleBar = new Panel { Dock = DockStyle.Top, Height = Dpi.S(44) };
      var logo = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, Bounds = Dpi.S(16, 12, 20, 20) };
      logo.Image = LoadLogo();
      resources.Add(logo.Image);
      var name = new Label { Text = AppInfo.Name, Font = titleFont, AutoSize = true, Location = new Point(Dpi.S(42), Dpi.S(11)) };
      var version = new Label { Text = AppInfo.Version + (connection.IsDemo ? "  ·  demo" : ""), Font = smallFont, AutoSize = true, Tag = "muted" };
      var minimize = new FlatButton("", Glyph.Minimize, ButtonStyle.Caption) { AccessibleName = L.T("Minimize"), TabStop = false };
      var close = new FlatButton("", Glyph.Close, ButtonStyle.Close) { AccessibleName = L.T("Close"), TabStop = false };
      minimize.Click += (s, e) => WindowState = FormWindowState.Minimized;
      close.Click += (s, e) => Close();
      titleBar.Controls.AddRange(new Control[] { logo, name, version, minimize, close });
      titleBar.Resize += (s, e) => {
        close.SetBounds(titleBar.Width - Dpi.S(46), 0, Dpi.S(46), titleBar.Height);
        minimize.SetBounds(close.Left - Dpi.S(46), 0, Dpi.S(46), titleBar.Height);
        version.Location = new Point(name.Right + Dpi.S(6), name.Top + Dpi.S(5));
      };
      MouseEventHandler drag = (s, e) => { if (e.Button == MouseButtons.Left) Win32.DragWindow(Handle); };
      titleBar.MouseDown += drag; logo.MouseDown += drag; name.MouseDown += drag; version.MouseDown += drag;
    }

    Image LoadLogo() {
      try { return Image.FromFile(System.IO.Path.Combine(Application.StartupPath, "assets", "volti-icon.png")); }
      catch (System.IO.IOException) {} catch (OutOfMemoryException) {} catch (ArgumentException) {}
      using (var icon = new Icon(appIcon, Dpi.S(24), Dpi.S(24))) return icon.ToBitmap();
    }

    void BuildTabs() {
      tabBar = new Panel { Dock = DockStyle.Top, Height = Dpi.S(44) };
      string[] names = { "Overview", "Settings", "Themes" };
      Glyph[] glyphs = { Glyph.Overview, Glyph.Gear, Glyph.Palette };
      tabs = new FlatButton[names.Length];
      for (int i = 0; i < names.Length; i++) {
        var target = (Page)i;
        var tab = new FlatButton(L.T(names[i]), glyphs[i], ButtonStyle.Tab) { AccessibleRole = AccessibleRole.PageTab };
        tab.Click += (s, e) => ShowPage(target);
        tabs[i] = tab;
        tabBar.Controls.Add(tab);
      }
      tabBar.Resize += (s, e) => {
        int x = Dpi.S(10);
        foreach (var tab in tabs) { tab.SetBounds(x, 0, tab.PreferredWidth, tabBar.Height); x = tab.Right + Dpi.S(2); }
      };
      tabBar.Paint += (s, e) => { using (var pen = new Pen(Theme.Line)) e.Graphics.DrawLine(pen, 0, tabBar.Height - 1, tabBar.Width, tabBar.Height - 1); };
    }

    void ShowPage(Page next) {
      page = next;
      devices.Visible = page == Page.Devices;
      settings.Visible = page == Page.Settings;
      themes.Visible = page == Page.Themes;
      for (int i = 0; i < tabs.Length; i++) { tabs[i].Selected = i == (int)page; tabs[i].Invalidate(); }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
      if (keyData == Keys.F5) { devices.DetectOrStart(); return true; }
      if (keyData == (Keys.Control | Keys.Tab)) { ShowPage((Page)(((int)page + 1) % 3)); return true; }
      if (keyData == (Keys.Control | Keys.Shift | Keys.Tab)) { ShowPage((Page)(((int)page + 2) % 3)); return true; }
      for (int i = 0; i < 3; i++)
        if (keyData == (Keys.Control | (Keys.D1 + i))) { ShowPage((Page)i); return true; }
      return base.ProcessCmdKey(ref msg, keyData);
    }

    internal void ShowSnapshot(PanelSnapshot snapshot, bool online) {
      var list = DeviceCatalog.From(snapshot);
      devices.SetDevices(list, online);
      string message;
      if (!online) message = L.T("Monitor offline");
      else {
        message = L.T("Monitor running");
        string latest = list.Select(d => d.ObservedTime).Where(t => t.Length > 0).OrderByDescending(t => t).FirstOrDefault();
        if (latest != null) message += "  ·  " + L.F("Last update {0}", latest);
        if (snapshot.Status == "Bluetooth detection failed") message += "  ·  " + L.T(snapshot.Status);
      }
      if (connection.IsDemo) message += "  ·  demo";
      status.SetState(online, message);
    }

    protected override void OnPaint(PaintEventArgs e) {
      base.OnPaint(e);
      using (var pen = new Pen(Theme.Line)) e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }

    internal void ApplyTheme() {
      BackColor = Theme.Surface;
      foreach (Control bar in new Control[] { titleBar, tabBar }) {
        bar.BackColor = Theme.Surface;
        foreach (Control child in bar.Controls) {
          child.BackColor = Theme.Surface;
          child.ForeColor = Equals(child.Tag, "muted") ? Theme.Muted : Theme.Ink;
        }
      }
      content.BackColor = Theme.Surface;
      devices.ApplyTheme();
      settings.ApplyTheme();
      themes.ApplyTheme();
      Invalidate(true);
    }

    // Native rounded corners and shadow on Windows 11; ignored elsewhere.
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    protected override void OnHandleCreated(EventArgs e) {
      base.OnHandleCreated(e);
      try { int round = 2; DwmSetWindowAttribute(Handle, 33, ref round, sizeof(int)); }
      catch (DllNotFoundException) {} catch (EntryPointNotFoundException) {}
    }

    // Resizing from the one-pixel frame and the bottom-right corner.
    protected override void WndProc(ref Message m) {
      const int NcHitTest = 0x84;
      base.WndProc(ref m);
      if (m.Msg != NcHitTest || WindowState != FormWindowState.Normal) return;
      var point = PointToClient(new Point(m.LParam.ToInt32() & 0xFFFF, (short)(m.LParam.ToInt32() >> 16)));
      int edge = Dpi.S(6);
      bool left = point.X < edge, right = point.X >= ClientSize.Width - edge, top = point.Y < edge, bottom = point.Y >= ClientSize.Height - edge;
      int hit = top && left ? 13 : top && right ? 14 : bottom && left ? 16 : bottom && right ? 17 : left ? 10 : right ? 11 : top ? 12 : bottom ? 15 : 0;
      if (hit != 0) m.Result = new IntPtr(hit);
    }

    protected override void Dispose(bool disposing) {
      if (disposing) {
        foreach (var resource in resources) resource.Dispose();
        titleFont.Dispose();
        smallFont.Dispose();
      }
      base.Dispose(disposing);
    }
  }
}
