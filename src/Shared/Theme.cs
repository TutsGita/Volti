// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Drawing;

namespace Volti {
  sealed class ThemeInfo {
    internal readonly string Name, Label;
    internal readonly Color Surface, Card, Accent, Ink, Low, Medium, High;
    internal ThemeInfo(string name, string label, string surface, string card, string accent, string ink,
                       string low, string medium, string high) {
      Name = name; Label = label;
      Surface = Hex(surface); Card = Hex(card); Accent = Hex(accent); Ink = Hex(ink);
      Low = Hex(low); Medium = Hex(medium); High = Hex(high);
    }
    static Color Hex(string value) { return ColorTranslator.FromHtml(value); }
    internal bool IsLight { get { return Surface.GetBrightness() > 0.6f; } }
  }

  // The active palette. Both processes read it; the panel also changes it.
  static class Theme {
    internal const string DefaultName = "Dracula";

    internal static readonly ThemeInfo[] All = {
      new ThemeInfo("Dracula", "Dracula", "#282A36", "#44475A", "#BD93F9", "#F8F8F2", "#FF737D", "#F1FA8C", "#50FA7B"),
      new ThemeInfo("Black", "Black", "#202020", "#323232", "#0078D4", "#FFFFFF", "#FF757C", "#FFC45A", "#68DC96"),
      new ThemeInfo("White", "White", "#F3F3F3", "#FFFFFF", "#0078D4", "#202020", "#AC2536", "#825600", "#217543"),
      new ThemeInfo("Gruvbox", "Gruvbox", "#282828", "#3C3836", "#D79921", "#EBDBB2", "#FB8072", "#FABD2F", "#B8BB26"),
      new ThemeInfo("Nord", "Nord", "#2E3440", "#3B4252", "#88C0D0", "#ECEFF4", "#EF909A", "#EBCB8B", "#A3BE8C"),
      new ThemeInfo("Frutiger Aero", "Frutiger Aero", "#C8EEFB", "#ECFCFF", "#007EA7", "#11384E", "#A83836", "#825600", "#207A3A"),
      new ThemeInfo("Blackout", "Blackout", "#000000", "#101010", "#D2D2D2", "#F1F1F1", "#FF7883", "#F5C478", "#9DD8B4"),
      new ThemeInfo("Rosé Pine", "Rosé Pine", "#191724", "#26233A", "#EBBCBA", "#E0DEF4", "#EB6F92", "#F6C177", "#9CCFD8"),
      new ThemeInfo("Catppuccin Macchiato", "Macchiato", "#24273A", "#363A4F", "#F5BDE6", "#CAD3F5", "#ED8796", "#EED49F", "#A6DA95"),
      new ThemeInfo("Catppuccin Latte", "Latte", "#EFF1F5", "#E6E9EF", "#8839EF", "#4C4F69", "#A6194A", "#865213", "#277143"),
      new ThemeInfo("SynthWave 84", "SynthWave 84", "#241B2F", "#38274B", "#FF7EDB", "#F4EDF9", "#FF6486", "#FFCC83", "#7AF0D8"),
      new ThemeInfo("Tokyo Night", "Tokyo Night", "#1A1B26", "#24283B", "#BB9AF7", "#C0CAF5", "#F7768E", "#E0AF68", "#9ECE6A")
    };

    internal static ThemeInfo Current = All[0];
    internal static Color Surface, Card, Accent, Ink;

    internal static ThemeInfo Find(string name) {
      foreach (var theme in All) if (theme.Name == name) return theme;
      return null;
    }

    internal static void Load() { Apply(Settings.Theme); }

    internal static void Apply(string name) {
      Current = Find(name) ?? Find(DefaultName);
      Surface = Current.Surface; Card = Current.Card; Accent = Current.Accent; Ink = Current.Ink;
      Color windows;
      if ((Current.Name == "Black" || Current.Name == "White") && Settings.TryWindowsAccent(out windows)) Accent = windows;
    }

    // Derived tones used by the panel.
    internal static Color Muted { get { return Mix(Surface, Ink, 0.62f); } }
    internal static Color Line { get { return Mix(Card, Ink, 0.14f); } }
    internal static Color Raised { get { return Mix(Card, Ink, 0.05f); } }
    internal static Color Hover { get { return Mix(Card, Accent, 0.16f); } }
    internal static Color Pressed { get { return Mix(Card, Accent, 0.28f); } }
    internal static Color OnAccent { get { return Accent.GetBrightness() > 0.55f ? Color.FromArgb(24, 24, 28) : Color.White; } }

    // Red at 0 %, yellow at 50 % and green at 100 %, in the current theme's colours.
    internal static Color Battery(int percent) {
      int p = Math.Max(0, Math.Min(100, percent));
      return p < 50 ? Mix(Current.Low, Current.Medium, p / 50f) : Mix(Current.Medium, Current.High, (p - 50) / 50f);
    }

    internal static Color Mix(Color a, Color b, float t) {
      return Color.FromArgb((int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }
  }
}
