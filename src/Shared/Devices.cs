// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Volti {
  static class Battery {
    internal const string Charging = "⚡";
    internal const string NoValue = "—";
    // Status values published for the two receiver slots.
    internal const string Disconnected = "Disconnected", Waiting = "Waiting for reading", NoResponse = "No response",
      AccessDenied = "Access denied", OtherModel = "-";

    internal static bool TryPercent(string value, out int percent) {
      percent = 0;
      if (value == null) return false;
      int i = value.IndexOf('%');
      return i > 0 && int.TryParse(value.Substring(0, i).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out percent);
    }

    internal static string MinuteStamp(DateTime utc) {
      return utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    // "≈ 3 h 20 min remaining" style text, or an empty string when no estimate exists.
    internal static string Remaining(int minutes) {
      if (minutes < 0) return "";
      return "≈ " + (minutes >= 60 ? (minutes / 60) + " h " : "") + (minutes % 60) + " min " + L.T("remaining");
    }
  }

  enum Transport { Usb, Bluetooth, Synapse, XInput }

  // One device as the panel and the tray show it, whatever reader produced it.
  sealed class DeviceView {
    internal string Id, Name, Level, Observed;
    internal Transport Transport;
    internal DeviceKind Kind;
    internal int Slot = -1;        // 0 = DELUX mouse receiver, 1 = JBL headset receiver, -1 = discovered device.
    internal int Percent = -1;     // -1 when the device reports a status instead of a percentage.
    internal bool Charging, Unavailable;

    internal bool HasAlerts { get { return Slot >= 0; } }
    internal bool HasPercent { get { return Percent >= 0; } }

    internal string TransportLabel {
      get {
        switch (Transport) {
          case Transport.Bluetooth: return "Bluetooth";
          case Transport.Synapse: return "Razer Synapse";
          case Transport.XInput: return "XInput";
          default: return "USB 2.4 GHz";
        }
      }
    }

    internal string DisplayName {
      get { string alias = Settings.Alias(Id); return alias.Length > 0 ? alias : Name; }
    }

    // Translated level text: "45%", "Low", "Waiting for reading" or a dash.
    internal string DisplayLevel {
      get {
        if (Unavailable) return Battery.NoValue;
        if (HasPercent) return Percent + "%";
        return L.T(Level);
      }
    }

    // HH:mm of the last reading, when known.
    internal string ObservedTime {
      get {
        string stamp = Observed == null ? "" : Observed.Split('|')[0];
        return stamp.Length == 16 ? stamp.Substring(11) : "";
      }
    }

    // Minutes left, -1 while still learning, or int.MinValue when there is no estimate at all.
    internal int RemainingMinutes {
      get {
        string[] parts = (Observed ?? "").Split('|');
        int minutes;
        return parts.Length == 2 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes) ? minutes : int.MinValue;
      }
    }

    internal bool Learning { get { return RemainingMinutes == -1; } }
  }

  static class DeviceCatalog {
    internal const string MouseId = "mouse", HeadsetId = "headset";
    internal const string MouseName = "DELUX M800 Mini", HeadsetName = "JBL Quantum 650";

    internal static Transport TransportOf(string id) {
      if (id.StartsWith("synapse", StringComparison.Ordinal)) return Transport.Synapse;
      if (id.StartsWith("receiver:", StringComparison.Ordinal)) return Transport.Usb;
      if (id.StartsWith("xinput:", StringComparison.Ordinal)) return Transport.XInput;
      return Transport.Bluetooth;
    }

    // Receiver slots appear once they have a reading or a problem worth showing.
    internal static bool SlotVisible(string level) {
      int percent;
      return Battery.TryPercent(level, out percent) || level == Battery.NoResponse || level == Battery.AccessDenied || level == Battery.Waiting;
    }

    internal static DeviceView Slot(int slot, string level, string observed) {
      int percent;
      bool known = Battery.TryPercent(level, out percent);
      var view = new DeviceView {
        Id = slot == 0 ? MouseId : HeadsetId,
        Slot = slot,
        Level = level ?? Battery.NoValue,
        Observed = observed ?? "",
        Transport = Transport.Usb,
        Kind = slot == 0 ? DeviceKind.Mouse : DeviceKind.Headset,
        Percent = known ? Math.Max(0, Math.Min(100, percent)) : -1,
        Charging = level != null && level.Contains(Battery.Charging)
      };
      view.Name = slot == 1 ? HeadsetName : known ? MouseName : L.T("Wireless mouse receiver");
      return view;
    }

    internal static DeviceView Discovered(DeviceReading row) {
      var view = new DeviceView {
        Id = row.Id,
        Level = row.Level,
        Observed = row.Observed,
        Transport = TransportOf(row.Id),
        Unavailable = row.Level == "-1",
        Charging = row.Level.Contains(Battery.Charging)
      };
      view.Name = view.Transport == Transport.XInput ? L.T("Controller") + " " + row.Id.Substring(7) : row.Name;
      view.Kind = DeviceArt.Kind(row.Id, row.Name);
      int percent;
      string number = row.Level.Split(' ')[0];
      if (!view.Unavailable && view.Transport != Transport.XInput &&
          int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out percent) && percent >= 0 && percent <= 100)
        view.Percent = percent;
      return view;
    }

    // All devices in a snapshot: the receiver slots first, then discovered devices in feed order.
    internal static List<DeviceView> From(PanelSnapshot snapshot, bool includeHiddenSlots = false) {
      var list = new List<DeviceView>();
      if (snapshot == null) return list;
      if (includeHiddenSlots || SlotVisible(snapshot.Mouse)) list.Add(Slot(0, snapshot.Mouse, snapshot.MouseTime));
      if (includeHiddenSlots || SlotVisible(snapshot.Headset)) list.Add(Slot(1, snapshot.Headset, snapshot.HeadsetTime));
      var seen = new HashSet<string>();
      foreach (var row in DeviceReading.Decode(snapshot.Devices ?? ""))
        if (seen.Add(row.Id)) list.Add(Discovered(row));
      return list;
    }
  }
}
