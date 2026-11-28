// SPDX-License-Identifier: AGPL-3.0-only
using System;

namespace Volti {
  enum AlertLevel { None = 0, Low = 1, Critical = 2 }

  // Low and critical thresholds for the two receiver slots. A level fires when the battery enters
  // its range and rearms once the battery rises above it.
  sealed class AlertTracker {
    readonly int[] low = new int[2], critical = new int[2];
    readonly AlertLevel[] states = new AlertLevel[2];

    internal void Load() {
      for (int slot = 0; slot < 2; slot++) Settings.LoadLimits(slot, out low[slot], out critical[slot]);
    }

    internal void SetLimits(int slot, int lowLimit, int criticalLimit) {
      low[slot] = lowLimit;
      critical[slot] = criticalLimit;
    }

    // Returns the level to announce, or None when nothing new happened.
    internal AlertLevel Check(int slot, string value) {
      int percent;
      if (!Battery.TryPercent(value, out percent)) return AlertLevel.None;
      AlertLevel state = percent <= critical[slot] ? AlertLevel.Critical : percent <= low[slot] ? AlertLevel.Low : AlertLevel.None;
      AlertLevel previous = states[slot];
      states[slot] = state;
      return state != AlertLevel.None && state > previous ? state : AlertLevel.None;
    }
  }
}
