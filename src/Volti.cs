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
[assembly: System.Reflection.AssemblyVersion("0.1.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersion("0.1.0-preview.1")]
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
    public FrostCard(){DoubleBuffered=true;ResizeRedraw=true;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
      using(var brush=new System.Drawing.Drawing2D.LinearGradientBrush(ClientRectangle,MainForm.Mix(MainForm.Card,MainForm.Ink,0.07f),MainForm.Card,90f))e.Graphics.FillRoundedRectangle(brush,1,1,Width-3,Height-3,20);
      using(var pen=new Pen(MainForm.Mix(MainForm.Card,MainForm.Ink,0.20f)))e.Graphics.DrawRoundedRectangle(pen,1,1,Width-3,Height-3,20);
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
    [DllImport("kernel32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool ReadFile(IntPtr file, byte[] buffer, uint count, out uint read, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool CancelIoEx(IntPtr file, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr CreateEvent(IntPtr security, bool manualReset, bool initialState, string name);
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
        var paths=new List<string>();
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
    internal static int ReadWithTimeout(IntPtr file, byte[] buffer, out uint read, uint timeout) {
      read=0; IntPtr ev=CreateEvent(IntPtr.Zero,true,false,null); if(ev==IntPtr.Zero)return -1;
      IntPtr ov=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Overlapped)));
      try {
        Overlapped value=new Overlapped(); value.Event=ev; Marshal.StructureToPtr(value,ov,false);
        if(ReadFile(file,buffer,(uint)buffer.Length,out read,ov))return 1;
        if(Marshal.GetLastWin32Error()!=997)return -1;
        uint wait=WaitForSingleObject(ev,timeout);
        if(wait==0)return GetOverlappedResult(file,ov,out read,false)?1:-1;
        if(wait==0x102){CancelIoEx(file,ov); if(GetOverlappedResult(file,ov,out read,true))return 1; return 0;}
        CancelIoEx(file,ov);GetOverlappedResult(file,ov,out read,true);return -1;
      } finally { Marshal.FreeHGlobal(ov); CloseHandle(ev); }
    }
  }

  sealed class MainForm : Form {
    readonly Label deluxValue, jblValue, footer;
    readonly Panel deluxFill, jblFill;
    readonly Panel overviewPanel, settingsPanel;
    readonly NotifyIcon trayMouse, trayHeadset;
    readonly ToolStripMenuItem trayDelux, trayJbl, trayStatus;
    CheckBox startupCheck;
    readonly Button overviewTab, settingsTab;
    readonly Icon appIcon;
    Icon trayMouseIcon, trayHeadsetIcon;

    bool exitRequested;
    bool updatingStartup;
    readonly List<IntPtr> handles=new List<IntPtr>();
    readonly List<Thread> readers=new List<Thread>();
    readonly object labelLock=new object();
    readonly Dictionary<Label,string> shownValues=new Dictionary<Label,string>();
    volatile bool stopping;
    public MainForm() {
      appIcon=VoltiBrand.CreateIcon();

      L.Load();Text="Volti"; Icon=appIcon; FormBorderStyle=FormBorderStyle.None; Width=450; Height=560; MinimumSize=new Size(450,560); MaximumSize=new Size(450,560); StartPosition=FormStartPosition.CenterScreen;
      BackColor=Color.FromArgb(20,12,15); ForeColor=Color.White; Font=new Font("Segoe UI",10);
      var accent=new Panel{Left=0,Top=36,Width=450,Height=3,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,BackColor=VoltiBrand.Red}; Controls.Add(accent);
      var logo=new PictureBox{Left=23,Top=54,Width=42,Height=42,SizeMode=PictureBoxSizeMode.Zoom,Image=Image.FromFile(System.IO.Path.Combine(Application.StartupPath,"assets","volti-icon.png"))}; Controls.Add(logo);
      Controls.Add(new Label{Text="Volti",Left=74,Top=55,Width=330,Height=25,ForeColor=Color.FromArgb(255,242,242),Font=new Font("Segoe UI Semibold",12,FontStyle.Bold)});
      Controls.Add(new Label{Text=L.T("Battery monitoring for your devices"),Left=75,Top=80,Width=330,Height=20,ForeColor=Color.FromArgb(190,164,166),Font=new Font("Segoe UI",9)});
      overviewTab=new Button{Text=L.T("Overview"),Left=24,Top=108,Width=126,Height=31,FlatStyle=FlatStyle.Flat,BackColor=VoltiBrand.Wine,ForeColor=Color.White,Font=new Font("Segoe UI",9,FontStyle.Bold),Cursor=Cursors.Hand};
      settingsTab=new Button{Text="⚙",Tag="settings-gear",AccessibleName="Settings",Left=24,Top=510,Width=40,Height=34,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(38,22,26),ForeColor=Color.FromArgb(217,180,182),Font=new Font("Segoe UI",9),Cursor=Cursors.Hand};
      overviewTab.FlatAppearance.BorderColor=Color.FromArgb(138,48,57);settingsTab.FlatAppearance.BorderColor=Color.FromArgb(79,40,46);
      overviewTab.Click+=(s,e)=>ShowSettings(false);settingsTab.Click+=(s,e)=>ShowSettings(true);Controls.Add(overviewTab);Controls.Add(settingsTab);
      overviewPanel=new Panel{Left=0,Top=148,Width=450,Height=350,BackColor=Color.Transparent};
      settingsPanel=new Panel{Left=0,Top=148,Width=450,Height=350,BackColor=Color.FromArgb(27,16,20),Visible=false};
      Controls.Add(overviewPanel);Controls.Add(settingsPanel);
      deluxValue=AddRow(overviewPanel,"DELUX M800 Mini",8,out deluxFill); jblValue=AddRow(overviewPanel,"JBL Quantum 650",158,out jblFill);
      var simulateButton=new Button{Text=L.T("Test battery notification"),Left=24,Top=173,Width=240,Height=34,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(61,27,32),ForeColor=Color.FromArgb(255,235,235),Font=new Font("Segoe UI",9,FontStyle.Bold),Cursor=Cursors.Hand};
      simulateButton.FlatAppearance.BorderColor=Color.FromArgb(147,53,62);simulateButton.FlatAppearance.MouseOverBackColor=Color.FromArgb(84,34,41);simulateButton.Click+=(s,e)=>ShowLowBatteryNotice("10%");simulateButton.Top=155;settingsPanel.Controls.Add(simulateButton);
      footer=new Label{Text=L.T("Looking for 2.4 GHz receivers..."),Left=25,Top=308,Width=395,Height=34,ForeColor=Color.FromArgb(199,168,170),Font=new Font("Segoe UI",9)}; overviewPanel.Controls.Add(footer);
      BuildSettingsPanel(); BuildThemes(); AddLanguageSelector(); ApplyTheme();
      var menu=new ContextMenuStrip{Renderer=new BatteryMenuRenderer(),BackColor=Color.FromArgb(32,34,37),ForeColor=Color.FromArgb(242,243,245),Font=new Font("Segoe UI",9),ShowImageMargin=false,ShowCheckMargin=false,Padding=new Padding(0,5,0,5),AutoSize=true};
      trayDelux=new ToolStripMenuItem(L.T("DELUX M800 Mini: searching...")){Enabled=false};
      trayJbl=new ToolStripMenuItem(L.T("JBL Quantum 650: searching...")){Enabled=false};
      trayStatus=new ToolStripMenuItem(L.T("Volti is connecting to receivers...")){Enabled=false};
      menu.Items.Add(trayDelux); menu.Items.Add(trayJbl); menu.Items.Add(new ToolStripSeparator());
      var openItem=new ToolStripMenuItem(L.T("Open Volti")); openItem.Click+=(s,e)=>ShowPanel(); menu.Items.Add(openItem);
      var exitItem=new ToolStripMenuItem(L.T("Exit")); exitItem.Click+=(s,e)=>{exitRequested=true;Close();}; menu.Items.Add(exitItem);
      trayMouseIcon=CreateTrayBatteryIcon(deluxValue.Text,false);trayHeadsetIcon=CreateTrayBatteryIcon(jblValue.Text,true);
      trayMouse=new NotifyIcon{Icon=trayMouseIcon,Text=L.T("DELUX mouse: searching..."),ContextMenuStrip=menu,Visible=true};
      trayHeadset=new NotifyIcon{Icon=trayHeadsetIcon,Text=L.T("JBL headset: searching..."),ContextMenuStrip=menu,Visible=true};
      trayMouse.DoubleClick+=(s,e)=>ShowPanel();trayHeadset.DoubleClick+=(s,e)=>ShowPanel();
      AddCustomTitleBar(); ApplyTheme();
      Shown+=(s,e)=>{BeginInvoke((Action)(()=>{Hide();ShowInTaskbar=false;}));new Thread(Connect){IsBackground=true,Name=L.T("HID discovery")}.Start();};
      FormClosing+=(s,e)=>{
        if(!exitRequested){e.Cancel=true;Hide();ShowInTaskbar=false;return;}
        stopping=true;trayMouse.Visible=false;trayHeadset.Visible=false;trayMouse.Dispose();trayHeadset.Dispose();if(trayMouseIcon!=null)trayMouseIcon.Dispose();if(trayHeadsetIcon!=null)trayHeadsetIcon.Dispose();if(!Object.ReferenceEquals(appIcon,SystemIcons.Application))appIcon.Dispose();
        Thread[] pending;
        lock(handles){foreach(IntPtr h in handles)Win32.CancelIoEx(h,IntPtr.Zero);pending=readers.ToArray();}
        bool completed=true;foreach(Thread reader in pending)if(!reader.Join(2000))completed=false;
        // If a driver stalls cancellation, process termination owns the remaining handles.
        if(completed)lock(handles){foreach(IntPtr h in handles)Win32.CloseHandle(h);handles.Clear();}
      };
    }
    void AddCustomTitleBar(){
      var bar=new Panel{Left=0,Top=0,Width=450,Height=36,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,BackColor=Color.FromArgb(5,5,7)};
      var mark=new PictureBox{Left=10,Top=9,Width=18,Height=18,SizeMode=PictureBoxSizeMode.Zoom,Image=Image.FromFile(System.IO.Path.Combine(Application.StartupPath,"assets","volti-icon.png"))};
      var title=new Label{Text="Volti",Left=34,Top=0,Width=280,Height=36,ForeColor=Color.FromArgb(255,240,241),Font=new Font("Segoe UI",9),TextAlign=ContentAlignment.MiddleLeft};
      bar.Controls.Add(mark);bar.Controls.Add(title);
      Label minimize=new Label{Text="−",Left=358,Top=0,Width=46,Height=36,ForeColor=Color.FromArgb(220,220,224),Font=new Font("Segoe UI",13),TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.Hand};
      Label close=new Label{Text="×",Left=404,Top=0,Width=46,Height=36,ForeColor=Color.FromArgb(220,220,224),Font=new Font("Segoe UI",15),TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.Hand};
      minimize.MouseEnter+=(s,e)=>minimize.BackColor=Card;minimize.MouseLeave+=(s,e)=>minimize.BackColor=Surface;minimize.Click+=(s,e)=>{Hide();ShowInTaskbar=false;};
      close.MouseEnter+=(s,e)=>close.BackColor=Card;close.MouseLeave+=(s,e)=>close.BackColor=Surface;close.Click+=(s,e)=>Close();
      bar.Controls.Add(minimize);bar.Controls.Add(close);bar.Paint+=(s,e)=>{using(var p=new Pen(Card)){e.Graphics.DrawLine(p,0,35,450,35);e.Graphics.DrawLine(p,357,7,357,29);e.Graphics.DrawLine(p,403,7,403,29);}};
      MouseEventHandler drag=(s,e)=>{if(e.Button==MouseButtons.Left){Win32.ReleaseCapture();Win32.SendMessage(Handle,0x00A1,new IntPtr(2),IntPtr.Zero);}};
      bar.MouseDown+=drag;mark.MouseDown+=drag;title.MouseDown+=drag;
      Controls.Add(bar);bar.BringToFront();
    }
    void ShowPanel(){ShowInTaskbar=true;Show();WindowState=FormWindowState.Normal;Activate();}
    void BuildSettingsPanel(){
      settingsPanel.Controls.Add(new Label{Text=L.T("Settings"),Left=24,Top=15,Width=390,Height=30,Font=new Font("Segoe UI",14,FontStyle.Bold)});
      startupCheck=new CheckBox{Text=L.T("Start with Windows"),Left=25,Top=70,Width=390,Height=28,Checked=StartupTaskExists()};
      startupCheck.CheckedChanged+=(s,e)=>{if(!updatingStartup)SetStartupTask(startupCheck.Checked);};settingsPanel.Controls.Add(startupCheck);
      settingsPanel.Controls.Add(new Label{Text=L.T("Listed in Windows Startup apps."),Left=25,Top=106,Width=395,Height=40,Font=new Font("Segoe UI",9)});
    }
    void AddLanguageSelector(){
      var language=new Button{Text=L.T("Language"),Left=160,Top=215,Width=120,Height=28};settingsPanel.Controls.Add(language);
      var languages=new ContextMenuStrip();
      foreach(string code in new[]{"en","pt-BR"}){string selected=code;var item=new ToolStripMenuItem(code=="en"?"English":"Português (Brasil)");item.Click+=(s,e)=>{L.SetLanguage(selected);L.Apply(this);language.Text=L.T("Language");foreach(ToolStripItem entry in trayMouse.ContextMenuStrip.Items)entry.Text=L.Convert(entry.Text);UpdateTray();Invalidate(true);};languages.Items.Add(item);}
      language.Click+=(s,e)=>languages.Show(language,new Point(0,language.Height));
    }
    void ShowSettings(bool show){themesPanel.Visible=false;overviewPanel.Visible=!show;settingsPanel.Visible=show;ApplyTheme();}
    static string ThemeName="Black";
    Panel themesPanel;
    internal static Color Surface, Ink, Card, Accent;
    void BuildThemes(){
      try{using(var k=Registry.CurrentUser.OpenSubKey(SettingsKey)){if(k!=null)ThemeName=(string)k.GetValue("Theme","Black");}}catch{}
      themesPanel=new Panel{Left=0,Top=148,Width=450,Height=350,Visible=false};Controls.Add(themesPanel);
      var themes=new Button{Text=L.T("Themes"),Left=25,Top=215,Width=110,Height=28};settingsPanel.Controls.Add(themes);
      themes.Click+=(s,e)=>{settingsPanel.Visible=false;overviewPanel.Visible=false;themesPanel.Visible=true;};
      string[] names={"Black","White","Gruvbox","Nord"};
      string[][] palettes={new[]{"#202020","#323232","#0078d4","#ffffff"},new[]{"#f3f3f3","#ffffff","#0078d4","#202020"},new[]{"#282828","#3c3836","#d79921","#ebdbb2"},new[]{"#2e3440","#3b4252","#88c0d0","#eceff4"}};
      for(int i=0;i<4;i++){string name=names[i];string[] colors=palettes[i];var tile=new Button{Text=L.T(name)+"   •   "+L.T("Palette"),Left=24+(i%2)*204,Top=15+(i/2)*145,Width=194,Height=125,TextAlign=ContentAlignment.BottomCenter,FlatStyle=FlatStyle.Flat};
        tile.Paint+=(s,e)=>{for(int j=0;j<4;j++)using(var b=new SolidBrush(ColorTranslator.FromHtml(colors[j])))e.Graphics.FillRectangle(b,10+j*43,12,43,52);if(ThemeName==name)using(var p=new Pen(Accent,3))e.Graphics.DrawRectangle(p,2,2,tile.Width-5,tile.Height-5);};
        tile.Click+=(s,e)=>{ThemeName=name;using(var k=Registry.CurrentUser.CreateSubKey(SettingsKey))k.SetValue("Theme",name);ApplyTheme();};themesPanel.Controls.Add(tile);
      }
    }
    void ApplyTheme(){
      bool light=ThemeName=="White";string bg=light?"#f3f3f3":"#202020",fg=light?"#202020":"#ffffff",card=light?"#ffffff":"#323232",accent="#0078d4";
      if(ThemeName=="Gruvbox"){bg="#282828";fg="#ebdbb2";card="#3c3836";accent="#d79921";}
      if(ThemeName=="Nord"){bg="#2e3440";fg="#eceff4";card="#3b4252";accent="#88c0d0";}
      Surface=ColorTranslator.FromHtml(bg);Ink=ColorTranslator.FromHtml(fg);Card=ColorTranslator.FromHtml(card);Accent=ColorTranslator.FromHtml(accent);
      if(ThemeName=="Black"||light)try{using(var k=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\DWM")){if(k!=null){int n=unchecked((int)Convert.ToUInt32(k.GetValue("AccentColor",0xffd47800)));Accent=Color.FromArgb(n&255,(n>>8)&255,(n>>16)&255);}}}catch{}
      PaintTheme(this);UpdateBatteryVisuals();if(trayMouse!=null){trayMouse.ContextMenuStrip.BackColor=Surface;trayMouse.ContextMenuStrip.ForeColor=Ink;UpdateTray();}Invalidate(true);
    }
    void PaintTheme(Control root){
      root.BackColor=root is Label || root is ProductGlyph || root is PictureBox?Color.Transparent:root is Button || root is FrostCard?Card:root.Parent is FrostCard?Card:Surface;root.ForeColor=Ink;
      Button button=root as Button;if(button!=null){button.UseVisualStyleBackColor=false;button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderColor=Accent;if((string)button.Tag=="settings-gear"){button.FlatAppearance.BorderSize=0;button.BackColor=Surface;}button.FlatAppearance.MouseOverBackColor=Mix(Card,Accent,0.18f);button.FlatAppearance.MouseDownBackColor=Mix(Card,Accent,0.30f);}
      foreach(Control child in root.Controls)PaintTheme(child);
    }
    internal static Color Mix(Color a,Color b,float t){return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}

    const string SettingsKey="Software\\Volti";
    readonly int[] lowLimits=new int[2],criticalLimits=new int[2],alertStates=new int[2];
    void AddAlerts(Panel card,bool headset,Label value){
      int i=headset?1:0;int low=LoadLimit(i,"Low",20),critical=LoadLimit(i,"Critical",5);low=Math.Max(2,low);critical=Math.Min(critical,low-1);lowLimits[i]=low;criticalLimits[i]=critical;
      card.Controls.Add(new Label{Text=L.T("Low (%)"),Left=16,Top=70,Width=100,Height=22});
      card.Controls.Add(new Label{Text=L.T("Critical (%)"),Left=200,Top=70,Width=140,Height=22});
      var l=new NumericUpDown{Left=16,Top=98,Width=150,Minimum=2,Maximum=100,Value=low};
      var c=new NumericUpDown{Left=200,Top=98,Width=150,Minimum=1,Maximum=low-1,Value=critical};
      card.Controls.Add(l);card.Controls.Add(c);
      l.ValueChanged+=(s,e)=>{c.Maximum=l.Value-1;lowLimits[i]=(int)l.Value;SaveLimit(i,"Low",lowLimits[i]);CheckAlert(headset,value.Text);};
      c.ValueChanged+=(s,e)=>{criticalLimits[i]=(int)c.Value;SaveLimit(i,"Critical",criticalLimits[i]);CheckAlert(headset,value.Text);};
    }
    static int LoadLimit(int device,string level,int fallback){try{using(var k=Registry.CurrentUser.OpenSubKey(SettingsKey)){return Math.Max(1,Math.Min(100,Convert.ToInt32(k==null?fallback:k.GetValue(device+level,fallback))));}}catch{return fallback;}}
    static void SaveLimit(int device,string level,int value){using(var k=Registry.CurrentUser.CreateSubKey(SettingsKey))k.SetValue(device+level,value);}
    void CheckAlert(bool headset,string value){int p;if(!TryPercent(value,out p))return;int i=headset?1:0;int state=p<=criticalLimits[i]?2:p<=lowLimits[i]?1:0;int previous=alertStates[i];alertStates[i]=state;if(state==0||state<=previous)return;
      var tray=headset?trayHeadset:trayMouse;if(tray==null)return;tray.ShowBalloonTip(6000,(state==2?L.T("Critical battery — "):L.T("Low battery — "))+(headset?"JBL Quantum 650":"DELUX M800 Mini"),L.T("Battery at ")+value+L.T(". Connect your device to charge."),ToolTipIcon.Warning);
    }
    Label AddRow(Panel parent,string name,int y,out Panel fill) {
      var card=new FrostCard{Left=20,Top=y,Width=410,Height=140,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,BackColor=Color.FromArgb(42,23,28)};
      parent.Controls.Add(card);
      card.Controls.Add(new ProductGlyph{Headset=name.StartsWith("JBL",StringComparison.Ordinal),Left=14,Top=12,Width=24,Height=26});
      card.Controls.Add(new Label{Text=name,Left=48,Top=10,Width=220,Height=22,Font=new Font("Segoe UI",10,FontStyle.Bold),ForeColor=Color.FromArgb(226,222,236)});
      var value=new Label{Text="—",Left=260,Top=5,Width=112,Height=32,Anchor=AnchorStyles.Top|AnchorStyles.Right,TextAlign=ContentAlignment.MiddleRight,Font=new Font("Segoe UI",17,FontStyle.Bold),ForeColor=VoltiBrand.RedSoft}; card.Controls.Add(value);
      var track=new Panel{Left=16,Top=46,Width=356,Height=5,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right,BackColor=Color.FromArgb(91,49,54)}; card.Controls.Add(track);
      fill=new Panel{Left=0,Top=0,Width=0,Height=5,BackColor=VoltiBrand.Red}; track.Controls.Add(fill);
      AddAlerts(card,name.StartsWith("JBL",StringComparison.Ordinal),value);return value;
    }
    void Set(Label label,string value){
      lock(labelLock){string prior;if(shownValues.TryGetValue(label,out prior)&&prior==value)return;shownValues[label]=value;}
      if(IsHandleCreated&&!IsDisposed)try{BeginInvoke((Action)(()=>{if(!IsDisposed){label.Text=value;UpdateBatteryVisuals();UpdateTray();CheckAlert(label==jblValue,value);}}));}catch{}
    }
    void ShowLowBatteryNotice(string value){trayHeadset.ShowBalloonTip(6000,L.T("Test notification"),L.T("This is a low battery notification preview."),ToolTipIcon.Warning);}
    static bool TryPercent(string value,out int percent){percent=0;int i=value.IndexOf('%');return i>0&&int.TryParse(value.Substring(0,i).Trim(),out percent);}
    void SetFill(Panel fill,Label value){int p;if(!TryPercent(value.Text,out p)){fill.Width=0;value.ForeColor=Ink;return;}Panel track=fill.Parent as Panel;if(track!=null){fill.Width=(track.ClientSize.Width*p)/100;}Color level=BatteryColor(p);fill.BackColor=level;value.ForeColor=level;}
    void UpdateBatteryVisuals(){SetFill(deluxFill,deluxValue);SetFill(jblFill,jblValue);}
    const string StartupTaskName="Pulso Duo - Monitor de Bateria";
    const string LegacyStartupTaskName="Bateria Leve - Monitor JBL";
    static ProcessStartInfo TaskInfo(string args){return new ProcessStartInfo{FileName="schtasks.exe",Arguments=args,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};}
    static bool TaskExists(string name){try{using(Process p=Process.Start(TaskInfo("/Query /TN \""+name+"\""))){return p.WaitForExit(5000)&&p.ExitCode==0;}}catch{return false;}}
    const string RunKey="Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    static bool StartupTaskExists(){try{using(var key=Registry.CurrentUser.OpenSubKey(RunKey)){return key!=null&&key.GetValue("Volti")!=null;}}catch(System.Security.SecurityException){return false;}}
    static void DeleteTask(string name){using(Process p=Process.Start(TaskInfo("/Delete /TN \""+name+"\" /F"))){if(!p.WaitForExit(10000)||p.ExitCode!=0)throw new InvalidOperationException(L.T("Could not remove the old startup task. Run Volti once as administrator to finish migration."));}}
    void SetStartupTask(bool enable){
      try{
        // Remove legacy launchers first to avoid starting two instances.
        foreach(string name in new[]{StartupTaskName,LegacyStartupTaskName})if(TaskExists(name))DeleteTask(name);
        using(var key=Registry.CurrentUser.CreateSubKey(RunKey)){if(enable)key.SetValue("Volti","\""+Application.ExecutablePath+"\"");else key.DeleteValue("Volti",false);}
      }catch(Exception ex){updatingStartup=true;startupCheck.Checked=StartupTaskExists();updatingStartup=false;MessageBox.Show(this,ex.Message,L.T("Volti startup"),MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }
    void Note(string value){
#if DEBUG
      Console.WriteLine("Status = "+value);
#endif
      if(IsHandleCreated&&!IsDisposed)try{BeginInvoke((Action)(()=>{if(!IsDisposed){footer.Text=value;trayStatus.Text=value;UpdateTray();}}));}catch{}
    }
    void UpdateTray(){
      string d=deluxValue.Text, j=jblValue.Text;
      trayDelux.Text="DELUX M800 Mini: "+d; trayJbl.Text="JBL Quantum 650: "+j;
      trayMouse.Text=TrayTip("Mouse DELUX M800 Mini: "+d);trayHeadset.Text=TrayTip("Headset JBL Quantum 650: "+j);
      UpdateTrayIcon(trayMouse,ref trayMouseIcon,d,false);UpdateTrayIcon(trayHeadset,ref trayHeadsetIcon,j,true);
    }
    static string TrayTip(string value){return value.Length>63?value.Substring(0,63):value;}
    void UpdateTrayIcon(NotifyIcon item,ref Icon current,string value,bool headset){Icon next=CreateTrayBatteryIcon(value,headset);Icon previous=current;current=next;item.Icon=next;if(previous!=null)previous.Dispose();}
    static Color BatteryColor(int percent){int p=Math.Max(0,Math.Min(100,percent));Color red=Color.FromArgb(242,70,74),yellow=Color.FromArgb(255,196,60),green=Color.FromArgb(58,205,110);if(ThemeName=="Gruvbox"){red=ColorTranslator.FromHtml("#cc241d");yellow=ColorTranslator.FromHtml("#d79921");green=ColorTranslator.FromHtml("#98971a");}if(ThemeName=="Nord"){red=ColorTranslator.FromHtml("#bf616a");yellow=ColorTranslator.FromHtml("#ebcb8b");green=ColorTranslator.FromHtml("#a3be8c");}Color a=p<=50?red:yellow;Color b=p<=50?yellow:green;double t=p<=50?p/50.0:(p-50)/50.0;return Color.FromArgb((int)Math.Round(a.R+(b.R-a.R)*t),(int)Math.Round(a.G+(b.G-a.G)*t),(int)Math.Round(a.B+(b.B-a.B)*t));}
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

      bool foundMouse=false,foundJbl=false;
      foreach(string path in Win32.HidPaths()) {
        if(stopping) return;
        string u=path.ToUpperInvariant();
        bool mouse=u.Contains("VID_1D57&PID_FA60&MI_02&COL03");
        bool jbl=u.Contains("VID_0ECB&PID_2125&MI_05&COL04");
        if(!mouse&&!jbl)continue;
        if(mouse){foundMouse=true;Set(deluxValue,L.T("Reading..."));StartReader(()=>ReadDelux(path));}
        if(jbl){foundJbl=true;Set(jblValue,L.T("Reading..."));StartReader(()=>ReadJbl(path));}
      }
      Note(!foundMouse&&!foundJbl?L.T("DELUX/JBL receivers not found."):foundMouse&&foundJbl?L.T("2.4 GHz readers active."):L.T("One receiver was not found."));
    }
    void StartReader(ThreadStart action){lock(handles){if(stopping)return;var reader=new Thread(action){IsBackground=true,Name="Volti HID reader"};readers.Add(reader);reader.Start();}}
    IntPtr Open(string path,bool overlapped){
      lock(handles){if(stopping)return new IntPtr(-1);IntPtr h=Win32.CreateFile(path,0xC0000000,3,IntPtr.Zero,3,overlapped?0x40000000u:0u,IntPtr.Zero);if(h!=new IntPtr(-1))handles.Add(h);return h;}
    }
    int FeatureLength(IntPtr h,out int inputLength){
      inputLength=64;
      IntPtr pp=IntPtr.Zero;
      try { if(!Win32.HidD_GetPreparsedData(h,out pp))return 64; var caps=new byte[128]; if(Win32.HidP_GetCaps(pp,caps)<0)return 64; int il=caps[4]|(caps[5]<<8); if(il>=9&&il<=512)inputLength=il; int n=caps[8]|(caps[9]<<8); return n>=11&&n<=512?n:64; }
      finally {if(pp!=IntPtr.Zero)Win32.HidD_FreePreparsedData(pp);}
    }
    void ReadDelux(string path){
      IntPtr h=Open(path,true); if(h==new IntPtr(-1)){Set(deluxValue,L.T("Access denied"));return;}
      byte[] report=new byte[5];
      while(!stopping){uint n;int result=Win32.ReadWithTimeout(h,report,out n,1000);if(result<0)break;if(result==0)continue;
        // M800 Mini: 03 29 40 01 NN; NN is the percentage returned by the receiver.
        if(n>=5&&report[0]==0x03&&report[1]==0x29&&report[2]==0x40&&report[4]<=100)Set(deluxValue,report[4]+"%");
      }
      if(!stopping)Set(deluxValue,L.T("Disconnected"));
    }
    void ReadJbl(string path){
      IntPtr h=Open(path,true); if(h==new IntPtr(-1)){Set(jblValue,L.T("Access denied"));return;}
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
          if(!Win32.HidD_SetFeature(h,request,(uint)request.Length)&&!stopping&&!gotBattery)Set(jblValue,L.T("No response"));
        }
        double remaining=(nextQuery-DateTime.UtcNow).TotalMilliseconds;
        uint timeout=(uint)Math.Max(1,Math.Min(60000,remaining));
        uint n; int read=Win32.ReadWithTimeout(h,input,out n,timeout); if(read<0){if(!stopping&&!gotBattery)Set(jblValue,L.T("No response"));break;} if(read==0){if(!gotBattery)Set(jblValue,L.T("No response"));continue;} if(n<10)continue;
        // A GET reply carries status (2 bytes), FeatureID (2), value length (2), then the value.
        ushort route=(ushort)(input[1]|(input[2]<<8)); ushort command=(ushort)(input[3]|(input[4]<<8)); ushort payload=(ushort)(input[7]|(input[8]<<8));
        if(input[0]==0x86&&route==0xDD00&&command==0x0001&&payload>=7&&n>=(uint)(9+payload)){
          ushort status=(ushort)(input[9]|(input[10]<<8)); ushort feature=(ushort)(input[11]|(input[12]<<8)); ushort valueLength=(ushort)(input[13]|(input[14]<<8));
          if((status&0x8000)==0&&feature==0x000D&&valueLength==1){
            byte info=input[15]; byte percent=(byte)(info&0x7F); if(percent<=100){gotBattery=true;Set(jblValue,percent+"%"+((info&0x80)!=0?"  ⚡":""));}
          }
        }
      }
      if(!stopping&&!gotBattery)Set(jblValue,L.T("Disconnected"));
    }
  }
  static class Program { [STAThread] static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);bool owner;using(var instance=new Mutex(true,"Local\\Volti.App",out owner)){if(!owner)return;Application.Run(new MainForm());}} }
}
