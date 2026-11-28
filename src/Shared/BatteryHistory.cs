// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Web.Script.Serialization;

namespace Volti {
  sealed class BatteryHistory {
    internal const int MaxHistoryBytes=64*1024;
    const int MaxSavedDevices=128;
    public sealed class Sample {
      [ScriptIgnore]
      public long Last {get;set;}
      [ScriptIgnore]
      public long SinceDrop {get;set;}
      [ScriptIgnore]
      public int Percent {get;set;}
      public double Minutes {get;set;}
      public double Drop {get;set;}
      public long LearnedAt {get;set;}
    }
    readonly Dictionary<string,Sample> devices;
    readonly string path;
    bool dirty;
    readonly Stopwatch sinceSave=new Stopwatch();
    internal BatteryHistory(string path){
      this.path=path;
      devices=new Dictionary<string,Sample>();
      try{
        if(File.Exists(path))devices=new JavaScriptSerializer().Deserialize<Dictionary<string,Sample>>(File.ReadAllText(path))??devices;
      }catch(IOException){}catch(UnauthorizedAccessException){}catch(ArgumentException){}catch(InvalidOperationException){}
    }
    readonly HashSet<string> present=new HashSet<string>();
    internal void FinishObservation(){
      foreach(var pair in devices){
        if(pair.Value!=null&&pair.Value.SinceDrop!=0&&!present.Contains(pair.Key)){
          pair.Value.SinceDrop=0;
        }
      }
      present.Clear();
    }
    internal string Observe(string key,string level,string observed){
      int percent;
      DateTime local;
      var token=level.Split(' ')[0].TrimEnd('%');
      if(!int.TryParse(token,out percent)||percent<0||percent>100||
         !DateTime.TryParseExact(observed,"yyyy-MM-dd HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.AssumeLocal,out local))return observed;
      present.Add(key);
      long now=local.ToUniversalTime().Ticks/TimeSpan.TicksPerMinute;
      bool charging=level.Contains("⚡");
      Sample state;
      if(!devices.TryGetValue(key,out state)||state==null){state=new Sample();devices[key]=state;}
      if(now>=state.Last&&(now!=state.Last||state.Percent!=(charging?-1:percent))){
        if(charging||state.Percent<0||now-state.Last>30||percent>state.Percent||state.SinceDrop==0)
          state.SinceDrop=now;
        else if(percent<state.Percent){
          if(now>state.SinceDrop){
            double weight=Math.Pow(0.99,state.Percent-percent);
            state.Minutes=state.Minutes*weight+(now-state.SinceDrop);
            state.Drop=state.Drop*weight+state.Percent-percent;
            state.LearnedAt=now;
            dirty=true;
          }
          state.SinceDrop=now;
        }
        state.Percent=charging?-1:percent;
        state.Last=now;
      }
      if(charging)return observed;
      int minutes=percent==0?0:state.Drop>=4&&state.Minutes>=10?(int)Math.Min(int.MaxValue,Math.Round(percent*state.Minutes/state.Drop)):-1;
      return observed+"|"+minutes.ToString(CultureInfo.InvariantCulture);
    }
    internal void Save(bool force){
      if(!dirty||(!force&&sinceSave.IsRunning&&sinceSave.Elapsed<TimeSpan.FromMinutes(5)))return;
      sinceSave.Restart();
      try{
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temp=path+".tmp";
        File.WriteAllBytes(temp,EncodeHistory());
        if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
        dirty=false;
      }catch(IOException){}catch(UnauthorizedAccessException){}
    }
    byte[] EncodeHistory(){
      var recent=devices.Where(pair=>pair.Value!=null&&pair.Value.Drop>0)
        .OrderByDescending(pair=>pair.Value.LearnedAt).Take(MaxSavedDevices).ToArray();
      var serializer=new JavaScriptSerializer();
      for(int count=recent.Length;;count/=2){
        var saved=recent.Take(count).ToDictionary(pair=>pair.Key,pair=>pair.Value);
        string json=serializer.Serialize(saved);
        if(Encoding.UTF8.GetByteCount(json)<=MaxHistoryBytes)return Encoding.UTF8.GetBytes(json);
      }
    }

    internal static string Caption(string observed,bool showObservation=true){
      string[] parts=observed.Split('|');
      int minutes;
      if(parts.Length!=2||!int.TryParse(parts[1],out minutes))return showObservation?observed:"";
      string when=showObservation&&parts[0].Length==16?parts[0].Substring(11)+"  ·  ":"";
      if(minutes<0)return showObservation?when+L.T("Learning battery life"):"";
      return when+"≈ "+(minutes>=60?(minutes/60)+" h ":"")+(minutes%60)+" min "+L.T("remaining");
    }
  }
}
