// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Volti {
  // A setting with a title, an optional description and a control on the right.
  sealed class SettingRow : Control {
    readonly string title, description;
    internal readonly Control Action;
    readonly Font titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
    readonly Font detailFont = new Font("Segoe UI", 9f);

    internal SettingRow(string title, string description, Control action) {
      SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
      this.title = title;
      this.description = description ?? "";
      Action = action;
      AccessibleName = title;
      AccessibleDescription = description;
      if (action != null) {
        if (string.IsNullOrEmpty(action.AccessibleName)) action.AccessibleName = title;
        Controls.Add(action);
      }
      Height = Dpi.S(this.description.Length > 0 ? 64 : 52);
    }

    protected override void OnResize(EventArgs e) {
      base.OnResize(e);
      if (Action == null) return;
      var button = Action as FlatButton;
      if (button != null) button.Width = Math.Min(button.PreferredWidth, Width / 2);
      Action.Location = new Point(Width - Dpi.S(16) - Action.Width, (Height - Action.Height) / 2);
    }

    protected override void OnPaint(PaintEventArgs e) {
      var g = e.Graphics;
      g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      g.Clear(Parent == null || Parent.BackColor.A == 0 ? Theme.Surface : Parent.BackColor);
      var card = new Rectangle(0, 0, Width - 1, Height - 1);
      g.FillRoundedRectangle(Theme.Card, card, Dpi.S(12));
      g.DrawRoundedRectangle(Theme.Line, card, Dpi.S(12));
      int right = Action == null ? Width - Dpi.S(16) : Action.Left - Dpi.S(12);
      const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine;
      if (description.Length == 0) {
        TextRenderer.DrawText(g, title, titleFont, new Rectangle(Dpi.S(16), 0, right - Dpi.S(16), Height), Theme.Ink, flags);
        return;
      }
      TextRenderer.DrawText(g, title, titleFont, new Rectangle(Dpi.S(16), Dpi.S(10), right - Dpi.S(16), Dpi.S(22)), Theme.Ink, flags);
      TextRenderer.DrawText(g, description, detailFont, new Rectangle(Dpi.S(16), Dpi.S(32), right - Dpi.S(16), Dpi.S(22)), Theme.Muted, flags);
    }

    protected override void Dispose(bool disposing) { if (disposing) { titleFont.Dispose(); detailFont.Dispose(); } base.Dispose(disposing); }
  }

  sealed class SettingsPage : Control {
    readonly ScrollPanel scroll = new ScrollPanel();
    readonly List<Control> stack = new List<Control>();
    readonly Font sectionFont = new Font("Segoe UI", 9f, FontStyle.Bold);
    readonly Toggle startup, alerts;
    readonly Dictionary<string, FlatButton> languages = new Dictionary<string, FlatButton>();
    internal event EventHandler PreferencesChanged, LanguageChanged, TestNotice, Detect, CheckUpdates;

    internal SettingsPage() {
      Controls.Add(scroll);

      Section("General");
      startup = new Toggle(L.T("Start with Windows"));
      startup.Set(Settings.StartupEnabled);
      startup.CheckedChanged += (s, e) => SetStartup(startup.Checked);
      Row("Start with Windows", "Listed in Windows Startup apps.", startup);
      alerts = new Toggle(L.T("Battery notifications"));
      alerts.Set(Settings.AlertsEnabled);
      alerts.CheckedChanged += (s, e) => { Settings.AlertsEnabled = alerts.Checked; Raise(PreferencesChanged); };
      Row("Battery notifications", "Low and critical alerts for receivers that support them.", alerts);
      var test = new FlatButton(L.T("Test"), Glyph.Bell);
      test.Click += (s, e) => Raise(TestNotice);
      Row("Test battery notification", null, test);

      Section("Language");
      var picker = new FlowLayoutPanel { AutoSize = false, WrapContents = true, Padding = Padding.Empty, Margin = Padding.Empty };
      foreach (var language in L.Languages) {
        string code = language[0];
        var button = new FlatButton(language[1], Glyph.None, ButtonStyle.Subtle) { Selected = code == L.Language, Margin = new Padding(0, 0, Dpi.S(6), Dpi.S(6)) };
        button.Width = button.PreferredWidth;
        button.Click += (s, e) => { if (code == L.Language) return; L.SetLanguage(code); Raise(PreferencesChanged); Raise(LanguageChanged); };
        languages[code] = button;
        picker.Controls.Add(button);
      }
      picker.Tag = "flow";
      Add(picker, Dpi.S(40));

      Section("Devices");
      var detect = new FlatButton(L.T("Detect"), Glyph.Refresh);
      detect.Click += (s, e) => Raise(Detect);
      Row("Detect devices", "Search USB receivers, Bluetooth and Razer Synapse now.", detect);
      var taskbar = new FlatButton(L.T("Open"), Glyph.Gear);
      taskbar.Click += (s, e) => OpenTaskbarSettings();
      Row("Tray icons", "Choose which icons stay next to the clock.", taskbar);

      Section("About");
      var github = new FlatButton("GitHub", Glyph.Info, ButtonStyle.Subtle);
      github.Click += (s, e) => { try { using (Process.Start(AppInfo.RepositoryUrl)) {} } catch (Exception) {} };
      Row(L.F("Version {0}", AppInfo.Version), "tutsgita · AGPL-3.0-only", github);
      var updates = new FlatButton(L.T("Check for updates"), Glyph.Refresh);
      updates.Click += (s, e) => Raise(CheckUpdates);
      Row("Updates", "Stable releases are verified with SHA-256 before installing.", updates);
    }

    void Raise(EventHandler handler) { if (handler != null) handler(this, EventArgs.Empty); }

    static void OpenTaskbarSettings() {
      try { using (Process.Start(new ProcessStartInfo("ms-settings:taskbar") { UseShellExecute = true })) {} }
      catch (Exception) { MessageBox.Show(L.T("Could not open Windows taskbar settings."), AppInfo.Name); }
    }

    void SetStartup(bool enable) {
      try { Settings.SetStartup(enable); }
      catch (Exception ex) {
        startup.Set(Settings.StartupEnabled);
        MessageBox.Show(FindForm(), ex.Message, L.T("Volti startup"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
      }
    }

    void Section(string text) {
      var label = new Label { Text = L.T(text).ToUpperInvariant(), Font = sectionFont, AutoEllipsis = true, Tag = "section", TextAlign = ContentAlignment.BottomLeft };
      Add(label, Dpi.S(34));
    }

    void Row(string title, string description, Control action) {
      Add(new SettingRow(L.T(title), description == null ? null : L.T(description), action), 0);
    }

    void Add(Control control, int height) {
      if (height > 0) control.Height = height;
      stack.Add(control);
      scroll.Content.Controls.Add(control);
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); Arrange(); }

    void Arrange() {
      scroll.SetBounds(Dpi.S(14), 0, ClientSize.Width - Dpi.S(14), ClientSize.Height);
      int width = Math.Max(Dpi.S(200), ClientSize.Width - Dpi.S(40)), y = Dpi.S(4);
      foreach (var control in stack) {
        bool section = Equals(control.Tag, "section");
        control.SetBounds(Dpi.S(6), y, width, control.Height);
        y += control.Height + (section ? Dpi.S(6) : Dpi.S(8));
      }
      scroll.ContentHeight = y + Dpi.S(8);
    }

    internal void ApplyTheme() {
      BackColor = scroll.BackColor = scroll.Content.BackColor = Theme.Surface;
      foreach (var control in stack) {
        control.BackColor = control is SettingRow ? Theme.Card : Theme.Surface;
        control.ForeColor = Equals(control.Tag, "section") ? Theme.Muted : Theme.Ink;
      }
      Invalidate(true);
    }

    protected override void Dispose(bool disposing) { if (disposing) sectionFont.Dispose(); base.Dispose(disposing); }
  }
}
