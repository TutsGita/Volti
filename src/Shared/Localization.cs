// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace Volti {
  // English strings are the keys. Other languages live in assets/lang/<code>.json.
  static class L {
    internal static readonly string[][] Languages = {
      new[] {"en", "English"},
      new[] {"pt-BR", "Português (Brasil)"},
      new[] {"zh-CN", "简体中文"}
    };

    internal static string Language = "en";
    internal static string CatalogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "lang");
    static readonly Dictionary<string, Dictionary<string, string>> Catalogs =
      new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    static Dictionary<string, string> Catalog(string code) {
      if (string.IsNullOrEmpty(code) || string.Equals(code, "en", StringComparison.OrdinalIgnoreCase)) return null;
      Dictionary<string, string> table;
      if (Catalogs.TryGetValue(code, out table)) return table;
      table = new Dictionary<string, string>(StringComparer.Ordinal);
      try {
        string path = Path.Combine(CatalogFolder, code + ".json");
        if (File.Exists(path)) {
          var parsed = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
          if (parsed != null) table = parsed;
        }
      } catch (IOException) {} catch (UnauthorizedAccessException) {} catch (ArgumentException) {} catch (InvalidOperationException) {}
      Catalogs[code] = table;
      return table;
    }

    internal static void Load() { Language = Settings.Language; }

    internal static void SetLanguage(string language) {
      Settings.Language = language;
      Language = language;
    }

    internal static string T(string english) {
      string value;
      var table = Catalog(Language);
      return table != null && table.TryGetValue(english, out value) ? value : english;
    }

    // Formats a translated template such as "{0} devices".
    internal static string F(string english, params object[] args) {
      return string.Format(System.Globalization.CultureInfo.CurrentCulture, T(english), args);
    }
  }
}
