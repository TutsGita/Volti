// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Volti {
  static class Win32 {
    internal static readonly IntPtr InvalidHandle = new IntPtr(-1);
    internal const uint GenericRead = 0x80000000;
    internal const uint GenericReadWrite = 0xC0000000;
    internal const int ErrorAccessDenied = 5;
    const int ErrorIoPending = 997;
    const uint WaitTimeout = 0x102;

    [StructLayout(LayoutKind.Sequential)] internal struct RawDevice { public IntPtr Handle; public uint Type; }
    [StructLayout(LayoutKind.Sequential)] internal struct Overlapped { public IntPtr Internal, InternalHigh; public uint Offset, OffsetHigh; public IntPtr Event; }

    [DllImport("user32.dll", SetLastError=true)] internal static extern uint GetRawInputDeviceList(IntPtr list, ref uint count, uint itemSize);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)] internal static extern uint GetRawInputDeviceInfo(IntPtr handle, uint command, StringBuilder data, ref uint size);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] internal static extern IntPtr CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool ReadFile(IntPtr file, IntPtr buffer, uint count, out uint read, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool WriteFile(IntPtr file, IntPtr buffer, uint count, out uint written, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool CancelIoEx(IntPtr file, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr CreateEvent(IntPtr security, bool manualReset, bool initialState, string name);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool SetEvent(IntPtr handle);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern uint WaitForMultipleObjects(uint count, IntPtr[] handles, bool waitAll, uint timeout);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool GetOverlappedResult(IntPtr file, IntPtr overlapped, out uint read, bool wait);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool DeviceIoControl(IntPtr file, uint code, IntPtr input, uint inputSize, IntPtr output, uint outputSize, out uint bytes, IntPtr overlapped);
    [DllImport("hid.dll")] internal static extern bool HidD_GetPreparsedData(IntPtr file, out IntPtr data);
    [DllImport("hid.dll")] internal static extern bool HidD_FreePreparsedData(IntPtr data);
    [DllImport("hid.dll")] internal static extern int HidP_GetCaps(IntPtr data, byte[] caps);
    [DllImport("shell32.dll", CharSet=CharSet.Unicode, ExactSpelling=true)] internal static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
    [DllImport("user32.dll", SetLastError=true)] internal static extern bool ReleaseCapture();
    [DllImport("user32.dll", CharSet=CharSet.Auto, SetLastError=true)] internal static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool DestroyIcon(IntPtr icon);

    // Moves a borderless window as if its caption had been dragged. Ignored where user32 is unavailable.
    internal static void DragWindow(IntPtr window) {
      try { ReleaseCapture(); SendMessage(window, 0x00A1, new IntPtr(2), IntPtr.Zero); }
      catch (DllNotFoundException) {} catch (EntryPointNotFoundException) {}
    }

    internal static void SetAppId(string id) {
      try { SetCurrentProcessExplicitAppUserModelID(id); }
      catch (DllNotFoundException) {} catch (EntryPointNotFoundException) {}
    }

    internal static string[] HidPaths() {
      uint itemSize = (uint)Marshal.SizeOf(typeof(RawDevice));
      uint count = 0;
      GetRawInputDeviceList(IntPtr.Zero, ref count, itemSize);
      if (count == 0 || count > 4096) return new string[0];
      IntPtr memory = Marshal.AllocHGlobal((int)(count * itemSize));
      try {
        uint actual = GetRawInputDeviceList(memory, ref count, itemSize);
        var paths = new List<string>();
        if (actual > count) return paths.ToArray();
        for (int i = 0; i < actual; i++) {
          var device = (RawDevice)Marshal.PtrToStructure(IntPtr.Add(memory, i * (int)itemSize), typeof(RawDevice));
          uint chars = 0;
          GetRawInputDeviceInfo(device.Handle, 0x20000007, null, ref chars);
          if (chars == 0 || chars > 2048) continue;
          var path = new StringBuilder((int)chars);
          GetRawInputDeviceInfo(device.Handle, 0x20000007, path, ref chars);
          paths.Add(path.ToString());
        }
        return paths.ToArray();
      } finally { Marshal.FreeHGlobal(memory); }
    }

    // Returns 1 for a completed transfer, 0 for a timeout and -1 for an I/O error or a stop request.
    internal static int ReadWithTimeout(IntPtr file, byte[] buffer, out uint read, uint timeout, IntPtr stopEvent = default(IntPtr)) {
      return TransferWithTimeout(file, buffer, out read, timeout, false, stopEvent);
    }

    // featureCode selects DeviceIoControl, for example IOCTL_HID_SET_FEATURE or IOCTL_HID_GET_FEATURE.
    internal static int TransferWithTimeout(IntPtr file, byte[] buffer, out uint read, uint timeout, bool write,
                                            IntPtr stopEvent = default(IntPtr), uint featureCode = 0) {
      read = 0;
      IntPtr ev = CreateEvent(IntPtr.Zero, true, false, null);
      if (ev == IntPtr.Zero) return -1;
      IntPtr ov = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Overlapped)));
      GCHandle pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
      try {
        var value = new Overlapped { Event = ev };
        Marshal.StructureToPtr(value, ov, false);
        IntPtr address = pinned.AddrOfPinnedObject();
        uint length = (uint)buffer.Length;
        bool done;
        if (featureCode != 0)
          done = DeviceIoControl(file, featureCode, write ? address : IntPtr.Zero, write ? length : 0,
                                 write ? IntPtr.Zero : address, write ? 0 : length, out read, ov);
        else
          done = write ? WriteFile(file, address, length, out read, ov) : ReadFile(file, address, length, out read, ov);
        if (done) return 1;
        if (Marshal.GetLastWin32Error() != ErrorIoPending) return -1;
        uint wait = stopEvent == IntPtr.Zero
          ? WaitForSingleObject(ev, timeout)
          : WaitForMultipleObjects(2, new[] { ev, stopEvent }, false, timeout);
        if (wait == 0) return GetOverlappedResult(file, ov, out read, false) ? 1 : -1;
        CancelIoEx(file, ov);
        bool completed = GetOverlappedResult(file, ov, out read, true);
        if (wait == WaitTimeout) return completed ? 1 : 0;
        return -1;
      } finally {
        pinned.Free();
        Marshal.FreeHGlobal(ov);
        CloseHandle(ev);
      }
    }

    // HIDP_CAPS layout: Usage, UsagePage, then input, output and feature report byte lengths.
    internal struct HidCaps {
      internal int Usage, UsagePage, InputLength, OutputLength, FeatureLength;
    }

    internal static bool TryGetCaps(IntPtr handle, out HidCaps caps) {
      caps = new HidCaps();
      IntPtr data;
      if (!HidD_GetPreparsedData(handle, out data)) return false;
      try {
        var raw = new byte[64];
        if (HidP_GetCaps(data, raw) < 0) return false;
        caps.Usage = raw[0] | (raw[1] << 8);
        caps.UsagePage = raw[2] | (raw[3] << 8);
        caps.InputLength = raw[4] | (raw[5] << 8);
        caps.OutputLength = raw[6] | (raw[7] << 8);
        caps.FeatureLength = raw[8] | (raw[9] << 8);
        return true;
      } finally { HidD_FreePreparsedData(data); }
    }
  }
}
