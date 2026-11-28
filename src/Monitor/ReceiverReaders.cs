// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Volti {
  // Receives readings from background reader threads. Implementations marshal to the UI thread.
  interface IReadingSink {
    void SetSlot(int slot, string value);
    void SetReceiver(string path, string name, int percent);
    void ReceiverPaths(string[] paths);
    void Note(string status);
  }

  // Finds supported USB receivers and keeps one reader thread per HID interface.
  sealed class ReceiverReaders : IDisposable {
    const string DeluxMouse = "VID_1D57&PID_FA60&MI_02&COL03";
    const string SharedMouse = "VID_1D57&PID_FA60";
    const string JblQuantum650 = "VID_0ECB&PID_2125&MI_05&COL04";
    const string Rapoo = "VID_24AE&PID_1871";
    const string Quantum910 = "VID_0ECB&PID_2088";
    const uint SetFeatureIoctl = 0xB0191;

    readonly IReadingSink sink;
    readonly List<IntPtr> handles = new List<IntPtr>();
    readonly List<Thread> threads = new List<Thread>();
    readonly HashSet<string> active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    readonly IntPtr stopEvent;
    volatile bool stopping;
    int discovering;

    internal ReceiverReaders(IReadingSink sink) {
      this.sink = sink;
      stopEvent = Win32.CreateEvent(IntPtr.Zero, true, false, null);
    }

    internal bool Discovering { get { return Interlocked.CompareExchange(ref discovering, 0, 0) != 0; } }

    // Starts a discovery pass unless one is already running. Returns false when it was skipped.
    internal bool Discover() {
      if (Interlocked.CompareExchange(ref discovering, 1, 0) != 0) return false;
      var thread = new Thread(() => {
        try { Connect(); } finally { Interlocked.Exchange(ref discovering, 0); }
      }) { IsBackground = true, Name = "Receiver discovery" };
      thread.Start();
      return true;
    }

    static bool Has(string path, string id) { return path.IndexOf(id, StringComparison.OrdinalIgnoreCase) >= 0; }
    static bool HasDevice(string path, string vidPid) { return Has(path, vidPid + "&") || Has(path, vidPid + "#"); }

    void Connect() {
      string[] paths = HyperXBattery.Paths();
      sink.ReceiverPaths(paths);
      sink.Note("Ready");

      // Mouse collections that share a Windows container with the DELUX interface belong to that receiver.
      var deluxContainers = new HashSet<Guid>();
      foreach (string path in paths) {
        Guid container;
        if (Has(path, DeluxMouse) && ReceiverContainer.TryGet(path, out container)) deluxContainers.Add(container);
      }
      if (!Array.Exists(paths, p => Has(p, JblQuantum650))) sink.SetSlot(1, Battery.Disconnected);
      if (!Array.Exists(paths, p => Has(p, DeluxMouse))) sink.SetSlot(0, Battery.Disconnected);

      foreach (string item in paths) {
        if (stopping) break;
        string path = item;
        bool delux = Has(path, DeluxMouse);
        if (HasDevice(path, Rapoo))
          StartReader(path, () => ReadPassive(path, "Rapoo MT760 Mini", 0xFF00, 0x0002, 7, ReceiverReports.Rapoo));
        if (HasDevice(path, Quantum910))
          StartReader(path, () => ReadPassive(path, "JBL Quantum 910 Wireless", 0xFF13, 0x0001, 0, ReceiverReports.Quantum910));
        if (delux)
          StartReader(path, () => ReadSharedMouse(path, true));
        Guid container;
        if (!delux && HasDevice(path, SharedMouse) && (!ReceiverContainer.TryGet(path, out container) || !deluxContainers.Contains(container)))
          StartReader(path, () => ReadSharedMouse(path, false));
        if (Has(path, JblQuantum650))
          StartReader(path, () => ReadJbl(path));
      }
    }

    void StartReader(string path, ThreadStart action) {
      lock (handles) {
        if (stopping || !active.Add(path)) return;
        var thread = new Thread(() => {
          try { action(); }
          finally { lock (handles) { active.Remove(path); threads.Remove(Thread.CurrentThread); } }
        }) { IsBackground = true, Name = "Volti HID reader" };
        threads.Add(thread);
        thread.Start();
      }
    }

    IntPtr Open(string path, uint access) {
      lock (handles) {
        if (stopping) return Win32.InvalidHandle;
        IntPtr h = Win32.CreateFile(path, access, 3, IntPtr.Zero, 3, 0x40000000u, IntPtr.Zero);
        if (h != Win32.InvalidHandle) handles.Add(h);
        return h;
      }
    }

    void Close(IntPtr handle) {
      lock (handles) { if (handles.Remove(handle)) Win32.CloseHandle(handle); }
    }

    static string OpenFailure() {
      return Marshal.GetLastWin32Error() == Win32.ErrorAccessDenied ? Battery.AccessDenied : Battery.Disconnected;
    }

    // Receivers that only emit reports when the device or vendor software decides to.
    void ReadPassive(string path, string name, int page, int usage, int length, Func<byte[], uint, int> parse) {
      IntPtr handle = Open(path, Win32.GenericRead);
      if (handle == Win32.InvalidHandle) return;
      try {
        Win32.HidCaps caps;
        if (!Win32.TryGetCaps(handle, out caps) || caps.InputLength < 2 || caps.InputLength > 512 ||
            caps.UsagePage != page || caps.Usage != usage || (length != 0 && caps.InputLength != length)) return;
        var report = new byte[caps.InputLength];
        int lastPercent = -1;
        string lastStamp = null;
        while (!stopping) {
          uint read;
          int result = Win32.ReadWithTimeout(handle, report, out read, uint.MaxValue, stopEvent);
          if (result < 0) break;
          if (result == 0) continue;
          int percent = parse(report, read);
          string stamp = Battery.MinuteStamp(DateTime.UtcNow);
          if (percent < 0 || (percent == lastPercent && stamp == lastStamp)) continue;
          lastStamp = stamp;
          lastPercent = percent;
          sink.SetReceiver(path, name, percent);
        }
      } finally {
        Close(handle);
        sink.SetReceiver(path, null, 0);
      }
    }

    // The DELUX receiver reports its own mouse in slot 0. Generic Beken receivers with the same IDs can
    // carry other mouse models, which become discovered devices instead.
    void ReadSharedMouse(string path, bool legacy) {
      IntPtr handle = Open(path, Win32.GenericRead);
      if (handle == Win32.InvalidHandle) { if (legacy) sink.SetSlot(0, OpenFailure()); return; }
      if (legacy) sink.SetSlot(0, Battery.Waiting);
      var report = new byte[5];
      string lastModel = null, publishedModel = null, publishedStamp = null;
      int publishedPercent = -1;
      try {
        Win32.HidCaps caps;
        if (!legacy && (!Win32.TryGetCaps(handle, out caps) || caps.UsagePage != 0x000A || caps.InputLength != 5)) return;
        while (!stopping) {
          uint read;
          int result = Win32.ReadWithTimeout(handle, report, out read, uint.MaxValue, stopEvent);
          if (result < 0) break;
          if (result == 0) continue;
          // M800 Mini: 03 29 40 01 NN, with NN already in percent.
          if (legacy && read == 5 && report[0] == 0x03 && report[1] == 0x29 && report[2] == 0x40 && report[3] == 0x01 && report[4] <= 100) {
            lastModel = null;
            if (publishedModel != null) { sink.SetReceiver(path, null, 0); publishedModel = null; }
            sink.SetSlot(0, report[4] + "%");
            continue;
          }
          string model;
          int percent;
          if (!ReceiverReports.TrySharedMouse(report, read, out model, out percent)) continue;
          // Require the same model twice in a row before trusting the model byte.
          string stamp = Battery.MinuteStamp(DateTime.UtcNow);
          if (lastModel == model && (publishedModel != model || publishedPercent != percent || publishedStamp != stamp)) {
            publishedStamp = stamp;
            if (legacy) sink.SetSlot(0, Battery.OtherModel);
            sink.SetReceiver(path, model, percent);
            publishedModel = model;
            publishedPercent = percent;
          }
          lastModel = model;
        }
      } finally {
        Close(handle);
        if (!stopping) {
          sink.SetReceiver(path, null, 0);
          if (legacy) sink.SetSlot(0, Battery.Disconnected);
        }
      }
    }

    // JBL Quantum 650: QuantumENGINE HID v3 GET of battery FeatureID 000D, once per minute.
    void ReadJbl(string path) {
      IntPtr handle = Open(path, Win32.GenericReadWrite);
      if (handle == Win32.InvalidHandle) { sink.SetSlot(1, OpenFailure()); return; }
      sink.SetSlot(1, Battery.Waiting);
      int featureLength = 64, inputLength = 64;
      Win32.HidCaps caps;
      if (Win32.TryGetCaps(handle, out caps)) {
        if (caps.InputLength >= 9 && caps.InputLength <= 512) inputLength = caps.InputLength;
        if (caps.FeatureLength >= 11 && caps.FeatureLength <= 512) featureLength = caps.FeatureLength;
      }
      var input = new byte[inputLength];
      bool gotBattery = false;
      DateTime nextQuery = DateTime.MinValue;
      while (!stopping) {
        DateTime now = DateTime.UtcNow;
        if (now >= nextQuery) {
          var request = new byte[featureLength];
          byte[] header = { 0x88, 0x00, 0xDD, 0x01, 0x00, 0x01, 0x00, 0x02, 0x00, 0x0D, 0x00 };
          Array.Copy(header, request, header.Length);
          nextQuery = now.AddSeconds(60);
          gotBattery = false;
          uint written;
          if (Win32.TransferWithTimeout(handle, request, out written, 400, true, stopEvent, SetFeatureIoctl) != 1 && !stopping)
            sink.SetSlot(1, Battery.NoResponse);
        }
        uint timeout = (uint)Math.Max(1, Math.Min(60000, (nextQuery - DateTime.UtcNow).TotalMilliseconds));
        uint n;
        int read = Win32.ReadWithTimeout(handle, input, out n, timeout, stopEvent);
        if (read < 0) { if (!stopping && !gotBattery) sink.SetSlot(1, Battery.NoResponse); break; }
        if (read == 0) { if (!gotBattery) sink.SetSlot(1, Battery.NoResponse); continue; }
        int percent;
        bool charging;
        if (TryJblBattery(input, n, out percent, out charging)) {
          gotBattery = true;
          sink.SetSlot(1, percent + "%" + (charging ? "  " + Battery.Charging : ""));
        }
      }
      Close(handle);
      if (!stopping) sink.SetSlot(1, Battery.Disconnected);
    }

    // A GET reply carries status (2 bytes), FeatureID (2), value length (2), then the value.
    internal static bool TryJblBattery(byte[] input, uint n, out int percent, out bool charging) {
      percent = 0; charging = false;
      if (n < 10 || input.Length < 16) return false;
      int route = input[1] | (input[2] << 8), command = input[3] | (input[4] << 8), payload = input[7] | (input[8] << 8);
      if (input[0] != 0x86 || route != 0xDD00 || command != 0x0001 || payload < 7 || n < 9 + payload) return false;
      int status = input[9] | (input[10] << 8), feature = input[11] | (input[12] << 8), valueLength = input[13] | (input[14] << 8);
      if ((status & 0x8000) != 0 || feature != 0x000D || valueLength != 1) return false;
      percent = input[15] & 0x7F;
      charging = (input[15] & 0x80) != 0;
      return percent <= 100;
    }

    public void Dispose() {
      stopping = true;
      if (stopEvent != IntPtr.Zero) Win32.SetEvent(stopEvent);
      Thread[] pending;
      lock (handles) {
        foreach (IntPtr h in handles) Win32.CancelIoEx(h, IntPtr.Zero);
        pending = threads.ToArray();
      }
      bool completed = true;
      foreach (Thread thread in pending) if (!thread.Join(2000)) completed = false;
      // Handles still owned by a thread that did not finish are left to process exit.
      if (!completed) return;
      lock (handles) {
        foreach (IntPtr h in handles) Win32.CloseHandle(h);
        handles.Clear();
        if (stopEvent != IntPtr.Zero) Win32.CloseHandle(stopEvent);
      }
    }
  }
}
