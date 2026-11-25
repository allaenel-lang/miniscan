using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;

namespace MiniScan
{
    internal enum ScanColorMode { Color, Gray, BlackWhite }
    internal enum ScanPaperKind { Auto, A4, A5, Letter }

    internal sealed class ScanSettings
    {
        public string SourceName = string.Empty;
        public int Dpi = 300;
        public ScanColorMode ColorMode = ScanColorMode.Color;
        public ScanPaperKind Paper = ScanPaperKind.Auto;
        public bool UseFeeder = false;
        public bool Duplex = false;

        public ScanSettings Clone()
        {
            ScanSettings s = new ScanSettings();
            s.SourceName = SourceName;
            s.Dpi = Dpi;
            s.ColorMode = ColorMode;
            s.Paper = Paper;
            s.UseFeeder = UseFeeder;
            s.Duplex = Duplex;
            return s;
        }

        public string ColorText()
        {
            switch (ColorMode)
            {
                case ScanColorMode.Gray: return "Оттенки серого";
                case ScanColorMode.BlackWhite: return "Чёрно-белый";
                default: return "Цветной";
            }
        }

        public string PaperText()
        {
            switch (Paper)
            {
                case ScanPaperKind.A4: return "A4";
                case ScanPaperKind.A5: return "A5";
                case ScanPaperKind.Letter: return "Letter";
                default: return "Авто";
            }
        }
    }

    internal sealed class ScannerInfo
    {
        public string Name = string.Empty;
        public string Manufacturer = string.Empty;
        public string ProductFamily = string.Empty;

        public string DisplayManufacturer
        {
            get
            {
                if (!string.IsNullOrEmpty(Manufacturer)) return Manufacturer;
                if (!string.IsNullOrEmpty(ProductFamily)) return ProductFamily;
                return "TWAIN-совместимый сканер";
            }
        }
    }

    internal sealed class ScannerCapabilities
    {
        public bool SourceOpen;
        public bool SupportsDpi;
        public List<int> Dpis = new List<int>();
        public bool SupportsColor;
        public List<ScanColorMode> ColorModes = new List<ScanColorMode>();
        public bool SupportsPaper;
        public List<ScanPaperKind> PaperKinds = new List<ScanPaperKind>();
        public bool SupportsFeeder;
        public bool SupportsDuplex;

        public static ScannerCapabilities Defaults()
        {
            ScannerCapabilities c = new ScannerCapabilities();
            c.SupportsDpi = true;
            c.Dpis.AddRange(new int[] { 100, 150, 200, 300, 400, 600 });
            c.SupportsColor = true;
            c.ColorModes.AddRange(new ScanColorMode[] { ScanColorMode.Color, ScanColorMode.Gray, ScanColorMode.BlackWhite });
            c.SupportsPaper = true;
            c.PaperKinds.AddRange(new ScanPaperKind[] { ScanPaperKind.Auto, ScanPaperKind.A4, ScanPaperKind.A5, ScanPaperKind.Letter });
            return c;
        }
    }

    internal static class JpegCodec
    {
        private static ImageCodecInfo _codec;

        public static ImageCodecInfo Codec
        {
            get
            {
                if (_codec == null)
                {
                    foreach (ImageCodecInfo c in ImageCodecInfo.GetImageEncoders())
                    {
                        if (string.Equals(c.MimeType, "image/jpeg", StringComparison.OrdinalIgnoreCase))
                        {
                            _codec = c;
                            break;
                        }
                    }
                }
                return _codec;
            }
        }

        public static EncoderParameters QualityParams(long quality)
        {
            EncoderParameters p = new EncoderParameters(1);
            p.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            return p;
        }

        public static byte[] Encode(Bitmap bitmap, long quality)
        {
            using (MemoryStream ms = new MemoryStream())
            using (EncoderParameters p = QualityParams(quality))
            {
                bitmap.Save(ms, Codec, p);
                return ms.ToArray();
            }
        }

        public static void Save(Bitmap bitmap, string path, long quality)
        {
            using (EncoderParameters p = QualityParams(quality))
            {
                bitmap.Save(path, Codec, p);
            }
        }

        public static Bitmap ToOpaque24(Bitmap source)
        {
            Bitmap b = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
            b.SetResolution(ValidDpi(source.HorizontalResolution, 300f), ValidDpi(source.VerticalResolution, 300f));
            using (Graphics g = Graphics.FromImage(b))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                using (SolidBrush white = new SolidBrush(Color.White))
                {
                    g.FillRectangle(white, 0, 0, b.Width, b.Height);
                }
                g.DrawImage(source, new Rectangle(0, 0, b.Width, b.Height));
            }
            return b;
        }

        public static float ValidDpi(float dpi, float fallback)
        {
            if (dpi < 50f || dpi > 2400f || float.IsNaN(dpi) || float.IsInfinity(dpi)) return fallback;
            return dpi;
        }
    }

    internal sealed class ExportImageData
    {
        public byte[] Jpeg;
        public int Width;
        public int Height;
        public double DpiX;
        public double DpiY;
    }

    internal sealed class PageItem : IDisposable
    {
        private Bitmap _thumbnail;
        private int _thumbnailRotation = -1;

        public byte[] JpegData;
        public int PixelWidth;
        public int PixelHeight;
        public double DpiX;
        public double DpiY;
        public int Rotation;
        public bool Saved;
        public DateTime Created;

        private PageItem()
        {
            Created = DateTime.Now;
        }

        public static PageItem FromBitmap(Bitmap source, double dpiX, double dpiY)
        {
            if (source == null) throw new ArgumentNullException("source");
            PageItem p = new PageItem();
            float dx = (float)(dpiX > 0 ? dpiX : 300);
            float dy = (float)(dpiY > 0 ? dpiY : 300);
            using (Bitmap opaque = JpegCodec.ToOpaque24(source))
            {
                try { opaque.SetResolution(dx, dy); }
                catch { }
                p.JpegData = JpegCodec.Encode(opaque, 94L);
                p.PixelWidth = opaque.Width;
                p.PixelHeight = opaque.Height;
            }
            p.DpiX = dx;
            p.DpiY = dy;
            p.Rotation = 0;
            p.Saved = false;
            return p;
        }

        public void Rotate(int delta)
        {
            Rotation = (Rotation + delta) % 360;
            if (Rotation < 0) Rotation += 360;
            if (Rotation % 90 != 0) Rotation = 0;
            Saved = false;
            ClearThumbnail();
        }

        public Bitmap CreateDisplayBitmap()
        {
            Bitmap bmp = Decode();
            if (Rotation != 0)
            {
                switch (Rotation)
                {
                    case 90: bmp.RotateFlip(RotateFlipType.Rotate90FlipNone); break;
                    case 180: bmp.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                    case 270: bmp.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
                }
            }
            return bmp;
        }

        public Bitmap Decode()
        {
            using (MemoryStream ms = new MemoryStream(JpegData))
            using (Image img = Image.FromStream(ms))
            {
                Bitmap bmp = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
                try { bmp.SetResolution((float)DpiX, (float)DpiY); }
                catch { }
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(img, new Rectangle(0, 0, bmp.Width, bmp.Height));
                }
                return bmp;
            }
        }

        public Bitmap GetThumbnail()
        {
            if (_thumbnail != null && _thumbnailRotation == Rotation) return _thumbnail;
            ClearThumbnail();
            using (Bitmap full = CreateDisplayBitmap())
            {
                int maxW = 640;
                int maxH = 440;
                double scale = Math.Min((double)maxW / full.Width, (double)maxH / full.Height);
                int w = Math.Max(1, (int)Math.Round(full.Width * scale));
                int h = Math.Max(1, (int)Math.Round(full.Height * scale));
                Bitmap thumb = new Bitmap(w, h, PixelFormat.Format24bppRgb);
                using (Graphics g = Graphics.FromImage(thumb))
                {
                    g.Clear(Color.White);
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(full, new Rectangle(0, 0, w, h));
                }
                _thumbnail = thumb;
                _thumbnailRotation = Rotation;
            }
            return _thumbnail;
        }

        public ExportImageData GetExportImage(bool forceReencode)
        {
            ExportImageData data = new ExportImageData();
            bool swap = Rotation == 90 || Rotation == 270;
            if (Rotation == 0 && !forceReencode)
            {
                data.Jpeg = JpegData;
                data.Width = PixelWidth;
                data.Height = PixelHeight;
                data.DpiX = DpiX;
                data.DpiY = DpiY;
                return data;
            }
            using (Bitmap rotated = CreateDisplayBitmap())
            {
                try { rotated.SetResolution((float)DpiX, (float)DpiY); }
                catch { }
                data.Jpeg = JpegCodec.Encode(rotated, 95L);
                data.Width = rotated.Width;
                data.Height = rotated.Height;
                data.DpiX = swap ? DpiY : DpiX;
                data.DpiY = swap ? DpiX : DpiY;
            }
            return data;
        }

        public void SaveJpeg(string path)
        {
            if (Rotation == 0)
            {
                File.WriteAllBytes(path, JpegData);
                return;
            }
            using (Bitmap bmp = CreateDisplayBitmap())
            {
                bool swap = Rotation == 90 || Rotation == 270;
                try { bmp.SetResolution((float)(swap ? DpiY : DpiX), (float)(swap ? DpiX : DpiY)); }
                catch { }
                JpegCodec.Save(bmp, path, 95L);
            }
        }

        public string SizeLabel()
        {
            bool landscape = Rotation == 90 || Rotation == 270;
            int w = landscape ? PixelHeight : PixelWidth;
            int h = landscape ? PixelWidth : PixelHeight;
            double dx = landscape ? DpiY : DpiX;
            double dy = landscape ? DpiX : DpiY;
            double wi = w / dx;
            double hi = h / dy;
            if (Math.Abs(wi - 8.27) < 0.25 && Math.Abs(hi - 11.69) < 0.3) return "A4";
            if (Math.Abs(wi - 5.83) < 0.25 && Math.Abs(hi - 8.27) < 0.3) return "A5";
            if (Math.Abs(wi - 8.5) < 0.3 && Math.Abs(hi - 11.0) < 0.3) return "Letter";
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0}×{1:0.0} см", wi * 2.54, hi * 2.54);
        }

        private void ClearThumbnail()
        {
            if (_thumbnail != null)
            {
                _thumbnail.Dispose();
                _thumbnail = null;
            }
            _thumbnailRotation = -1;
        }

        public void Dispose()
        {
            ClearThumbnail();
        }
    }

    internal sealed class AppSettings
    {
        public string SourceName = string.Empty;
        public int Dpi = 300;
        public ScanColorMode ColorMode = ScanColorMode.Color;
        public ScanPaperKind Paper = ScanPaperKind.Auto;
        public bool UseFeeder = false;
        public bool Duplex = false;
        public int WindowWidth = 1240;
        public int WindowHeight = 800;
        public string LastExportFolder = string.Empty;
        public bool AlwaysSave = false;
        public string AlwaysSaveFolder = string.Empty;
        public int ScanCount = 1;
        public int ScanPause = 10;

        private static string SettingsPath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MiniScan");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "settings.ini");
            }
        }

        public static AppSettings Load()
        {
            AppSettings s = new AppSettings();
            try
            {
                if (!File.Exists(SettingsPath)) return s;
                foreach (string line in File.ReadAllLines(SettingsPath, Encoding.UTF8))
                {
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    string key = line.Substring(0, idx).Trim();
                    string val = line.Substring(idx + 1).Trim();
                    try
                    {
                        switch (key)
                        {
                            case "Source": s.SourceName = val; break;
                            case "Dpi": s.Dpi = int.Parse(val, CultureInfo.InvariantCulture); break;
                            case "Color": s.ColorMode = (ScanColorMode)Enum.Parse(typeof(ScanColorMode), val, true); break;
                            case "Paper": s.Paper = (ScanPaperKind)Enum.Parse(typeof(ScanPaperKind), val, true); break;
                            case "Feeder": s.UseFeeder = val == "1"; break;
                            case "Duplex": s.Duplex = val == "1"; break;
                            case "WindowWidth": s.WindowWidth = int.Parse(val, CultureInfo.InvariantCulture); break;
                            case "WindowHeight": s.WindowHeight = int.Parse(val, CultureInfo.InvariantCulture); break;
                            case "LastExportFolder": s.LastExportFolder = val; break;
                            case "AlwaysSave": s.AlwaysSave = val == "1"; break;
                            case "AlwaysSaveFolder": s.AlwaysSaveFolder = val; break;
                            case "ScanCount": s.ScanCount = int.Parse(val, CultureInfo.InvariantCulture); break;
                            case "AutoScanCount": s.ScanCount = int.Parse(val, CultureInfo.InvariantCulture); break;
                            case "ScanPause": s.ScanPause = int.Parse(val, CultureInfo.InvariantCulture); break;
                            case "AutoContinueDelay": s.ScanPause = int.Parse(val, CultureInfo.InvariantCulture); break;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            if (s.Dpi < 75 || s.Dpi > 1200) s.Dpi = 300;
            if (s.WindowWidth < 900) s.WindowWidth = 1240;
            if (s.WindowHeight < 600) s.WindowHeight = 800;
            if (s.ScanPause < 0) s.ScanPause = 0;
            if (s.ScanPause > 120) s.ScanPause = 120;
            if (s.ScanCount < 1) s.ScanCount = 1;
            if (s.ScanCount > 999) s.ScanCount = 999;
            return s;
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Source=" + SourceName);
                sb.AppendLine("Dpi=" + Dpi.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Color=" + ColorMode.ToString());
                sb.AppendLine("Paper=" + Paper.ToString());
                sb.AppendLine("Feeder=" + (UseFeeder ? "1" : "0"));
                sb.AppendLine("Duplex=" + (Duplex ? "1" : "0"));
                sb.AppendLine("WindowWidth=" + WindowWidth.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("WindowHeight=" + WindowHeight.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("LastExportFolder=" + LastExportFolder);
                sb.AppendLine("AlwaysSave=" + (AlwaysSave ? "1" : "0"));
                sb.AppendLine("AlwaysSaveFolder=" + AlwaysSaveFolder);
                sb.AppendLine("ScanCount=" + ScanCount.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("ScanPause=" + ScanPause.ToString(CultureInfo.InvariantCulture));
                File.WriteAllText(SettingsPath, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }

        public ScanSettings ToScanSettings()
        {
            ScanSettings s = new ScanSettings();
            s.SourceName = SourceName;
            s.Dpi = Dpi;
            s.ColorMode = ColorMode;
            s.Paper = Paper;
            s.UseFeeder = UseFeeder;
            s.Duplex = Duplex;
            return s;
        }

        public void Apply(ScanSettings s)
        {
            SourceName = s.SourceName;
            Dpi = s.Dpi;
            ColorMode = s.ColorMode;
            Paper = s.Paper;
            UseFeeder = s.UseFeeder;
            Duplex = s.Duplex;
        }
    }
}