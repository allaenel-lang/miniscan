using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MiniScan
{
    internal static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(245, 247, 250);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceAlt = Color.FromArgb(249, 250, 252);
        public static readonly Color SurfaceSoft = Color.FromArgb(242, 245, 250);
        public static readonly Color Border = Color.FromArgb(226, 231, 238);
        public static readonly Color BorderStrong = Color.FromArgb(208, 215, 225);
        public static readonly Color Text = Color.FromArgb(28, 34, 43);
        public static readonly Color TextSoft = Color.FromArgb(73, 83, 98);
        public static readonly Color Muted = Color.FromArgb(111, 121, 136);
        public static readonly Color Faint = Color.FromArgb(150, 159, 172);
        public static readonly Color Accent = Color.FromArgb(35, 99, 235);
        public static readonly Color AccentHover = Color.FromArgb(27, 86, 214);
        public static readonly Color AccentPressed = Color.FromArgb(21, 72, 182);
        public static readonly Color AccentSoft = Color.FromArgb(235, 242, 255);
        public static readonly Color AccentSoftHover = Color.FromArgb(225, 236, 255);
        public static readonly Color Green = Color.FromArgb(24, 145, 93);
        public static readonly Color GreenSoft = Color.FromArgb(231, 248, 240);
        public static readonly Color Amber = Color.FromArgb(178, 112, 16);
        public static readonly Color AmberSoft = Color.FromArgb(255, 247, 228);
        public static readonly Color Red = Color.FromArgb(199, 58, 58);
        public static readonly Color RedSoft = Color.FromArgb(253, 238, 238);
        public static readonly Color PreviewBg = Color.FromArgb(235, 239, 245);
        public static readonly Color Shadow = Color.FromArgb(28, 35, 48);

        public static readonly Font Body = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font BodyMedium = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Small = new Font("Segoe UI", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font SmallBold = new Font("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Label = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font LabelBold = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Section = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font DialogTitle = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font ScannerName = new Font("Segoe UI", 12.5f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Button = new Font("Segoe UI", 9.5f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font ButtonSmall = new Font("Segoe UI", 8.5f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Logo = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font Count = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Point);

        private static int _systemDpi;

        public static int SystemDpi
        {
            get
            {
                if (_systemDpi == 0)
                {
                    try
                    {
                        using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) _systemDpi = (int)Math.Round(g.DpiX);
                    }
                    catch { _systemDpi = 96; }
                    if (_systemDpi < 96) _systemDpi = 96;
                }
                return _systemDpi;
            }
        }

        public static int S(Control c, int value)
        {
            int dpi = c != null ? c.DeviceDpi : 0;
            if (dpi <= 96) dpi = SystemDpi;
            return (int)Math.Round(value * dpi / 96.0);
        }

        public static float SF(Control c, float value)
        {
            int dpi = c != null ? c.DeviceDpi : 0;
            if (dpi <= 96) dpi = SystemDpi;
            return (float)(value * dpi / 96.0);
        }

        public static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d <= 0f || r.Width <= 0f || r.Height <= 0f)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            path.AddArc(r.X, r.Y, d, d, 180f, 90f);
            path.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
            path.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics g, RectangleF r, float radius, Color color)
        {
            if (r.Width <= 0f || r.Height <= 0f) return;
            using (GraphicsPath p = RoundedRect(r, radius))
            using (SolidBrush b = new SolidBrush(color))
            {
                g.FillPath(b, p);
            }
        }

        public static void DrawRounded(Graphics g, RectangleF r, float radius, Color color, float width)
        {
            if (r.Width <= 1f || r.Height <= 1f) return;
            using (GraphicsPath p = RoundedRect(r, radius))
            using (Pen pen = new Pen(color, width))
            {
                pen.Alignment = PenAlignment.Inset;
                g.DrawPath(pen, p);
            }
        }

        public static void DrawCard(Graphics g, Rectangle r, int radius, Color fill, Color border)
        {
            RectangleF rf = new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1f, r.Height - 1f);
            FillRounded(g, rf, radius, fill);
            DrawRounded(g, rf, radius, border, 1f);
        }

        public static void DrawChip(Graphics g, Control c, string text, Font font, Color back, Color fore, ref int x, int y, int height)
        {
            int pad = S(c, 9);
            int gap = S(c, 6);
            Size sz = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            Rectangle r = new Rectangle(x, y, sz.Width + pad * 2, height);
            FillRounded(g, r, height / 2f, back);
            TextRenderer.DrawText(g, text, font, r, fore, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            x = r.Right + gap;
        }

        public static void DrawDot(Graphics g, int x, int y, int size, Color color)
        {
            using (SolidBrush b = new SolidBrush(color))
            {
                g.FillEllipse(b, x, y, size, size);
            }
        }

        public static string FitText(Graphics g, string text, Font font, int maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (TextRenderer.MeasureText(g, text, font, Size.Empty, TextFormatFlags.NoPadding).Width <= maxWidth) return text;
            string s = text;
            while (s.Length > 1)
            {
                s = s.Substring(0, s.Length - 1);
                string candidate = s.TrimEnd() + "…";
                if (TextRenderer.MeasureText(g, candidate, font, Size.Empty, TextFormatFlags.NoPadding).Width <= maxWidth) return candidate;
            }
            return "…";
        }
    }

    internal sealed class AppColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Theme.Surface; } }
        public override Color ImageMarginGradientBegin { get { return Theme.Surface; } }
        public override Color ImageMarginGradientMiddle { get { return Theme.Surface; } }
        public override Color ImageMarginGradientEnd { get { return Theme.Surface; } }
        public override Color MenuBorder { get { return Theme.Border; } }
        public override Color MenuItemBorder { get { return Theme.AccentSoft; } }
        public override Color MenuItemSelected { get { return Theme.AccentSoft; } }
        public override Color MenuItemSelectedGradientBegin { get { return Theme.AccentSoft; } }
        public override Color MenuItemSelectedGradientEnd { get { return Theme.AccentSoft; } }
        public override Color MenuItemPressedGradientBegin { get { return Theme.AccentSoft; } }
        public override Color MenuItemPressedGradientEnd { get { return Theme.AccentSoft; } }
        public override Color SeparatorDark { get { return Theme.Border; } }
        public override Color SeparatorLight { get { return Theme.Surface; } }
    }
}