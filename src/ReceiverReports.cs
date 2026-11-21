// SPDX-License-Identifier: AGPL-3.0-only
// Protocols adapted for Volti on 2026-11-20. See THIRD_PARTY_NOTICES.md.
using System;
using System.Collections.Generic;
using System.Text;

namespace Volti {
  static class ReceiverReports {
    internal static bool TrySharedMouse(byte[] report, uint length, out string name, out int percent) {
      name = null;
      percent = 0;
      if(length != 5 || report[0] != 0x03 || report[2] != 0x40) return false;
      switch(report[1]) {
        case 0x10:
          name = "Attack Shark R1";
          percent = report[4] * 10;
          return percent <= 100;
        case 0x85: name = "Attack Shark X6"; break;
        case 0x55: name = "Attack Shark X11"; break;
        case 0x07: name = "Attack Shark X11 SE"; break;
        case 0x20: name = "DELUX M600 Pro"; break;
        default: return false;
      }
      percent = report[1] == 0x85 ? report[4] * 10 : report[4];
      return report[3] == 0x01 && percent <= 100;
    }

    internal static int Rapoo(byte[] report, uint length) {
      return length == 7 && report[0] == 0xBB && report[1] == 0xB0 &&
        report[3] == 0x40 && report[6] <= 100 ? report[6] : -1;
    }

    internal static int Quantum910(byte[] report, uint length) {
      return length >= 2 && report[0] == 0x08 && report[1] <= 100 ? report[1] : -1;
    }
  }

  sealed partial class MainForm {
    readonly SortedDictionary<string,string> receiverRows = new SortedDictionary<string,string>();
    IntPtr readerStopEvent;

    void SetReceiverBattery(string path, string name, int percent) {
      string id = "receiver:" + path.ToUpperInvariant();
      Post(() => {
        if(name == null) {
          if(!receiverRows.Remove(id)) return;
        } else {
          string row = Convert.ToBase64String(Encoding.UTF8.GetBytes(id)) + "\t" +
            Convert.ToBase64String(Encoding.UTF8.GetBytes(name)) + "\t" + percent;
          string previous;
          if(receiverRows.TryGetValue(id, out previous) && previous == row) return;
          receiverRows[id] = row;
        }
        RefreshDiscoveredTray();
        Publish();
      });
    }

    string DiscoveredSnapshot() {
      var feeds=new List<string>();
      if(bluetoothSnapshot.Length>0)feeds.Add(bluetoothSnapshot);
      if(receiverRows.Count>0)feeds.AddRange(receiverRows.Values);
      if(synapseSnapshot.Length>0)feeds.Add(synapseSnapshot);
      return string.Join("\n",feeds);
    }

    void ClosePassiveReader(IntPtr handle) {
      lock(handles) {
        if(handles.Remove(handle)) Win32.CloseHandle(handle);
      }
    }

    static bool TryInputCaps(IntPtr handle, out int page, out int usage, out int length) {
      page = usage = length = 0;
      IntPtr data;
      if(!Win32.HidD_GetPreparsedData(handle, out data)) return false;
      try {
        var caps = new byte[64];
        if(Win32.HidP_GetCaps(data, caps) < 0) return false;
        usage = caps[0] | caps[1] << 8;
        page = caps[2] | caps[3] << 8;
        length = caps[4] | caps[5] << 8;
        return length >= 2 && length <= 512;
      } finally { Win32.HidD_FreePreparsedData(data); }
    }

    void ReadPassiveReceiver(string path, string name, int expectedPage, int expectedUsage,
                             int expectedLength, Func<byte[],uint,int> parse) {
      IntPtr handle = Open(path, true, 0x80000000);
      if(handle == new IntPtr(-1)) return;
      try {
        int page, usage, length;
        if(!TryInputCaps(handle, out page, out usage, out length) || page != expectedPage ||
           usage != expectedUsage || (expectedLength != 0 && length != expectedLength)) return;
        var report = new byte[length];
        int lastPercent = -1;
        while(!stopping) {
          uint read;
          int result = Win32.ReadWithTimeout(handle, report, out read, 1000, readerStopEvent);
          if(result < 0) break;
          if(result == 0) continue;
          int percent = parse(report, read);
          if(percent < 0 || percent == lastPercent) continue;
          lastPercent = percent;
          SetReceiverBattery(path, name, percent);
        }
      } finally {
        ClosePassiveReader(handle);
        SetReceiverBattery(path, null, 0);
      }
    }
  }
}
