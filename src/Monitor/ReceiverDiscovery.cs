// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Volti {
  sealed class DeviceChangeWindow : NativeWindow {
    readonly Action changed;
    internal DeviceChangeWindow(Action changed) {
      this.changed=changed;
      CreateHandle(new CreateParams {Caption="Volti hardware notifications"});
    }
    protected override void WndProc(ref Message message) {
      if(message.Msg==0x219 && message.WParam.ToInt64()==7)changed();
      base.WndProc(ref message);
    }
  }

  static class ReceiverContainer {
    [StructLayout(LayoutKind.Sequential)]
    struct PropertyKey {internal Guid Format; internal uint Id;}
    [DllImport("cfgmgr32.dll",CharSet=CharSet.Unicode)]
    static extern uint CM_Get_Device_Interface_Property(string path,ref PropertyKey key,out uint type,
      [Out] byte[] data,ref uint size,uint flags);
    [DllImport("cfgmgr32.dll",CharSet=CharSet.Unicode)]
    static extern uint CM_Locate_DevNode(out uint node,string instance,uint flags);
    [DllImport("cfgmgr32.dll",CharSet=CharSet.Unicode)]
    static extern uint CM_Get_DevNode_Property(uint node,ref PropertyKey key,out uint type,out Guid data,ref uint size,uint flags);

    internal static bool TryGet(string path,out Guid container) {
      container=Guid.Empty;
      var instanceKey=new PropertyKey{Format=new Guid("78c34fc8-104a-4aca-9ea4-524d52996e57"),Id=256};
      var containerKey=new PropertyKey{Format=new Guid("8c7ed206-3f8a-4827-b3ab-ae9e1faefc6c"),Id=2};
      var buffer=new byte[1024];
      uint type,node,size=(uint)buffer.Length;
      if(CM_Get_Device_Interface_Property(path,ref instanceKey,out type,buffer,ref size,0)!=0||type!=18)return false;
      string instance=System.Text.Encoding.Unicode.GetString(buffer,0,(int)size).TrimEnd('\0');
      if(CM_Locate_DevNode(out node,instance,0)!=0)return false;
      size=16;
      return CM_Get_DevNode_Property(node,ref containerKey,out type,out container,ref size,0)==0&&type==13&&container!=Guid.Empty;
    }
  }
}
