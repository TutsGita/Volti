// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Volti {
  // Checks the latest stable GitHub release, verifies the setup against SHA256SUMS.txt and runs it.
  static class Updater {
    const string LatestRelease = "https://api.github.com/repos/" + AppInfo.Repository + "/releases/latest";
    const string Downloads = AppInfo.RepositoryUrl + "/releases/download/";

    internal sealed class Release {
      internal Version Version;
      internal string Tag, SetupName, SetupUrl, Notes;
    }

    // Parses the GitHub API reply. Returns null unless it describes a newer release with a setup asset.
    internal static Release Parse(string json, Version current) {
      var release = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
      object tagValue;
      string tag = release != null && release.TryGetValue("tag_name", out tagValue) ? tagValue as string : null;
      if (tag == null || !Regex.IsMatch(tag, @"^v\d+\.\d+\.\d+$")) return null;
      var latest = new Version(tag.Substring(1));
      if (latest <= new Version(current.Major, current.Minor, Math.Max(0, current.Build))) return null;
      string setup = "Volti-" + latest + "-Setup.exe";
      string url = Downloads + tag + "/" + setup;
      object assets;
      // Arrays arrive as object[] on .NET Framework and as ArrayList on Mono.
      var list = release.TryGetValue("assets", out assets) ? assets as System.Collections.IEnumerable : null;
      if (list == null || assets is string) return null;
      bool listed = list.OfType<Dictionary<string, object>>()
        .Any(a => a.ContainsKey("browser_download_url") && Equals(a["browser_download_url"], url));
      if (!listed) return null;
      object body;
      release.TryGetValue("body", out body);
      return new Release { Version = latest, Tag = tag, SetupName = setup, SetupUrl = url, Notes = Highlights(body as string) };
    }

    // Up to four bullet points from the release notes, without Markdown links or emphasis.
    internal static string Highlights(string body) {
      string[] lines = (body ?? "").Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0 && !x.StartsWith("#")).ToArray();
      string[] bullets = lines.Where(x => Regex.IsMatch(x, @"^[-*+]\s+")).ToArray();
      var picked = (bullets.Length > 0 ? bullets : lines)
        .Select(x => "• " + Regex.Replace(Regex.Replace(x, @"^[-*+]\s+", ""), @"\[([^\]]+)\]\([^)]+\)", "$1").Replace("**", "").Trim())
        .Take(4)
        .Select(x => x.Length <= 160 ? x : x.Substring(0, 157) + "…");
      return string.Join(Environment.NewLine, picked);
    }

    internal static string ExpectedHash(string sums, string file) {
      var match = Regex.Match(sums, @"(?im)^([0-9a-f]{64})[ \t]+\*?" + Regex.Escape(file) + @"\s*$");
      return match.Success ? match.Groups[1].Value : null;
    }

    // Runs at most once at a time across both processes. onLaunch runs after the setup has started.
    // When manual is true the user also hears about "no update" and network failures.
    internal static async void Check(IWin32Window owner, bool manual, Func<bool> stopping, Action onLaunch) {
      using (var gate = new Semaphore(1, 1, "Local\\Volti.UpdatePrompt")) {
        if (!gate.WaitOne(0)) return;
        bool accepted = false;
        string package = null;
        try {
          ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
          using (var client = new WebClient()) {
            client.Headers["User-Agent"] = "Volti-Updater";
            var current = typeof(Updater).Assembly.GetName().Version;
            var release = Parse(await client.DownloadStringTaskAsync(LatestRelease), current);
            if (stopping()) return;
            if (release == null) {
              if (manual) MessageBox.Show(owner, L.T("Volti is up to date."), AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
              return;
            }
            string prompt = "Volti " + current.ToString(3) + " → " + release.Version + "\n\n" +
              (release.Notes.Length > 0 ? release.Notes + "\n\n" : "") + L.T("Download and install this update?");
            if (MessageBox.Show(owner, prompt, AppInfo.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            accepted = true;
            string expected = ExpectedHash(await client.DownloadStringTaskAsync(Downloads + release.Tag + "/SHA256SUMS.txt"), release.SetupName);
            if (expected == null) throw new InvalidDataException(L.T("Update verification failed"));
            package = Path.Combine(Path.GetTempPath(), "Volti-" + Guid.NewGuid().ToString("N") + ".exe");
            await client.DownloadFileTaskAsync(new Uri(release.SetupUrl), package);
            using (var stream = File.OpenRead(package))
            using (var sha = SHA256.Create()) {
              string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
              if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException(L.T("Update verification failed"));
            }
            if (stopping()) return;
            string args = "/SILENT /CLOSEAPPLICATIONS /NORESTART /SUPPRESSMSGBOXES /VOLTIUPDATE /DIR=\"" + Application.StartupPath + "\"";
            if (!Settings.InstalledHere()) args += " /PORTABLEUPDATE";
            using (Process.Start(new ProcessStartInfo(package, args) { UseShellExecute = false })) {}
            package = null; // The setup owns the file now.
            onLaunch();
          }
        } catch (Exception ex) {
          if ((accepted || manual) && !stopping())
            MessageBox.Show(owner, ex.Message, L.T("Update failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        } finally {
          if (package != null) {
            try { File.Delete(package); } catch (IOException) {} catch (UnauthorizedAccessException) {}
          }
          gate.Release();
        }
      }
    }
  }
}
