using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MiniScan
{
    internal enum StatusKind { Idle, Ready, Busy, Warning, Error }

    internal sealed class ScannerCard : Control
    {
        private readonly FlatButton _scanButton;
        private readonly NumericUpDown _countBox;
        private readonly Label _countLabel;
        private readonly NumericUpDown _delayBox;
        private readonly Label _delayLabel;
        private readonly FlatButton _sourceButton;
        private readonly FlatButton _settingsButton;
        private readonly ToolTip _tip;
        private string _scannerName = string.Empty;
        private string _status = "Сканер не выбран";
        private StatusKind _statusKind = StatusKind.Idle;
        private string[] _chips = new string[0];
        private bool _scanningActive;
        private bool _busy;
        private bool _queueActive;
        private bool _updatingValues;
        private Rectangle _logoRect;
        private Rectangle _labelRect;
        private Rectangle _nameRect;
        private Rectangle _statusRect;
        private int _chipsX;
        private int _chipsRight;

        public event EventHandler ScanClick;
        public event EventHandler PauseChanged;
        public event EventHandler ScanCountChanged;
        public event EventHandler SourceClick;
        public event EventHandler SettingsClick;

        public ScannerCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
            Height = Theme.S(this, 76);

            _scanButton = new FlatButton();
            _scanButton.Kind = ButtonKind.Primary;
            _scanButton.Icon = IconKind.Scanner;
            _scanButton.Text = "Сканировать";
            _scanButton.BackColor = Theme.Surface;
            _scanButton.Height = Theme.S(this, 38);
            _scanButton.Click += delegate { if (!_busy && ScanClick != null) ScanClick(this, EventArgs.Empty); };
            Controls.Add(_scanButton);

            _countLabel = new Label();
            _countLabel.Text = "Сканов";
            _countLabel.Font = Theme.Small;
            _countLabel.ForeColor = Theme.Muted;
            _countLabel.BackColor = Theme.Surface;
            _countLabel.AutoSize = false;
            _countLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(_countLabel);

            _countBox = new NumericUpDown();
            _countBox.Minimum = 1;
            _countBox.Maximum = 999;
            _countBox.Value = 1;
            _countBox.Increment = 1;
            _countBox.Font = Theme.Small;
            _countBox.TextAlign = HorizontalAlignment.Center;
            _countBox.BorderStyle = BorderStyle.FixedSingle;
            _countBox.BackColor = Theme.Surface;
            _countBox.ForeColor = Theme.Text;
            _countBox.ValueChanged += delegate
            {
                if (!_updatingValues && ScanCountChanged != null) ScanCountChanged(this, EventArgs.Empty);
            };
            Controls.Add(_countBox);

            _delayLabel = new Label();
            _delayLabel.Text = "Пауза, с";
            _delayLabel.Font = Theme.Small;
            _delayLabel.ForeColor = Theme.Muted;
            _delayLabel.BackColor = Theme.Surface;
            _delayLabel.AutoSize = false;
            _delayLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(_delayLabel);

            _delayBox = new NumericUpDown();
            _delayBox.Minimum = 0;
            _delayBox.Maximum = 120;
            _delayBox.Value = 10;
            _delayBox.Increment = 1;
            _delayBox.Font = Theme.Small;
            _delayBox.TextAlign = HorizontalAlignment.Center;
            _delayBox.BorderStyle = BorderStyle.FixedSingle;
            _delayBox.BackColor = Theme.Surface;
            _delayBox.ForeColor = Theme.Text;
            _delayBox.ValueChanged += delegate
            {
                if (!_updatingValues && PauseChanged != null) PauseChanged(this, EventArgs.Empty);
            };
            Controls.Add(_delayBox);

            _sourceButton = new FlatButton();
            _sourceButton.Kind = ButtonKind.Secondary;
            _sourceButton.Icon = IconKind.Refresh;
            _sourceButton.ShowText = false;
            _sourceButton.IconSize = 16;
            _sourceButton.BackColor = Theme.Surface;
            _sourceButton.Click += delegate { if (!_busy && SourceClick != null) SourceClick(this, EventArgs.Empty); };
            Controls.Add(_sourceButton);

            _settingsButton = new FlatButton();
            _settingsButton.Kind = ButtonKind.Secondary;
            _settingsButton.Icon = IconKind.Gear;
            _settingsButton.ShowText = false;
            _settingsButton.IconSize = 18;
            _settingsButton.BackColor = Theme.Surface;
            _settingsButton.Click += delegate { if (!_busy && SettingsClick != null) SettingsClick(this, EventArgs.Empty); };
            Controls.Add(_settingsButton);

            _tip = new ToolTip();
            _tip.SetToolTip(_sourceButton, "Сменить сканер");
            _tip.SetToolTip(_settingsButton, "Настройки сканирования");
            _tip.SetToolTip(_countBox, "Сколько сканов выполнить за один запуск");
            _tip.SetToolTip(_countLabel, "Сколько сканов выполнить за один запуск");
            _tip.SetToolTip(_delayBox, "Пауза после очередного скана перед следующим, секунд");
            _tip.SetToolTip(_delayLabel, "Пауза после очередного скана перед следующим, секунд");
        }

        public int ScanPause { get { return (int)_delayBox.Value; } }
        public int ScanCount { get { return (int)_countBox.Value; } }

        public void SetScanValues(int scans, int seconds)
        {
            if (_delayBox == null || _countBox == null) return;
            _updatingValues = true;
            try
            {
                _delayBox.Value = Math.Max(0, Math.Min(120, seconds));
                _countBox.Value = Math.Max(1, Math.Min(999, scans));
            }
            finally
            {
                _updatingValues = false;
            }
        }

        public void SetData(string scannerName, string status, StatusKind kind, string[] chips, bool scanningActive, bool busy, int queueRemaining)
        {
            _scannerName = scannerName;
            _status = status;
            _statusKind = kind;
            _chips = chips != null ? chips : new string[0];
            _scanningActive = scanningActive;
            _busy = busy;
            _queueActive = queueRemaining > 0;
            if (_busy)
            {
                _scanButton.Kind = ButtonKind.Primary;
                _scanButton.Icon = IconKind.Scanner;
                _scanButton.Text = "Сканирование…";
                _scanButton.Enabled = false;
            }
            else if (_queueActive)
            {
                _scanButton.Kind = ButtonKind.DangerSoft;
                _scanButton.Icon = IconKind.Close;
                _scanButton.Text = "Стоп (" + queueRemaining + ")";
                _scanButton.Enabled = true;
            }
            else
            {
                _scanButton.Kind = ButtonKind.Primary;
                _scanButton.Icon = IconKind.Scanner;
                _scanButton.Text = scanningActive ? "Сканировать далее" : "Сканировать";
                _scanButton.Enabled = true;
            }
            _sourceButton.Enabled = !busy;
            _settingsButton.Enabled = !busy;
            LayoutButtons();
            Invalidate();
        }

        public void SetScanEnabled(bool enabled)
        {
            _scanButton.Enabled = enabled && !_busy;
        }

        private void LayoutButtons()
        {
            if (_scanButton == null || _countBox == null || _countLabel == null ||
                _delayBox == null || _delayLabel == null || _sourceButton == null || _settingsButton == null) return;
            int pad = Theme.S(this, 16);
            int gap = Theme.S(this, 8);
            int h = Theme.S(this, 38);
            int y = (Height - h) / 2;
            int scanW = Theme.S(this, _scanningActive || _busy ? 190 : 158);
            _scanButton.SetBounds(Width - pad - scanW, y, scanW, h);

            int countW = Theme.S(this, 52);
            int delayW = Theme.S(this, 58);
            int groupGap = Theme.S(this, 8);
            int fieldH = Theme.S(this, 23);
            int labelH = Theme.S(this, 14);
            int labelY = Theme.S(this, 9);
            int fieldY = Theme.S(this, 33);
            int groupW = countW + groupGap + delayW;
            int countX = _scanButton.Left - gap - groupW;
            _countLabel.SetBounds(countX, labelY, countW, labelH);
            _countBox.SetBounds(countX, fieldY, countW, fieldH);
            int delayX = countX + countW + groupGap;
            _delayLabel.SetBounds(delayX, labelY, delayW, labelH);
            _delayBox.SetBounds(delayX, fieldY, delayW, fieldH);

            int logo = Theme.S(this, 30);
            int lx = pad;
            int textX = lx + logo + Theme.S(this, 14);
            int textW = Theme.S(this, 230);
            int change = Theme.S(this, 28);
            int changeX = textX + textW + Theme.S(this, 6);
            int gearX = changeX + change + Theme.S(this, 4);
            int chipStart = gearX + change + Theme.S(this, 14);
            int buttonsRight = countX - Theme.S(this, 8);
            int minChips = Theme.S(this, 120);
            if (chipStart > buttonsRight - minChips)
            {
                textW = Math.Max(Theme.S(this, 90), textW - (chipStart - (buttonsRight - minChips)));
                changeX = textX + textW + Theme.S(this, 6);
                gearX = changeX + change + Theme.S(this, 4);
                chipStart = gearX + change + Theme.S(this, 14);
            }
            if (chipStart > buttonsRight) chipStart = buttonsRight;

            _logoRect = new Rectangle(lx, (Height - logo) / 2, logo, logo);
            _labelRect = new Rectangle(textX, Theme.S(this, 6), textW, Theme.S(this, 16));
            _nameRect = new Rectangle(textX, Theme.S(this, 23), textW, Theme.S(this, 24));
            _statusRect = new Rectangle(textX, Theme.S(this, 47), textW, Theme.S(this, 18));
            _chipsX = chipStart;
            _chipsRight = Math.Max(chipStart, buttonsRight);

            _sourceButton.SetBounds(changeX, (Height - change) / 2, change, change);
            _settingsButton.SetBounds(gearX, (Height - change) / 2, change, change);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutButtons();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (Pen p = new Pen(Theme.Border, 1f))
            {
                g.DrawLine(p, 0, Height - 1, Width, Height - 1);
            }

            if (_logoRect.Width > 0)
            {
                Theme.FillRounded(g, _logoRect, Theme.S(this, 9), Theme.Accent);
                IconPainter.Draw(g, IconKind.Scanner, new RectangleF(_logoRect.X + Theme.S(this, 6), _logoRect.Y + Theme.S(this, 6), _logoRect.Width - Theme.S(this, 12), _logoRect.Height - Theme.S(this, 12)), Color.White, _logoRect.Width, false);
            }
            TextRenderer.DrawText(g, "МОДУЛЬ СКАНИРОВАНИЯ", Theme.SmallBold, _labelRect, Theme.Faint,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            string name = string.IsNullOrEmpty(_scannerName) ? "Сканер не выбран" : _scannerName;
            TextRenderer.DrawText(g, Theme.FitText(g, name, Theme.ScannerName, _nameRect.Width), Theme.ScannerName, _nameRect, Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

            Color statusColor = Theme.Muted;
            if (_statusKind == StatusKind.Ready) statusColor = Theme.Green;
            else if (_statusKind == StatusKind.Busy) statusColor = Theme.Accent;
            else if (_statusKind == StatusKind.Warning) statusColor = Theme.Amber;
            else if (_statusKind == StatusKind.Error) statusColor = Theme.Red;
            int dot = Theme.S(this, 7);
            Theme.DrawDot(g, _statusRect.X, _statusRect.Y + ( _statusRect.Height - dot) / 2, dot, statusColor);
            TextRenderer.DrawText(g, _status, Theme.Small, new Rectangle(_statusRect.X + dot + Theme.S(this, 7), _statusRect.Y, _statusRect.Width - dot - Theme.S(this, 7), _statusRect.Height), statusColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

            int chipX = _chipsX;
            int chipH = Theme.S(this, 24);
            int chipY = (Height - chipH) / 2;
            for (int i = 0; i < _chips.Length; i++)
            {
                int chipW = TextRenderer.MeasureText(_chips[i], Theme.Small, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width + Theme.S(this, 18);
                if (chipX + chipW > _chipsRight) break;
                bool muted = _statusKind == StatusKind.Idle || _statusKind == StatusKind.Warning;
                Theme.DrawChip(g, this, _chips[i], Theme.Small, muted ? Theme.SurfaceSoft : Theme.AccentSoft, muted ? Theme.Muted : Theme.Accent, ref chipX, chipY, chipH);
            }
        }
    }

    internal sealed class PageThumbnail : Control
    {
        private bool _selected;
        private bool _current;
        private bool _hover;
        private int _hoverRotate;
        private int _index;
        private readonly PageItem _page;

        public event EventHandler<ThumbnailMouseEventArgs> Activated;
        public event EventHandler<ThumbnailRotateEventArgs> RotateRequested;

        public PageThumbnail(PageItem page, int index)
        {
            _page = page;
            _index = index;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);
            BackColor = Theme.Surface;
            Height = Theme.S(this, 102);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public PageItem Page { get { return _page; } }
        public int Index { get { return _index; } set { _index = value; Invalidate(); } }
        public bool Selected { get { return _selected; } set { _selected = value; Invalidate(); } }
        public bool Current { get { return _current; } set { _current = value; Invalidate(); } }

        private Rectangle RotateLeftRect
        {
            get
            {
                int b = Theme.S(this, 20);
                return new Rectangle(Width - Theme.S(this, 6) - b, Height / 2 - b - Theme.S(this, 1), b, b);
            }
        }

        private Rectangle RotateRightRect
        {
            get
            {
                int b = Theme.S(this, 20);
                return new Rectangle(Width - Theme.S(this, 6) - b, Height / 2 + Theme.S(this, 1), b, b);
            }
        }

        private void RaiseRotate(int angle)
        {
            if (RotateRequested == null) return;
            ThumbnailRotateEventArgs args = new ThumbnailRotateEventArgs();
            args.Angle = angle;
            RotateRequested(this, args);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _hoverRotate = 0;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int hover = RotateLeftRect.Contains(e.Location) ? 1 : (RotateRightRect.Contains(e.Location) ? 2 : 0);
            if (hover != _hoverRotate)
            {
                _hoverRotate = hover;
                Invalidate();
            }
            Cursor = Cursors.Hand;
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                if (RotateLeftRect.Contains(e.Location))
                {
                    RaiseRotate(-90);
                    return;
                }
                if (RotateRightRect.Contains(e.Location))
                {
                    RaiseRotate(90);
                    return;
                }
            }
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                if (Activated != null)
                {
                    ThumbnailMouseEventArgs args = new ThumbnailMouseEventArgs();
                    args.Button = e.Button;
                    args.Modifiers = ModifierKeys;
                    args.ScreenPoint = PointToScreen(e.Location);
                    Activated(this, args);
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                if (Activated != null)
                {
                    ThumbnailMouseEventArgs args = new ThumbnailMouseEventArgs();
                    args.Button = MouseButtons.Left;
                    args.Modifiers = ModifierKeys;
                    args.ScreenPoint = PointToScreen(new Point(Width / 2, Height / 2));
                    Activated(this, args);
                }
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        private void DrawRotateButton(Graphics g, Rectangle r, IconKind icon, bool hot)
        {
            Theme.FillRounded(g, r, Theme.S(this, 6), hot ? Theme.AccentSoft : Theme.SurfaceAlt);
            Theme.DrawRounded(g, r, Theme.S(this, 6), hot ? Theme.Accent : Theme.Border, 1f);
            IconPainter.Draw(g, icon, new RectangleF(r.X + Theme.S(this, 4), r.Y + Theme.S(this, 4), r.Width - Theme.S(this, 8), r.Height - Theme.S(this, 8)), hot ? Theme.Accent : Theme.Muted, r.Width, false);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Rectangle card = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = _selected ? Color.FromArgb(248, 251, 255) : Theme.Surface;
            Color border = _selected ? Theme.Accent : (_hover ? Theme.BorderStrong : Theme.Border);
            float bw = _selected ? Theme.S(this, 2) : Theme.S(this, 1);
            using (GraphicsPath path = Theme.RoundedRect(card, Theme.S(this, 9)))
            using (SolidBrush b = new SolidBrush(fill))
            using (Pen p = new Pen(border, bw))
            {
                p.Alignment = PenAlignment.Inset;
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }

            if (_current)
            {
                Theme.FillRounded(g, new Rectangle(0, Theme.S(this, 10), Theme.S(this, 3), Height - Theme.S(this, 20)), Theme.S(this, 2), Theme.Accent);
            }

            int inset = Theme.S(this, 6);
            int maxH = Math.Max(1, Height - inset * 2);
            int maxW = Math.Max(Theme.S(this, 36), Width - inset * 2 - Theme.S(this, 90));
            int areaW = Theme.S(this, 46);
            int areaH = maxH;
            Bitmap thumb = null;
            try { thumb = _page.GetThumbnail(); }
            catch { }
            if (thumb != null && thumb.Width > 0 && thumb.Height > 0)
            {
                double aspect = (double)thumb.Width / thumb.Height;
                areaW = (int)Math.Round(maxH * aspect);
                if (areaW > maxW) areaW = maxW;
                areaH = (int)Math.Round(areaW / aspect);
                if (areaH > maxH) areaH = maxH;
            }
            Rectangle imageArea = new Rectangle(inset, (Height - areaH) / 2, areaW, areaH);
            Theme.FillRounded(g, imageArea, Theme.S(this, 5), Theme.PreviewBg);
            if (thumb != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                using (GraphicsPath clip = Theme.RoundedRect(imageArea, Theme.S(this, 5)))
                {
                    g.SetClip(clip);
                    g.DrawImage(thumb, imageArea);
                    g.ResetClip();
                }
                Theme.DrawRounded(g, imageArea, Theme.S(this, 5), Theme.Border, 1f);
            }

            if (_selected)
            {
                int badge = Theme.S(this, 16);
                Rectangle br = new Rectangle(imageArea.X + Theme.S(this, 3), imageArea.Y + Theme.S(this, 3), badge, badge);
                using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillEllipse(b, br);
                IconPainter.Draw(g, IconKind.Check, new RectangleF(br.X + Theme.S(this, 4), br.Y + Theme.S(this, 4), br.Width - Theme.S(this, 8), br.Height - Theme.S(this, 8)), Color.White, badge, false);
            }

            int textX = imageArea.Right + Theme.S(this, 9);
            int textW = Math.Max(0, RotateLeftRect.Left - textX - Theme.S(this, 5));
            string title = "Страница " + (_index + 1).ToString();
            TextRenderer.DrawText(g, title, Theme.LabelBold, new Rectangle(textX, 0, textW, Height), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

            DrawRotateButton(g, RotateLeftRect, IconKind.RotateLeft, _hoverRotate == 1);
            DrawRotateButton(g, RotateRightRect, IconKind.RotateRight, _hoverRotate == 2);
        }
    }

    internal sealed class ThumbnailRotateEventArgs : EventArgs
    {
        public int Angle;
    }

    internal sealed class ThumbnailMouseEventArgs : EventArgs
    {
        public MouseButtons Button;
        public Keys Modifiers;
        public Point ScreenPoint;
    }

    internal sealed class EmptyPagesControl : Control
    {
        public EmptyPagesControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = Width / 2;
            int cy = Height / 2 - Theme.S(this, 24);
            int box = Theme.S(this, 58);
            Rectangle r = new Rectangle(cx - box / 2, cy - box / 2, box, box);
            Theme.FillRounded(g, r, Theme.S(this, 15), Theme.SurfaceSoft);
            IconPainter.Draw(g, IconKind.Scanner, new RectangleF(r.X + Theme.S(this, 14), r.Y + Theme.S(this, 14), box - Theme.S(this, 28), box - Theme.S(this, 28)), Theme.Faint, box, false);
            Rectangle t = new Rectangle(Theme.S(this, 16), r.Bottom + Theme.S(this, 15), Width - Theme.S(this, 32), Theme.S(this, 24));
            TextRenderer.DrawText(g, "Пока нет страниц", Theme.LabelBold, t, Theme.TextSoft,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            Rectangle s = new Rectangle(Theme.S(this, 24), t.Bottom + Theme.S(this, 2), Width - Theme.S(this, 48), Theme.S(this, 42));
            TextRenderer.DrawText(g, "Нажмите «Сканировать», чтобы добавить первую страницу", Theme.Small, s, Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        }
    }

    internal sealed class PreviewControl : Control
    {
        private PageItem _page;
        private Bitmap _bitmap;
        private float _zoom = 1f;
        private int _scrollX;
        private int _scrollY;
        private bool _dragging;
        private bool _hbarDragging;
        private int _dragX;
        private int _dragY;
        private int _dragScrollX;
        private int _dragScrollY;

        private struct HBarInfo
        {
            public bool Visible;
            public int Max;
            public int TrackX;
            public int TrackY;
            public int TrackW;
            public int BarH;
            public int ThumbX;
            public int ThumbW;
        }

        public event EventHandler ZoomChanged;

        public PreviewControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.PreviewBg;
        }

        public PageItem Page { get { return _page; } }
        public float Zoom { get { return _zoom; } }
        public int ZoomPercent { get { return (int)Math.Round(_zoom * 100f); } }

        public void SetPage(PageItem page)
        {
            if (_bitmap != null) { _bitmap.Dispose(); _bitmap = null; }
            _page = page;
            _zoom = 1f;
            _scrollX = 0;
            _scrollY = 0;
            OnZoomChanged();
            if (_page != null)
            {
                try { _bitmap = _page.CreateDisplayBitmap(); } catch { _bitmap = null; }
            }
            UpdateCursor();
            Invalidate();
        }

        public void RefreshImage()
        {
            SetPage(_page);
        }

        public void ZoomIn() { SetZoom(_zoom + 0.25f); }
        public void ZoomOut() { SetZoom(_zoom - 0.25f); }
        public void FitWidth() { SetZoom(1f); }

        private void SetZoom(float value)
        {
            float z = Math.Max(0.25f, Math.Min(3f, value));
            if (Math.Abs(z - _zoom) < 0.001f) return;
            _zoom = z;
            _scrollX = 0;
            _scrollY = 0;
            OnZoomChanged();
            UpdateCursor();
            Invalidate();
        }

        private void OnZoomChanged()
        {
            EventHandler handler = ZoomChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private int MaxScroll()
        {
            if (_bitmap == null || Width <= 0) return 0;
            int margin = Theme.S(this, 16);
            int contentW = Math.Max(1, Width - margin * 2 - Theme.S(this, 8));
            double scale = (double)contentW / _bitmap.Width * _zoom;
            if (scale <= 0) return 0;
            int h = (int)Math.Round(_bitmap.Height * scale);
            return Math.Max(0, h + margin * 2 - Height);
        }

        private int MaxScrollX()
        {
            if (_bitmap == null || Width <= 0) return 0;
            int margin = Theme.S(this, 16);
            int contentW = Math.Max(1, Width - margin * 2 - Theme.S(this, 8));
            double scale = (double)contentW / _bitmap.Width * _zoom;
            if (scale <= 0) return 0;
            int w = (int)Math.Round(_bitmap.Width * scale);
            return Math.Max(0, (w - contentW + 1) / 2);
        }

        private HBarInfo GetHBarInfo()
        {
            HBarInfo info = new HBarInfo();
            if (_bitmap == null || Width <= 0) return info;
            int margin = Theme.S(this, 16);
            int contentW = Math.Max(1, Width - margin * 2 - Theme.S(this, 8));
            double scale = (double)contentW / _bitmap.Width * _zoom;
            if (scale <= 0) return info;
            int w = (int)Math.Round(_bitmap.Width * scale);
            int max = Math.Max(0, (w - contentW + 1) / 2);
            if (max <= 0) return info;
            info.Visible = true;
            info.Max = max;
            info.BarH = Theme.S(this, 5);
            info.TrackX = margin;
            info.TrackY = Height - margin + (margin - info.BarH) / 2;
            info.TrackW = Math.Max(1, Width - margin * 2);
            int thumbW = Math.Max(Theme.S(this, 26), (int)Math.Round((double)info.TrackW * Width / (w + margin * 2)));
            if (thumbW > info.TrackW) thumbW = info.TrackW;
            info.ThumbW = thumbW;
            int range = Math.Max(1, info.TrackW - info.ThumbW);
            double ratio = (double)(_scrollX + max) / (2.0 * max);
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;
            info.ThumbX = info.TrackX + (int)Math.Round(range * ratio);
            return info;
        }

        private void UpdateCursor()
        {
            Cursor = _bitmap != null && (MaxScroll() > 0 || MaxScrollX() > 0) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.PreviewBg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            if (_bitmap == null)
            {
                DrawEmpty(g);
                return;
            }
            int margin = Theme.S(this, 16);
            int contentW = Math.Max(1, Width - margin * 2 - Theme.S(this, 8));
            double fit = (double)contentW / _bitmap.Width;
            if (fit <= 0) return;
            double scale = fit * _zoom;
            int w = Math.Max(1, (int)Math.Round(_bitmap.Width * scale));
            int h = Math.Max(1, (int)Math.Round(_bitmap.Height * scale));
            int maxScroll = Math.Max(0, h + margin * 2 - Height);
            if (_scrollY > maxScroll) _scrollY = maxScroll;
            if (_scrollY < 0) _scrollY = 0;
            int maxScrollX = Math.Max(0, (w - contentW + 1) / 2);
            if (_scrollX > maxScrollX) _scrollX = maxScrollX;
            if (_scrollX < -maxScrollX) _scrollX = -maxScrollX;
            int x = margin + (contentW - w) / 2 - _scrollX;
            int y = maxScroll > 0 ? margin - _scrollY : (Height - h) / 2;
            g.SetClip(ClientRectangle);
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(28, 20, 28, 42)))
            {
                Theme.FillRounded(g, new RectangleF(x + Theme.S(this, 5), y + Theme.S(this, 6), w, h), Theme.S(this, 3), Color.FromArgb(28, 20, 28, 42));
            }
            using (SolidBrush white = new SolidBrush(Color.White))
            {
                g.FillRectangle(white, x, y, w, h);
            }
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(_bitmap, new Rectangle(x, y, w, h));
            using (Pen p = new Pen(Color.FromArgb(40, 70, 80, 95), 1f))
            {
                g.DrawRectangle(p, x, y, w - 1, h - 1);
            }
            DrawScrollbar(g, maxScroll, h + margin * 2);
            DrawScrollbarH(g);
        }

        private void DrawScrollbar(Graphics g, int maxScroll, int contentH)
        {
            if (maxScroll <= 0 || contentH <= 0) return;
            int margin = Theme.S(this, 16);
            int sbW = Theme.S(this, 5);
            int trackX = Width - margin - sbW;
            int trackY = margin;
            int trackH = Math.Max(1, Height - margin * 2);
            int thumbH = Math.Max(Theme.S(this, 26), (int)Math.Round((double)trackH * Height / contentH));
            if (thumbH > trackH) thumbH = trackH;
            int thumbY = trackY + (int)Math.Round((double)(trackH - thumbH) * _scrollY / maxScroll);
            Theme.FillRounded(g, new RectangleF(trackX, trackY, sbW, trackH), sbW / 2f, Color.FromArgb(26, 70, 80, 95));
            Theme.FillRounded(g, new RectangleF(trackX, thumbY, sbW, thumbH), sbW / 2f, Color.FromArgb(105, 70, 80, 95));
        }

        private void DrawScrollbarH(Graphics g)
        {
            HBarInfo info = GetHBarInfo();
            if (!info.Visible) return;
            Theme.FillRounded(g, new RectangleF(info.TrackX, info.TrackY, info.TrackW, info.BarH), info.BarH / 2f, Color.FromArgb(26, 70, 80, 95));
            Theme.FillRounded(g, new RectangleF(info.ThumbX, info.TrackY, info.ThumbW, info.BarH), info.BarH / 2f, Color.FromArgb(105, 70, 80, 95));
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int maxX = MaxScrollX();
            if (_bitmap != null && maxX > 0 && (ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                _scrollX = Math.Max(-maxX, Math.Min(maxX, _scrollX - e.Delta / 2));
                Invalidate();
                return;
            }
            int max = MaxScroll();
            if (_bitmap != null && max > 0)
            {
                _scrollY = Math.Max(0, Math.Min(max, _scrollY - e.Delta / 2));
                Invalidate();
                return;
            }
            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _bitmap != null)
            {
                HBarInfo hb = GetHBarInfo();
                int hitPad = Theme.S(this, 4);
                if (hb.Visible && e.X >= hb.TrackX && e.X <= hb.TrackX + hb.TrackW &&
                    e.Y >= hb.TrackY - hitPad && e.Y <= hb.TrackY + hb.BarH + hitPad)
                {
                    _hbarDragging = true;
                    _dragX = e.X;
                    _dragScrollX = _scrollX;
                    if (e.X < hb.ThumbX || e.X > hb.ThumbX + hb.ThumbW)
                    {
                        int range = Math.Max(1, hb.TrackW - hb.ThumbW);
                        double ratio = (double)(e.X - hb.TrackX - hb.ThumbW / 2) / range;
                        if (ratio < 0) ratio = 0;
                        if (ratio > 1) ratio = 1;
                        _scrollX = (int)Math.Round(ratio * 2.0 * hb.Max - hb.Max);
                        _dragScrollX = _scrollX;
                    }
                    Capture = true;
                    Cursor = Cursors.Hand;
                    Invalidate();
                    base.OnMouseDown(e);
                    return;
                }
                if (MaxScroll() > 0 || MaxScrollX() > 0)
                {
                    _dragging = true;
                    _dragX = e.X;
                    _dragY = e.Y;
                    _dragScrollX = _scrollX;
                    _dragScrollY = _scrollY;
                    Capture = true;
                    Cursor = Cursors.Hand;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_hbarDragging)
            {
                HBarInfo hb = GetHBarInfo();
                if (hb.Visible && hb.Max > 0)
                {
                    int range = Math.Max(1, hb.TrackW - hb.ThumbW);
                    int delta = e.X - _dragX;
                    int next = _dragScrollX + (int)Math.Round((double)delta * 2.0 * hb.Max / range);
                    _scrollX = Math.Max(-hb.Max, Math.Min(hb.Max, next));
                    Invalidate();
                }
                base.OnMouseMove(e);
                return;
            }
            if (_dragging)
            {
                int maxY = MaxScroll();
                int maxX = MaxScrollX();
                _scrollY = Math.Max(0, Math.Min(maxY, _dragScrollY + (_dragY - e.Y)));
                _scrollX = Math.Max(-maxX, Math.Min(maxX, _dragScrollX + (_dragX - e.X)));
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_hbarDragging)
            {
                _hbarDragging = false;
                Capture = false;
                UpdateCursor();
            }
            if (_dragging)
            {
                _dragging = false;
                Capture = false;
                UpdateCursor();
            }
            base.OnMouseUp(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateCursor();
        }

        private void DrawEmpty(Graphics g)
        {
            int cx = Width / 2;
            int cy = Height / 2 - Theme.S(this, 18);
            int box = Theme.S(this, 78);
            Rectangle r = new Rectangle(cx - box / 2, cy - box / 2, box, box);
            Theme.FillRounded(g, r, Theme.S(this, 22), Color.FromArgb(245, 248, 252));
            IconPainter.Draw(g, IconKind.Scanner, new RectangleF(r.X + Theme.S(this, 19), r.Y + Theme.S(this, 19), box - Theme.S(this, 38), box - Theme.S(this, 38)), Theme.Faint, box, false);
            Rectangle t = new Rectangle(Theme.S(this, 20), r.Bottom + Theme.S(this, 18), Width - Theme.S(this, 40), Theme.S(this, 28));
            TextRenderer.DrawText(g, "Здесь появится страница", Theme.Section, t, Theme.TextSoft,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            Rectangle s = new Rectangle(Theme.S(this, 30), t.Bottom + Theme.S(this, 2), Width - Theme.S(this, 60), Theme.S(this, 24));
            TextRenderer.DrawText(g, "Выберите миниатюру слева или начните сканирование", Theme.Small, s, Theme.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _bitmap != null) _bitmap.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class FooterBar : Control
    {
        private int _pages;
        private int _selected;

        public FooterBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Bg;
            Height = 38;
        }

        public void SetCounts(int pages, int selected)
        {
            _pages = pages;
            _selected = selected;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Bg);
            int pad = Theme.S(this, 24);
            TextRenderer.DrawText(g, "Изображения сохраняются без распознавания текста", Theme.Small, new Rectangle(pad, 0, Width / 2, Height), Theme.Faint,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            string right = "Страниц: " + _pages + "    ·    Выбрано: " + _selected;
            TextRenderer.DrawText(g, right, Theme.Small, new Rectangle(Width / 2, 0, Width - pad - Width / 2, Height), Theme.Faint,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}