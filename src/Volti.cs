// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("Volti")]
[assembly: System.Reflection.AssemblyProduct("Volti")]
[assembly: System.Reflection.AssemblyVersion("0.3.3.0")]
[assembly: System.Reflection.AssemblyInformationalVersion("0.3.3")]
namespace Volti {
  sealed class BatteryMenuColors : ProfessionalColorTable {
    static Color Surface {get{return MainForm.Surface;}} static Color Hover {get{return MainForm.Card;}} static Color Muted {get{return MainForm.Accent;}}
    public override Color ToolStripDropDownBackground { get { return Surface; } }
    public override Color ImageMarginGradientBegin { get { return Surface; } }
    public override Color ImageMarginGradientMiddle { get { return Surface; } }
    public override Color ImageMarginGradientEnd { get { return Surface; } }
    public override Color MenuBorder { get { return Color.FromArgb(25,11,14); } }
    public override Color MenuItemBorder { get { return Hover; } }
    public override Color MenuItemSelected { get { return Hover; } }
    public override Color MenuItemSelectedGradientBegin { get { return Hover; } }
    public override Color MenuItemSelectedGradientEnd { get { return Hover; } }
    public override Color MenuItemPressedGradientBegin { get { return Hover; } }
    public override Color MenuItemPressedGradientEnd { get { return Hover; } }
    public override Color SeparatorDark { get { return Muted; } }
    public override Color SeparatorLight { get { return Surface; } }
    public override Color ToolStripBorder { get { return Surface; } }
  }
  sealed class BatteryMenuRenderer : ToolStripProfessionalRenderer {
    public BatteryMenuRenderer() : base(new BatteryMenuColors()) { RoundedEdges=true; }
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) {
      if(e.Item.Tag as string=="brand"){using(var b=new SolidBrush(Color.FromArgb(39,20,24)))e.Graphics.FillRectangle(b,new Rectangle(Point.Empty,e.Item.Size));return;}
      base.OnRenderMenuItemBackground(e);
    }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
      if(e.Item.Tag as string=="brand") {
        Rectangle text=new Rectangle(52,0,Math.Max(1,e.Item.Width-60),e.Item.Height);
        TextRenderer.DrawText(e.Graphics,"Volti",e.TextFont,text,Color.FromArgb(255,242,242),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        return;
      }
      base.OnRenderItemText(e);
    }
  }
  sealed class ProductGlyph : Control {
    public bool Headset;
    public ProductGlyph(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;}
    protected override void OnPaint(PaintEventArgs e){
      base.OnPaint(e); e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      DeviceArt.Draw(e.Graphics,Headset,ForeColor,new Rectangle(0,0,Width,Height));
    }
  }
  sealed class FrostCard : Panel {
    internal bool Hero;
    public FrostCard(){SetStyle(ControlStyles.SupportsTransparentBackColor,true);DoubleBuffered=true;ResizeRedraw=true;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      var bounds=new Rectangle(1,1,Width-3,Height-3);
      Color top=Hero?MainForm.Mix(MainForm.Card,MainForm.Accent,0.30f):MainForm.IsAeroTheme?Color.White:MainForm.Mix(MainForm.Card,MainForm.Ink,0.04f);
      Color bottom=MainForm.IsAeroTheme?Color.FromArgb(174,230,248):MainForm.Mix(MainForm.Card,MainForm.Surface,0.32f);
      using(var brush=new System.Drawing.Drawing2D.LinearGradientBrush(bounds,top,bottom,65f))
        e.Graphics.FillRoundedRectangle(brush,bounds.X,bounds.Y,bounds.Width,bounds.Height,32);
      using(var pen=new Pen(MainForm.Mix(MainForm.Card,MainForm.Ink,0.16f)))
        e.Graphics.DrawRoundedRectangle(pen,bounds.X,bounds.Y,bounds.Width,bounds.Height,32);
    }
  }
  static class GraphicsExtensions {
    internal static void DrawRoundedRectangle(this Graphics g,Pen p,float x,float y,float w,float h,float r){using(var path=new System.Drawing.Drawing2D.GraphicsPath()){path.AddArc(x,y,r,r,180,90);path.AddArc(x+w-r,y,r,r,270,90);path.AddArc(x+w-r,y+h-r,r,r,0,90);path.AddArc(x,y+h-r,r,r,90,90);path.CloseFigure();g.DrawPath(p,path);}}
    internal static void FillRoundedRectangle(this Graphics g,Brush b,float x,float y,float w,float h,float r){using(var path=new System.Drawing.Drawing2D.GraphicsPath()){path.AddArc(x,y,r,r,180,90);path.AddArc(x+w-r,y,r,r,270,90);path.AddArc(x+w-r,y+h-r,r,r,0,90);path.AddArc(x,y+h-r,r,r,90,90);path.CloseFigure();g.FillPath(b,path);}}
  }

  static class VoltiBrand {
    internal static readonly Color Red=Color.FromArgb(220,67,76), RedSoft=Color.FromArgb(255,104,112), Wine=Color.FromArgb(64,22,27);
    internal static Icon CreateIcon() { return new Icon(System.IO.Path.Combine(Application.StartupPath,"assets","Volti.ico")); }
  }

  static class DeviceArt {
    static readonly Dictionary<bool,Image> Images=new Dictionary<bool,Image>();
    internal static void Draw(Graphics g,bool headset,Color color,Rectangle bounds){
      Image image;if(!Images.TryGetValue(headset,out image)){image=Image.FromFile(System.IO.Path.Combine(Application.StartupPath,"assets",headset?"headphone-svgrepo-com.png":"mouse-svgrepo-com.png"));Images.Add(headset,image);}
      using(var attr=new System.Drawing.Imaging.ImageAttributes()){
        var matrix=new System.Drawing.Imaging.ColorMatrix();matrix.Matrix00=color.R/255f;matrix.Matrix11=color.G/255f;matrix.Matrix22=color.B/255f;attr.SetColorMatrix(matrix);
        g.DrawImage(image,bounds,0,0,image.Width,image.Height,GraphicsUnit.Pixel,attr);
      }
    }
  }
  static class Win32 {
    [StructLayout(LayoutKind.Sequential)] internal struct RawDevice { public IntPtr Handle; public uint Type; }
    [StructLayout(LayoutKind.Sequential)] internal struct Overlapped { public IntPtr Internal, InternalHigh; public uint Offset, OffsetHigh; public IntPtr Event; }
    [DllImport("user32.dll", SetLastError=true)] internal static extern uint GetRawInputDeviceList(IntPtr list, ref uint count, uint itemSize);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)] internal static extern uint GetRawInputDeviceInfo(IntPtr handle, uint command, StringBuilder data, ref uint size);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] internal static extern IntPtr CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool ReadFile(IntPtr file, IntPtr buffer, uint count, out uint read, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool WriteFile(IntPtr file, IntPtr buffer, uint count, out uint written, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool CancelIoEx(IntPtr file, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr CreateEvent(IntPtr security, bool manualReset, bool initialState, string name);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool SetEvent(IntPtr handle);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern uint WaitForMultipleObjects(uint count, IntPtr[] handles, bool waitAll, uint timeout);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool GetOverlappedResult(IntPtr file, IntPtr overlapped, out uint read, bool wait);
    [DllImport("kernel32.dll")] internal static extern bool ResetEvent(IntPtr handle);
    [DllImport("kernel32.dll")] internal static extern bool CloseHandle(IntPtr handle);
    [DllImport("hid.dll")] internal static extern bool HidD_GetPreparsedData(IntPtr file, out IntPtr data);
    [DllImport("hid.dll")] internal static extern bool HidD_FreePreparsedData(IntPtr data);
    [DllImport("hid.dll")] internal static extern int HidP_GetCaps(IntPtr data, byte[] caps);
    [DllImport("hid.dll")] internal static extern bool HidD_SetFeature(IntPtr file, byte[] buffer, uint length);
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool ReleaseCapture();
    [DllImport("user32.dll",CharSet=CharSet.Auto,SetLastError=true)] internal static extern IntPtr SendMessage(IntPtr hwnd,uint message,IntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll",SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool DestroyIcon(IntPtr icon);
    internal static string[] HidPaths() {
      uint count=0; GetRawInputDeviceList(IntPtr.Zero,ref count,(uint)Marshal.SizeOf(typeof(RawDevice)));
      if(count==0 || count>4096) return new string[0];
      IntPtr memory=Marshal.AllocHGlobal((int)count*Marshal.SizeOf(typeof(RawDevice)));
      try {
        uint actual=GetRawInputDeviceList(memory,ref count,(uint)Marshal.SizeOf(typeof(RawDevice)));
        var paths=new List<string>(); if(actual>count)return paths.ToArray();
        for(int i=0;i<actual;i++) {
          RawDevice d=(RawDevice)Marshal.PtrToStructure(IntPtr.Add(memory,i*Marshal.SizeOf(typeof(RawDevice))),typeof(RawDevice));
          uint chars=0; GetRawInputDeviceInfo(d.Handle,0x20000007,null,ref chars);
          if(chars==0 || chars>2048) continue;
          var path=new StringBuilder((int)chars); GetRawInputDeviceInfo(d.Handle,0x20000007,path,ref chars);
          paths.Add(path.ToString());
        }
        return paths.ToArray();
      } finally { Marshal.FreeHGlobal(memory); }
    }
    // Returns 1 for a report, 0 for timeout, and -1 for an I/O error.
    internal static int ReadWithTimeout(IntPtr file, byte[] buffer, out uint read, uint timeout, IntPtr stopEvent = default(IntPtr)) {
      return TransferWithTimeout(file,buffer,out read,timeout,false,stopEvent);
    }
    internal static int TransferWithTimeout(IntPtr file, byte[] buffer, out uint read, uint timeout, bool write, IntPtr stopEvent = default(IntPtr)) {
      read=0; IntPtr ev=CreateEvent(IntPtr.Zero,true,false,null); if(ev==IntPtr.Zero)return -1;
      IntPtr ov=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Overlapped)));
      GCHandle pinned=GCHandle.Alloc(buffer,GCHandleType.Pinned);
      try {
        Overlapped value=new Overlapped(); value.Event=ev; Marshal.StructureToPtr(value,ov,false);
        IntPtr address=pinned.AddrOfPinnedObject();
        bool transferred=write?WriteFile(file,address,(uint)buffer.Length,out read,ov):ReadFile(file,address,(uint)buffer.Length,out read,ov);
        if(transferred)return 1;
        if(Marshal.GetLastWin32Error()!=997)return -1;
        uint wait=stopEvent==IntPtr.Zero?WaitForSingleObject(ev,timeout):WaitForMultipleObjects(2,new[]{ev,stopEvent},false,uint.MaxValue);
        if(wait==0)return GetOverlappedResult(file,ov,out read,false)?1:-1;
        if(wait==0x102){CancelIoEx(file,ov); if(GetOverlappedResult(file,ov,out read,true))return 1; return 0;}
        CancelIoEx(file,ov);GetOverlappedResult(file,ov,out read,true);return -1;
      } finally { pinned.Free(); Marshal.FreeHGlobal(ov); CloseHandle(ev); }
    }
  }

  sealed partial class MainForm : ApplicationContext {
    Label deluxValue, jblValue, footer;
    Panel deluxFill, jblFill;
    Panel overviewPanel, settingsPanel;
    GlassScrollPanel overviewScroll;
    readonly NotifyIcon trayMouse, trayHeadset;
    readonly ToolStripMenuItem trayDelux, trayJbl, trayStatus;
    CheckBox startupCheck;

    readonly Icon appIcon;
    Icon trayMouseIcon, trayHeadsetIcon;


    bool updatingStartup;
    readonly List<IntPtr> handles=new List<IntPtr>();
    readonly List<Thread> readers=new List<Thread>();


    volatile bool stopping;
    readonly Control dispatcher=new Control();
    Form window;
    readonly Font bodyFont=new Font("Segoe UI",10);
    readonly ToolStripMenuItem openItem, exitItem, taskbarItem;
    readonly ContextMenuStrip menu;
    readonly string[] readings={"—","—"};
    readonly string[] queuedReadings=new string[2];
    readonly object readingLock=new object();
    string receiverStatus="Ready";
    readonly List<IDisposable> panelResources=new List<IDisposable>();
    Font batteryFont, statusFont;
    readonly bool panelProcess;
    readonly PanelLink link=new PanelLink();
    readonly System.Windows.Forms.Timer bridgeTimer=new System.Windows.Forms.Timer{Interval=250};

    public MainForm(bool panelProcess) {
      this.panelProcess=panelProcess;
      L.Load();LoadTheme();
      LoadLimits();
      appIcon=VoltiBrand.CreateIcon();
      var handle=dispatcher.Handle;
      bridgeTimer.Tick+=(s,e)=>SyncProcesses();
      bridgeTimer.Start();
      if(panelProcess){ShowPanel();return;}
      menu=new ContextMenuStrip{Renderer=new BatteryMenuRenderer(),ShowImageMargin=false,ShowCheckMargin=false};
      trayDelux=new ToolStripMenuItem{Enabled=false};
      trayJbl=new ToolStripMenuItem{Enabled=false};
      trayStatus=new ToolStripMenuItem{Enabled=false};
      openItem=new ToolStripMenuItem(L.T("Open Volti"));openItem.Click+=(s,e)=>LaunchPanel();
      exitItem=new ToolStripMenuItem(L.T("Exit"));exitItem.Click+=(s,e)=>ExitThread();
      taskbarItem=new ToolStripMenuItem(L.T("Windows taskbar settings"));
      taskbarItem.Click+=(s,e)=>{
        try {using(var settings=Process.Start(new ProcessStartInfo("ms-settings:taskbar"){UseShellExecute=true})){}}
        catch(System.ComponentModel.Win32Exception){MessageBox.Show(L.T("Could not open Windows taskbar settings."),"Volti");}
      };
      menu.Items.AddRange(new ToolStripItem[]{trayDelux,trayJbl,new ToolStripSeparator(),openItem,taskbarItem,exitItem});
      trayMouse=new NotifyIcon{ContextMenuStrip=menu};
      trayHeadset=new NotifyIcon{ContextMenuStrip=menu};
      trayMouse.DoubleClick+=(s,e)=>LaunchPanel();trayHeadset.DoubleClick+=(s,e)=>LaunchPanel();
      ApplyTheme();RefreshVisibility();Publish();DetectBluetooth();
      readerStopEvent=Win32.CreateEvent(IntPtr.Zero,true,false,null);
      DetectHyperX();
      DetectSynapse();
      new Thread(Connect){IsBackground=true,Name="HID discovery"}.Start();
    }
    void BuildWindow() {
      window=new Form{Text="Volti",Icon=appIcon,FormBorderStyle=FormBorderStyle.None,ClientSize=new Size(450,556),StartPosition=FormStartPosition.CenterScreen,Font=bodyFont};
      window.SizeChanged+=(sender,args)=>RoundWindow();
      RoundWindow();
      window.FormClosed+=(s,e)=>dispatcher.BeginInvoke((Action)ExitThread);
      window.Disposed+=(s,e)=>{foreach(var resource in panelResources)resource.Dispose();panelResources.Clear();overviewScroll=null;overviewPanel=settingsPanel=themesPanel=deluxFill=jblFill=null;deluxValue=jblValue=footer=null;batteryFont=statusFont=null;startupCheck=null;window=null;};
      batteryFont=PanelFont("Segoe UI",17,FontStyle.Bold);
      statusFont=PanelFont("Segoe UI",10,FontStyle.Bold);

      var hero=new FrostCard{Hero=true,Left=20,Top=44,Width=410,Height=114};
      hero.Controls.Add(new Label{Text=L.T("Battery"),Left=17,Top=12,Width=371,Height=39,Font=PanelFont("Segoe UI",24,FontStyle.Bold)});
      hero.Controls.Add(new Label{Text=L.T("Battery monitoring for your devices"),Left=19,Top=60,Width=190,Height=43,Font=PanelFont("Segoe UI",9)});
      var detect=new RoundedButton{Text=L.T("Detect devices"),Left=224,Top=60,Width=168,Height=38,Tag="primary"};
      detect.Click+=(s,e)=>{ShowSettings(false);link.Detect.Set();};
      hero.Controls.Add(detect);
      window.Controls.Add(hero);
      var navigation=new FrostCard{Left=20,Top=496,Width=410,Height=48,Anchor=AnchorStyles.Left|AnchorStyles.Bottom};
      var overviewTab=new RoundedButton{Tag="overview-tab",Text=L.T("Overview"),Left=8,Top=6,Width=128,Height=36};
      var settingsTab=new RoundedButton{Tag="settings-tab",Text=L.T("Settings"),Left=140,Top=6,Width=130,Height=36};
      var themesTab=new RoundedButton{Tag="themes-tab",Text=L.T("Themes"),Left=274,Top=6,Width=128,Height=36};
      overviewTab.Click+=(s,e)=>ShowSettings(false);
      settingsTab.Click+=(s,e)=>ShowSettings(true);
      themesTab.Click+=(s,e)=>{settingsPanel.Visible=false;overviewScroll.Visible=false;themesPanel.Visible=true;ApplyTheme();};
      navigation.Controls.AddRange(new Control[]{overviewTab,settingsTab,themesTab});
      window.Controls.Add(navigation);
      overviewScroll=new GlassScrollPanel{Left=0,Top=170,Width=450,Height=314,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Bottom};
      overviewPanel=overviewScroll.Content;
      settingsPanel=new FrostCard{Left=20,Top=170,Width=410,Height=309,Visible=false};
      window.Controls.Add(overviewScroll);window.Controls.Add(settingsPanel);
      deluxValue=AddRow(overviewPanel,"DELUX M800 Mini",8,out deluxFill); jblValue=AddRow(overviewPanel,"JBL Quantum 650",158,out jblFill);
      AddVisibility(deluxValue.Parent,"mouse");AddVisibility(jblValue.Parent,"headset");
      var simulateButton=new RoundedButton{Text=L.T("Test battery notification"),Left=24,Top=173,Width=240,Height=34,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(61,27,32),ForeColor=Color.FromArgb(255,235,235),Font=PanelFont("Segoe UI",9,FontStyle.Bold),Cursor=Cursors.Hand};
      simulateButton.Click+=(s,e)=>ShowLowBatteryNotice("10%");simulateButton.Top=151;settingsPanel.Controls.Add(simulateButton);
      footer=new Label{Text=L.T("Looking for 2.4 GHz receivers..."),Left=25,Top=372,Width=395,Height=34,ForeColor=Color.FromArgb(199,168,170),Font=PanelFont("Segoe UI",9)}; overviewPanel.Controls.Add(footer);
      overviewScroll.ContentHeight=footer.Bottom+8;
      BuildSettingsPanel();BuildThemes();AddLanguageSelector();AddCustomTitleBar();
      ApplyTheme();
    }
    void ReleaseWindow(){if(window!=null)window.Dispose();}
    protected override void ExitThreadCore(){
      stopping=true;
      if(readerStopEvent!=IntPtr.Zero)Win32.SetEvent(readerStopEvent);
      if(applicationTray!=null)applicationTray.Dispose();
      foreach(var icon in bluetoothIcons.Values)icon.Dispose();
      foreach(var icon in receiverTrayIcons.Values)icon.Dispose();
      bridgeTimer.Dispose();link.Dispose();
      ReleaseWindow();
      if(trayMouse!=null){trayMouse.Dispose();trayHeadset.Dispose();menu.Dispose();}
      if(trayMouseIcon!=null)trayMouseIcon.Dispose();
      if(trayHeadsetIcon!=null)trayHeadsetIcon.Dispose();
      appIcon.Dispose();bodyFont.Dispose();
      Thread[] pending;
      lock(handles){foreach(IntPtr h in handles)Win32.CancelIoEx(h,IntPtr.Zero);pending=readers.ToArray();}
      bool completed=true;foreach(Thread reader in pending)if(!reader.Join(2000))completed=false;
      if(completed)lock(handles){foreach(IntPtr h in handles)Win32.CloseHandle(h);handles.Clear();if(readerStopEvent!=IntPtr.Zero)Win32.CloseHandle(readerStopEvent);}
      dispatcher.Dispose();base.ExitThreadCore();
    }
    Font PanelFont(string family,float size,FontStyle style=FontStyle.Regular){var font=new Font(family,size,style);panelResources.Add(font);return font;}
    Image LoadPanelLogo(){var image=Image.FromFile(System.IO.Path.Combine(Application.StartupPath,"assets","volti-icon.png"));panelResources.Add(image);return image;}
    void RoundWindow(){
      if(window.ClientSize.Width<24||window.ClientSize.Height<24)return;
      using(var path=new System.Drawing.Drawing2D.GraphicsPath()){
        int w=window.ClientSize.Width,h=window.ClientSize.Height;
        path.AddArc(0,0,22,22,180,90);
        path.AddArc(w-22,0,22,22,270,90);
        path.AddArc(w-22,h-22,22,22,0,90);
        path.AddArc(0,h-22,22,22,90,90);
        path.CloseFigure();
        var previous=window.Region;
        window.Region=new Region(path);
        if(previous!=null)previous.Dispose();
      }
    }
    void AddCustomTitleBar(){
      var bar=new Panel{Left=0,Top=0,Width=450,Height=36,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,BackColor=Color.FromArgb(5,5,7)};
      var mark=new PictureBox{Left=10,Top=9,Width=18,Height=18,SizeMode=PictureBoxSizeMode.Zoom,Image=LoadPanelLogo()};
      var title=new Label{Text="Volti",Left=34,Top=0,Width=280,Height=36,ForeColor=Color.FromArgb(255,240,241),Font=PanelFont("Segoe UI",9),TextAlign=ContentAlignment.MiddleLeft};
      bar.Controls.Add(mark);bar.Controls.Add(title);
      var minimize=new RoundedButton{Text="–",AccessibleName=L.T("Minimize"),Left=352,Top=6,Width=36,Height=28};
      var close=new RoundedButton{Text="×",AccessibleName=L.T("Close"),Left=394,Top=6,Width=36,Height=28};
      minimize.Click+=(sender,args)=>window.WindowState=FormWindowState.Minimized;
      close.Click+=(sender,args)=>window.Close();
      bar.Controls.Add(minimize);
      bar.Controls.Add(close);
      MouseEventHandler drag=(s,e)=>{if(e.Button==MouseButtons.Left){Win32.ReleaseCapture();Win32.SendMessage(window.Handle,0x00A1,new IntPtr(2),IntPtr.Zero);}};
      bar.MouseDown+=drag;mark.MouseDown+=drag;title.MouseDown+=drag;
      window.Controls.Add(bar);bar.BringToFront();
    }
    void ShowPanel(){
      bool opening=window==null;
      if(opening)BuildWindow();
      window.Show();
      if(window.WindowState==FormWindowState.Minimized)window.WindowState=FormWindowState.Normal;
      if(opening)ApplyTheme();
      window.Activate();
    }
    void LaunchPanel(){
      try{
        var path=System.IO.Path.Combine(Application.StartupPath,"Volti.UI.exe");
        using(var process=Process.Start(new ProcessStartInfo(path){UseShellExecute=false})){}
      }catch(Exception ex){trayHeadset.ShowBalloonTip(5000,"Volti",ex.Message,ToolTipIcon.Warning);}
    }
    void Publish(){link.Publish(readings[0],readings[1],receiverStatus,DiscoveredSnapshot());}
    void LoadLimits(){
      for(int i=0;i<2;i++){
        lowLimits[i]=Math.Max(2,LoadLimit(i,"Low",20));
        criticalLimits[i]=Math.Min(lowLimits[i]-1,LoadLimit(i,"Critical",5));
      }
    }
    void SyncProcesses(){
      if(panelProcess){
        string[] snapshot=link.Read();
        if(link.Activate.WaitOne(0))ShowPanel();
        if(snapshot==null||snapshot[2].Length==0||window==null)return;
        if(readings[0]==snapshot[0]&&readings[1]==snapshot[1]&&receiverStatus==snapshot[2]&&bluetoothSnapshot==snapshot[3])return;
        readings[0]=snapshot[0];readings[1]=snapshot[1];receiverStatus=snapshot[2];
        bluetoothSnapshot=snapshot[3];
        RefreshPanel();
      }else{
        bool manual=link.Detect.WaitOne(0);
        if(manual)new Thread(Connect){IsBackground=true,Name="Receiver discovery"}.Start();
        if(manual||DateTime.UtcNow>=nextBluetoothScan){DetectBluetooth();DetectHyperX();}
        if(manual||DateTime.UtcNow>=nextSynapseScan)DetectSynapse();
        if(link.Preferences.WaitOne(0)){
          string oldTheme=ThemeName,oldLanguage=L.Language;
          LoadTheme();L.Load();LoadLimits();
          RefreshVisibility();
          openItem.Text=L.T("Open Volti");exitItem.Text=L.T("Exit");taskbarItem.Text=L.T("Windows taskbar settings");
          if(oldTheme!=ThemeName||oldLanguage!=L.Language)ApplyTheme();
          if(oldTheme!=ThemeName)RefreshDiscoveredTray();
          CheckAlert(false,readings[0]);CheckAlert(true,readings[1]);
        }
        if(link.Preview.WaitOne(0))ShowLowBatteryNotice("");
      }
    }
    void RefreshPanel(){deluxValue.Text=L.T(readings[0]);jblValue.Text=L.T(readings[1]);footer.Text=L.T("No battery readings yet");UpdateBatteryVisuals();RenderBluetooth();}
    void BuildSettingsPanel(){
      settingsPanel.Controls.Add(new Label{Text=L.T("Settings"),Left=24,Top=15,Width=362,Height=30,Font=PanelFont("Segoe UI",14,FontStyle.Bold)});
      startupCheck=new CheckBox{Text=L.T("Start with Windows"),Left=25,Top=65,Width=360,Height=28,Checked=StartupTaskExists()};
      startupCheck.CheckedChanged+=(s,e)=>{if(!updatingStartup)SetStartupTask(startupCheck.Checked);};settingsPanel.Controls.Add(startupCheck);
      settingsPanel.Controls.Add(new Label{Text=L.T("Listed in Windows Startup apps."),Left=25,Top=100,Width=360,Height=40,Font=PanelFont("Segoe UI",9)});
    }
    void AddLanguageSelector(){
      var language=new RoundedButton{Text=L.T("Language"),Left=24,Top=210,Width=150,Height=35};settingsPanel.Controls.Add(language);
      var languages=new ContextMenuStrip();panelResources.Add(languages);
      foreach(var entry in new[]{new[]{"en","English"},new[]{"pt-BR","Português (Brasil)"},new[]{"zh-CN","简体中文"}}){
        string selected=entry[0];var item=new ToolStripMenuItem(entry[1]);
        item.Click+=(s,e)=>{L.SetLanguage(selected);L.Apply(window);RefreshPanel();link.Preferences.Set();window.Invalidate(true);};
        languages.Items.Add(item);
      }
      language.Click+=(s,e)=>{languages.Renderer=new BatteryMenuRenderer();languages.BackColor=Surface;foreach(ToolStripItem item in languages.Items)item.ForeColor=Ink;languages.Show(language,new Point(0,language.Height));};
    }
    void ShowSettings(bool show){themesPanel.Visible=false;overviewScroll.Visible=!show;settingsPanel.Visible=show;ApplyTheme();}
    static string ThemeName="GymOrganizer";
    Panel themesPanel;
    internal static Color Surface, Ink, Card, Accent;
    void LoadTheme(){
      try{using(var k=Registry.CurrentUser.OpenSubKey(SettingsKey)){if(k!=null)ThemeName=(string)k.GetValue("Theme","GymOrganizer");}}catch{}
    }
    void BuildThemes(){
      themesPanel=new Panel{Left=20,Top=170,Width=410,Height=309,Visible=false};window.Controls.Add(themesPanel);
      string[] names={"GymOrganizer","Black","White","Gruvbox","Nord","Frutiger Aero","Dracula"};
      string[][] palettes={new[]{"#171722","#302b42","#bd93f9","#50fa7b"},new[]{"#202020","#323232","#0078d4","#ffffff"},new[]{"#f3f3f3","#ffffff","#0078d4","#202020"},new[]{"#282828","#3c3836","#d79921","#ebdbb2"},new[]{"#2e3440","#3b4252","#88c0d0","#eceff4"},new[]{"#c8eefb","#ecfcff","#007ea7","#287842"},new[]{"#282A36","#44475A","#BD93F9","#50FA7B"}};
      for(int i=0;i<names.Length;i++){
        string name=names[i];
        var tile=new RoundedButton{Text=L.T(name),Tag=name,Left=3+(i%2)*206,Top=5+(i/2)*75,Width=198,Height=69,
          Swatches=Array.ConvertAll(palettes[i],ColorTranslator.FromHtml)};
        tile.Click+=(s,e)=>{ThemeName=name;using(var k=Registry.CurrentUser.CreateSubKey(SettingsKey))k.SetValue("Theme",name);ApplyTheme();link.Preferences.Set();};themesPanel.Controls.Add(tile);
      }
    }
    internal static bool IsAeroTheme {get {return ThemeName=="Frutiger Aero";}}
    void ApplyTheme(){
      bool light=ThemeName=="White";string bg=light?"#f3f3f3":"#202020",fg=light?"#202020":"#ffffff",card=light?"#ffffff":"#323232",accent="#0078d4";
      if(ThemeName=="GymOrganizer"){bg="#171722";fg="#f8f8f2";card="#302b42";accent="#bd93f9";}
      if(ThemeName=="Gruvbox"){bg="#282828";fg="#ebdbb2";card="#3c3836";accent="#d79921";}
      if(ThemeName=="Nord"){bg="#2e3440";fg="#eceff4";card="#3b4252";accent="#88c0d0";}
      if(ThemeName=="Dracula"){bg="#282A36";fg="#F8F8F2";card="#44475A";accent="#BD93F9";}
      if(IsAeroTheme){bg="#c8eefb";fg="#11384e";card="#ecfcff";accent="#007ea7";}
      Surface=ColorTranslator.FromHtml(bg);Ink=ColorTranslator.FromHtml(fg);Card=ColorTranslator.FromHtml(card);Accent=ColorTranslator.FromHtml(accent);
      if(ThemeName=="Black"||light)try{using(var k=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\DWM")){if(k!=null){int n=unchecked((int)Convert.ToUInt32(k.GetValue("AccentColor",0xffd47800)));Accent=Color.FromArgb(n&255,(n>>8)&255,(n>>16)&255);}}}catch{}
      if(window!=null){PaintTheme(window);UpdateBatteryVisuals();RenderBluetooth();window.Invalidate(true);}
      if(menu!=null){menu.BackColor=Surface;menu.ForeColor=Ink;UpdateTray();}
    }
    void PaintTheme(Control root){
      root.BackColor=root is FrostCard || root is Label || root is ProductGlyph || root is PictureBox?Color.Transparent:root is Button || root is FrostCard?Card:root.Parent is FrostCard?Card:Surface;root.ForeColor=Ink;
      var rounded=root as RoundedButton;
      if(rounded!=null){
        rounded.BackColor=Color.Transparent;
        rounded.Selected=rounded.Swatches!=null?Equals(rounded.Tag,ThemeName):
          Equals(rounded.Tag,"overview-tab")&&overviewScroll.Visible||
          Equals(rounded.Tag,"settings-tab")&&settingsPanel.Visible||
          Equals(rounded.Tag,"themes-tab")&&themesPanel.Visible;
      }
      Button button=root as Button;if(button!=null){button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderColor=Accent;if((string)button.Tag=="settings-gear"){button.FlatAppearance.BorderSize=0;button.BackColor=Surface;}button.FlatAppearance.MouseOverBackColor=Mix(Card,Accent,0.18f);button.FlatAppearance.MouseDownBackColor=Mix(Card,Accent,0.30f);}
      foreach(Control child in root.Controls)PaintTheme(child);
      var picker=root as ThresholdPicker;if(picker!=null)picker.ApplyPalette();
    }
    internal static Color Mix(Color a,Color b,float t){return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}

    const string SettingsKey="Software\\Volti";
    readonly int[] lowLimits=new int[2],criticalLimits=new int[2],alertStates=new int[2];
    void AddAlerts(Panel card,bool headset){
      int i=headset?1:0;
      var lowLabel=new Label{Text=L.T("Low (%)"),Left=16,Top=70,Width=140,Height=22};
      var criticalLabel=new Label{Text=L.T("Critical (%)"),Left=200,Top=70,Width=150,Height=22};
      var low=new ThresholdPicker(16,lowLabel.Text);
      var critical=new ThresholdPicker(200,criticalLabel.Text);
      Action refresh=()=>{low.SetRange(criticalLimits[i]+1,100,lowLimits[i]);critical.SetRange(1,lowLimits[i]-1,criticalLimits[i]);};
      refresh();
      low.Committed+=number=>{SaveLimit(i,"Low",number);lowLimits[i]=number;refresh();link.Preferences.Set();};
      critical.Committed+=number=>{SaveLimit(i,"Critical",number);criticalLimits[i]=number;refresh();link.Preferences.Set();};
      card.Controls.AddRange(new Control[]{lowLabel,criticalLabel,low,critical});
    }
    static int LoadLimit(int device,string level,int fallback){try{using(var k=Registry.CurrentUser.OpenSubKey(SettingsKey)){return Math.Max(1,Math.Min(100,Convert.ToInt32(k==null?fallback:k.GetValue(device+level,fallback))));}}catch{return fallback;}}
    static void SaveLimit(int device,string level,int value){using(var k=Registry.CurrentUser.CreateSubKey(SettingsKey))k.SetValue(device+level,value);}
    void CheckAlert(bool headset,string value){int p;if(!TryPercent(value,out p))return;int i=headset?1:0;int state=p<=criticalLimits[i]?2:p<=lowLimits[i]?1:0;int previous=alertStates[i];alertStates[i]=state;if(state==0||state<=previous)return;
      var tray=VisibleTray();tray.ShowBalloonTip(6000,(state==2?L.T("Critical battery — "):L.T("Low battery — "))+(headset?"JBL Quantum 650":"DELUX M800 Mini"),L.T("Battery at ")+value+L.T(". Connect your device to charge."),ToolTipIcon.Warning);
    }
    Label AddRow(Panel parent,string name,int y,out Panel fill) {
      var card=new FrostCard{Left=20,Top=y,Width=400,Height=140,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,BackColor=Color.FromArgb(42,23,28)};
      parent.Controls.Add(card);
      card.Controls.Add(new ProductGlyph{Headset=name.StartsWith("JBL",StringComparison.Ordinal),Left=14,Top=12,Width=24,Height=26});
      card.Controls.Add(new Label{Text=name,Left=48,Top=10,Width=194,Height=22,Font=PanelFont("Segoe UI",10,FontStyle.Bold),ForeColor=Color.FromArgb(226,222,236)});
      var value=new Label{Text="—",Left=259,Top=5,Width=125,Height=32,Anchor=AnchorStyles.Top|AnchorStyles.Right,TextAlign=ContentAlignment.MiddleRight,Font=batteryFont,ForeColor=VoltiBrand.RedSoft}; card.Controls.Add(value);
      var track=new Panel{Left=16,Top=44,Width=368,Height=5,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,BackColor=Color.FromArgb(91,49,54)}; card.Controls.Add(track);
      fill=new Panel{Left=0,Top=0,Width=0,Height=5,BackColor=VoltiBrand.Red}; track.Controls.Add(fill);
      AddAlerts(card,name.StartsWith("JBL",StringComparison.Ordinal));return value;
    }
    void Set(bool headset,string value){
      int i=headset?1:0;
      lock(readingLock){if(queuedReadings[i]==value)return;queuedReadings[i]=value;}
      Post(()=>{
        readings[i]=value;
        if(window!=null){(headset?jblValue:deluxValue).Text=L.T(value);UpdateBatteryVisuals();}
        Publish();UpdateTray();RefreshVisibility();CheckAlert(headset,value);
      });
    }
    void Post(Action update){
      if(stopping)return;
      try{dispatcher.BeginInvoke((Action)(()=>{if(!stopping)update();}));}catch(InvalidOperationException){}
    }
    NotifyIcon VisibleTray(){
      if(applicationTray.Visible)return applicationTray;
      if(trayMouse.Visible)return trayMouse;
      if(trayHeadset.Visible)return trayHeadset;
      foreach(var icon in bluetoothIcons.Values)if(icon.Visible)return icon;
      return applicationTray;
    }
    void ShowLowBatteryNotice(string value){if(panelProcess){link.Preview.Set();return;}VisibleTray().ShowBalloonTip(6000,L.T("Test notification"),L.T("This is a low battery notification preview."),ToolTipIcon.Warning);}
    static bool TryPercent(string value,out int percent){percent=0;int i=value.IndexOf('%');return i>0&&int.TryParse(value.Substring(0,i).Trim(),out percent);}
    void SetFill(Panel fill,Label value){int p;if(!TryPercent(value.Text,out p)){fill.Width=0;value.ForeColor=Ink;value.Font=statusFont;return;}value.Font=batteryFont;Panel track=fill.Parent as Panel;if(track!=null){fill.Width=(track.ClientSize.Width*p)/100;}Color level=BatteryColor(p);fill.BackColor=level;value.ForeColor=level;}
    void UpdateBatteryVisuals(){
      deluxFill.Parent.BackColor=Mix(Card,Ink,0.14f);
      jblFill.Parent.BackColor=Mix(Card,Ink,0.14f);
      SetFill(deluxFill,deluxValue);
      SetFill(jblFill,jblValue);
    }
    const string RunKey="Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    static bool StartupTaskExists(){try{using(var key=Registry.CurrentUser.OpenSubKey(RunKey)){return key!=null&&key.GetValue("Volti")!=null;}}catch(System.Security.SecurityException){return false;}}
    void SetStartupTask(bool enable){
      try{
        using(var key=Registry.CurrentUser.CreateSubKey(RunKey)){if(enable)key.SetValue("Volti","\""+System.IO.Path.Combine(Application.StartupPath,"Volti.exe")+"\"");else key.DeleteValue("Volti",false);}
      }catch(Exception ex){updatingStartup=true;startupCheck.Checked=StartupTaskExists();updatingStartup=false;MessageBox.Show(window,ex.Message,L.T("Volti startup"),MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
    void Note(string value){Post(()=>{receiverStatus=value;trayStatus.Text=L.T(value);Publish();});}
    void UpdateTray(){
      string d=L.T(readings[0]), j=L.T(readings[1]);trayStatus.Text=L.T(receiverStatus);
      trayDelux.Text="DELUX M800 Mini: "+d; trayJbl.Text="JBL Quantum 650: "+j;
      trayMouse.Text=TrayTip("Mouse DELUX M800 Mini: "+d);trayHeadset.Text=TrayTip("Headset JBL Quantum 650: "+j);
      UpdateTrayIcon(trayMouse,ref trayMouseIcon,d,false);UpdateTrayIcon(trayHeadset,ref trayHeadsetIcon,j,true);
    }
    static string TrayTip(string value){return value.Length>63?value.Substring(0,63):value;}
    void UpdateTrayIcon(NotifyIcon item,ref Icon current,string value,bool headset){Icon next=CreateTrayBatteryIcon(value,headset);Icon previous=current;current=next;item.Icon=next;if(previous!=null)previous.Dispose();}
    static Color BatteryColor(int percent){int p=Math.Max(0,Math.Min(100,percent));Color red=Color.FromArgb(242,70,74),yellow=Color.FromArgb(255,196,60),green=Color.FromArgb(58,205,110);if(ThemeName=="Gruvbox"){red=ColorTranslator.FromHtml("#cc241d");yellow=ColorTranslator.FromHtml("#d79921");green=ColorTranslator.FromHtml("#98971a");}if(ThemeName=="Nord"){red=ColorTranslator.FromHtml("#bf616a");yellow=ColorTranslator.FromHtml("#ebcb8b");green=ColorTranslator.FromHtml("#a3be8c");}if(ThemeName=="Dracula"){red=ColorTranslator.FromHtml("#FF5555");yellow=ColorTranslator.FromHtml("#F1FA8C");green=ColorTranslator.FromHtml("#50FA7B");}if(IsAeroTheme){red=Color.FromArgb(191,55,52);yellow=Color.FromArgb(161,102,0);green=Color.FromArgb(32,122,58);}Color a=p<=50?red:yellow;Color b=p<=50?yellow:green;double t=p<=50?p/50.0:(p-50)/50.0;return Color.FromArgb((int)Math.Round(a.R+(b.R-a.R)*t),(int)Math.Round(a.G+(b.G-a.G)*t),(int)Math.Round(a.B+(b.B-a.B)*t));}
    static Icon CreateTrayBatteryIcon(string value,bool headset){
      int percent;bool known=TryPercent(value,out percent);Color level=known?BatteryColor(percent):Color.FromArgb(130,132,143);
      using(Bitmap bitmap=new Bitmap(32,32,System.Drawing.Imaging.PixelFormat.Format32bppArgb)){
        using(Graphics g=Graphics.FromImage(bitmap))using(SolidBrush device=new SolidBrush(level))using(Pen outline=new Pen(level,1.7f))using(Pen detail=new Pen(Color.FromArgb(155,22,24,29),1.5f)){
          g.Clear(Color.Transparent);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
          DeviceArt.Draw(g,headset,level,new Rectangle(0,0,32,32));
        }
        IntPtr handle=bitmap.GetHicon();try{using(Icon source=Icon.FromHandle(handle)){return (Icon)source.Clone();}}finally{Win32.DestroyIcon(handle);}
      }
    }
    void Connect(){


      var paths=HyperXBattery.Paths();
      Note("Ready");
      bool legacyMousePresent=Array.Exists(paths,path=>path.IndexOf("VID_1D57&PID_FA60&MI_02&COL03",StringComparison.OrdinalIgnoreCase)>=0);
      foreach(string path in paths) {
        if(stopping) return;
        string u=path.ToUpperInvariant();
        bool mouse=u.Contains("VID_1D57&PID_FA60&MI_02&COL03");
        bool jbl=u.Contains("VID_0ECB&PID_2125&MI_05&COL04");
        if(u.Contains("VID_24AE&PID_1871&") || u.Contains("VID_24AE&PID_1871#"))
          StartReader(path,()=>ReadPassiveReceiver(path,"Rapoo MT760 Mini",0xFF00,0x0002,7,ReceiverReports.Rapoo));
        if(u.Contains("VID_0ECB&PID_2088&") || u.Contains("VID_0ECB&PID_2088#"))
          StartReader(path,()=>ReadPassiveReceiver(path,"JBL Quantum 910 Wireless",0xFF13,0x0001,0,ReceiverReports.Quantum910));
        if(mouse){StartReader(path,()=>ReadSharedMouse(path,true));}
        if(!legacyMousePresent&&(u.Contains("VID_1D57&PID_FA60&")||u.Contains("VID_1D57&PID_FA60#")))StartReader(path,()=>ReadSharedMouse(path,false));
        if(jbl){StartReader(path,()=>ReadJbl(path));}
      }
    }
    readonly HashSet<string> activeReaders=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    void StartReader(string path,ThreadStart action){
      lock(handles){
        if(stopping||!activeReaders.Add(path))return;
        var reader=new Thread(()=>{
          try{action();}
          finally{lock(handles){activeReaders.Remove(path);readers.Remove(Thread.CurrentThread);}}
        }){IsBackground=true,Name="Volti HID reader"};
        readers.Add(reader);
        reader.Start();
      }
    }
    IntPtr Open(string path,bool overlapped,uint access=0xC0000000){
      lock(handles){if(stopping)return new IntPtr(-1);IntPtr h=Win32.CreateFile(path,access,3,IntPtr.Zero,3,overlapped?0x40000000u:0u,IntPtr.Zero);if(h!=new IntPtr(-1))handles.Add(h);return h;}
    }
    int FeatureLength(IntPtr h,out int inputLength){
      inputLength=64;
      IntPtr pp=IntPtr.Zero;
      try { if(!Win32.HidD_GetPreparsedData(h,out pp))return 64; var caps=new byte[128]; if(Win32.HidP_GetCaps(pp,caps)<0)return 64; int il=caps[4]|(caps[5]<<8); if(il>=9&&il<=512)inputLength=il; int n=caps[8]|(caps[9]<<8); return n>=11&&n<=512?n:64; }
      finally {if(pp!=IntPtr.Zero)Win32.HidD_FreePreparsedData(pp);}
    }
    void ReadSharedMouse(string path,bool legacy){
      IntPtr handle=Open(path,true,0x80000000);
      if(handle==new IntPtr(-1)){if(legacy)Set(false,"Access denied");return;}
      var report=new byte[5];
      string lastModel=null, publishedModel=null;
      int publishedPercent=-1;
      try {
        int page, usage, length;
        if(!legacy&&(!TryInputCaps(handle,out page,out usage,out length)||page!=0x000A||length!=5))return;
        while(!stopping) {
          uint read;
          int result=Win32.ReadWithTimeout(handle,report,out read,1000,readerStopEvent);
          if(result<0)break;
          if(result==0)continue;
          // M800 Mini: 03 29 40 01 NN, with NN already in percent.
          if(legacy&&read==5&&report[0]==0x03&&report[1]==0x29&&report[2]==0x40&&report[4]<=100) {
            lastModel=null;
            if(publishedModel!=null){SetReceiverBattery(path,null,0);publishedModel=null;}
            Set(false,report[4]+"%");
            continue;
          }
          string model;
          int percent;
          if(!ReceiverReports.TrySharedMouse(report,read,out model,out percent))continue;
          // A generic Beken receiver can be paired with different mouse models.
          if(lastModel==model&&(publishedModel!=model||publishedPercent!=percent)){
            if(legacy)Set(false,"-");
            SetReceiverBattery(path,model,percent);
            publishedModel=model;
            publishedPercent=percent;
          }
          lastModel=model;
        }
      } finally {
        ClosePassiveReader(handle);
        if(!stopping){SetReceiverBattery(path,null,0);if(legacy)Set(false,"Disconnected");}
      }
    }
    void ReadJbl(string path){
      IntPtr h=Open(path,true); if(h==new IntPtr(-1)){Set(true,"Access denied");return;}
      int inputLength; int length=FeatureLength(h,out inputLength); byte[] input=new byte[inputLength]; bool gotBattery=false;
      DateTime nextQuery=DateTime.MinValue;
      while(!stopping){
        DateTime now=DateTime.UtcNow;
        if(now>=nextQuery){
          byte[] request=new byte[length];
          // QuantumENGINE HID v3: GET command 1, route DD00, battery FeatureID 000D.
          request[0]=0x88; request[1]=0x00; request[2]=0xDD;
          request[3]=0x01; request[4]=0x00; request[5]=0x01; request[6]=0x00;
          request[7]=0x02; request[8]=0x00; request[9]=0x0D; request[10]=0x00;
          nextQuery=now.AddSeconds(60);
          gotBattery=false;
          if(!Win32.HidD_SetFeature(h,request,(uint)request.Length)&&!stopping&&!gotBattery)Set(true,"No response");
        }
        double remaining=(nextQuery-DateTime.UtcNow).TotalMilliseconds;
        uint timeout=(uint)Math.Max(1,Math.Min(60000,remaining));
        uint n; int read=Win32.ReadWithTimeout(h,input,out n,timeout); if(read<0){if(!stopping&&!gotBattery)Set(true,"No response");break;} if(read==0){if(!gotBattery)Set(true,"No response");continue;} if(n<10)continue;
        // A GET reply carries status (2 bytes), FeatureID (2), value length (2), then the value.
        ushort route=(ushort)(input[1]|(input[2]<<8)); ushort command=(ushort)(input[3]|(input[4]<<8)); ushort payload=(ushort)(input[7]|(input[8]<<8));
        if(input[0]==0x86&&route==0xDD00&&command==0x0001&&payload>=7&&n>=(uint)(9+payload)){
          ushort status=(ushort)(input[9]|(input[10]<<8)); ushort feature=(ushort)(input[11]|(input[12]<<8)); ushort valueLength=(ushort)(input[13]|(input[14]<<8));
          if((status&0x8000)==0&&feature==0x000D&&valueLength==1){
            byte info=input[15]; byte percent=(byte)(info&0x7F); if(percent<=100){gotBattery=true;Set(true,percent+"%"+((info&0x80)!=0?"  ⚡":""));}
          }
        }
      }
      ClosePassiveReader(h);
      if(!stopping)Set(true,"Disconnected");
    }
  }
  static class Program {
    [STAThread] static void Main(){
#if VOLTI_PANEL
      bool panel=true;
#else
      bool panel=false;
#endif
      bool owner;
      using(var instance=new Mutex(true,panel?"Local\\Volti.Panel":"Local\\Volti.App",out owner)){
        if(!owner){if(panel)using(var link=new PanelLink())link.Activate.Set();return;}
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm(panel));
      }
    }
  }
}
