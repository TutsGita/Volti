// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Text;

namespace Volti {
  sealed class DeviceReading {
    internal readonly string Id, Name, Level, Observed;
    internal DeviceReading(string id,string name,string level,string observed){
      Id=id; Name=name; Level=level; Observed=observed;
    }

    // The two Base64 fields allow names and identifiers to contain arbitrary delimiters.
    internal string Encode(){
      return Convert.ToBase64String(Encoding.UTF8.GetBytes(Id))+"\t"+
        Convert.ToBase64String(Encoding.UTF8.GetBytes(Name))+"\t"+Level+"\t"+Observed;
    }

    internal static IEnumerable<DeviceReading> Decode(string devices){
      foreach(string line in devices.Split('\n')){
        var fields=line.Split('\t');
        if(fields.Length!=3&&fields.Length!=4)continue;
        string id,name;
        try{
          id=Encoding.UTF8.GetString(Convert.FromBase64String(fields[0]));
          name=Encoding.UTF8.GetString(Convert.FromBase64String(fields[1]));
        }catch(FormatException){continue;}
        if(id.Length!=0)yield return new DeviceReading(id,name,fields[2],fields.Length==4?fields[3]:"");
      }
    }
  }
}
