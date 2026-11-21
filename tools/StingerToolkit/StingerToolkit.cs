// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Volti {
  static class StingerToolkit {
    static readonly object LogLock=new object();
    static readonly Stopwatch Clock=new Stopwatch();
    static volatile bool stopping;
    static int reports;

    static int Main(string[] args) {
      Console.OutputEncoding=Encoding.UTF8;
      try {return Run(args);}
      catch(IOException ex){Console.Error.WriteLine("Erro ao gravar a captura: "+ex.Message);return 1;}
      catch(UnauthorizedAccessException ex){Console.Error.WriteLine("Sem acesso: "+ex.Message);return 1;}
    }

    static int Run(string[] args) {
      if(args.Length>0 && args[0]=="--help"){
        Console.WriteLine("StingerToolkit.exe [--list|--help]\nSem argumentos, registra a entrada HID passivamente.");
        return 0;
      }
      bool list=args.Length==1&&args[0]=="--list";
      if(args.Length!=0&&!list){Console.Error.WriteLine("Use --help para ver as opções.");return 2;}
      var paths=HyperXBattery.Paths().Where(HyperXBattery.IsOriginalStinger)
        .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path=>path,StringComparer.OrdinalIgnoreCase).ToArray();
      Console.WriteLine("Cloud Stinger Wireless PS4 (0951:16DA): "+paths.Length+" interfaces HID.");
      if(paths.Length==0){
        Console.WriteLine("Conecte o receptor USB original e execute novamente.");
        return 2;
      }
      if(list)return 0;
      if(Console.IsInputRedirected){Console.Error.WriteLine("Abra o toolkit em um terminal interativo.");return 2;}

      string name="stinger-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff",CultureInfo.InvariantCulture)+".tsv";
      string file=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),name);
      using(var log=new StreamWriter(new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.Read),new UTF8Encoding(true))){
        log.AutoFlush=true;
        log.WriteLine("ms\tevento\tinterface\tdados");
        Clock.Start();
        var workers=new List<Thread>();
        Console.WriteLine("Arquivo: "+file);
        Console.WriteLine("Enter: anotar estado observado | Esc: encerrar");
        Console.WriteLine("Exemplos de anotação: ligado, volume+, carregando, 50% (se conhecido).");
        ConsoleCancelEventHandler cancel=(sender,e)=>{e.Cancel=true;stopping=true;};
        Console.CancelKeyPress+=cancel;
        try {
          for(int i=0;i<paths.Length;i++){
            string path=paths[i], label="hid"+(i+1);
            var reader=new Thread(()=>Capture(path,label,log)){IsBackground=true};
            workers.Add(reader);
            reader.Start();
          }
          while(!stopping){
            if(!workers.Any(worker=>worker.IsAlive))break;
            if(!Console.KeyAvailable){Thread.Sleep(80);continue;}
            var key=Console.ReadKey(true).Key;
            if(key==ConsoleKey.Escape)break;
            if(key==ConsoleKey.Enter){
              Console.Write("Estado observado: ");
              string note=Console.ReadLine();
              if(!string.IsNullOrWhiteSpace(note))Write(log,"marca","",note.Trim().Replace('\t',' '));
            }
          }
        } finally {
          stopping=true;
          foreach(var worker in workers)worker.Join();
          Console.CancelKeyPress-=cancel;
        }
        Write(log,"fim","",reports.ToString(CultureInfo.InvariantCulture));
      }
      Console.WriteLine("Relatórios recebidos: "+reports);
      if(reports==0)Console.WriteLine("Nenhum relatório passivo nesta sessão. Veja o README para capturar uma consulta de outro programa.");
      return 0;
    }

    static void Capture(string path,string label,StreamWriter log) {
      var handle=Win32.CreateFile(path,0x80000000,3,IntPtr.Zero,3,0x40000000,IntPtr.Zero);
      if(handle==new IntPtr(-1)){
        int error=Marshal.GetLastWin32Error();
        Write(log,"erro",label,"CreateFile Win32="+error);
        return;
      }
      try {
        int length=InputLength(handle,log,label);
        if(length==0)return;
        var buffer=new byte[length];
        while(!stopping){
          uint bytes;
          int result=Win32.ReadWithTimeout(handle,buffer,out bytes,500);
          if(result==0)continue;
          if(result<0){Write(log,"erro",label,"Leitura interrompida ou receptor removido");break;}
          if(bytes==0)continue;
          Write(log,"relatorio",label,BitConverter.ToString(buffer,0,(int)bytes).Replace('-',' '));
          int count=Interlocked.Increment(ref reports);
          if(count==1)Console.WriteLine("\nPrimeiro relatório recebido.");
        }
      } catch(IOException ex){
        Console.Error.WriteLine("Falha no arquivo de captura: "+ex.Message);
        stopping=true;
      } finally {Win32.CloseHandle(handle);}
    }

    static int InputLength(IntPtr handle,StreamWriter log,string label){
      IntPtr data;
      if(!Win32.HidD_GetPreparsedData(handle,out data)){Write(log,"erro",label,"Descritor HID indisponível");return 0;}
      try {
        var caps=new byte[64];
        if(Win32.HidP_GetCaps(data,caps)<0){Write(log,"erro",label,"Falha ao obter capacidades HID");return 0;}
        int length=caps[4]|(caps[5]<<8);
        Write(log,"interface",label,string.Format(CultureInfo.InvariantCulture,"uso={0:X2}{1:X2}:{2:X2}{3:X2} entrada={4}",caps[3],caps[2],caps[1],caps[0],length));
        return length>0&&length<=4096?length:0;
      } finally {Win32.HidD_FreePreparsedData(data);}
    }

    static void Write(StreamWriter log,string kind,string label,string data){
      lock(LogLock){
        log.WriteLine(Clock.ElapsedMilliseconds.ToString(CultureInfo.InvariantCulture)+"\t"+kind+"\t"+label+"\t"+data);
      }
    }
  }
}
