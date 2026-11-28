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

    sealed class CachedTail {
      internal long Length;
      internal DateTime Created, Written;
      internal byte[] Bytes;
      internal string Text;
    }
    static readonly Dictionary<string,CachedTail> tails=new Dictionary<string,CachedTail>(StringComparer.OrdinalIgnoreCase);

    internal static string Tail(string path) {
      var file=new FileInfo(path);
      if(!file.Exists||file.LastWriteTimeUtc<DateTime.UtcNow.AddMinutes(-15)){lock(tails)tails.Remove(path);return "";}
      CachedTail prior;
      lock(tails)tails.TryGetValue(path,out prior);
      using(var stream=file.Open(FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){
        long length=stream.Length, start=Math.Max(0,length-MaxLogBytes);
        bool same=prior!=null&&prior.Created==file.CreationTimeUtc;
        if(same&&length==prior.Length&&file.LastWriteTimeUtc==prior.Written)return prior.Text;
        var bytes=new byte[(int)(length-start)];
        int copied=0;
        if(same&&length>prior.Length&&start<prior.Length){
          copied=(int)(prior.Length-start);
          Buffer.BlockCopy(prior.Bytes,prior.Bytes.Length-copied,bytes,0,copied);
        }
        stream.Position=start+copied;
        using(var reader=new BinaryReader(stream)){
          while(copied<bytes.Length){
            int count=reader.Read(bytes,copied,bytes.Length-copied);
            if(count==0){lock(tails)tails.Remove(path);return "";}
            copied+=count;
          }
        }
        int first=start==0?0:Array.IndexOf(bytes,(byte)10)+1;
        if(start!=0&&first==0)first=bytes.Length;
        string text=Encoding.UTF8.GetString(bytes,first,bytes.Length-first).TrimStart('﻿');
        lock(tails){
          if(prior==null&&tails.Count>=8)tails.Clear();
          tails[path]=new CachedTail{Bytes=bytes,Text=text,Length=length,Created=file.CreationTimeUtc,Written=file.LastWriteTimeUtc};
        }
        return text;
      }
    }

    static string Field(Dictionary<string,object> data, string key) {
      object value;
      return data.TryGetValue(key,out value) && value != null ? Convert.ToString(value,CultureInfo.InvariantCulture) : "";
    }

    static bool Recent(string stamp, DateTime now,out DateTime time) {
      return DateTime.TryParse(stamp,CultureInfo.InvariantCulture,DateTimeStyles.AssumeLocal,out time) &&
        time.ToUniversalTime() >= now.AddMinutes(-15) && time.ToUniversalTime() <= now.AddMinutes(1);
    }

    static string Row(string id, string name, int percent, bool charging,DateTime observed) {
      string level=percent.ToString(CultureInfo.InvariantCulture)+(charging?"  ⚡":"");
      return new DeviceReading(id,name.Trim(),level,Battery.MinuteStamp(observed)).Encode();
    }

    internal static string ParseV4(string log, DateTime now) {
      const string marker = "connectingDeviceData: ";
      foreach(string line in log.Split('\n').Reverse()) {
        int start = line.IndexOf(marker,StringComparison.Ordinal);
        if(start < 0)continue;
        int bracket = line.IndexOf(']');
        DateTime observed;
        if(!line.StartsWith("[") || bracket<=1 || !Recent(line.Substring(1,bracket-1),now,out observed))continue;
        object json;
        try {json = new JavaScriptSerializer {MaxJsonLength=MaxLogBytes, RecursionLimit=24}.DeserializeObject(line.Substring(start+marker.Length));}
        catch(ArgumentException) {continue;}
        catch(InvalidOperationException) {continue;}
        if(!(json is object[]) && !(json is Dictionary<string,object>))continue;
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
          rows[id] = Row("synapse4:"+id,name,(int)Math.Round(percent,MidpointRounding.AwayFromZero),status=="charging"||status=="charge",observed);
        }
        return string.Join("\n",rows.Values);
      }
      return null;
    }

    internal static string ParseV3(string log, DateTime now) {
      var rows = new SortedDictionary<string,string>(StringComparer.Ordinal);
      foreach(Match entry in V3Event.Matches(log)) {
        DateTime observed;
        if(!Recent(entry.Groups["stamp"].Value,now,out observed))continue;
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
            int.TryParse(level.Groups[2].Value,out state) && state!=0,observed);
      }
      return string.Join("\n",rows.Values);
    }

    internal static string Scan() {
      return Scan(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Razer"));
    }

    internal static string Scan(string root) {
      var now = DateTime.UtcNow;
      try {
        string folder = Path.Combine(root,"RazerAppEngine","User Data","Logs");
        if(Directory.Exists(folder)) {
          foreach(var log in new DirectoryInfo(folder).EnumerateFiles("systray_systrayv*.log")
            .Where(file=>V4File.IsMatch(file.Name)).OrderByDescending(file=>file.LastWriteTimeUtc).Take(4)) {
            string snapshot;
            try{snapshot=ParseV4(Tail(log.FullName),now);}
            catch(IOException){continue;}
            catch(UnauthorizedAccessException){continue;}
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
