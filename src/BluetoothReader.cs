// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Volti {
  static class BluetoothReader {
    [StructLayout(LayoutKind.Sequential)] struct Device {internal int Size;internal Guid Class;internal uint Instance;internal IntPtr Reserved;}
    [StructLayout(LayoutKind.Sequential)] struct Key {internal Guid Id;internal uint Property;internal Key(string id,uint property){Id=new Guid(id);Property=property;}}
    [DllImport("setupapi.dll",CharSet=CharSet.Unicode)] static extern IntPtr SetupDiGetClassDevs(IntPtr guid,string enumerator,IntPtr parent,uint flags);
    [DllImport("setupapi.dll")] static extern bool SetupDiEnumDeviceInfo(IntPtr set,uint index,ref Device device);
    [DllImport("setupapi.dll",CharSet=CharSet.Unicode)] static extern bool SetupDiGetDeviceInstanceId(IntPtr set,ref Device device,StringBuilder id,int length,out int required);
    [DllImport("setupapi.dll",CharSet=CharSet.Unicode)] static extern bool SetupDiGetDeviceProperty(IntPtr set,ref Device device,ref Key key,out uint type,byte[] data,uint size,out uint needed,uint flags);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    static byte[] Property(IntPtr set,ref Device device,Key key){
      uint type,size;var bytes=new byte[2048];
      return SetupDiGetDeviceProperty(set,ref device,ref key,out type,bytes,(uint)bytes.Length,out size,0)?bytes:null;
    }
    internal static string Scan(){
      var rows=new List<string>();
      IntPtr set=SetupDiGetClassDevs(IntPtr.Zero,null,IntPtr.Zero,6);
      if(set==new IntPtr(-1))throw new System.ComponentModel.Win32Exception();
      try{
        for(uint index=0;;index++){
          var device=new Device{Size=Marshal.SizeOf(typeof(Device))};
          if(!SetupDiEnumDeviceInfo(set,index,ref device))break;
          int required;var id=new StringBuilder(512);
          if(!SetupDiGetDeviceInstanceId(set,ref device,id,id.Capacity,out required))continue;
          if(!id.ToString().StartsWith("BTH",StringComparison.OrdinalIgnoreCase))continue;
          var battery=Property(set,ref device,new Key("104ea319-6ee2-4701-bd47-8ddbf425bbe5",2));
          if(battery==null||battery[0]>100)continue;
          var name=Property(set,ref device,new Key("a45c254e-df1c-4efd-8020-67d146a850e0",14));
          string title=name==null?id.ToString():Encoding.Unicode.GetString(name).TrimEnd('\0');
          rows.Add(Convert.ToBase64String(Encoding.UTF8.GetBytes(id.ToString()))+"\t"+Convert.ToBase64String(Encoding.UTF8.GetBytes(title))+"\t"+battery[0]);
        }
      }finally{SetupDiDestroyDeviceInfoList(set);}
      return string.Join("\n",rows.ToArray());
    }
  }
}
