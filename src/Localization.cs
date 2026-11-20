// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
namespace Volti {
  static class L {
    internal static string Language="en";
    static readonly Dictionary<string,string> Portuguese=new Dictionary<string,string>{
      {"Black", "Preto"},
      {"White", "Branco"},
      {"Language", "Idioma"},
      {"Palette", "Paleta"},
      {"Test notification", "Notificação de teste"},
      {"This is a low battery notification preview.", "Esta é uma prévia do aviso de bateria baixa."},
      {"Battery monitoring for your devices", "Monitor de energia dos seus dispositivos"},
      {"Overview", "Visão geral"},
      {"Test battery notification", "⚡  Simular aviso de bateria baixa"},
      {"Looking for 2.4 GHz receivers...", "Procurando receptores 2,4 GHz..."},
      {"DELUX M800 Mini: searching...", "DELUX M800 Mini: procurando..."},
      {"JBL Quantum 650: searching...", "JBL Quantum 650: procurando..."},
      {"Volti is connecting to receivers...", "Volti conectando aos receptores..."},
      {"Open Volti", "Abrir Volti"},
      {"Exit", "Sair"},
      {"DELUX mouse: searching...", "Mouse DELUX: procurando..."},
      {"JBL headset: searching...", "Headset JBL: procurando..."},
      {"Settings", "Configurações"},
      {"Start with Windows", "Iniciar com o Windows"},
      {"Listed in Windows Startup apps.", "Disponível em Aplicativos de inicialização do Windows."},
      {"Themes", "Temas"},
      {"Low (%)", "Baixa (%)"},
      {"Critical (%)", "Muito baixa (%)"},
      {"Critical battery — ", "Bateria muito baixa — "},
      {"Low battery — ", "Bateria baixa — "},
      {"Battery at ", "Bateria em "},
      {". Connect your device to charge.", ". Conecte o dispositivo para carregar."},
      {"Volti startup", "Inicialização do Volti"},
      {"Could not remove the old startup task. Run Volti once as administrator to finish migration.", "Não foi possível remover a tarefa antiga. Abra o Volti uma vez como administrador para concluir a migração."},
      {"HID discovery", "Localizador HID"},
      {"Reading...", "Lendo..."},
      {"DELUX battery", "Bateria Delux"},
      {"JBL battery", "Bateria JBL"},
      {"DELUX/JBL receivers not found.", "Receptores Delux/JBL não encontrados."},
      {"2.4 GHz readers active.", "Leitores 2,4 GHz ativos."},
      {"One receiver was not found.", "Um receptor não foi encontrado."},
      {"Access denied", "Sem acesso"},
      {"Disconnected", "Desconectado"},
      {"No response", "Sem resposta"},
      {"Readers", "Leitores"},
    };
    internal static void Load(){try{using(var key=Registry.CurrentUser.OpenSubKey("Software\\Volti")){if(key!=null)Language=(string)key.GetValue("Language","en");}}catch{} }
    internal static void SetLanguage(string language){using(var key=Registry.CurrentUser.CreateSubKey("Software\\Volti"))key.SetValue("Language",language);Language=language;}
    internal static string T(string english){string value;return Language=="pt-BR"&&Portuguese.TryGetValue(english,out value)?value:english;}
    internal static string Convert(string text){string[] parts=text.Split(new[]{"   •   "},StringSplitOptions.None);if(parts.Length==2)return Convert(parts[0])+"   •   "+Convert(parts[1]);foreach(var entry in Portuguese){if(text==entry.Value||text==entry.Key)return T(entry.Key);}return text;}
    internal static void Apply(Control control){control.Text=Convert(control.Text);foreach(Control child in control.Controls)Apply(child);}
  }
}
