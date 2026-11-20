// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace Volti {
  sealed class PanelLink : IDisposable {
    const string Prefix="Local\\Volti.02.";
    readonly MemoryMappedFile map;
    readonly Mutex access;
    internal readonly EventWaitHandle Activate, Preferences, Preview, Detect;

    internal PanelLink(){
      map=MemoryMappedFile.CreateOrOpen(Prefix+"Readings.Bluetooth",131072);
      access=new Mutex(false,Prefix+"Snapshot");
      Activate=Signal("Activate");
      Preferences=Signal("Preferences");
      Preview=Signal("Preview");
      Detect=Signal("Detect");
    }

    static EventWaitHandle Signal(string name){return new EventWaitHandle(false,EventResetMode.AutoReset,Prefix+name);}

    bool Enter(){
      try{return access.WaitOne(50);}
      catch(AbandonedMutexException){return true;}
    }

    internal void Publish(string mouse,string headset,string status,string bluetooth){
      if(!Enter())return;
      try{
        using(var view=map.CreateViewStream())
        using(var writer=new BinaryWriter(view)){
          writer.Write(mouse);writer.Write(headset);writer.Write(status);
          writer.Write(bluetooth);
        }
      }finally{access.ReleaseMutex();}
    }

    internal string[] Read(){
      if(!Enter())return null;
      try{
        using(var view=map.CreateViewStream())
        using(var reader=new BinaryReader(view))
          return new[]{reader.ReadString(),reader.ReadString(),reader.ReadString(),reader.ReadString()};
      }catch(IOException){return null;}
      finally{access.ReleaseMutex();}
    }

    public void Dispose(){Detect.Dispose();Preview.Dispose();Preferences.Dispose();Activate.Dispose();access.Dispose();map.Dispose();}
  }
}
