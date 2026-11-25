using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace MiniScan
{
    internal sealed class CheckOption : Control
    {
        private bool _checked;
        private bool _hover;
        private string _description = string.Empty;

        public event EventHandler CheckedChanged;

        public CheckOption()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);
            BackColor = Theme.Bg;
            Height = 66;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        public string Description { get { return _description; } set { _description = value; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnClick(EventArgs e)
        {
            if (Enabled) Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                Checked = !Checked;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = _checked ? Theme.AccentSoft : (_hover && Enabled ? Theme.SurfaceAlt : Theme.Surface);
            Color border = _checked ? Color.FromArgb(168, 196, 247) : (_hover && Enabled ? Theme.BorderStrong : Theme.Border);
            using (GraphicsPath path = Theme.RoundedRect(r, Theme.S(this, 10)))
            using (SolidBrush b = new SolidBrush(fill))
            using (Pen p = new Pen(border, Theme.S(this, 1)))
            {
                p.Alignment = PenAlignment.Inset;
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
            int box = Theme.S(this, 20);
            Rectangle br = new Rectangle(Theme.S(this, 14), (Height - box) / 2, box, box);
            if (_checked)
            {
                Theme.FillRounded(g, br, Theme.S(this, 5), Enabled ? Theme.Accent : Theme.Faint);
                IconPainter.Draw(g, IconKind.Check, new RectangleF(br.X + Theme.S(this, 4), br.Y + Theme.S(this, 4), br.Width - Theme.S(this, 8), br.Height - Theme.S(this, 8)), Color.White, box, false);
            }
            else
            {
                Theme.FillRounded(g, br, Theme.S(this, 5), Theme.Surface);
                Theme.DrawRounded(g, br, Theme.S(this, 5), Enabled ? Theme.BorderStrong : Theme.Border, Theme.S(this, 1));
            }
            int tx = br.Right + Theme.S(this, 12);
            Color titleColor = Enabled ? Theme.Text : Theme.Faint;
            TextRenderer.DrawText(g, Text, Theme.LabelBold, new Rectangle(tx, Theme.S(this, 10), Width - tx - Theme.S(this, 12), Theme.S(this, 22)), titleColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            if (!string.IsNullOrEmpty(_description))
            {
                TextRenderer.DrawText(g, _description, Theme.Small, new Rectangle(tx, Theme.S(this, 31), Width - tx - Theme.S(this, 12), Theme.S(this, 20)), Theme.Muted,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            }
            if (Focused && ShowFocusCues)
            {
                Theme.DrawRounded(g, new Rectangle(Theme.S(this, 3), Theme.S(this, 3), Width - Theme.S(this, 6), Height - Theme.S(this, 6)), Theme.S(this, 8), Color.FromArgb(100, Theme.Accent), 1f);
            }
        }
    }

    internal sealed class SourceRow : Control
    {
        private readonly ScannerInfo _info;
        private bool _selected;
        private bool _hover;

        public SourceRow(ScannerInfo info)
        {
            _info = info;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.Selectable, true);
            BackColor = Theme.Bg;
            Height = 68;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public ScannerInfo Info { get { return _info; } }
        public bool Selected { get { return _selected; } set { _selected = value; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = _selected ? Theme.AccentSoft : (_hover ? Theme.SurfaceAlt : Theme.Surface);
            Color border = _selected ? Color.FromArgb(168, 196, 247) : (_hover ? Theme.BorderStrong : Theme.Border);
            using (GraphicsPath path = Theme.RoundedRect(r, Theme.S(this, 10)))
            using (SolidBrush b = new SolidBrush(fill))
            using (Pen p = new Pen(border, Theme.S(this, 1)))
            {
                p.Alignment = PenAlignment.Inset;
                g.FillPath(b, path);
                g.DrawPath(p, path);
            }
            int tile = Theme.S(this, 38);
            Rectangle tr = new Rectangle(Theme.S(this, 13), (Height - tile) / 2, tile, tile);
            Theme.FillRounded(g, tr, Theme.S(this, 10), _selected ? Color.White : Theme.AccentSoft);
            IconPainter.Draw(g, IconKind.Scanner, new RectangleF(tr.X + Theme.S(this, 9), tr.Y + Theme.S(this, 9), tile - Theme.S(this, 18), tile - Theme.S(this, 18)), Theme.Accent, tile, false);
            int tx = tr.Right + Theme.S(this, 12);
            int radio = Theme.S(this, 18);
            int right = Width - Theme.S(this, 15) - radio;
            TextRenderer.DrawText(g, Theme.FitText(g, _info.Name, Theme.LabelBold, right - tx - Theme.S(this, 10)), Theme.LabelBold, new Rectangle(tx, Theme.S(this, 22), right - tx - Theme.S(this, 10), Theme.S(this, 22)), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            string sub = _info.DisplayManufacturer;
            TextRenderer.DrawText(g, sub, Theme.Small, new Rectangle(tx, Theme.S(this, 42), right - tx - Theme.S(this, 10), Theme.S(this, 18)), Theme.Muted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            Rectangle rr = new Rectangle(right, (Height - radio) / 2, radio, radio);
            if (_selected)
            {
                using (SolidBrush b = new SolidBrush(Theme.Accent)) g.FillEllipse(b, rr);
                using (SolidBrush b = new SolidBrush(Color.White)) g.FillEllipse(b, rr.X + Theme.S(this, 6), rr.Y + Theme.S(this, 6), radio - Theme.S(this, 12), radio - Theme.S(this, 12));
            }
            else
            {
                using (SolidBrush b = new SolidBrush(Theme.Surface)) g.FillEllipse(b, rr);
                using (Pen p = new Pen(Theme.BorderStrong, 1f)) g.DrawEllipse(p, rr);
            }
        }
    }

    internal sealed class SourceSelectDialog : Form
    {
        private readonly Func<List<ScannerInfo>> _provider;
        private readonly Panel _list;
        private readonly Label _empty;
        private readonly Label _error;
        private readonly FlatButton _selectButton;
        private readonly FlatButton _refreshButton;
        private readonly ToolTip _tip;
        private List<ScannerInfo> _sources = new List<ScannerInfo>();
        private string _selected;

        public string SelectedSource { get { return _selected; } }

        public SourceSelectDialog(Func<List<ScannerInfo>> provider, string currentSource)
        {
            _provider = provider;
            _selected = currentSource;
            Text = "Выбор сканера";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg;
            Font = Theme.Body;
            AutoScaleMode = AutoScaleMode.None;
            CreateHandle();
            ClientSize = new Size(Theme.S(this, 560), Theme.S(this, 500));
            KeyPreview = true;

            Label title = new Label();
            title.Text = "Выберите сканер";
            title.Font = Theme.DialogTitle;
            title.ForeColor = Theme.Text;
            title.BackColor = Theme.Bg;
            title.AutoSize = false;
            title.SetBounds(Theme.S(this, 24), Theme.S(this, 20), Theme.S(this, 400), Theme.S(this, 30));
            Controls.Add(title);

            Label sub = new Label();
            sub.Text = "Доступные TWAIN-модули сканирования на этом компьютере";
            sub.Font = Theme.Small;
            sub.ForeColor = Theme.Muted;
            sub.BackColor = Theme.Bg;
            sub.AutoSize = false;
            sub.SetBounds(Theme.S(this, 24), Theme.S(this, 50), Theme.S(this, 460), Theme.S(this, 20));
            Controls.Add(sub);

            _refreshButton = new FlatButton();
            _refreshButton.Kind = ButtonKind.Ghost;
            _refreshButton.Icon = IconKind.Refresh;
            _refreshButton.ShowText = false;
            _refreshButton.IconSize = 18;
            _refreshButton.BackColor = Theme.Bg;
            _refreshButton.SetBounds(ClientSize.Width - Theme.S(this, 60), Theme.S(this, 22), Theme.S(this, 36), Theme.S(this, 34));
            _refreshButton.Click += delegate { LoadSources(); };
            Controls.Add(_refreshButton);
            _tip = new ToolTip();
            _tip.SetToolTip(_refreshButton, "Обновить список сканеров");

            _list = new Panel();
            _list.BackColor = Theme.Bg;
            _list.AutoScroll = true;
            _list.SetBounds(Theme.S(this, 18), Theme.S(this, 80), ClientSize.Width - Theme.S(this, 36), ClientSize.Height - Theme.S(this, 160));
            _list.Resize += delegate { LayoutRows(); };
            Controls.Add(_list);

            _empty = new Label();
            _empty.Text = "Сканеры не найдены.\r\nПодключите сканер и нажмите кнопку обновления.";
            _empty.Font = Theme.Body;
            _empty.ForeColor = Theme.Muted;
            _empty.BackColor = Theme.Bg;
            _empty.TextAlign = ContentAlignment.MiddleCenter;
            _empty.AutoSize = false;
            _empty.SetBounds(Theme.S(this, 34), Theme.S(this, 150), ClientSize.Width - Theme.S(this, 68), Theme.S(this, 70));
            Controls.Add(_empty);

            _error = new Label();
            _error.Font = Theme.Small;
            _error.ForeColor = Theme.Red;
            _error.BackColor = Theme.Bg;
            _error.AutoSize = false;
            _error.SetBounds(Theme.S(this, 24), ClientSize.Height - Theme.S(this, 112), ClientSize.Width - Theme.S(this, 48), Theme.S(this, 20));
            Controls.Add(_error);

            BorderPanel footer = new BorderPanel();
            footer.DrawBottom = false;
            footer.DrawTop = true;
            footer.BackColor = Theme.Bg;
            footer.SetBounds(0, ClientSize.Height - Theme.S(this, 70), ClientSize.Width, Theme.S(this, 70));
            Controls.Add(footer);

            FlatButton cancel = new FlatButton();
            cancel.Kind = ButtonKind.Secondary;
            cancel.Text = "Отмена";
            cancel.BackColor = Theme.Bg;
            cancel.SetBounds(ClientSize.Width - Theme.S(this, 24) - Theme.S(this, 104) - Theme.S(this, 10) - Theme.S(this, 138), Theme.S(this, 16), Theme.S(this, 104), Theme.S(this, 38));
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            footer.Controls.Add(cancel);

            _selectButton = new FlatButton();
            _selectButton.Kind = ButtonKind.Primary;
            _selectButton.Text = "Выбрать";
            _selectButton.BackColor = Theme.Bg;
            _selectButton.SetBounds(ClientSize.Width - Theme.S(this, 24) - Theme.S(this, 138), Theme.S(this, 16), Theme.S(this, 138), Theme.S(this, 38));
            _selectButton.Enabled = false;
            _selectButton.Click += delegate { AcceptSelection(); };
            footer.Controls.Add(_selectButton);

            KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
                else if (e.KeyCode == Keys.Enter && _selectButton.Enabled) AcceptSelection();
            };

            Shown += delegate { LoadSources(); };
        }

        private void LoadSources()
        {
            _error.Text = string.Empty;
            Cursor = Cursors.WaitCursor;
            try
            {
                if (_provider != null) _sources = _provider();
                if (_sources == null) _sources = new List<ScannerInfo>();
            }
            catch (Exception ex)
            {
                _sources = new List<ScannerInfo>();
                _error.Text = "Не удалось получить список: " + ex.Message;
            }
            finally { Cursor = Cursors.Default; }
            BuildRows();
        }

        private void BuildRows()
        {
            _list.SuspendLayout();
            while (_list.Controls.Count > 0)
            {
                Control c = _list.Controls[0];
                _list.Controls.RemoveAt(0);
                c.Dispose();
            }
            bool anySelected = false;
            int y = 0;
            for (int i = 0; i < _sources.Count; i++)
            {
                SourceRow row = new SourceRow(_sources[i]);
                row.SetBounds(0, y, Math.Max(Theme.S(this, 200), _list.ClientSize.Width - Theme.S(this, 18)), Theme.S(this, 68));
                row.Selected = string.Equals(_sources[i].Name, _selected, StringComparison.OrdinalIgnoreCase);
                if (row.Selected) anySelected = true;
                row.Click += delegate(object s, EventArgs e)
                {
                    SourceRow clicked = (SourceRow)s;
                    _selected = clicked.Info.Name;
                    foreach (Control c in _list.Controls)
                    {
                        SourceRow r = c as SourceRow;
                        if (r != null) r.Selected = r == clicked;
                    }
                    _selectButton.Enabled = true;
                };
                row.DoubleClick += delegate { AcceptSelection(); };
                _list.Controls.Add(row);
                y += Theme.S(this, 76);
            }
            _empty.Visible = _sources.Count == 0;
            _selectButton.Enabled = anySelected;
            _list.ResumeLayout();
        }

        private void LayoutRows()
        {
            int y = 0;
            foreach (Control c in _list.Controls)
            {
                SourceRow row = c as SourceRow;
                if (row == null) continue;
                row.SetBounds(0, y, Math.Max(Theme.S(this, 200), _list.ClientSize.Width - Theme.S(this, 18)), Theme.S(this, 68));
                y += Theme.S(this, 76);
            }
        }

        private void AcceptSelection()
        {
            if (string.IsNullOrEmpty(_selected)) return;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class SettingsDialog : Form
    {
        private readonly ScanSettings _original;
        private readonly ScannerCapabilities _caps;
        private readonly ComboBox _color;
        private readonly ComboBox _dpi;
        private readonly ComboBox _paper;
        private readonly CheckBox _feeder;
        private readonly CheckBox _duplex;
        private readonly Label _note;
        private ScanSettings _result;

        public ScanSettings ResultSettings { get { return _result; } }

        private sealed class ComboItem
        {
            public string Text;
            public object Value;
            public override string ToString() { return Text; }
        }

        public SettingsDialog(ScanSettings current, ScannerCapabilities caps, string scannerName)
        {
            _original = current.Clone();
            _caps = caps != null ? caps : ScannerCapabilities.Defaults();
            Text = "Настройки сканирования";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg;
            Font = Theme.Body;
            AutoScaleMode = AutoScaleMode.None;
            CreateHandle();
            ClientSize = new Size(Theme.S(this, 560), Theme.S(this, 454));
            KeyPreview = true;

            Label title = new Label();
            title.Text = "Настройки сканирования";
            title.Font = Theme.DialogTitle;
            title.ForeColor = Theme.Text;
            title.BackColor = Theme.Bg;
            title.AutoSize = false;
            title.SetBounds(Theme.S(this, 24), Theme.S(this, 18), Theme.S(this, 450), Theme.S(this, 30));
            Controls.Add(title);

            Label sub = new Label();
            sub.Text = string.IsNullOrEmpty(scannerName) ? "Параметры по умолчанию" : scannerName;
            sub.Font = Theme.Small;
            sub.ForeColor = Theme.Muted;
            sub.BackColor = Theme.Bg;
            sub.AutoSize = false;
            sub.SetBounds(Theme.S(this, 24), Theme.S(this, 48), Theme.S(this, 500), Theme.S(this, 20));
            Controls.Add(sub);

            CardPanel card = new CardPanel();
            card.SetBounds(Theme.S(this, 24), Theme.S(this, 82), ClientSize.Width - Theme.S(this, 48), Theme.S(this, 196));
            Controls.Add(card);

            Label l1 = MakeLabel("Цветовой режим");
            l1.SetBounds(Theme.S(this, 20), Theme.S(this, 16), Theme.S(this, 220), Theme.S(this, 20));
            card.Controls.Add(l1);
            _color = MakeCombo();
            _color.SetBounds(Theme.S(this, 20), Theme.S(this, 38), Theme.S(this, 236), Theme.S(this, 30));
            card.Controls.Add(_color);

            Label l2 = MakeLabel("Разрешение");
            l2.SetBounds(Theme.S(this, 276), Theme.S(this, 16), Theme.S(this, 200), Theme.S(this, 20));
            card.Controls.Add(l2);
            _dpi = MakeCombo();
            _dpi.SetBounds(Theme.S(this, 276), Theme.S(this, 38), Theme.S(this, 212), Theme.S(this, 30));
            card.Controls.Add(_dpi);

            Label l3 = MakeLabel("Формат бумаги");
            l3.SetBounds(Theme.S(this, 20), Theme.S(this, 90), Theme.S(this, 220), Theme.S(this, 20));
            card.Controls.Add(l3);
            _paper = MakeCombo();
            _paper.SetBounds(Theme.S(this, 20), Theme.S(this, 112), Theme.S(this, 236), Theme.S(this, 30));
            card.Controls.Add(_paper);

            _feeder = MakeCheck("Использовать автоподатчик");
            _feeder.SetBounds(Theme.S(this, 276), Theme.S(this, 98), Theme.S(this, 220), Theme.S(this, 24));
            _feeder.Enabled = _caps.SupportsFeeder;
            _feeder.CheckedChanged += delegate { UpdateDuplex(); };
            card.Controls.Add(_feeder);

            _duplex = MakeCheck("Двустороннее сканирование");
            _duplex.SetBounds(Theme.S(this, 276), Theme.S(this, 128), Theme.S(this, 230), Theme.S(this, 24));
            _duplex.Enabled = _caps.SupportsDuplex && _feeder.Enabled;
            card.Controls.Add(_duplex);

            Label hint = new Label();
            hint.Text = "Совет: для текстовых документов оптимально 300 dpi.";
            hint.Font = Theme.Small;
            hint.ForeColor = Theme.Muted;
            hint.BackColor = Theme.Surface;
            hint.AutoSize = false;
            hint.SetBounds(Theme.S(this, 20), Theme.S(this, 158), Theme.S(this, 470), Theme.S(this, 20));
            card.Controls.Add(hint);

            _note = new Label();
            _note.Font = Theme.Small;
            _note.ForeColor = Theme.Amber;
            _note.BackColor = Theme.Bg;
            _note.AutoSize = false;
            _note.SetBounds(Theme.S(this, 24), Theme.S(this, 288), ClientSize.Width - Theme.S(this, 48), Theme.S(this, 54));
            Controls.Add(_note);
            if (string.IsNullOrEmpty(scannerName))
            {
                _note.Text = "Сканер не выбран. Параметры сохранятся и будут применены после выбора сканера.";
            }
            else if (!_caps.SupportsFeeder)
            {
                _note.Text = "У этого сканера автоподатчик не обнаружен. Доступно сканирование с планшета.";
            }

            BorderPanel footer = new BorderPanel();
            footer.DrawBottom = false;
            footer.DrawTop = true;
            footer.BackColor = Theme.Bg;
            footer.SetBounds(0, ClientSize.Height - Theme.S(this, 76), ClientSize.Width, Theme.S(this, 76));
            Controls.Add(footer);

            FlatButton cancel = new FlatButton();
            cancel.Kind = ButtonKind.Secondary;
            cancel.Text = "Отмена";
            cancel.BackColor = Theme.Bg;
            cancel.SetBounds(ClientSize.Width - Theme.S(this, 24) - Theme.S(this, 104) - Theme.S(this, 10) - Theme.S(this, 138), Theme.S(this, 19), Theme.S(this, 104), Theme.S(this, 38));
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            footer.Controls.Add(cancel);

            FlatButton ok = new FlatButton();
            ok.Kind = ButtonKind.Primary;
            ok.Text = "Применить";
            ok.BackColor = Theme.Bg;
            ok.SetBounds(ClientSize.Width - Theme.S(this, 24) - Theme.S(this, 138), Theme.S(this, 19), Theme.S(this, 138), Theme.S(this, 38));
            ok.Click += delegate { Accept(); };
            footer.Controls.Add(ok);

            KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
                else if (e.KeyCode == Keys.Enter) { Accept(); }
            };

            FillCombos();
        }

        private Label MakeLabel(string text)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Theme.LabelBold;
            l.ForeColor = Theme.TextSoft;
            l.BackColor = Theme.Surface;
            l.AutoSize = false;
            return l;
        }

        private ComboBox MakeCombo()
        {
            ComboBox c = new ComboBox();
            c.DropDownStyle = ComboBoxStyle.DropDownList;
            c.FlatStyle = FlatStyle.Flat;
            c.Font = Theme.Body;
            c.BackColor = Theme.Surface;
            c.ForeColor = Theme.Text;
            return c;
        }

        private CheckBox MakeCheck(string text)
        {
            CheckBox c = new CheckBox();
            c.Text = text;
            c.Font = Theme.Label;
            c.ForeColor = Theme.TextSoft;
            c.BackColor = Theme.Surface;
            c.FlatStyle = FlatStyle.Flat;
            c.AutoSize = false;
            return c;
        }

        private void FillCombos()
        {
            List<ScanColorMode> modes = new List<ScanColorMode>(_caps.ColorModes);
            if (modes.Count == 0) modes.AddRange(new ScanColorMode[] { ScanColorMode.Color, ScanColorMode.Gray, ScanColorMode.BlackWhite });
            for (int i = 0; i < modes.Count; i++)
            {
                ComboItem it = new ComboItem();
                it.Value = modes[i];
                if (modes[i] == ScanColorMode.Color) it.Text = "Цветной";
                else if (modes[i] == ScanColorMode.Gray) it.Text = "Оттенки серого";
                else it.Text = "Чёрно-белый";
                _color.Items.Add(it);
                if (modes[i] == _original.ColorMode) _color.SelectedIndex = _color.Items.Count - 1;
            }
            if (_color.SelectedIndex < 0) _color.SelectedIndex = 0;

            List<int> dpis = new List<int>(_caps.Dpis);
            if (dpis.Count == 0) dpis.AddRange(new int[] { 100, 150, 200, 300, 400, 600 });
            if (!dpis.Contains(_original.Dpi)) dpis.Add(_original.Dpi);
            dpis.Sort();
            for (int i = 0; i < dpis.Count; i++)
            {
                ComboItem it = new ComboItem();
                it.Value = dpis[i];
                it.Text = dpis[i].ToString() + " dpi";
                _dpi.Items.Add(it);
                if (dpis[i] == _original.Dpi) _dpi.SelectedIndex = _dpi.Items.Count - 1;
            }
            if (_dpi.SelectedIndex < 0) _dpi.SelectedIndex = 0;

            List<ScanPaperKind> papers = new List<ScanPaperKind>(_caps.PaperKinds);
            if (papers.Count == 0) papers.AddRange(new ScanPaperKind[] { ScanPaperKind.Auto, ScanPaperKind.A4, ScanPaperKind.A5, ScanPaperKind.Letter });
            for (int i = 0; i < papers.Count; i++)
            {
                ComboItem it = new ComboItem();
                it.Value = papers[i];
                if (papers[i] == ScanPaperKind.Auto) it.Text = "Авто (по размеру оригинала)";
                else if (papers[i] == ScanPaperKind.A4) it.Text = "A4";
                else if (papers[i] == ScanPaperKind.A5) it.Text = "A5";
                else it.Text = "Letter";
                _paper.Items.Add(it);
                if (papers[i] == _original.Paper) _paper.SelectedIndex = _paper.Items.Count - 1;
            }
            if (_paper.SelectedIndex < 0) _paper.SelectedIndex = 0;

            _feeder.Checked = _caps.SupportsFeeder && _original.UseFeeder;
            _duplex.Checked = _caps.SupportsDuplex && _original.Duplex;
            UpdateDuplex();
        }

        private void UpdateDuplex()
        {
            _duplex.Enabled = _feeder.Enabled && _feeder.Checked && _caps.SupportsDuplex;
            if (!_duplex.Enabled) _duplex.Checked = false;
        }

        private void Accept()
        {
            ScanSettings s = _original.Clone();
            ComboItem ci = _color.SelectedItem as ComboItem;
            if (ci != null) s.ColorMode = (ScanColorMode)ci.Value;
            ci = _dpi.SelectedItem as ComboItem;
            if (ci != null) s.Dpi = (int)ci.Value;
            ci = _paper.SelectedItem as ComboItem;
            if (ci != null) s.Paper = (ScanPaperKind)ci.Value;
            s.UseFeeder = _caps.SupportsFeeder && _feeder.Checked;
            s.Duplex = s.UseFeeder && _caps.SupportsDuplex && _duplex.Checked;
            _result = s;
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal static class SavePath
    {
        public static string DesktopFolder
        {
            get
            {
                string d = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (string.IsNullOrEmpty(d)) return string.Empty;
                try { return Path.GetFullPath(d).TrimEnd('\\', '/'); }
                catch { return d.TrimEnd('\\', '/'); }
            }
        }

        public static string Display(string full)
        {
            if (string.IsNullOrEmpty(full)) return string.Empty;
            string desktop = DesktopFolder;
            string f;
            try { f = Path.GetFullPath(full).TrimEnd('\\', '/'); }
            catch { return full; }
            if (desktop.Length == 0) return f;
            if (string.Equals(f, desktop, StringComparison.OrdinalIgnoreCase)) return "Рабочий стол";
            if (f.StartsWith(desktop + "\\", StringComparison.OrdinalIgnoreCase))
                return "Рабочий стол\\" + f.Substring(desktop.Length + 1);
            return f;
        }

        public static string Resolve(string text)
        {
            string t = (text ?? string.Empty).Trim().Trim('"');
            if (t.Length == 0) return t;
            string desktop = DesktopFolder;
            if (desktop.Length == 0) return t;
            if (string.Equals(t, "Рабочий стол", StringComparison.OrdinalIgnoreCase)) return desktop;
            int cut = "Рабочий стол".Length;
            if (t.Length > cut && (t[cut] == '\\' || t[cut] == '/'))
                return Path.Combine(desktop, t.Substring(cut + 1));
            return t;
        }
    }

    internal sealed class ExportPdfDialog : Form
    {
        private readonly int _count;
        private readonly CheckOption _perPage;
        private readonly CheckBox _always;
        private readonly Label _pathLabel;
        private readonly TextBox _path;
        private readonly Label _hint;
        private readonly Label _error;
        private string _folder;
        private string _singleFile;
        private bool _loading;

        public bool PerPage { get { return _perPage.Checked; } }
        public bool AlwaysSave { get { return _always.Checked; } }
        public string OutputPath { get { return ResolvePath(_path.Text); } }
        public string OutputFolder
        {
            get
            {
                string p = OutputPath;
                if (PerPage) return p;
                try { return Path.GetDirectoryName(p); }
                catch { return string.Empty; }
            }
        }

        public ExportPdfDialog(int count, string initialFolder, bool alwaysSave)
        {
            _count = count;
            _folder = string.IsNullOrEmpty(initialFolder) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : initialFolder;
            if (string.IsNullOrEmpty(_folder)) _folder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            _singleFile = Path.Combine(_folder, "Скан.pdf");

            Text = "Сохранить в PDF";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg;
            Font = Theme.Body;
            AutoScaleMode = AutoScaleMode.None;
            CreateHandle();
            ClientSize = new Size(Theme.S(this, 580), Theme.S(this, 466));
            KeyPreview = true;

            Label title = new Label();
            title.Text = "Сохранить в PDF";
            title.Font = Theme.DialogTitle;
            title.ForeColor = Theme.Text;
            title.BackColor = Theme.Bg;
            title.AutoSize = false;
            title.SetBounds(Theme.S(this, 24), Theme.S(this, 20), Theme.S(this, 420), Theme.S(this, 30));
            Controls.Add(title);

            Label sub = new Label();
            sub.Text = "Выбрано страниц: " + count;
            sub.Font = Theme.Small;
            sub.ForeColor = Theme.Muted;
            sub.BackColor = Theme.Bg;
            sub.AutoSize = false;
            sub.SetBounds(Theme.S(this, 24), Theme.S(this, 50), Theme.S(this, 420), Theme.S(this, 20));
            Controls.Add(sub);

            _perPage = new CheckOption();
            _perPage.Text = "Каждая страница отдельно";
            _perPage.Description = "Создать отдельный PDF-файл для каждой страницы";
            _perPage.BackColor = Theme.Bg;
            _perPage.SetBounds(Theme.S(this, 24), Theme.S(this, 86), ClientSize.Width - Theme.S(this, 48), Theme.S(this, 66));
            _perPage.CheckedChanged += delegate { UpdateMode(); };
            Controls.Add(_perPage);

            _pathLabel = new Label();
            _pathLabel.Text = "Файл";
            _pathLabel.Font = Theme.LabelBold;
            _pathLabel.ForeColor = Theme.TextSoft;
            _pathLabel.BackColor = Theme.Bg;
            _pathLabel.AutoSize = false;
            _pathLabel.SetBounds(Theme.S(this, 24), Theme.S(this, 174), Theme.S(this, 200), Theme.S(this, 20));
            Controls.Add(_pathLabel);

            _path = new TextBox();
            _path.Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
            _path.BorderStyle = BorderStyle.FixedSingle;
            _path.BackColor = Theme.Surface;
            _path.ForeColor = Theme.Text;
            _path.SetBounds(Theme.S(this, 24), Theme.S(this, 196), ClientSize.Width - Theme.S(this, 48) - Theme.S(this, 92), Theme.S(this, 28));
            Controls.Add(_path);

            FlatButton browse = new FlatButton();
            browse.Kind = ButtonKind.Secondary;
            browse.Icon = IconKind.Folder;
            browse.Text = "Обзор…";
            browse.BackColor = Theme.Bg;
            browse.SetBounds(ClientSize.Width - Theme.S(this, 24) - Theme.S(this, 82), Theme.S(this, 193), Theme.S(this, 82), Theme.S(this, 34));
            browse.Click += delegate { Browse(); };
            Controls.Add(browse);

            _always = new CheckBox();
            _always.Text = "Всегда сохранять в эту папку";
            _always.Font = Theme.Small;
            _always.ForeColor = Theme.TextSoft;
            _always.BackColor = Theme.Bg;
            _always.FlatStyle = FlatStyle.Flat;
            _always.AutoSize = false;
            _always.Cursor = Cursors.Hand;
            _always.SetBounds(Theme.S(this, 22), Theme.S(this, 232), ClientSize.Width - Theme.S(this, 44), Theme.S(this, 24));
            _always.Checked = alwaysSave;
            Controls.Add(_always);

            _hint = new Label();
            _hint.Font = Theme.Small;
            _hint.ForeColor = Theme.Muted;
            _hint.BackColor = Theme.Bg;
            _hint.AutoSize = false;
            _hint.SetBounds(Theme.S(this, 24), Theme.S(this, 262), ClientSize.Width - Theme.S(this, 48), Theme.S(this, 38));
            Controls.Add(_hint);

            _error = new Label();
            _error.Font = Theme.Small;
            _error.ForeColor = Theme.Red;
            _error.BackColor = Theme.Bg;
            _error.AutoSize = false;
            _error.SetBounds(Theme.S(this, 24), Theme.S(this, 302), ClientSize.Width - Theme.S(this, 48), Theme.S(this, 40));
            Controls.Add(_error);

            BorderPanel footer = new BorderPanel();
            footer.DrawBottom = false;
            footer.DrawTop = true;
            footer.BackColor = Theme.Bg;
            footer.SetBounds(0, ClientSize.Height - Theme.S(this, 76), ClientSize.Width, Theme.S(this, 76));
            Controls.Add(footer);

            FlatButton cancel = new FlatButton();
            cancel.Kind = ButtonKind.Secondary;
            cancel.Text = "Отмена";
            cancel.BackColor = Theme.Bg;
            cancel.SetBounds(ClientSize.Width - Theme.S(this, 24) - Theme.S(this, 104) - Theme.S(this, 10) - Theme.S(this, 138), Theme.S(this, 19), Theme.S(this, 104), Theme.S(this, 38));
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            footer.Controls.Add(cancel);

            FlatButton ok = new FlatButton();
            ok.Kind = ButtonKind.Primary;
            ok.Icon = IconKind.Save;
            ok.Text = "Сохранить";
            ok.BackColor = Theme.Bg;
            ok.SetBounds(ClientSize.Width - Theme.S(this, 24) - Theme.S(this, 138), Theme.S(this, 19), Theme.S(this, 138), Theme.S(this, 38));
            ok.Click += delegate { Accept(); };
            footer.Controls.Add(ok);

            KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
                else if (e.KeyCode == Keys.Enter && !_path.Focused) { Accept(); }
            };

            _loading = true;
            _perPage.Checked = false;
            _path.Text = _singleFile;
            _loading = false;
            UpdateMode();
        }

        private static string DesktopFolder
        {
            get { return SavePath.DesktopFolder; }
        }

        private static string DisplayPath(string full)
        {
            return SavePath.Display(full);
        }

        private static string ResolvePath(string text)
        {
            return SavePath.Resolve(text);
        }

        private void UpdateMode()
        {
            if (_loading) return;
            _error.Text = string.Empty;
            string current = ResolvePath(_path.Text);
            if (_perPage.Checked)
            {
                _pathLabel.Text = "Папка для PDF-файлов";
                if (!string.IsNullOrEmpty(current) && current.ToLowerInvariant().EndsWith(".pdf")) _singleFile = current;
                else if (!string.IsNullOrEmpty(current) && Directory.Exists(current)) _folder = current;
                _path.Text = DisplayPath(_folder);
                _hint.Text = "Будут созданы файлы: Скан_001.pdf, Скан_002.pdf и т. д. (" + _count + " шт.)";
            }
            else
            {
                _pathLabel.Text = "Файл";
                _path.Text = DisplayPath(_singleFile);
                _hint.Text = "Все выбранные страницы будут сохранены в один документ.";
            }
        }

        private void Browse()
        {
            if (_perPage.Checked)
            {
                using (FolderBrowserDialog f = new FolderBrowserDialog())
                {
                    f.Description = "Выберите папку для PDF-файлов";
                    string current = ResolvePath(_path.Text);
                    f.SelectedPath = Directory.Exists(current) ? current : _folder;
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        _folder = f.SelectedPath;
                        _path.Text = DisplayPath(_folder);
                    }
                }
            }
            else
            {
                using (SaveFileDialog s = new SaveFileDialog())
                {
                    s.Title = "Сохранить PDF";
                    s.Filter = "PDF-файл (*.pdf)|*.pdf";
                    s.DefaultExt = "pdf";
                    s.AddExtension = true;
                    s.OverwritePrompt = true;
                    string current = ResolvePath(_path.Text);
                    s.FileName = Path.GetFileName(current);
                    try { s.InitialDirectory = Path.GetDirectoryName(current); } catch { }
                    if (s.ShowDialog(this) == DialogResult.OK)
                    {
                        _singleFile = s.FileName;
                        _path.Text = DisplayPath(_singleFile);
                    }
                }
            }
        }

        private void Accept()
        {
            string p = ResolvePath(_path.Text).Trim();
            if (p.Length == 0)
            {
                _error.Text = "Укажите путь для сохранения.";
                return;
            }
            try
            {
                p = Path.GetFullPath(p);
                if (_perPage.Checked)
                {
                    if (File.Exists(p))
                    {
                        _error.Text = "Укажите папку, а не существующий файл.";
                        return;
                    }
                    Directory.CreateDirectory(p);
                    _folder = p;
                    _path.Text = DisplayPath(p);
                }
                else
                {
                    if (Directory.Exists(p)) p = Path.Combine(p, "Скан.pdf");
                    if (!p.ToLowerInvariant().EndsWith(".pdf")) p += ".pdf";
                    string dir = Path.GetDirectoryName(p);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    _singleFile = p;
                    _path.Text = DisplayPath(p);
                }
            }
            catch (Exception ex)
            {
                _error.Text = "Некорректный путь: " + ex.Message;
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class ExportJpgDialog : Form
    {
        private readonly bool _multi;
        private readonly CheckBox _always;
        private readonly Label _pathLabel;
        private readonly TextBox _path;
        private readonly Label _hint;
        private readonly Label _error;
        private string _folder;
        private string _singleFile;

        public bool Multi { get { return _multi; } }
        public bool AlwaysSave { get { return _always.Checked; } }
        public string OutputPath { get { return SavePath.Resolve(_path.Text); } }
        public string OutputFolder
        {
            get
            {
                string p = OutputPath;
                if (_multi) return p;
                try { return Path.GetDirectoryName(p); }
                catch { return string.Empty; }
            }
        }

        public ExportJpgDialog(int count, string initialFolder, bool alwaysSave)
        {
            _multi = count > 1;
            _folder = string.IsNullOrEmpty(initialFolder) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : initialFolder;
            if (string.IsNullOrEmpty(_folder)) _folder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            _singleFile = Path.Combine(_folder, "Скан.jpg");

            Text = "Сохранить в JPG";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg;
            Font = Theme.Body;
            AutoScaleMode = AutoScaleMode.None;
            CreateHandle();
            ClientSize = new Size(Theme.S(this, 560), Theme.S(this, 372));
            KeyPreview = true;

            Label title = new Label();
            title.Text = "Сохранить в JPG";
            title.Font = Theme.DialogTitle;
            title.ForeColor = Theme.Text;
            title.BackColor = Theme.Bg;
            title.AutoSize = false;
            title.SetBounds(Theme.S(this, 24), Theme.S(this, 18), Theme.S(this, 440), Theme.S(this, 30));
            Controls.Add(title);

            Label sub = new Label();
            sub.Text = "Выбрано страниц: " + count;
            sub.Font = Theme.Small;
            sub.ForeColor = Theme.Muted;
            sub.BackColor = Theme.Bg;
            sub.AutoSize = false;
            sub.SetBounds(Theme.S(this, 24), Theme.S(this, 48), Theme.S(this, 440), Theme.S(this, 20));
            Controls.Add(sub);

            Label desc = new Label();
            desc.Text = "JPG сохраняются без распознавания текста.";
            desc.Font = Theme.Small;
            desc.ForeColor = Theme.Muted;
            desc.BackColor = Theme.Bg;
            desc.AutoSize = false;
            desc.SetBounds(Theme.S(this, 24), Theme.S(this, 78), ClientSize.Width - Theme.S(this, 48), Theme.S(this, 22));
            Controls.Add(desc);

            _pathLabel = new Label();
            _pathLabel.Text = _multi ? "Папка для JPG-файлов" : "Файл";
            _pathLabel.Font = Theme.LabelBold;
            _pathLabel.ForeColor = Theme.TextSoft;
            _pathLabel.BackColor = Theme.Bg;
            _pathLabel.AutoSize = false;
            _pathLabel.SetBounds(Theme.S(this, 24), Theme.S(this, 118), Theme.S(this, 260), Theme.S(this, 20));
            Controls.Add(_pathLabel);

            _path = new TextBox();
            _path.Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
            _path.BorderStyle = BorderStyle.FixedSingle;
            _path.BackColor = Theme.Surface;
            _path.ForeColor = Theme.Text;
            _path.SetBounds(Theme.S(this, 24), Theme.S(this, 140), ClientSize.Width - Theme.S(this, 48) - Theme.S(this, 92), Theme.S(this, 28));
            Controls.Add(_path);

            FlatButton browse = new FlatButton();
            browse.Kind = ButtonKind.Secondary;
            browse.Icon = IconKind.Folder;
            browse.Text = "Обзор…";
            browse.BackColor = Theme.Bg;
            browse.SetBounds(ClientSize.Width - Theme.S(this, 24) - Theme.S(this, 82), Theme.S(this, 137), Theme.S(this, 82), Theme.S(this, 34));
            browse.Click += delegate { Browse(); };
            Controls.Add(browse);

            _always = new CheckBox();
            _always.Text = "Всегда сохранять в эту папку";
            _always.Font = Theme.Small;
            _always.ForeColor = Theme.TextSoft;
            _always.BackColor = Theme.Bg;
            _always.FlatStyle = FlatStyle.Flat;
            _always.AutoSize = false;
            _always.Cursor = Cursors.Hand;
            _always.SetBounds(Theme.S(this, 22), Theme.S(this, 182), ClientSize.Width - Theme.S(this, 44), Theme.S(this, 24));
            _always.Checked = alwaysSave;
            Controls.Add(_always);

            _hint = new Label();
            _hint.Text = _multi ? "Будут созданы файлы: Скан_001.jpg, Скан_002.jpg и т. д." : "Текущая страница будет сохранена в JPG.";
            _hint.Font = Theme.Small;
            _hint.ForeColor = Theme.Muted;
            _hint.BackColor = Theme.Bg;
            _hint.AutoSize = false;
            _hint.SetBounds(Theme.S(this, 24), Theme.S(this, 212), ClientSize.Width - Theme.S(this, 48), Theme.S(this, 30));
            Controls.Add(_hint);

            _error = new Label();
            _error.Font = Theme.Small;
            _error.ForeColor = Theme.Red;
            _error.BackColor = Theme.Bg;
            _error.AutoSize = false;
            _error.SetBounds(Theme.S(this, 24), Theme.S(this, 250), ClientSize.Width - Theme.S(this, 48), Theme.S(this, 32));
            Controls.Add(_error);

            BorderPanel footer = new BorderPanel();
            footer.DrawBottom = false;
            footer.DrawTop = true;
            footer.BackColor = Theme.Bg;
            footer.SetBounds(0, ClientSize.Height - Theme.S(this, 76), ClientSize.Width, Theme.S(this, 76));
            Controls.Add(footer);

            FlatButton cancel = new FlatButton();
            cancel.Kind = ButtonKind.Secondary;
            cancel.Text = "Отмена";
            cancel.BackColor = Theme.Bg;
            cancel.SetBounds(ClientSize.Width - Theme.S(this, 24) - Theme.S(this, 104) - Theme.S(this, 10) - Theme.S(this, 138), Theme.S(this, 19), Theme.S(this, 104), Theme.S(this, 38));
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            footer.Controls.Add(cancel);

            FlatButton ok = new FlatButton();
            ok.Kind = ButtonKind.Primary;
            ok.Icon = IconKind.Save;
            ok.Text = "Сохранить";
            ok.BackColor = Theme.Bg;
            ok.SetBounds(ClientSize.Width - Theme.S(this, 24) - Theme.S(this, 138), Theme.S(this, 19), Theme.S(this, 138), Theme.S(this, 38));
            ok.Click += delegate { Accept(); };
            footer.Controls.Add(ok);

            KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
                else if (e.KeyCode == Keys.Enter && !_path.Focused) { Accept(); }
            };

            _path.Text = SavePath.Display(_multi ? _folder : _singleFile);
        }

        private void Browse()
        {
            if (_multi)
            {
                using (FolderBrowserDialog f = new FolderBrowserDialog())
                {
                    f.Description = "Выберите папку для JPG-изображений";
                    string current = SavePath.Resolve(_path.Text);
                    f.SelectedPath = Directory.Exists(current) ? current : _folder;
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        _folder = f.SelectedPath;
                        _path.Text = SavePath.Display(_folder);
                    }
                }
            }
            else
            {
                using (SaveFileDialog s = new SaveFileDialog())
                {
                    s.Title = "Сохранить JPG";
                    s.Filter = "JPEG-изображение (*.jpg)|*.jpg;*.jpeg|Все файлы (*.*)|*.*";
                    s.DefaultExt = "jpg";
                    s.AddExtension = true;
                    s.OverwritePrompt = true;
                    string current = SavePath.Resolve(_path.Text);
                    s.FileName = Path.GetFileName(current);
                    try { s.InitialDirectory = Path.GetDirectoryName(current); } catch { }
                    if (s.ShowDialog(this) == DialogResult.OK)
                    {
                        _singleFile = s.FileName;
                        _path.Text = SavePath.Display(_singleFile);
                    }
                }
            }
        }

        private void Accept()
        {
            string p = SavePath.Resolve(_path.Text).Trim();
            if (p.Length == 0)
            {
                _error.Text = "Укажите путь для сохранения.";
                return;
            }
            try
            {
                p = Path.GetFullPath(p);
                if (_multi)
                {
                    if (File.Exists(p))
                    {
                        _error.Text = "Укажите папку, а не существующий файл.";
                        return;
                    }
                    Directory.CreateDirectory(p);
                    _folder = p;
                    _path.Text = SavePath.Display(p);
                }
                else
                {
                    if (Directory.Exists(p)) p = Path.Combine(p, "Скан.jpg");
                    if (!p.ToLowerInvariant().EndsWith(".jpg") && !p.ToLowerInvariant().EndsWith(".jpeg")) p += ".jpg";
                    string dir = Path.GetDirectoryName(p);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    _singleFile = p;
                    _path.Text = SavePath.Display(p);
                }
            }
            catch (Exception ex)
            {
                _error.Text = "Некорректный путь: " + ex.Message;
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}