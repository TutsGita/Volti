// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace Volti {
  sealed class PanelSnapshot {
    internal string Mouse, Headset, Status, Devices, ProcessId, MouseTime, HeadsetTime;
    internal static PanelSnapshot Offline(){
      return new PanelSnapshot{Mouse="—",Headset="—",Status="Monitor offline",Devices="",MouseTime="",HeadsetTime=""};
    }
  }

  sealed class PanelLink : IDisposable {
    const string DefaultPrefix="Local\\Volti.03.";
    readonly MemoryMappedFile map;
    readonly Mutex access;
    readonly string processId;
    internal readonly EventWaitHandle Activate, Preferences, Preview, Detect, Updated, UpdateExit, ScanDone;

    readonly string prefix;
    internal PanelLink(string prefix=DefaultPrefix){
      this.prefix=prefix;
      using(var process=System.Diagnostics.Process.GetCurrentProcess())processId=process.Id.ToString();
      map=MemoryMappedFile.CreateOrOpen(prefix+"Readings.Bluetooth",131072);
      access=new Mutex(false,prefix+"Snapshot");
      Activate=Signal("Activate");
      Preferences=Signal("Preferences");
      Preview=Signal("Preview");
      Detect=Signal("Detect");
      UpdateExit=Signal("UpdateExit");
      ScanDone=Signal("ScanDone");
      Updated=Signal("Updated");
    }

    EventWaitHandle Signal(string name){return new EventWaitHandle(false,EventResetMode.AutoReset,prefix+name);}

    bool Enter(){
      try{return access.WaitOne(50);}
      catch(AbandonedMutexException){return true;}
    }

    internal bool Publish(string mouse,string headset,string status,string bluetooth,string mouseTime,string headsetTime){
      if(!Enter())return false;
      try{
        using(var view=map.CreateViewStream())
        using(var writer=new BinaryWriter(view)){
          writer.Write(mouse);writer.Write(headset);writer.Write(status);
          writer.Write(bluetooth);writer.Write(processId);writer.Write(mouseTime);writer.Write(headsetTime);
        }
      }finally{access.ReleaseMutex();}
      Updated.Set();return true;
    }

    internal PanelSnapshot Read(){
      if(!Enter())return null;
      try{
        using(var view=map.CreateViewStream())
        using(var reader=new BinaryReader(view))
          return new PanelSnapshot{
            Mouse=reader.ReadString(),Headset=reader.ReadString(),Status=reader.ReadString(),Devices=reader.ReadString(),
            ProcessId=reader.ReadString(),MouseTime=reader.ReadString(),HeadsetTime=reader.ReadString()
          };
      }catch(IOException){return null;}
      finally{access.ReleaseMutex();}
    }

    public void Dispose(){ScanDone.Dispose();UpdateExit.Dispose();Updated.Dispose();Detect.Dispose();Preview.Dispose();Preferences.Dispose();Activate.Dispose();access.Dispose();map.Dispose();}
  }
}
