// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
namespace Volti {
  sealed partial class MainForm {
    string bluetoothSnapshot="";
    DateTime nextBluetoothScan=DateTime.MinValue;
    bool scanning;
    NotifyIcon applicationTray;
    readonly Dictionary<string,NotifyIcon> bluetoothIcons=new Dictionary<string,NotifyIcon>();
    readonly List<Control> bluetoothCards=new List<Control>();
    static string VisibilityKey(string id){
      using(var hash=System.Security.Cryptography.SHA256.Create())return "Tray"+BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(id))).Replace("-","");
    }
    static bool IsTrayVisible(string id){try{using(var key=Registry.CurrentUser.OpenSubKey(SettingsKey))return key==null||Convert.ToInt32(key.GetValue(VisibilityKey(id),1))!=0;}catch(System.Security.SecurityException){return true;}}
    void AddVisibility(Control card,string id){
      card.Height=171;
      var check=new CheckBox{Text=L.T("Show in tray"),Left=16,Top=140,Width=350,Height=24,Checked=IsTrayVisible(id)};
      check.CheckedChanged+=(s,e)=>{using(var key=Registry.CurrentUser.CreateSubKey(SettingsKey))key.SetValue(VisibilityKey(id),check.Checked?1:0);link.Preferences.Set();};
      card.Controls.Add(check);
      if(id=="headset")card.Top=191;
    }
    void RefreshVisibility(){
      trayMouse.Visible=IsTrayVisible("mouse");trayHeadset.Visible=IsTrayVisible("headset");
      bool any=trayMouse.Visible||trayHeadset.Visible;
      foreach(var pair in bluetoothIcons){pair.Value.Visible=IsTrayVisible(pair.Key);any|=pair.Value.Visible;}
      if(applicationTray==null){applicationTray=new NotifyIcon{Icon=appIcon,Text="Volti",ContextMenuStrip=menu};applicationTray.DoubleClick+=(s,e)=>LaunchPanel();}
      applicationTray.Visible=!any;
    }
    void DetectBluetooth(){
      if(scanning)return;
      scanning=true;nextBluetoothScan=DateTime.UtcNow.AddMinutes(1);
      new Thread(()=>{
        string snapshot;
        try{snapshot=BluetoothReader.Scan();}
        catch{Post(()=>{scanning=false;Note("Bluetooth detection failed");});return;}
        Post(()=>{
          scanning=false;bluetoothSnapshot=snapshot;
          var present=new HashSet<string>();
          foreach(var row in BluetoothRows()){
            present.Add(row[0]);NotifyIcon icon;
            if(!bluetoothIcons.TryGetValue(row[0],out icon)){
              icon=new NotifyIcon{Icon=appIcon,ContextMenuStrip=menu};
              icon.DoubleClick+=(s,e)=>LaunchPanel();bluetoothIcons.Add(row[0],icon);
            }
            icon.Text=TrayTip(row[1]+": "+row[2]+"%");icon.Visible=IsTrayVisible(row[0]);
          }
          foreach(string id in new List<string>(bluetoothIcons.Keys))if(!present.Contains(id)){bluetoothIcons[id].Dispose();bluetoothIcons.Remove(id);}
          RefreshVisibility();Publish();
        });
      }){IsBackground=true,Name="Bluetooth battery discovery"}.Start();
    }
    List<string[]> BluetoothRows(){
      var rows=new List<string[]>();
      foreach(string line in bluetoothSnapshot.Split('\n')){
        var parts=line.Split('\t');if(parts.Length!=3)continue;
        rows.Add(new[]{Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])),Encoding.UTF8.GetString(Convert.FromBase64String(parts[1])),parts[2]});
      }
      return rows;
    }
    void RenderBluetooth(){
      if(overviewPanel==null)return;
      foreach(var card in bluetoothCards){overviewPanel.Controls.Remove(card);card.Dispose();}bluetoothCards.Clear();
      overviewPanel.AutoScrollPosition=Point.Empty;
      footer.Top=372;
      int y=414;
      foreach(var row in BluetoothRows()){
        var card=new FrostCard{Left=20,Top=y,Width=400,Height=96};
        card.Controls.Add(new Label{Text=row[1],Left=15,Top=10,Width=365,Height=28,AutoEllipsis=true});
        card.Controls.Add(new Label{Text="Bluetooth  •  "+row[2]+"%",Left=15,Top=39,Width=365,Height=23});
        var check=new CheckBox{Text=L.T("Show in tray"),Left=15,Top=66,Width=365,Checked=IsTrayVisible(row[0])};
        string id=row[0];
        check.CheckedChanged+=(s,e)=>{using(var key=Registry.CurrentUser.CreateSubKey(SettingsKey))key.SetValue(VisibilityKey(id),check.Checked?1:0);link.Preferences.Set();};
        card.Controls.Add(check);overviewPanel.Controls.Add(card);bluetoothCards.Add(card);PaintTheme(card);y+=108;
      }
      overviewPanel.AutoScrollMinSize=new Size(0,y);
    }
  }
}
