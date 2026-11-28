// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Threading;

namespace Volti {
  // Queries HyperX receivers on the thread pool. All members run on the UI thread.
  sealed class HyperXScanner {
    const string OriginalStingerId = "hyperx-stinger-0951-16da";
    readonly Action<Action> post;
    readonly Action<string, string, int> setReceiver;
    readonly HashSet<string> receivers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> querying = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> removedDuringQuery = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    internal HyperXScanner(Action<Action> post, Action<string, string, int> setReceiver) {
      this.post = post;
      this.setReceiver = setReceiver;
    }

    internal bool Busy { get { return querying.Count > 0; } }

    internal void Scan(string[] paths) {
      var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (string path in paths) {
        if (HyperXBattery.IsOriginalStinger(path)) {
          // Detected, but its battery protocol is still unknown: show the card with a dash.
          if (present.Add(OriginalStingerId)) setReceiver(OriginalStingerId, "HyperX Cloud Stinger Wireless (PS4)", -1);
          continue;
        }
        string name = HyperXBattery.DeviceName(path);
        if (name == null || !present.Add(path) || !querying.Add(path)) continue;
        string devicePath = path;
        ThreadPool.QueueUserWorkItem(_ => {
          int percent = HyperXBattery.Query(devicePath);
          post(() => {
            querying.Remove(devicePath);
            if (!removedDuringQuery.Remove(devicePath) && receivers.Contains(devicePath))
              setReceiver(devicePath, percent < 0 ? null : name, percent);
          });
        });
      }
      foreach (string path in receivers) {
        if (present.Contains(path)) continue;
        if (querying.Contains(path)) removedDuringQuery.Add(path);
        setReceiver(path, null, 0);
      }
      receivers.Clear();
      receivers.UnionWith(present);
    }
  }
}
