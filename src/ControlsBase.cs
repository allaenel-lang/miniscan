using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MiniScan
{
    internal enum ButtonKind { Primary, Secondary, Ghost, Soft, Danger, DangerSoft }

    internal sealed class FlatButton : Control
    {
        private bool _hover;
        private bool _pressed;
        private ButtonKind _kind = ButtonKind.Secondary;
        private IconKind _icon = IconKind.None;
        private bool _showText = true;
        private int _radius = 9;
        private int _iconSize = 18;
        private int _horizontalPadding = 16;

        public FlatButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);
            Font = Theme.Button;
            Cursor = Cursors.Hand;
            TabStop = true;
            TextAlign = ContentAlignment.MiddleCenter;
            BackColor = Theme.Surface;
            Size = new Size(120, 40);
        }

        public ButtonKind Kind { get { return _kind; } set { _kind = value; Invalidate(); } }
        public IconKind Icon { get { return _icon; } set { _icon = value; Invalidate(); } }
        public bool ShowText { get { return _showText; } set { _showText = value; Invalidate(); } }
        public int CornerRadius { get { return _radius; } set { _radius = value; Invalidate(); } }
        public int IconSize { get { return _iconSize; } set { _iconSize = value; Invalidate(); } }
        public int HorizontalPadding { get { return _horizontalPadding; } set { _horizontalPadding = value; Invalidate(); } }
        public ContentAlignment TextAlign { get; set; }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Focus();
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int s = Theme.S(this, 1);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            float radius = Theme.S(this, _radius);
            Color fill = Color.Empty;
            Color border = Color.Empty;
            Color fore = Theme.Text;

            if (!Enabled)
            {
                if (_kind == ButtonKind.Primary)
                {
                    fill = Color.FromArgb(203, 212, 225);
                    fore = Color.White;
                }
                else if (_kind == ButtonKind.Soft)
                {
                    fill = Color.FromArgb(244, 246, 249);
                    border = Theme.Border;
                    fore = Theme.Faint;
                }
                else if (_kind == ButtonKind.Danger || _kind == ButtonKind.DangerSoft)
                {
                    fill = Theme.Surface;
                    border = Theme.Border;
                    fore = Color.FromArgb(190, 170, 170);
                }
                else
                {
                    fill = Theme.Surface;
                    border = Theme.Border;
                    fore = Theme.Faint;
                }
            }
            else
            {
                switch (_kind)
                {
                    case ButtonKind.Primary:
                        fill = _pressed ? Theme.AccentPressed : (_hover ? Theme.AccentHover : Theme.Accent);
                        fore = Color.White;
                        break;
                    case ButtonKind.Secondary:
                        fill = _hover ? Theme.AccentSoft : Theme.Surface;
                        border = _hover ? Color.FromArgb(190, 210, 240) : Theme.BorderStrong;
                        fore = _hover ? Theme.Accent : Theme.Text;
                        break;
                    case ButtonKind.Ghost:
                        fill = _hover ? (_pressed ? Color.FromArgb(235, 239, 245) : Color.FromArgb(242, 245, 249)) : BackColor;
                        fore = Theme.TextSoft;
                        break;
                    case ButtonKind.Soft:
                        fill = _pressed ? Theme.AccentSoftHover : (_hover ? Color.FromArgb(226, 237, 255) : Theme.AccentSoft);
                        border = Color.FromArgb(206, 222, 250);
                        fore = Theme.Accent;
                        break;
                    case ButtonKind.Danger:
                        fill = _pressed ? Color.FromArgb(185, 43, 43) : (_hover ? Color.FromArgb(210, 55, 55) : Theme.Red);
                        fore = Color.White;
                        break;
                    case ButtonKind.DangerSoft:
                        fill = _hover ? Color.FromArgb(252, 230, 230) : Theme.RedSoft;
                        border = Color.FromArgb(245, 212, 212);
                        fore = Theme.Red;
                        break;
                }
            }

            if (fill != Color.Empty)
            {
                using (GraphicsPath path = Theme.RoundedRect(r, Theme.S(this, _radius)))
                using (SolidBrush b = new SolidBrush(fill))
                {
                    g.FillPath(b, path);
                }
            }
            if (border != Color.Empty)
            {
                using (GraphicsPath path = Theme.RoundedRect(r, Theme.S(this, _radius)))
                using (Pen pen = new Pen(border, Math.Max(1f, s)))
                {
                    pen.Alignment = PenAlignment.Inset;
                    g.DrawPath(pen, path);
                }
            }

            Font f = Font;
            int iconPx = Theme.S(this, _iconSize);
            int gap = _showText && _icon != IconKind.None ? Theme.S(this, 8) : 0;
            Size textSize = Size.Empty;
            if (_showText && !string.IsNullOrEmpty(Text))
            {
                textSize = TextRenderer.MeasureText(g, Text, f, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            }
            int total = (iconPx > 0 && _icon != IconKind.None ? iconPx : 0) + gap + textSize.Width;
            int startX = (Width - total) / 2;
            if (TextAlign == ContentAlignment.MiddleLeft)
            {
                startX = Theme.S(this, _horizontalPadding);
            }
            int centerY = Height / 2;
            if (_icon != IconKind.None && iconPx > 0)
            {
                RectangleF ir = new RectangleF(startX, centerY - iconPx / 2f, iconPx, iconPx);
                IconPainter.Draw(g, _icon, ir, fore, iconPx, false);
                startX += iconPx + gap;
            }
            if (_showText && !string.IsNullOrEmpty(Text))
            {
                Rectangle tr = new Rectangle(startX, 0, Math.Max(0, Width - startX - Theme.S(this, 4)), Height);
                TextRenderer.DrawText(g, Text, f, tr, fore,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
            }

            if (Focused && ShowFocusCues)
            {
                Rectangle fr = new Rectangle(Theme.S(this, 3), Theme.S(this, 3), Width - Theme.S(this, 6), Height - Theme.S(this, 6));
                using (Pen fp = new Pen(Color.FromArgb(120, fore), 1f))
                {
                    fp.DashStyle = DashStyle.Dot;
                    using (GraphicsPath path = Theme.RoundedRect(fr, Theme.S(this, Math.Max(4, _radius - 3))))
                    {
                        g.DrawPath(fp, path);
                    }
                }
            }
        }
    }

    internal class CardPanel : Panel
    {
        public int Corner { get; set; }
        public Color CardColor { get; set; }
        public Color CardBorder { get; set; }

        public CardPanel()
        {
            Corner = 14;
            CardColor = Theme.Surface;
            CardBorder = Theme.Border;
            BackColor = Theme.Surface;
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            if (Width > 4 && Height > 4)
            {
                using (GraphicsPath path = Theme.RoundedRect(new Rectangle(0, 0, Width, Height), Theme.S(this, Corner)))
                {
                    Region old = Region;
                    Region = new Region(path);
                    if (old != null) old.Dispose();
                }
            }
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Theme.DrawCard(g, r, Theme.S(this, Corner), CardColor, CardBorder);
            base.OnPaint(e);
        }
    }

    internal sealed class BorderPanel : Panel
    {
        public bool DrawTop { get; set; }
        public bool DrawBottom { get; set; }
        public Color LineColor { get; set; }

        public BorderPanel()
        {
            DrawTop = false;
            DrawBottom = true;
            LineColor = Theme.Border;
            BackColor = Theme.Surface;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(LineColor, 1f))
            {
                if (DrawTop) e.Graphics.DrawLine(p, 0, 0, Width, 0);
                if (DrawBottom) e.Graphics.DrawLine(p, 0, Height - 1, Width, Height - 1);
            }
        }
    }

    internal sealed class BusyOverlay : Control
    {
        private readonly Timer _timer;
        private int _angle;
        private string _message = "Сканирование…";
        private string _sub = "Подождите, сканер передаёт изображение";

        public BusyOverlay()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            _timer = new Timer();
            _timer.Interval = 45;
            _timer.Tick += delegate { _angle = (_angle + 12) % 360; Invalidate(); };
            Visible = false;
        }

        public void SetMessage(string message, string sub)
        {
            _message = message;
            _sub = sub;
            Invalidate();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (_timer == null) return;
            if (Visible) _timer.Start(); else _timer.Stop();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush b = new SolidBrush(Color.FromArgb(232, 255, 255, 255)))
            {
                g.FillRectangle(b, ClientRectangle);
            }
            int size = Theme.S(this, 38);
            int cx = Width / 2;
            int cy = Height / 2 - Theme.S(this, 18);
            using (Pen p = new Pen(Color.FromArgb(225, 232, 242), Theme.S(this, 4)))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawArc(p, cx - size / 2, cy - size / 2, size, size, 0f, 360f);
            }
            using (Pen p = new Pen(Theme.Accent, Theme.S(this, 4)))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawArc(p, cx - size / 2, cy - size / 2, size, size, _angle, 105f);
            }
            Rectangle t = new Rectangle(0, cy + size / 2 + Theme.S(this, 18), Width, Theme.S(this, 28));
            TextRenderer.DrawText(g, _message, Theme.Section, t, Theme.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            Rectangle st = new Rectangle(0, t.Bottom + Theme.S(this, 2), Width, Theme.S(this, 24));
            TextRenderer.DrawText(g, _sub, Theme.Small, st, Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}