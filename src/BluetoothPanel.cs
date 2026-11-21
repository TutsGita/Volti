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
    DateTime nextSynapseScan=DateTime.MinValue;
    string synapseSnapshot="";
    bool scanningSynapse;
    bool scanning;
    NotifyIcon applicationTray;
    readonly Dictionary<string,NotifyIcon> bluetoothIcons=new Dictionary<string,NotifyIcon>();
    readonly Dictionary<string,Icon> receiverTrayIcons=new Dictionary<string,Icon>();
    readonly Dictionary<string,string> receiverIconStates=new Dictionary<string,string>();
    readonly List<Control> bluetoothCards=new List<Control>();
    static string VisibilityKey(string id){
      using(var hash=System.Security.Cryptography.SHA256.Create())return "Tray"+BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(id))).Replace("-","");
    }
    static bool IsTrayVisible(string id){try{using(var key=Registry.CurrentUser.OpenSubKey(SettingsKey))return key==null||Convert.ToInt32(key.GetValue(VisibilityKey(id),1))!=0;}catch(System.Security.SecurityException){return true;}}
    void AddVisibility(Control card,string id){
      var check=new CheckBox{Text=L.T("Show in tray"),Left=16,Top=145,Width=196,Height=24,Checked=IsTrayVisible(id)};
      check.CheckedChanged+=(s,e)=>{using(var key=Registry.CurrentUser.CreateSubKey(SettingsKey))key.SetValue(VisibilityKey(id),check.Checked?1:0);link.Preferences.Set();};
      var details=new RoundedButton{Text=L.T("Options"),Left=295,Top=54,Width=89,Height=24};
      card.Controls.Add(check);
      card.Controls.Add(details);
      foreach(Control child in card.Controls)if(child!=details && child.Top>=70){child.Top+=12;child.Visible=false;}
      card.Height=86;
      details.Click+=(s,e)=>{
        bool expanded=card.Height==86;
        foreach(Control child in card.Controls)if(child!=details && child.Top>=82)child.Visible=expanded;
        card.Height=expanded?190:86;
        RenderBluetooth();
      };
    }
    void RefreshVisibility(){
      int percent;
      trayDelux.Available=TryPercent(readings[0],out percent);
      trayJbl.Available=TryPercent(readings[1],out percent);
      trayMouse.Visible=trayDelux.Available&&IsTrayVisible("mouse");
      trayHeadset.Visible=trayJbl.Available&&IsTrayVisible("headset");
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
        string controllers=ControllerBattery.Scan();
        try{snapshot=BluetoothReader.Scan();}
        catch{snapshot="";Post(()=>Note("Bluetooth detection failed"));}
        if(controllers.Length>0)snapshot=snapshot.Length==0?controllers:snapshot+"\n"+controllers;
        Post(()=>{
          scanning=false;bluetoothSnapshot=snapshot;
          RefreshDiscoveredTray();Publish();
        });
      }){IsBackground=true,Name="Bluetooth battery discovery"}.Start();
    }
    void DetectSynapse(){
      if(scanningSynapse)return;
      scanningSynapse=true;
      nextSynapseScan=DateTime.UtcNow.AddSeconds(30);
      ThreadPool.QueueUserWorkItem(_=>{
        string snapshot=SynapseBattery.Scan();
        Post(()=>{
          scanningSynapse=false;
          if(synapseSnapshot==snapshot)return;
          synapseSnapshot=snapshot;
          RefreshDiscoveredTray();
          Publish();
        });
      });
    }
    void RefreshDiscoveredTray(){
      var present=new HashSet<string>();
      foreach(var row in DeviceRows(DiscoveredSnapshot())){
        if(row[2]=="-1")continue;
        string id=row[0], name=row[1], value=DisplayLevel(row);
        present.Add(id);
        NotifyIcon icon;
        if(!bluetoothIcons.TryGetValue(id,out icon)){
          icon=new NotifyIcon{Icon=appIcon,ContextMenuStrip=menu};
          icon.DoubleClick+=(s,e)=>LaunchPanel();
          bluetoothIcons.Add(id,icon);
        }
        icon.Text=TrayTip(DisplayName(row)+": "+value);
        int percent;
        if(!TryPercent(value,out percent))continue;
        string state=name+"|"+value+"|"+ThemeName, oldState;
        if(receiverIconStates.TryGetValue(id,out oldState)&&state==oldState)continue;
        Icon current;
        receiverTrayIcons.TryGetValue(id,out current);
        if(id.StartsWith("receiver:",StringComparison.Ordinal)) {
          UpdateTrayIcon(icon,ref current,value,name.StartsWith("JBL ",StringComparison.Ordinal)||name.StartsWith("HyperX Cloud ",StringComparison.Ordinal));
        } else {
          Icon next=CreateLevelIcon(percent);
          icon.Icon=next;
          if(current!=null)current.Dispose();
          current=next;
        }
        receiverTrayIcons[id]=current;
        receiverIconStates[id]=state;
      }
      foreach(string id in new List<string>(bluetoothIcons.Keys)){
        if(present.Contains(id))continue;
        bluetoothIcons[id].Dispose();
        bluetoothIcons.Remove(id);
        Icon previous;
        if(receiverTrayIcons.TryGetValue(id,out previous)){
          previous.Dispose();
          receiverTrayIcons.Remove(id);
          receiverIconStates.Remove(id);
        }
      }
      RefreshVisibility();
    }
    static Icon CreateLevelIcon(int percent){
      using(var image=new Bitmap(32,32))
      using(var graphics=Graphics.FromImage(image))
      using(var pen=new Pen(BatteryColor(percent),2))
      using(var brush=new SolidBrush(pen.Color)){
        graphics.DrawRoundedRectangle(pen,2,7,25,18,5);
        graphics.FillRectangle(brush,28,12,3,8);
        graphics.FillRectangle(brush,5,10,19*percent/100,12);
        IntPtr handle=image.GetHicon();
        try{using(var icon=Icon.FromHandle(handle))return (Icon)icon.Clone();}
        finally{Win32.DestroyIcon(handle);}
      }
    }
    List<string[]> DeviceRows(string snapshot){
      var rows=new List<string[]>();
      foreach(string line in snapshot.Split('\n')){
        var parts=line.Split('\t');if(parts.Length!=3)continue;
        rows.Add(new[]{Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])),Encoding.UTF8.GetString(Convert.FromBase64String(parts[1])),parts[2]});
      }
      return rows;
    }
    static string DisplayLevel(string[] row){
      if(row[2]=="-1")return "—";
      if(row[0].StartsWith("xinput:",StringComparison.Ordinal))return L.T(row[2]);
      int flag=row[2].IndexOf(' ');
      return flag<0?row[2]+"%":row[2].Insert(flag,"%");
    }
    static string DisplayName(string[] row){return row[0].StartsWith("xinput:")?L.T("Controller")+" "+row[0].Substring(7):row[1];}
    void RenderBluetooth(){
      if(overviewPanel==null)return;
      foreach(var card in bluetoothCards){overviewPanel.Controls.Remove(card);card.Dispose();}bluetoothCards.Clear();
      int y=8, percent;
      foreach(var value in new[]{deluxValue,jblValue}){
        var card=value.Parent;
        bool present=TryPercent(value==deluxValue?readings[0]:readings[1],out percent);
        card.Visible=present;
        if(present){card.Top=y;y+=card.Height+12;}
      }
      foreach(var row in DeviceRows(bluetoothSnapshot)){
        var card=new FrostCard{Left=20,Top=y,Width=400,Height=100};
        string source=row[0].StartsWith("synapse",StringComparison.Ordinal)?"Razer Synapse":
          row[0].StartsWith("receiver:",StringComparison.Ordinal)?"USB 2.4 GHz":
          row[0].StartsWith("xinput:",StringComparison.Ordinal)?"XInput":"Bluetooth";
        card.Controls.Add(new Label{Text=DisplayName(row),Left=16,Top=12,Width=267,Height=27,AutoEllipsis=true,Font=bodyFont});
        card.Controls.Add(new Label{Text=row[2]=="-1"?L.T("Battery reading unavailable"):source,Left=16,Top=44,Width=220,Height=23});
        var check=new CheckBox{Text=L.T("Show in tray"),Left=236,Top=44,Width=150,Height=25,Checked=IsTrayVisible(row[0])};
        string level=DisplayLevel(row);
        var value=new Label{Text=level,Left=282,Top=8,Width=102,Height=34,TextAlign=ContentAlignment.MiddleRight,Font=batteryFont};
        card.Controls.Add(value);
        Panel track=null,fill=null;
        if(TryPercent(level,out percent)) {
          track=new Panel{Left=16,Top=79,Width=368,Height=6};
          fill=new Panel{Width=0,Height=6};
          track.Controls.Add(fill);card.Controls.Add(track);
        } else
          value.Font=statusFont;
        string id=row[0];
        if(row[2]!="-1") {
          card.Controls.Add(check);
          check.CheckedChanged+=(s,e)=>{using(var key=Registry.CurrentUser.CreateSubKey(SettingsKey))key.SetValue(VisibilityKey(id),check.Checked?1:0);link.Preferences.Set();};
          check.Enter+=(s,e)=>overviewScroll.Reveal(check);
        } else check.Dispose();
        overviewPanel.Controls.Add(card);bluetoothCards.Add(card);PaintTheme(card);
        if(track!=null){track.BackColor=Mix(Card,Ink,0.14f);SetFill(fill,value);}
        y+=112;
      }
      footer.Visible=y==8;
      footer.SetBounds(24,18,380,56);
      footer.TextAlign=ContentAlignment.MiddleCenter;
      footer.Text=L.T(Array.IndexOf(readings,"Access denied")>=0?"Battery access unavailable. Try detecting again.":"No battery readings yet");
      overviewScroll.ContentHeight=footer.Visible?footer.Bottom+8:y;
    }
  }
}
