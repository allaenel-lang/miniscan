using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using NTwain;
using NTwain.Data;

namespace MiniScan
{
    internal sealed class ScannedPageEventArgs : EventArgs
    {
        public Bitmap Bitmap;
        public double DpiX;
        public double DpiY;
    }

    internal sealed class ScanFinishedEventArgs : EventArgs
    {
        public bool Success;
        public bool Canceled;
        public string Error;
        public int PagesAdded;
    }

    internal sealed class ScannerService : IDisposable
    {
        private readonly List<DataSource> _dataSources = new List<DataSource>();
        private readonly List<ScannedPageEventArgs> _pendingPages = new List<ScannedPageEventArgs>();
        private TwainSession _twain;
        private DataSource _source;
        private IntPtr _handle;
        private bool _scanInProgress;
        private bool _sourcesLoaded;
        private bool _disposing;
        private string _lastError;
        private int _pagesAdded;
        private ScanSettings _settings = new ScanSettings();

        public event EventHandler<ScannedPageEventArgs> PageScanned;
        public event EventHandler<ScanFinishedEventArgs> ScanFinished;

        public bool IsInitialized { get { return _twain != null && _twain.IsDsmOpen; } }
        public bool IsScanning { get { return _scanInProgress; } }
        public string OpenedSourceName { get { return _source != null ? _source.Name : null; } }

        public bool Initialize(IntPtr handle, out string error)
        {
            error = null;
            _handle = handle;
            if (_twain != null)
            {
                if (_twain.IsDsmOpen) return true;
                try { _twain.Close(); } catch { }
                _twain = null;
            }
            try
            {
                TWIdentity appId = TWIdentity.CreateFromAssembly(DataGroups.Image, Assembly.GetExecutingAssembly());
                _twain = new TwainSession(appId);
                _twain.TransferReady += OnTransferReady;
                _twain.TransferError += OnTransferError;
                _twain.TransferCanceled += OnTransferCanceled;
                _twain.DataTransferred += OnDataTransferred;
                _twain.SourceDisabled += OnSourceDisabled;
                ReturnCode rc = _twain.Open(new WindowsFormsMessageLoopHook(handle));
                if (rc != ReturnCode.Success)
                {
                    error = "Не удалось открыть TWAIN-менеджер (" + rc + "). Проверьте, что установлен TWAIN-драйвер сканера.";
                    _twain = null;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = "Не удалось инициализировать TWAIN: " + ex.Message;
                _twain = null;
                return false;
            }
        }

        public List<ScannerInfo> RefreshSources(out string error)
        {
            error = null;
            List<ScannerInfo> result = new List<ScannerInfo>();
            _dataSources.Clear();
            _sourcesLoaded = true;
            if (_twain == null || !_twain.IsDsmOpen)
            {
                error = "TWAIN-менеджер не инициализирован.";
                return result;
            }
            try
            {
                foreach (DataSource ds in _twain.GetSources())
                {
                    ScannerInfo info = new ScannerInfo();
                    info.Name = ds.Name;
                    info.Manufacturer = ds.Manufacturer;
                    info.ProductFamily = ds.ProductFamily;
                    result.Add(info);
                    _dataSources.Add(ds);
                }
            }
            catch (Exception ex)
            {
                error = "Не удалось получить список сканеров: " + ex.Message;
            }
            return result;
        }

        public bool SourceExists(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!_sourcesLoaded) return false;
            foreach (DataSource ds in _dataSources)
            {
                if (string.Equals(ds.Name, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public bool OpenSource(string name, out string error)
        {
            error = null;
            if (_twain == null || !_twain.IsDsmOpen)
            {
                error = "TWAIN-менеджер не инициализирован.";
                return false;
            }
            if (string.IsNullOrEmpty(name))
            {
                error = "Сканер не выбран.";
                return false;
            }
            if (_source != null && _source.IsOpen && string.Equals(_source.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            CloseSource();
            if (_dataSources.Count == 0)
            {
                string refreshError;
                RefreshSources(out refreshError);
                if (!string.IsNullOrEmpty(refreshError))
                {
                    error = refreshError;
                    return false;
                }
            }
            DataSource found = null;
            foreach (DataSource ds in _dataSources)
            {
                if (string.Equals(ds.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    found = ds;
                    break;
                }
            }
            if (found == null)
            {
                error = "Сканер «" + name + "» не найден. Выберите другой модуль сканирования.";
                return false;
            }
            try
            {
                ReturnCode rc = found.Open();
                if (rc != ReturnCode.Success)
                {
                    error = "Не удалось открыть сканер (" + rc + "). Проверьте подключение и драйвер.";
                    return false;
                }
                _source = found;
                return true;
            }
            catch (Exception ex)
            {
                error = "Не удалось открыть сканер: " + ex.Message;
                return false;
            }
        }

        public void CloseSource()
        {
            if (_source == null) return;
            try
            {
                if (_source.IsOpen)
                {
                    if (_twain != null && _twain.IsSourceEnabled) _twain.ForceStepDown(4);
                    _source.Close();
                }
            }
            catch { }
            _source = null;
        }

        public ScannerCapabilities GetCapabilities()
        {
            if (_source == null || !_source.IsOpen) return ScannerCapabilities.Defaults();
            ScannerCapabilities caps = ScannerCapabilities.Defaults();
            caps.SourceOpen = true;
            caps.Dpis.Clear();
            caps.ColorModes.Clear();
            caps.PaperKinds.Clear();
            caps.SupportsDpi = false;
            caps.SupportsColor = false;
            caps.SupportsPaper = false;
            try
            {
                if (_source.Capabilities.ICapXResolution.IsSupported)
                {
                    caps.SupportsDpi = true;
                    foreach (TWFix32 v in _source.Capabilities.ICapXResolution.GetValues())
                    {
                        float f = (float)v;
                        int dpi = (int)Math.Round(f);
                        if (dpi >= 75 && dpi <= 1200 && !caps.Dpis.Contains(dpi)) caps.Dpis.Add(dpi);
                    }
                    if (caps.Dpis.Count == 0 || caps.Dpis.Count > 40)
                    {
                        caps.Dpis.Clear();
                        caps.Dpis.AddRange(new int[] { 100, 150, 200, 300, 400, 600 });
                    }
                    caps.Dpis.Sort();
                }
            }
            catch { if (caps.Dpis.Count == 0) caps.Dpis.AddRange(new int[] { 100, 150, 200, 300, 400, 600 }); }
            try
            {
                if (_source.Capabilities.ICapPixelType.IsSupported)
                {
                    caps.SupportsColor = true;
                    foreach (PixelType p in _source.Capabilities.ICapPixelType.GetValues())
                    {
                        if (p == PixelType.RGB || p == PixelType.SRGB) AddUnique(caps.ColorModes, ScanColorMode.Color);
                        else if (p == PixelType.Gray) AddUnique(caps.ColorModes, ScanColorMode.Gray);
                        else if (p == PixelType.BlackWhite) AddUnique(caps.ColorModes, ScanColorMode.BlackWhite);
                    }
                }
            }
            catch { }
            if (caps.ColorModes.Count == 0)
            {
                caps.SupportsColor = false;
                caps.ColorModes.AddRange(new ScanColorMode[] { ScanColorMode.Color, ScanColorMode.Gray, ScanColorMode.BlackWhite });
            }
            try
            {
                if (_source.Capabilities.ICapSupportedSizes.IsSupported)
                {
                    caps.SupportsPaper = true;
                    caps.PaperKinds.Add(ScanPaperKind.Auto);
                    foreach (SupportedSize p in _source.Capabilities.ICapSupportedSizes.GetValues())
                    {
                        if (p == SupportedSize.A4) AddUnique(caps.PaperKinds, ScanPaperKind.A4);
                        else if (p == SupportedSize.A5) AddUnique(caps.PaperKinds, ScanPaperKind.A5);
                        else if (p == SupportedSize.USLetter) AddUnique(caps.PaperKinds, ScanPaperKind.Letter);
                    }
                }
            }
            catch { }
            if (caps.PaperKinds.Count == 0)
            {
                caps.SupportsPaper = false;
                caps.PaperKinds.AddRange(new ScanPaperKind[] { ScanPaperKind.Auto, ScanPaperKind.A4, ScanPaperKind.A5, ScanPaperKind.Letter });
            }
            try { caps.SupportsFeeder = _source.Capabilities.CapFeederEnabled.IsSupported && _source.Capabilities.CapFeederEnabled.CanSet; } catch { }
            try { caps.SupportsDuplex = _source.Capabilities.CapDuplexEnabled.IsSupported && _source.Capabilities.CapDuplexEnabled.CanSet; } catch { }
            return caps;
        }

        private static void AddUnique(List<ScanColorMode> list, ScanColorMode v)
        {
            if (!list.Contains(v)) list.Add(v);
        }

        private static void AddUnique(List<ScanPaperKind> list, ScanPaperKind v)
        {
            if (!list.Contains(v)) list.Add(v);
        }

        public bool StartScan(ScanSettings settings, out string error, out string warning)
        {
            error = null;
            warning = null;
            if (_scanInProgress)
            {
                error = "Сканирование уже выполняется.";
                return false;
            }
            if (_twain == null || !_twain.IsDsmOpen)
            {
                error = "TWAIN-менеджер не инициализирован.";
                return false;
            }
            if (!OpenSource(settings.SourceName, out error)) return false;
            _settings = settings.Clone();
            warning = ApplySettings(settings);
            _pendingPages.Clear();
            _lastError = null;
            _pagesAdded = 0;
            _scanInProgress = true;
            try
            {
                ReturnCode rc = _source.Enable(SourceEnableMode.NoUI, false, _handle);
                if (rc != ReturnCode.Success && !_twain.IsSourceEnabled)
                {
                    rc = _source.Enable(SourceEnableMode.ShowUI, false, _handle);
                }
                if (rc != ReturnCode.Success && !_twain.IsSourceEnabled)
                {
                    _scanInProgress = false;
                    error = "Не удалось запустить сканирование (" + rc + ").";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _scanInProgress = false;
                error = "Не удалось запустить сканирование: " + ex.Message;
                return false;
            }
        }

        private string ApplySettings(ScanSettings s)
        {
            string warning = null;
            try { if (_source.Capabilities.ICapUnits.IsSupported) _source.Capabilities.ICapUnits.SetValue(Unit.Inches); } catch { }
            try
            {
                if (_source.Capabilities.ICapXferMech.IsSupported)
                {
                    _source.Capabilities.ICapXferMech.SetValue(XferMech.Native);
                }
            }
            catch { }
            try
            {
                if (_source.Capabilities.ICapPixelType.IsSupported)
                {
                    PixelType wanted = PixelType.RGB;
                    if (s.ColorMode == ScanColorMode.Gray) wanted = PixelType.Gray;
                    if (s.ColorMode == ScanColorMode.BlackWhite) wanted = PixelType.BlackWhite;
                    ReturnCode rc = _source.Capabilities.ICapPixelType.SetValue(wanted);
                    if (rc != ReturnCode.Success)
                    {
                        warning = "Сканер не применил цветовой режим; использованы его настройки по умолчанию.";
                    }
                }
            }
            catch { warning = "Не удалось применить цветовой режим."; }
            warning = ApplyDpi(s.Dpi, warning);
            if (s.Paper != ScanPaperKind.Auto)
            {
                try
                {
                    SupportedSize size = SupportedSize.A4;
                    if (s.Paper == ScanPaperKind.A5) size = SupportedSize.A5;
                    if (s.Paper == ScanPaperKind.Letter) size = SupportedSize.USLetter;
                    if (_source.Capabilities.ICapSupportedSizes.IsSupported)
                    {
                        _source.Capabilities.ICapSupportedSizes.SetValue(size);
                    }
                }
                catch { }
            }
            try
            {
                if (_source.Capabilities.CapFeederEnabled.IsSupported)
                {
                    _source.Capabilities.CapFeederEnabled.SetValue(s.UseFeeder ? BoolType.True : BoolType.False);
                }
            }
            catch { }
            try
            {
                if (_source.Capabilities.CapDuplexEnabled.IsSupported)
                {
                    _source.Capabilities.CapDuplexEnabled.SetValue((s.UseFeeder && s.Duplex) ? BoolType.True : BoolType.False);
                }
            }
            catch { }
            try
            {
                if (_source.Capabilities.CapAutoFeed.IsSupported)
                {
                    _source.Capabilities.CapAutoFeed.SetValue(BoolType.False);
                }
            }
            catch { }
            try
            {
                if (_source.Capabilities.CapXferCount.IsSupported)
                {
                    _source.Capabilities.CapXferCount.SetValue(1);
                }
            }
            catch { }
            return warning;
        }

        private string ApplyDpi(int dpi, string warning)
        {
            try
            {
                if (!_source.Capabilities.ICapXResolution.IsSupported) return warning;
                ReturnCode rx = _source.Capabilities.ICapXResolution.SetValue((float)dpi);
                ReturnCode ry = ReturnCode.Success;
                if (_source.Capabilities.ICapYResolution.IsSupported)
                {
                    ry = _source.Capabilities.ICapYResolution.SetValue((float)dpi);
                }
                if (rx != ReturnCode.Success || ry != ReturnCode.Success)
                {
                    int nearest = NearestDpi(dpi);
                    _source.Capabilities.ICapXResolution.SetValue((float)nearest);
                    if (_source.Capabilities.ICapYResolution.IsSupported) _source.Capabilities.ICapYResolution.SetValue((float)nearest);
                    warning = "Запрошенное разрешение " + dpi + " dpi недоступно; установлено " + nearest + " dpi.";
                }
            }
            catch
            {
                warning = "Не удалось применить разрешение; использованы настройки сканера.";
            }
            return warning;
        }

        private int NearestDpi(int wanted)
        {
            try
            {
                int best = wanted;
                int bestDelta = int.MaxValue;
                foreach (TWFix32 v in _source.Capabilities.ICapXResolution.GetValues())
                {
                    int d = (int)Math.Round((float)v);
                    if (d < 75 || d > 1200) continue;
                    int delta = Math.Abs(d - wanted);
                    if (delta < bestDelta) { bestDelta = delta; best = d; }
                }
                return best;
            }
            catch { return wanted; }
        }

        private void OnDataTransferred(object sender, DataTransferredEventArgs e)
        {
            if (_disposing) return;
            Bitmap bmp = null;
            try
            {
                if (e.TransferType == XferMech.Native)
                {
                    using (Stream stream = e.GetNativeImageStream())
                    {
                        if (stream != null)
                        {
                            using (Image img = Image.FromStream(stream))
                            {
                                bmp = new Bitmap(img);
                            }
                        }
                    }
                }
                else if (e.TransferType == XferMech.Memory || e.TransferType == XferMech.MemFile)
                {
                    if (e.MemoryData != null && e.MemoryData.Length > 0) bmp = BitmapFromDib(e.MemoryData);
                }
                else if (e.TransferType == XferMech.File)
                {
                    if (!string.IsNullOrEmpty(e.FileDataPath) && File.Exists(e.FileDataPath))
                    {
                        using (Image img = Image.FromFile(e.FileDataPath)) bmp = new Bitmap(img);
                    }
                }
                if (bmp == null)
                {
                    _lastError = "Сканер передал данные в неподдерживаемом формате.";
                    return;
                }
                double dx = 0;
                double dy = 0;
                try { dx = e.ImageInfo.XResolution; dy = e.ImageInfo.YResolution; } catch { }
                if (dx < 50 || dx > 2400) dx = _settings.Dpi;
                if (dy < 50 || dy > 2400) dy = _settings.Dpi;
                try { bmp.SetResolution((float)dx, (float)dy); } catch { }
                ScannedPageEventArgs page = new ScannedPageEventArgs();
                page.Bitmap = bmp;
                page.DpiX = dx;
                page.DpiY = dy;
                _pendingPages.Add(page);
                bmp = null;
            }
            catch (Exception ex)
            {
                _lastError = "Ошибка при получении изображения: " + ex.Message;
            }
            finally
            {
                if (bmp != null) bmp.Dispose();
            }
        }

        private static Bitmap BitmapFromDib(byte[] data)
        {
            if (data == null || data.Length < 40) return null;
            if (data[0] == (byte)'B' && data[1] == (byte)'M')
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (Image img = Image.FromStream(ms))
                {
                    return new Bitmap(img);
                }
            }
            int biSize = BitConverter.ToInt32(data, 0);
            if (biSize < 12 || biSize > data.Length) return null;
            int bitCount = BitConverter.ToUInt16(data, 14);
            int clrUsed = biSize >= 40 ? (int)BitConverter.ToUInt32(data, 32) : 0;
            int paletteEntries = 0;
            if (bitCount <= 8) paletteEntries = clrUsed > 0 ? clrUsed : (1 << bitCount);
            int paletteBytes = paletteEntries * (biSize == 12 ? 3 : 4);
            int offset = 14 + biSize + paletteBytes;
            if (offset >= data.Length) return null;
            using (MemoryStream ms = new MemoryStream(14 + data.Length))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((byte)'B');
                bw.Write((byte)'M');
                bw.Write(14 + data.Length);
                bw.Write((short)0);
                bw.Write((short)0);
                bw.Write(offset);
                bw.Write(data);
                bw.Flush();
                ms.Position = 0;
                using (Image img = Image.FromStream(ms))
                {
                    return new Bitmap(img);
                }
            }
        }

        private void OnTransferReady(object sender, TransferReadyEventArgs e)
        {
            if (_disposing)
            {
                e.CancelAll = true;
            }
        }

        private void OnTransferError(object sender, TransferErrorEventArgs e)
        {
            string message = "Ошибка передачи изображения";
            if (e.Exception != null) message += ": " + e.Exception.Message;
            else if (e.ReturnCode != ReturnCode.Success) message += " (" + e.ReturnCode + ")";
            _lastError = message;
        }

        private void OnTransferCanceled(object sender, TransferCanceledEventArgs e)
        {
            _lastError = "Сканирование отменено.";
        }

        private void OnSourceDisabled(object sender, EventArgs e)
        {
            if (!_scanInProgress || _disposing) return;
            _scanInProgress = false;
            List<ScannedPageEventArgs> pages = new List<ScannedPageEventArgs>(_pendingPages);
            _pendingPages.Clear();
            _pagesAdded = pages.Count;
            foreach (ScannedPageEventArgs page in pages)
            {
                try
                {
                    if (PageScanned != null) PageScanned(this, page);
                }
                catch (Exception ex)
                {
                    _lastError = "Ошибка обработки страницы: " + ex.Message;
                }
                finally
                {
                    if (page.Bitmap != null) page.Bitmap.Dispose();
                }
            }
            ScanFinishedEventArgs args = new ScanFinishedEventArgs();
            args.PagesAdded = pages.Count;
            args.Success = pages.Count > 0;
            args.Error = _lastError;
            args.Canceled = pages.Count == 0 && string.IsNullOrEmpty(_lastError);
            if (pages.Count == 0 && string.IsNullOrEmpty(args.Error))
            {
                args.Canceled = true;
            }
            if (ScanFinished != null) ScanFinished(this, args);
        }

        public void Dispose()
        {
            _disposing = true;
            try { if (_twain != null && _twain.IsSourceEnabled) _twain.ForceStepDown(4); } catch { }
            CloseSource();
            if (_twain != null)
            {
                try { _twain.Close(); } catch { }
                _twain = null;
            }
            foreach (ScannedPageEventArgs p in _pendingPages) if (p.Bitmap != null) p.Bitmap.Dispose();
            _pendingPages.Clear();
        }
    }
}