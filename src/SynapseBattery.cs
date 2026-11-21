// SPDX-License-Identifier: AGPL-3.0-only
// Log formats documented by LeonardoVidolin/razer-battery-widget.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace Volti {
  static class SynapseBattery {
    const int MaxLogBytes = 512 * 1024;
    static readonly Regex V3Event = new Regex(
      @"^(?<stamp>[^\r\n]+?) INFO[^\r\n]*?_(?<kind>OnBatteryLevelChanged|OnDeviceRemoved|OnDeviceLoaded)\b(?<body>.*?)(?=^[^\r\n]+? (?:TRACE|DEBUG|INFO|WARN|ERROR|FATAL)\b|\z)",
      RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    static readonly Regex Handle = new Regex(@"Handle:\s*(\d+)", RegexOptions.IgnoreCase);
    static readonly Regex Name = new Regex(@"Name:[ \t]*([^\r\n]+?)(?=\s+Handle:|\s+level\s+\d|\r?$)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    static readonly Regex Level = new Regex(@"\blevel\s+(\d+)(?:\s+state\s+(\d+))?", RegexOptions.IgnoreCase);
    static readonly Regex V4File = new Regex(@"^systray_systrayv\d+\.log$", RegexOptions.IgnoreCase);

    static string Tail(string path) {
      if(File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddMinutes(-15))return "";
      using(var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) {
        stream.Position = Math.Max(0, stream.Length - MaxLogBytes);
        using(var reader = new StreamReader(stream, Encoding.UTF8)) {
          if(stream.Position > 0)reader.ReadLine();
          return reader.ReadToEnd();
        }
      }
    }

    static string Field(Dictionary<string,object> data, string key) {
      object value;
      return data.TryGetValue(key,out value) && value != null ? Convert.ToString(value,CultureInfo.InvariantCulture) : "";
    }

    static bool Recent(string stamp, DateTime now) {
      DateTime time;
      return !DateTime.TryParse(stamp,CultureInfo.InvariantCulture,DateTimeStyles.AssumeLocal,out time) ||
        time.ToUniversalTime() >= now.AddMinutes(-15);
    }

    static string Row(string id, string name, int percent, bool charging) {
      return Convert.ToBase64String(Encoding.UTF8.GetBytes(id)) + "\t" +
        Convert.ToBase64String(Encoding.UTF8.GetBytes(name.Trim())) + "\t" +
        percent.ToString(CultureInfo.InvariantCulture) + (charging ? "  ⚡" : "");
    }

    internal static string ParseV4(string log, DateTime now) {
      const string marker = "connectingDeviceData: ";
      foreach(string line in log.Split('\n').Reverse()) {
        int start = line.IndexOf(marker,StringComparison.Ordinal);
        if(start < 0)continue;
        int bracket = line.IndexOf(']');
        if(line.StartsWith("[") && bracket > 0 && !Recent(line.Substring(1,bracket-1),now))return "";
        object json;
        try {json = new JavaScriptSerializer {MaxJsonLength=MaxLogBytes, RecursionLimit=24}.DeserializeObject(line.Substring(start+marker.Length));}
        catch(ArgumentException) {continue;}
        catch(InvalidOperationException) {continue;}
        var entries = json as object[] ?? new[] {json};
        var rows = new SortedDictionary<string,string>(StringComparer.Ordinal);
        foreach(object entry in entries) {
          var device = entry as Dictionary<string,object>;
          if(device == null || string.Equals(Field(device,"isConnected"),"False",StringComparison.OrdinalIgnoreCase))continue;
          object powerObject;
          if(!device.TryGetValue("powerStatus",out powerObject))continue;
          var power = powerObject as Dictionary<string,object>;
          if(power == null)continue;
          double percent = -1;
          foreach(string key in new[] {"level","batteryLevel","percent","percentage"}) {
            if(double.TryParse(Field(power,key).Trim().TrimEnd('%'),NumberStyles.Float,CultureInfo.InvariantCulture,out percent))break;
            percent = -1;
          }
          if(!(percent >= 0 && percent <= 100))continue;
          string name = Field(device,"name");
          object names;
          if(device.TryGetValue("name",out names) && names is Dictionary<string,object>) {
            var localized = (Dictionary<string,object>)names;
            name = new[] {Field(localized,"en"),Field(localized,"en_US")}
              .Concat(localized.Values.OfType<string>()).FirstOrDefault(x=>!string.IsNullOrWhiteSpace(x));
          }
          if(string.IsNullOrWhiteSpace(name))name = "Razer";
          string id = Field(device,"serialNumber");
          if(id.Length == 0)id = Field(device,"deviceContainerId");
          if(id.Length == 0)id = name;
          string status = Field(power,"chargingStatus").Trim().ToLowerInvariant();
          rows[id] = Row("synapse4:"+id,name,(int)Math.Round(percent,MidpointRounding.AwayFromZero),status=="charging"||status=="charge");
        }
        return string.Join("\n",rows.Values);
      }
      return null;
    }

    internal static string ParseV3(string log, DateTime now) {
      var rows = new SortedDictionary<string,string>(StringComparer.Ordinal);
      foreach(Match entry in V3Event.Matches(log)) {
        if(!Recent(entry.Groups["stamp"].Value,now))continue;
        string body = entry.Groups["body"].Value;
        var handle = Handle.Match(body);
        if(!handle.Success)continue;
        string id = handle.Groups[1].Value;
        if(!entry.Groups["kind"].Value.Equals("OnBatteryLevelChanged",StringComparison.OrdinalIgnoreCase)) {
          rows.Remove(id);
          continue;
        }
        var name = Name.Match(body);
        var level = Level.Match(body);
        int percent, state;
        if(name.Success && level.Success && int.TryParse(level.Groups[1].Value,out percent) && percent<=100)
          rows[id] = Row("synapse3:"+name.Groups[1].Value.Trim()+":"+id,name.Groups[1].Value,percent,
            int.TryParse(level.Groups[2].Value,out state) && state!=0);
      }
      return string.Join("\n",rows.Values);
    }

    internal static string Scan() {
      var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Razer");
      var now = DateTime.UtcNow;
      try {
        string folder = Path.Combine(root,"RazerAppEngine","User Data","Logs");
        if(Directory.Exists(folder)) {
          foreach(var log in new DirectoryInfo(folder).EnumerateFiles("systray_systrayv*.log")
            .Where(file=>V4File.IsMatch(file.Name)).OrderByDescending(file=>file.LastWriteTimeUtc).Take(4)) {
            string snapshot = ParseV4(Tail(log.FullName),now);
            if(snapshot != null)return snapshot;
          }
        }
        return ParseV3(Tail(Path.Combine(root,"Synapse3","Log","Razer Synapse 3.log")),now);
      } catch(IOException) {return "";}
        catch(UnauthorizedAccessException) {return "";}
        catch(RegexMatchTimeoutException) {return "";}
    }
  }
}
