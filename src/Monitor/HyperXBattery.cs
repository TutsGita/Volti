// SPDX-License-Identifier: AGPL-3.0-only
// HyperX protocol adapted from HyperBatteryMonitor. See THIRD_PARTY_NOTICES.md.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Volti {
  static class HyperXBattery {
    static readonly Guid HidClass = new Guid("4d1e55b2-f16f-11cf-88cb-001111000030");
    const string ControlCollection = "&MI_03&COL03#";
    const string Stinger2 = "VID_03F0&PID_0D93";
    const string Kingston = "VID_0951&PID_170B";
    const string PlayStation = "VID_03F0&PID_0A93";
    const string Dts = "VID_03F0&PID_0A8E";

    static readonly byte[] Stinger2Request = {0x06,0xFF,0xBB,0x02};
    static readonly byte[] CoreConnection = {
      0xFF,0x07,0x00,0xFD,0x04,0x00,0x00,0x02,0x82,0x03,0x05,0x29,0x0F,0x00,0x00,0x03,0x68,0x10,0x00,0x00
    };
    static readonly byte[] CoreRequest = {
      0xFF,0x07,0x00,0xFD,0x04,0x0C,0xF1,0x02,0x01,0x04,0xF0,0x0C
    };
    static readonly double[] Charging = {3400,3830.9,3870.7,3916.2,3929.9,3945.6,3967.4,4000,4049.7,4090,4130};
    static readonly double[] Discharging = {3517.4,3650.8,3699.8,3738.1,3763.3,3784.7,3838.1,3905.2,3954.2,4042.3,4120.5};

    [DllImport("cfgmgr32.dll", CharSet=CharSet.Unicode)]
    static extern uint CM_Get_Device_Interface_List_Size(out uint length, ref Guid guid, string instance, uint flags);
    [DllImport("cfgmgr32.dll", CharSet=CharSet.Unicode)]
    static extern uint CM_Get_Device_Interface_List(ref Guid guid, string instance, [Out] char[] buffer, uint length, uint flags);

    internal static string[] Paths() {
      var guid = HidClass;
      for(int attempt=0;attempt<2;attempt++) {
        uint length;
        if(CM_Get_Device_Interface_List_Size(out length,ref guid,null,0)!=0 || length==0 || length>262144)
          break;
        var buffer=new char[length];
        uint result=CM_Get_Device_Interface_List(ref guid,null,buffer,length,0);
        if(result==0)return new string(buffer).Split(new[]{'\0'},StringSplitOptions.RemoveEmptyEntries);
        if(result!=0x1A)break;
      }
      return Win32.HidPaths();
    }

    internal static bool IsOriginalStinger(string path){
      return path.IndexOf("#VID_0951&PID_16DA&",StringComparison.OrdinalIgnoreCase)>=0 ||
        path.IndexOf("#VID_0951&PID_16DA#",StringComparison.OrdinalIgnoreCase)>=0;
    }

    internal static string DeviceName(string path) {
      if(IsHaste2(path)) return "HyperX Pulsefire Haste 2 Wireless";
      if(Matches(path,Stinger2)) return "HyperX Cloud Stinger 2 Wireless";
      if(Matches(path,Kingston)) return "HyperX Cloud Stinger Core Wireless";
      if(Matches(path,PlayStation)) return "HyperX Cloud Stinger Core Wireless (PlayStation)";
      if(Matches(path,Dts)) return "HyperX Cloud Stinger Core Wireless (DTS)";
      return null;
    }

    static bool Matches(string path,string id) {
      return path.IndexOf("#"+id+ControlCollection,StringComparison.OrdinalIgnoreCase)>=0;
    }

    static bool IsHaste2(string path) {
      return path.IndexOf("#VID_03F0&PID_0D97&MI_02",StringComparison.OrdinalIgnoreCase)>=0 ||
        path.IndexOf("#VID_03F0&PID_0F98&MI_02",StringComparison.OrdinalIgnoreCase)>=0;
    }

    internal static int Stinger2Percent(byte[] report,uint bytes) {
      return bytes>=8 && report[0]==0x06 && report[1]==0xFF && report[2]==0xBB &&
        report[3]==0x02 && report[7]<=100 ? report[7] : -1;
    }

    internal static int CorePercent(byte[] report,bool strictHeader) {
      if(report.Length<13 || report[0]!=0xFF || (strictHeader && (report[1]!=0x12 || report[2]!=0x05)))
        return -1;
      int charge=report[9], millivolts=report[11] | (report[12]<<8);
      if((charge!=3 && charge!=5 && charge!=6) || millivolts<3000 || millivolts>4500)return -1;
      if(charge==6)return 100;
      var curve=charge==5?Charging:Discharging;
      for(int i=1;i<curve.Length;i++) {
        if(millivolts<curve[i])
          return Math.Max(0,(int)Math.Floor((i-1)*10+10*(millivolts-curve[i-1])/(curve[i]-curve[i-1])));
      }
      return 100;
    }

    internal static int Query(string path) {
      bool secondGeneration=Matches(path,Stinger2);
      bool haste2=IsHaste2(path);
      IntPtr handle=Win32.CreateFile(path,0xC0000000,3,IntPtr.Zero,3,
        0x40000000u,IntPtr.Zero);
      if(handle==new IntPtr(-1))return -1;
      try {
        IntPtr data;
        if(!Win32.HidD_GetPreparsedData(handle,out data))return -1;
        var caps=new byte[64];
        try {if(Win32.HidP_GetCaps(data,caps)<0)return -1;}
        finally {Win32.HidD_FreePreparsedData(data);}
        int page=caps[2] | (caps[3]<<8);
        if(page<0xFF00)return -1;
        if(haste2) {
          if(page!=0xFF00 || caps[0]!=1 || caps[1]!=0 ||
            caps[4]!=64 || caps[5]!=0 || caps[6]!=64 || caps[7]!=0)return -1;
          if(QueryHasteByte(handle,0x52,0x01)!=1)return -1;
          Thread.Sleep(10);
          int percent=QueryHasteByte(handle,0x50,0x02);
          return percent<=100?percent:-1;
        }
        if(secondGeneration)return QueryStinger2(handle,caps);
        int length=caps[8] | (caps[9]<<8);
        if(length<32 || length>512)return -1;
        var request=new byte[length];
        var reply=new byte[length];
        Array.Copy(CoreConnection,request,CoreConnection.Length);
        if(!Feature(handle,request,true) ||
          !ReadFeature(handle,reply) || reply[11]!=1)return -1;
        Array.Clear(request,0,length);
        Array.Copy(CoreRequest,request,CoreRequest.Length);
        if(!Feature(handle,request,true))return -1;
        bool strict=!Matches(path,Kingston);
        for(int attempt=0;attempt<(strict?10:1);attempt++) {
          if(!ReadFeature(handle,reply))break;
          int percent=CorePercent(reply,strict);
          if(percent>=0)return percent;
        }
        return -1;
      } finally {Win32.CloseHandle(handle);}
    }

    static int QueryHasteByte(IntPtr handle, byte command, byte subcommand) {
      var frame=new byte[64];
      frame[0]=command;
      frame[1]=subcommand;
      uint count;
      if(Win32.TransferWithTimeout(handle,frame,out count,400,true)!=1 || count!=64)return -1;
      var clock=Stopwatch.StartNew();
      while(clock.ElapsedMilliseconds<600) {
        uint remaining=(uint)Math.Max(1,600-clock.ElapsedMilliseconds);
        if(Win32.ReadWithTimeout(handle,frame,out count,remaining)!=1)break;
        if(count>=3 && frame[0]==command+1 && frame[1]==subcommand)return frame[2];
      }
      return -1;
    }

    static bool Feature(IntPtr handle,byte[] report,bool write) {
      // hidclass.h: IOCTL_HID_SET_FEATURE and IOCTL_HID_GET_FEATURE.
      uint transferred;
      return Win32.TransferWithTimeout(handle,report,out transferred,400,write,IntPtr.Zero,write?0xB0191u:0xB0192u)==1;
    }

    static bool ReadFeature(IntPtr handle,byte[] reply) {
      Thread.Sleep(20);
      Array.Clear(reply,0,reply.Length);
      reply[0]=0xFF;
      return Feature(handle,reply,false);
    }

    static int QueryStinger2(IntPtr handle,byte[] caps) {
      int input=caps[4] | (caps[5]<<8), output=caps[6] | (caps[7]<<8);
      if(input<8 || input>512 || output<Stinger2Request.Length || output>512)return -1;
      var request=new byte[output];
      Array.Copy(Stinger2Request,request,Stinger2Request.Length);
      uint written;
      if(Win32.TransferWithTimeout(handle,request,out written,400,true)!=1 || written!=output)return -1;
      var reply=new byte[input];
      var clock=Stopwatch.StartNew();
      while(clock.ElapsedMilliseconds<600) {
        uint bytes;
        int remaining=Math.Max(1,600-(int)clock.ElapsedMilliseconds);
        if(Win32.ReadWithTimeout(handle,reply,out bytes,(uint)remaining)!=1)break;
        int percent=Stinger2Percent(reply,bytes);
        if(percent>=0)return percent;
      }
      return -1;
    }
  }
}
