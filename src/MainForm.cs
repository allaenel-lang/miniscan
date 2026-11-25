using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MiniScan
{
    internal sealed partial class MainForm : Form
    {
        private readonly AppSettings _app;
        private ScanSettings _settings;
        private ScannerCapabilities _caps = ScannerCapabilities.Defaults();
        private readonly ScannerService _service = new ScannerService();
        private readonly List<PageItem> _pages = new List<PageItem>();
        private readonly HashSet<PageItem> _selected = new HashSet<PageItem>();
        private readonly List<PageThumbnail> _thumbs = new List<PageThumbnail>();
        private PageItem _current;
        private int _anchor = -1;
        private bool _scanActive;
        private bool _busy;
        private bool _closing;
        private int _scanPauseSeconds = 10;
        private int _pauseRemaining;
        private int _batchRemaining;
        private int _batchTotal;
        private Timer _scanTimer;
        private bool _updating;
        private bool _serviceDisposed;
        private string _statusText = "Сканер не выбран";
        private StatusKind _statusKind = StatusKind.Idle;
        private string _scanWarning;
        private string _sourceError;

        private Panel _content;
        private ScannerCard _scannerCard;
        private CardPanel _leftCard;
        private CardPanel _rightCard;
        private BorderPanel _pagesHeader;
        private BorderPanel _previewHeader;
        private Label _pagesTitle;
        private Label _pagesCount;
        private Label _previewTitle;
        private Label _previewMeta;
        private FlatButton _zoomOutButton;
        private FlatButton _zoomInButton;
        private FlatButton _zoomButton;
        private FlatButton _selectAllButton;
        private FlatButton _saveButton;
        private FlatButton _deleteButton;
        private Panel _listHost;
        private FlowLayoutPanel _pagesFlow;
        private EmptyPagesControl _emptyPages;
        private CardPanel _previewHost;
        private PreviewControl _preview;
        private BusyOverlay _busyOverlay;
        private FooterBar _footer;
        private ToolTip _tip;
        private ContextMenuStrip _pageMenu;
        private ContextMenuStrip _saveMenu;

        public MainForm()
        {
            _app = AppSettings.Load();
            _settings = _app.ToScanSettings();
            _scanPauseSeconds = Math.Max(0, Math.Min(120, _app.ScanPause));
            _scanTimer = new Timer();
            _scanTimer.Interval = 1000;
            _scanTimer.Tick += OnScanTimerTick;
            Text = "Mini Scan — сканирование в PDF и JPG";
            BackColor = Theme.Bg;
            Font = Theme.Body;
            AutoScaleMode = AutoScaleMode.None;
            KeyPreview = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            CreateHandle();
            BuildUi();
            BuildMenus();
            ApplyWindowSize();
            _service.PageScanned += OnPageScanned;
            _service.ScanFinished += OnScanFinished;
            Shown += delegate { InitializeStartup(); };
            FormClosing += OnFormClosing;
            KeyDown += OnFormKeyDown;
            Resize += delegate { LayoutAll(); };
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        }

        private void BuildUi()
        {
            _content = new Panel();
            _content.BackColor = Theme.Bg;
            Controls.Add(_content);

            _footer = new FooterBar();
            _footer.Height = Theme.S(this, 38);
            Controls.Add(_footer);

            _scannerCard = new ScannerCard();
            _scannerCard.ScanClick += delegate { OnScanClick(); };
            _scannerCard.PauseChanged += delegate { SaveScanPause(); };
            _scannerCard.ScanCountChanged += delegate { SaveScanCount(); };
            _scannerCard.SourceClick += delegate { ShowSourceDialog(); };
            _scannerCard.SettingsClick += delegate { ShowSettingsDialog(); };
            _scannerCard.SetScanValues(_app.ScanCount, _scanPauseSeconds);
            _content.Controls.Add(_scannerCard);

            BuildLeftCard();
            BuildRightCard();
            LayoutAll();
            UpdateUi();
        }

        private void BuildLeftCard()
        {
            _leftCard = new CardPanel();
            _leftCard.CardColor = Theme.Surface;
            _leftCard.BackColor = Theme.Surface;
            _content.Controls.Add(_leftCard);

            _pagesHeader = new BorderPanel();
            _pagesHeader.DrawTop = false;
            _pagesHeader.DrawBottom = true;
            _pagesHeader.BackColor = Theme.Surface;
            _leftCard.Controls.Add(_pagesHeader);

            _pagesTitle = new Label();
            _pagesTitle.Text = "Страницы";
            _pagesTitle.Font = Theme.Section;
            _pagesTitle.ForeColor = Theme.Text;
            _pagesTitle.BackColor = Theme.Surface;
            _pagesTitle.AutoSize = false;
            _pagesHeader.Controls.Add(_pagesTitle);

            _pagesCount = new Label();
            _pagesCount.Text = "Нет страниц";
            _pagesCount.Font = Theme.Small;
            _pagesCount.ForeColor = Theme.Muted;
            _pagesCount.BackColor = Theme.Surface;
            _pagesCount.AutoSize = false;
            _pagesCount.AutoEllipsis = true;
            _pagesHeader.Controls.Add(_pagesCount);

            _selectAllButton = new FlatButton();
            _selectAllButton.Kind = ButtonKind.Ghost;
            _selectAllButton.Text = "Выбрать все";
            _selectAllButton.Font = Theme.SmallBold;
            _selectAllButton.BackColor = Theme.Surface;
            _selectAllButton.Click += delegate { ToggleSelectAll(); };
            _pagesHeader.Controls.Add(_selectAllButton);

            _listHost = new Panel();
            _listHost.BackColor = Theme.Surface;
            _leftCard.Controls.Add(_listHost);

            _pagesFlow = new FlowLayoutPanel();
            _pagesFlow.Dock = DockStyle.Fill;
            _pagesFlow.BackColor = Theme.Surface;
            _pagesFlow.AutoScroll = true;
            _pagesFlow.FlowDirection = FlowDirection.TopDown;
            _pagesFlow.WrapContents = false;
            _pagesFlow.Padding = new Padding(Theme.S(this, 8), Theme.S(this, 8), Theme.S(this, 8), Theme.S(this, 8));
            _pagesFlow.ClientSizeChanged += delegate { LayoutThumbs(); };
            _listHost.Controls.Add(_pagesFlow);

            _emptyPages = new EmptyPagesControl();
            _emptyPages.Dock = DockStyle.Fill;
            _emptyPages.Visible = true;
            _listHost.Controls.Add(_emptyPages);
            _emptyPages.BringToFront();
        }

        private void BuildRightCard()
        {
            _rightCard = new CardPanel();
            _rightCard.CardColor = Theme.Surface;
            _rightCard.BackColor = Theme.Surface;
            _content.Controls.Add(_rightCard);

            _previewHeader = new BorderPanel();
            _previewHeader.DrawTop = false;
            _previewHeader.DrawBottom = true;
            _previewHeader.BackColor = Theme.Surface;
            _rightCard.Controls.Add(_previewHeader);

            _previewTitle = new Label();
            _previewTitle.Text = "Предпросмотр";
            _previewTitle.Font = Theme.Section;
            _previewTitle.ForeColor = Theme.Text;
            _previewTitle.BackColor = Theme.Surface;
            _previewTitle.AutoSize = false;
            _previewHeader.Controls.Add(_previewTitle);

            _previewMeta = new Label();
            _previewMeta.Text = "Выберите страницу слева";
            _previewMeta.Font = Theme.Small;
            _previewMeta.ForeColor = Theme.Muted;
            _previewMeta.BackColor = Theme.Surface;
            _previewMeta.AutoSize = false;
            _previewMeta.AutoEllipsis = true;
            _previewHeader.Controls.Add(_previewMeta);

            _zoomOutButton = new FlatButton();
            _zoomOutButton.Kind = ButtonKind.Ghost;
            _zoomOutButton.Text = "−";
            _zoomOutButton.Font = Theme.Button;
            _zoomOutButton.BackColor = Theme.Surface;
            _zoomOutButton.Click += delegate { _preview.ZoomOut(); UpdateZoomUi(); };
            _previewHeader.Controls.Add(_zoomOutButton);

            _zoomButton = new FlatButton();
            _zoomButton.Kind = ButtonKind.Soft;
            _zoomButton.Text = "100%";
            _zoomButton.Font = Theme.SmallBold;
            _zoomButton.BackColor = Theme.Surface;
            _zoomButton.Click += delegate { _preview.FitWidth(); UpdateZoomUi(); };
            _previewHeader.Controls.Add(_zoomButton);

            _zoomInButton = new FlatButton();
            _zoomInButton.Kind = ButtonKind.Ghost;
            _zoomInButton.Text = "+";
            _zoomInButton.Font = Theme.Button;
            _zoomInButton.BackColor = Theme.Surface;
            _zoomInButton.Click += delegate { _preview.ZoomIn(); UpdateZoomUi(); };
            _previewHeader.Controls.Add(_zoomInButton);

            _deleteButton = MakeIconButton(IconKind.Trash, "Удалить страницу");
            _deleteButton.Click += delegate { DeleteSelectedPages(); };
            _previewHeader.Controls.Add(_deleteButton);

            _saveButton = new FlatButton();
            _saveButton.Kind = ButtonKind.Soft;
            _saveButton.Icon = IconKind.Save;
            _saveButton.Text = "Сохранить";
            _saveButton.BackColor = Theme.Surface;
            _saveButton.Click += delegate { ShowSaveMenu(); };
            _previewHeader.Controls.Add(_saveButton);

            _previewHost = new CardPanel();
            _previewHost.CardColor = Theme.PreviewBg;
            _previewHost.CardBorder = Theme.PreviewBg;
            _previewHost.Corner = 10;
            _previewHost.BackColor = Theme.PreviewBg;
            _rightCard.Controls.Add(_previewHost);

            _preview = new PreviewControl();
            _preview.Dock = DockStyle.Fill;
            _preview.ZoomChanged += delegate { UpdateZoomUi(); };
            _previewHost.Controls.Add(_preview);

            _busyOverlay = new BusyOverlay();
            _busyOverlay.Dock = DockStyle.Fill;
            _previewHost.Controls.Add(_busyOverlay);
            _busyOverlay.BringToFront();

            _tip = new ToolTip();
            _tip.SetToolTip(_zoomOutButton, "Уменьшить масштаб");
            _tip.SetToolTip(_zoomButton, "Вписать страницу по ширине");
            _tip.SetToolTip(_zoomInButton, "Увеличить масштаб");
            _tip.SetToolTip(_deleteButton, "Удалить выбранные страницы");
            _tip.SetToolTip(_saveButton, "Сохранить в PDF или JPG");
        }

        private FlatButton MakeIconButton(IconKind icon, string tip)
        {
            FlatButton b = new FlatButton();
            b.Kind = ButtonKind.Ghost;
            b.Icon = icon;
            b.ShowText = false;
            b.IconSize = 18;
            b.BackColor = Theme.Surface;
            b.Height = Theme.S(this, 34);
            return b;
        }

        private void ApplyWindowSize()
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            if (wa.Width < 400 || wa.Height < 400) wa = Screen.PrimaryScreen.Bounds;
            if (wa.Width < 400 || wa.Height < 400) wa = new Rectangle(0, 0, 1024, 768);
            int minW = Math.Min(Theme.S(this, 960), wa.Width - 40);
            int minH = Math.Min(Theme.S(this, 640), wa.Height - 40);
            MinimumSize = new Size(Math.Max(800, minW), Math.Max(560, minH));
            int w = _app.WindowWidth > 0 ? _app.WindowWidth : 1240;
            int h = _app.WindowHeight > 0 ? _app.WindowHeight : 800;
            w = Math.Min(Math.Max(w, minW), wa.Width - 30);
            h = Math.Min(Math.Max(h, minH), wa.Height - 30);
            int maxClientH = Math.Max(minH, wa.Height - Theme.S(this, 48));
            if (h > maxClientH) h = maxClientH;
            ClientSize = new Size(w, h);
            StartPosition = FormStartPosition.Manual;
            int left = wa.Left + Math.Max(0, (wa.Width - Width) / 2);
            Location = new Point(left, wa.Top);
            LayoutAll();
        }

        private void LayoutAll()
        {
            if (_content == null || _footer == null) return;
            int fh = Theme.S(this, 36);
            _footer.SetBounds(0, ClientSize.Height - fh, ClientSize.Width, fh);
            _content.SetBounds(0, 0, ClientSize.Width, Math.Max(0, ClientSize.Height - fh));
            LayoutContent();
        }

        private void LayoutContent()
        {
            if (_content == null || _scannerCard == null || _leftCard == null || _rightCard == null) return;
            int pad = Theme.S(this, 18);
            int gap = Theme.S(this, 12);
            int w = Math.Max(0, _content.ClientSize.Width - pad * 2);
            int scannerH = Theme.S(this, 76);
            _scannerCard.SetBounds(0, 0, _content.ClientSize.Width, scannerH);
            int y = scannerH + gap;
            int h = Math.Max(0, _content.ClientSize.Height - y - Theme.S(this, 8));
            int leftW = Theme.S(this, 240);
            leftW = Math.Min(leftW, Math.Max(Theme.S(this, 200), w - gap - Theme.S(this, 420)));
            int rightW = Math.Max(0, w - leftW - gap);
            _leftCard.SetBounds(pad, y, leftW, h);
            _rightCard.SetBounds(pad + leftW + gap, y, rightW, h);
            LayoutLeft();
            LayoutRight();
        }

        private void LayoutLeft()
        {
            if (_pagesHeader == null) return;
            int w = _leftCard.ClientSize.Width;
            int h = _leftCard.ClientSize.Height;
            int headerH = Theme.S(this, 48);
            _pagesHeader.SetBounds(0, 0, w, headerH);
            int pad = Theme.S(this, 12);
            int buttonW = Theme.S(this, 92);
            int textW = Math.Max(0, w - pad * 2 - buttonW - Theme.S(this, 6));
            _pagesTitle.SetBounds(pad, Theme.S(this, 4), textW, Theme.S(this, 22));
            _pagesCount.SetBounds(pad, Theme.S(this, 24), textW, Theme.S(this, 16));
            _selectAllButton.SetBounds(w - pad - buttonW, Theme.S(this, 10), buttonW, Theme.S(this, 28));
            _listHost.SetBounds(0, headerH, w, Math.Max(0, h - headerH));
            LayoutThumbs();
        }

        private void LayoutThumbs()
        {
            if (_pagesFlow == null) return;
            int available = _pagesFlow.ClientSize.Width - _pagesFlow.Padding.Horizontal - Theme.S(this, 14) - 2;
            if (available < Theme.S(this, 150)) available = Theme.S(this, 150);
            int thumbH = Theme.S(this, 102);
            int gap = Theme.S(this, 6);
            for (int i = 0; i < _thumbs.Count; i++)
            {
                _thumbs[i].Width = available;
                _thumbs[i].Height = thumbH;
                _thumbs[i].Margin = new Padding(0, 0, 0, gap);
            }
        }

        private void LayoutRight()
        {
            if (_previewHeader == null) return;
            int w = _rightCard.ClientSize.Width;
            int h = _rightCard.ClientSize.Height;
            int headerH = Theme.S(this, 52);
            _previewHeader.SetBounds(0, 0, w, headerH);
            int pad = Theme.S(this, 16);
            int icon = Theme.S(this, 34);
            int gap = Theme.S(this, 4);
            int saveW = Theme.S(this, 118);
            int zoomW = Theme.S(this, 30);
            int zoomLabelW = Theme.S(this, 54);
            int right = w - pad;
            _saveButton.SetBounds(right - saveW, Theme.S(this, 12), saveW, Theme.S(this, 34));
            int bx = _saveButton.Left - gap - icon;
            _deleteButton.SetBounds(bx, Theme.S(this, 10), icon, icon);
            bx -= gap + icon;
            bx -= gap + zoomW;
            _zoomInButton.SetBounds(bx, Theme.S(this, 10), zoomW, Theme.S(this, 34));
            bx -= gap + zoomLabelW;
            _zoomButton.SetBounds(bx, Theme.S(this, 10), zoomLabelW, Theme.S(this, 34));
            bx -= gap + zoomW;
            _zoomOutButton.SetBounds(bx, Theme.S(this, 10), zoomW, Theme.S(this, 34));
            int textW = Math.Max(0, bx - pad - Theme.S(this, 10));
            _previewTitle.SetBounds(pad, Theme.S(this, 5), textW, Theme.S(this, 23));
            _previewMeta.SetBounds(pad, Theme.S(this, 26), textW, Theme.S(this, 17));
            int margin = Theme.S(this, 12);
            int top = headerH + Theme.S(this, 8);
            _previewHost.SetBounds(margin, top, Math.Max(0, w - margin * 2), Math.Max(0, h - top - margin));
        }

        private void InitializeStartup()
        {
            if (_closing) return;
            Cursor = Cursors.WaitCursor;
            try
            {
                SetStatus("Поиск сканеров…", StatusKind.Busy);
                if (!EnsureTwain()) return;
                List<ScannerInfo> sources = RefreshSourcesInternal();
                if (sources.Count == 0)
                {
                    SetStatus(string.IsNullOrEmpty(_sourceError) ? "Сканеры не найдены" : "Ошибка TWAIN", string.IsNullOrEmpty(_sourceError) ? StatusKind.Warning : StatusKind.Error);
                    return;
                }
                bool savedExists = !string.IsNullOrEmpty(_settings.SourceName) && _service.SourceExists(_settings.SourceName);
                if (savedExists)
                {
                    if (OpenCurrentSource(false))
                    {
                        SetStatus("Готов к работе", StatusKind.Ready);
                    }
                    else
                    {
                        SetStatus("Сканер недоступен", StatusKind.Error);
                    }
                }
                else if (string.IsNullOrEmpty(_settings.SourceName) && sources.Count == 1)
                {
                    _settings.SourceName = sources[0].Name;
                    _app.SourceName = _settings.SourceName;
                    _app.Save();
                    if (OpenCurrentSource(false)) SetStatus("Готов к работе", StatusKind.Ready);
                }
                else
                {
                    if (!string.IsNullOrEmpty(_settings.SourceName))
                    {
                        _settings.SourceName = string.Empty;
                        _app.SourceName = string.Empty;
                        _app.Save();
                    }
                    SetStatus(sources.Count > 1 ? "Выберите сканер" : "Сканер не выбран", StatusKind.Idle);
                }
            }
            finally
            {
                Cursor = Cursors.Default;
                UpdateUi();
            }
        }

        private bool EnsureTwain()
        {
            if (_service.IsInitialized) return true;
            string error;
            if (!_service.Initialize(Handle, out error))
            {
                SetStatus("TWAIN недоступен", StatusKind.Error);
                if (!string.IsNullOrEmpty(error)) _sourceError = error;
                return false;
            }
            return true;
        }

        private List<ScannerInfo> RefreshSourcesInternal()
        {
            string error;
            List<ScannerInfo> list = _service.RefreshSources(out error);
            _sourceError = error;
            return list;
        }

        private bool OpenCurrentSource(bool showError)
        {
            if (string.IsNullOrEmpty(_settings.SourceName)) return false;
            if (!EnsureTwain()) return false;
            string error;
            if (!_service.OpenSource(_settings.SourceName, out error))
            {
                if (showError && !string.IsNullOrEmpty(error)) MessageBox.Show(this, error, "Сканер недоступен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            _caps = _service.GetCapabilities();
            NormalizeSettings();
            _app.Apply(_settings);
            _app.Save();
            return true;
        }

        private void NormalizeSettings()
        {
            if (_caps.Dpis.Count > 0 && !_caps.Dpis.Contains(_settings.Dpi))
            {
                int best = _caps.Dpis[0];
                int delta = Math.Abs(best - _settings.Dpi);
                for (int i = 1; i < _caps.Dpis.Count; i++)
                {
                    int d = Math.Abs(_caps.Dpis[i] - _settings.Dpi);
                    if (d < delta) { delta = d; best = _caps.Dpis[i]; }
                }
                _settings.Dpi = best;
            }
            if (_caps.ColorModes.Count > 0 && !_caps.ColorModes.Contains(_settings.ColorMode)) _settings.ColorMode = _caps.ColorModes[0];
            if (_caps.PaperKinds.Count > 0 && !_caps.PaperKinds.Contains(_settings.Paper))
            {
                _settings.Paper = _caps.PaperKinds.Contains(ScanPaperKind.Auto) ? ScanPaperKind.Auto : _caps.PaperKinds[0];
            }
            if (!_caps.SupportsFeeder) { _settings.UseFeeder = false; _settings.Duplex = false; }
            if (!_caps.SupportsDuplex) _settings.Duplex = false;
        }

        private void ShowSourceDialog()
        {
            if (_busy) return;
            if (!EnsureTwain())
            {
                MessageBox.Show(this, string.IsNullOrEmpty(_sourceError) ? "Не удалось инициализировать TWAIN." : _sourceError, "TWAIN недоступен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Func<List<ScannerInfo>> provider = delegate
            {
                string error;
                List<ScannerInfo> list = _service.RefreshSources(out error);
                _sourceError = error;
                return list;
            };
            string chosen = null;
            using (SourceSelectDialog dlg = new SourceSelectDialog(provider, _settings.SourceName))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK) chosen = dlg.SelectedSource;
            }
            if (string.IsNullOrEmpty(chosen)) return;
            _settings.SourceName = chosen;
            _app.SourceName = chosen;
            _app.Apply(_settings);
            _app.Save();
            if (OpenCurrentSource(true))
            {
                SetStatus("Готов к работе", StatusKind.Ready);
            }
            else
            {
                SetStatus("Сканер недоступен", StatusKind.Error);
            }
            UpdateUi();
        }

        private void ShowSettingsDialog()
        {
            if (_busy) return;
            ScannerCapabilities caps = _caps != null ? _caps : ScannerCapabilities.Defaults();
            if (!string.IsNullOrEmpty(_settings.SourceName))
            {
                if (OpenCurrentSource(false)) caps = _caps;
            }
            using (SettingsDialog dlg = new SettingsDialog(_settings, caps, _settings.SourceName))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.ResultSettings != null)
                {
                    _settings = dlg.ResultSettings;
                    _settings.SourceName = _app.SourceName;
                    _app.Apply(_settings);
                    _app.Save();
                    SetStatus("Настройки сохранены", StatusKind.Ready);
                }
            }
            UpdateUi();
        }

        private bool EnsureScannerForScan()
        {
            if (!EnsureTwain())
            {
                MessageBox.Show(this, string.IsNullOrEmpty(_sourceError) ? "Не удалось инициализировать TWAIN." : _sourceError, "TWAIN недоступен", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (string.IsNullOrEmpty(_settings.SourceName) || !_service.SourceExists(_settings.SourceName))
            {
                ShowSourceDialog();
                if (string.IsNullOrEmpty(_settings.SourceName)) return false;
            }
            if (!OpenCurrentSource(true)) return false;
            return true;
        }

        private void OnScanClick()
        {
            if (_busy || _closing) return;
            if (_batchRemaining > 0)
            {
                StopQueuedScans();
                return;
            }
            StopScanTimer();
            int count = _scannerCard != null ? _scannerCard.ScanCount : 1;
            _batchTotal = Math.Max(1, Math.Min(999, count));
            _batchRemaining = _batchTotal - 1;
            StartScanCore();
        }

        private void StopQueuedScans()
        {
            StopScanTimer();
            _batchRemaining = 0;
            _batchTotal = 0;
            SetStatus("Очередь сканирования остановлена", _scanActive ? StatusKind.Ready : StatusKind.Idle);
            UpdateUi();
        }

        private void StartScanCore()
        {
            if (_busy || _closing) return;
            if (!EnsureScannerForScan())
            {
                AbortBatch();
                return;
            }
            bool newDocument = !_scanActive;
            if (newDocument && HasUnsavedPages())
            {
                DialogResult r = MessageBox.Show(this, "В текущем документе есть несохранённые страницы. Начать новый документ?\r\nТекущие страницы будут удалены.", "Новый документ", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes)
                {
                    AbortBatch();
                    return;
                }
            }
            if (newDocument) ClearDocument();
            _busy = true;
            _scanActive = true;
            _scanWarning = null;
            SetStatus("Сканирование…", StatusKind.Busy);
            _busyOverlay.SetMessage("Сканирование…", "Подождите, сканер передаёт изображение");
            UpdateUi();
            string error;
            string warning;
            bool ok = _service.StartScan(_settings.Clone(), out error, out warning);
            if (!ok)
            {
                _busy = false;
                if (_pages.Count == 0) _scanActive = false;
                SetStatus("Ошибка сканирования", StatusKind.Error);
                UpdateUi();
                AbortBatch();
                if (!string.IsNullOrEmpty(error)) MessageBox.Show(this, error, "Не удалось сканировать", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!string.IsNullOrEmpty(warning)) _scanWarning = warning;
        }

        private void OnPageScanned(object sender, ScannedPageEventArgs e)
        {
            if (_closing) return;
            try
            {
                PageItem page = PageItem.FromBitmap(e.Bitmap, e.DpiX, e.DpiY);
                _pages.Add(page);
                PageThumbnail thumb = new PageThumbnail(page, _pages.Count - 1);
                thumb.Activated += OnThumbActivated;
                thumb.RotateRequested += OnThumbRotate;
                _thumbs.Add(thumb);
                _pagesFlow.Controls.Add(thumb);
                _selected.Clear();
                _selected.Add(page);
                _current = page;
                _anchor = _pages.Count - 1;
                LayoutThumbs();
                SetStatus("Страница " + _pages.Count + " добавлена", StatusKind.Ready);
                UpdateUi();
                try { _pagesFlow.ScrollControlIntoView(thumb); } catch { }
            }
            catch (Exception ex)
            {
                SetStatus("Ошибка обработки страницы", StatusKind.Error);
                MessageBox.Show(this, "Не удалось подготовить изображение: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateUi();
            }
        }

        private void OnScanFinished(object sender, ScanFinishedEventArgs e)
        {
            if (_closing) return;
            _busy = false;
            _busyOverlay.Visible = false;
            if (e.Success)
            {
                _scanActive = _pages.Count > 0;
                if (!string.IsNullOrEmpty(_scanWarning)) SetStatus("Готово · настройки скорректированы", StatusKind.Warning);
                else SetStatus(e.PagesAdded > 1 ? "Добавлено страниц: " + e.PagesAdded : "Готов к работе", StatusKind.Ready);
            }
            else if (e.Canceled)
            {
                _scanActive = _pages.Count > 0;
                SetStatus("Сканирование отменено", _scanActive ? StatusKind.Ready : StatusKind.Idle);
            }
            else
            {
                _scanActive = _pages.Count > 0;
                SetStatus("Ошибка сканирования", StatusKind.Error);
                string msg = string.IsNullOrEmpty(e.Error) ? "Сканер не передал изображение." : e.Error;
                MessageBox.Show(this, msg, "Ошибка сканирования", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            UpdateUi();
            if (e.Success)
            {
                if (_batchRemaining > 0) StartBatchCountdown();
                else FinishBatch();
            }
            else
            {
                AbortBatch();
            }
            RestoreFocusSoon();
        }

        private void SaveScanPause()
        {
            if (_scannerCard == null) return;
            _scanPauseSeconds = Math.Max(0, Math.Min(120, _scannerCard.ScanPause));
            _app.ScanPause = _scanPauseSeconds;
            _app.Save();
            if (_scanTimer != null && _scanTimer.Enabled)
            {
                _pauseRemaining = _scanPauseSeconds;
                if (_pauseRemaining > 0) SetStatus(BatchStatus(), StatusKind.Busy);
                else
                {
                    StopScanTimer();
                    StartNextBatchScan();
                }
            }
        }

        private void SaveScanCount()
        {
            if (_scannerCard == null) return;
            _app.ScanCount = Math.Max(1, Math.Min(999, _scannerCard.ScanCount));
            _app.Save();
        }

        private string BatchStatus()
        {
            return "Осталось " + _batchRemaining + " · пауза " + _pauseRemaining + " с";
        }

        private void StartBatchCountdown()
        {
            if (_closing || _batchRemaining <= 0)
            {
                FinishBatch();
                return;
            }
            StopScanTimer();
            _pauseRemaining = Math.Max(0, _scanPauseSeconds);
            if (_pauseRemaining <= 0)
            {
                StartNextBatchScan();
                return;
            }
            SetStatus(BatchStatus(), StatusKind.Busy);
            _scanTimer.Start();
        }

        private void OnScanTimerTick(object sender, EventArgs e)
        {
            if (_closing || _busy)
            {
                StopScanTimer();
                return;
            }
            _pauseRemaining--;
            if (_pauseRemaining > 0)
            {
                SetStatus(BatchStatus(), StatusKind.Busy);
                return;
            }
            StopScanTimer();
            StartNextBatchScan();
        }

        private void StartNextBatchScan()
        {
            if (_closing || _busy) return;
            if (_batchRemaining <= 0)
            {
                FinishBatch();
                return;
            }
            _batchRemaining--;
            StartScanCore();
        }

        private void FinishBatch()
        {
            int total = _batchTotal;
            StopScanTimer();
            _batchRemaining = 0;
            _batchTotal = 0;
            if (total > 1 && string.IsNullOrEmpty(_scanWarning))
                SetStatus("Готово · сканов: " + total, StatusKind.Ready);
        }

        private void AbortBatch()
        {
            StopScanTimer();
            _batchRemaining = 0;
            _batchTotal = 0;
            if (!_closing && _scannerCard != null) UpdateUi();
        }

        private void StopScanTimer()
        {
            if (_scanTimer != null) _scanTimer.Stop();
            _pauseRemaining = 0;
        }

        private void RestoreFocusSoon()
        {
            if (_closing || IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke(new MethodInvoker(delegate
                {
                    if (_closing || IsDisposed) return;
                    try { NativeFocus.Restore(this); } catch { }
                }));
            }
            catch { }
        }

        private void OnThumbActivated(object sender, ThumbnailMouseEventArgs e)
        {
            PageThumbnail thumb = sender as PageThumbnail;
            if (thumb == null) return;
            int idx = _thumbs.IndexOf(thumb);
            if (idx < 0) return;
            _current = thumb.Page;
            if (e.Button == MouseButtons.Right)
            {
                if (!_selected.Contains(thumb.Page))
                {
                    _selected.Clear();
                    _selected.Add(thumb.Page);
                    _anchor = idx;
                }
                UpdateUi();
                _pageMenu.Show(e.ScreenPoint);
                return;
            }
            if ((e.Modifiers & Keys.Shift) == Keys.Shift && _anchor >= 0)
            {
                _selected.Clear();
                int a = Math.Min(_anchor, idx);
                int b = Math.Max(_anchor, idx);
                for (int i = a; i <= b; i++) _selected.Add(_pages[i]);
            }
            else if ((e.Modifiers & Keys.Control) == Keys.Control)
            {
                if (_selected.Contains(thumb.Page)) _selected.Remove(thumb.Page);
                else _selected.Add(thumb.Page);
                _anchor = idx;
            }
            else
            {
                _selected.Clear();
                _selected.Add(thumb.Page);
                _anchor = idx;
            }
            thumb.Focus();
            UpdateUi();
        }

        private void OnThumbRotate(object sender, ThumbnailRotateEventArgs e)
        {
            if (_busy) return;
            PageThumbnail thumb = sender as PageThumbnail;
            if (thumb == null) return;
            _current = thumb.Page;
            if (!_selected.Contains(thumb.Page))
            {
                _selected.Clear();
                _selected.Add(thumb.Page);
                _anchor = _thumbs.IndexOf(thumb);
            }
            RotatePages(e.Angle);
        }

        private void ToggleSelectAll()
        {
            if (_pages.Count == 0) return;
            bool all = _selected.Count == _pages.Count;
            _selected.Clear();
            if (!all)
            {
                foreach (PageItem p in _pages) _selected.Add(p);
                if (_current == null) _current = _pages[0];
                if (_anchor < 0) _anchor = 0;
            }
            UpdateUi();
        }

        private void UpdateUi()
        {
            if (_updating || _scannerCard == null) return;
            _updating = true;
            try
            {
                List<string> chips = new List<string>();
                chips.Add(ColorChipText());
                chips.Add(_settings.Dpi + " dpi");
                chips.Add(_settings.PaperText());
                if (_settings.UseFeeder) chips.Add("АПД");
                if (_settings.Duplex) chips.Add("Дуплекс");
                _scannerCard.SetData(_settings.SourceName, _statusText, _statusKind, chips.ToArray(), _scanActive, _busy, _batchRemaining);
                _pagesCount.Text = _pages.Count == 0 ? "Нет страниц" : (_pages.Count + " " + PagesWord(_pages.Count));
                bool allSelected = _pages.Count > 0 && _selected.Count == _pages.Count;
                _selectAllButton.Text = allSelected ? "Снять выбор" : "Выбрать все";
                _selectAllButton.Enabled = _pages.Count > 0 && !_busy;
                _footer.SetCounts(_pages.Count, _selected.Count);
                _emptyPages.Visible = _pages.Count == 0;
                if (_emptyPages.Visible) _emptyPages.BringToFront();
                for (int i = 0; i < _thumbs.Count; i++)
                {
                    _thumbs[i].Index = i;
                    _thumbs[i].Selected = _selected.Contains(_thumbs[i].Page);
                    _thumbs[i].Current = _thumbs[i].Page == _current;
                }
                bool hasCurrent = _current != null;
                _deleteButton.Enabled = hasCurrent && !_busy;
                _saveButton.Enabled = _pages.Count > 0 && !_busy;
                _previewMeta.Text = hasCurrent ? PageMeta(_current) : "Выберите страницу слева";
                if (_preview.Page != _current) _preview.SetPage(_current);
                UpdateZoomUi();
                _busyOverlay.Visible = _busy;
                if (_busy) _busyOverlay.BringToFront();
            }
            finally
            {
                _updating = false;
            }
        }

        private void UpdateZoomUi()
        {
            if (_zoomButton == null || _preview == null) return;
            bool has = _current != null && !_busy;
            _zoomOutButton.Enabled = has && _preview.Zoom > 0.26f;
            _zoomInButton.Enabled = has && _preview.Zoom < 2.99f;
            _zoomButton.Enabled = has;
            _zoomButton.Text = _preview.ZoomPercent + "%";
        }

        private string ColorChipText()
        {
            switch (_settings.ColorMode)
            {
                case ScanColorMode.Gray: return "Серый";
                case ScanColorMode.BlackWhite: return "Ч/б";
                default: return "Цвет";
            }
        }

        private string PageMeta(PageItem p)
        {
            int idx = _pages.IndexOf(p) + 1;
            bool landscape = p.Rotation == 90 || p.Rotation == 270;
            int w = landscape ? p.PixelHeight : p.PixelWidth;
            int h = landscape ? p.PixelWidth : p.PixelHeight;
            int dpi = (int)Math.Round(p.DpiX);
            return "Страница " + idx + " · " + w + " × " + h + " px · " + dpi + " dpi";
        }

        private static string PagesWord(int n)
        {
            int m10 = n % 10;
            int m100 = n % 100;
            if (m10 == 1 && m100 != 11) return "страница";
            if (m10 >= 2 && m10 <= 4 && (m100 < 10 || m100 >= 20)) return "страницы";
            return "страниц";
        }

        private bool HasUnsavedPages()
        {
            for (int i = 0; i < _pages.Count; i++) if (!_pages[i].Saved) return true;
            return false;
        }

        private void ClearDocument()
        {
            _preview.SetPage(null);
            _selected.Clear();
            _current = null;
            _anchor = -1;
            _pagesFlow.SuspendLayout();
            while (_pagesFlow.Controls.Count > 0)
            {
                Control c = _pagesFlow.Controls[0];
                _pagesFlow.Controls.RemoveAt(0);
                c.Dispose();
            }
            _thumbs.Clear();
            foreach (PageItem p in _pages) p.Dispose();
            _pages.Clear();
            _pagesFlow.ResumeLayout();
            _scanActive = false;
            UpdateUi();
        }

        private void SetStatus(string text, StatusKind kind)
        {
            _statusText = text;
            _statusKind = kind;
            UpdateUi();
        }

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                ToggleSelectAll();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                if (e.Shift) ExportJpg(); else ExportPdf();
                e.SuppressKeyPress = true;
            }
            else if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add))
            {
                if (_current != null) { _preview.ZoomIn(); UpdateZoomUi(); }
                e.SuppressKeyPress = true;
            }
            else if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract))
            {
                if (_current != null) { _preview.ZoomOut(); UpdateZoomUi(); }
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.D0)
            {
                if (_current != null) { _preview.FitWidth(); UpdateZoomUi(); }
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedPages();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                OnScanClick();
                e.SuppressKeyPress = true;
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_closing) return;
            StopScanTimer();
            if (_busy)
            {
                DialogResult r = MessageBox.Show(this, "Сканирование ещё выполняется. Прервать и закрыть программу?", "Закрытие", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) { e.Cancel = true; return; }
            }
            if (HasUnsavedPages())
            {
                DialogResult r = MessageBox.Show(this, "В документе есть несохранённые страницы. Выйти без сохранения?", "Несохранённые страницы", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) { e.Cancel = true; return; }
            }
            _closing = true;
            try
            {
                _app.WindowWidth = WindowState == FormWindowState.Normal ? ClientSize.Width : RestoreBounds.Width;
                _app.WindowHeight = WindowState == FormWindowState.Normal ? ClientSize.Height : RestoreBounds.Height;
                _app.Apply(_settings);
                _app.Save();
            }
            catch { }
            if (!_serviceDisposed)
            {
                try { _service.Dispose(); } catch { }
                _serviceDisposed = true;
            }
            foreach (PageItem p in _pages) p.Dispose();
            _pages.Clear();
            if (_pageMenu != null) _pageMenu.Dispose();
            if (_saveMenu != null) _saveMenu.Dispose();
            if (_tip != null) _tip.Dispose();
        }
    }
}