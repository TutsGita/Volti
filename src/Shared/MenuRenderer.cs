// SPDX-License-Identifier: AGPL-3.0-only
using System.Drawing;
using System.Windows.Forms;

namespace Volti {
  // Themed context menus for the tray and the panel.
  sealed class MenuRenderer : ToolStripProfessionalRenderer {
    sealed class Colors : ProfessionalColorTable {
      public override Color ToolStripDropDownBackground { get { return Theme.Surface; } }
      public override Color ImageMarginGradientBegin { get { return Theme.Surface; } }
      public override Color ImageMarginGradientMiddle { get { return Theme.Surface; } }
      public override Color ImageMarginGradientEnd { get { return Theme.Surface; } }
      public override Color MenuBorder { get { return Theme.Line; } }
      public override Color MenuItemBorder { get { return Theme.Card; } }
      public override Color MenuItemSelected { get { return Theme.Card; } }
      public override Color MenuItemSelectedGradientBegin { get { return Theme.Card; } }
      public override Color MenuItemSelectedGradientEnd { get { return Theme.Card; } }
      public override Color MenuItemPressedGradientBegin { get { return Theme.Card; } }
      public override Color MenuItemPressedGradientEnd { get { return Theme.Card; } }
      public override Color SeparatorDark { get { return Theme.Line; } }
      public override Color SeparatorLight { get { return Theme.Surface; } }
      public override Color ToolStripBorder { get { return Theme.Surface; } }
    }
    internal MenuRenderer() : base(new Colors()) { RoundedEdges = true; }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) {
      // Disabled device lines stay readable instead of using the system grey.
      e.TextColor = e.Item.Enabled ? Theme.Ink : Theme.Muted;
      base.OnRenderItemText(e);
    }
  }
}
