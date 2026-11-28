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
}
