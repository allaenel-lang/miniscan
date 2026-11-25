using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MiniScan
{
    internal sealed class PdfImagePage
    {
        public byte[] Jpeg;
        public int PixelWidth;
        public int PixelHeight;
        public double DpiX;
        public double DpiY;
    }

    internal static class PdfWriter
    {
        public static void Write(string path, IList<PdfImagePage> pages)
        {
            if (pages == null || pages.Count == 0) throw new ArgumentException("Нет страниц для PDF.", "pages");
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                List<long> offsets = new List<long>();
                offsets.Add(0L);
                byte[] header = Encoding.ASCII.GetBytes("%PDF-1.4\n");
                fs.Write(header, 0, header.Length);
                byte[] marker = new byte[] { 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A };
                fs.Write(marker, 0, marker.Length);

                int pageCount = pages.Count;
                int infoId = 3 + pageCount * 3;
                int objectCount = infoId + 1;

                WriteObject(fs, offsets, 1, "<< /Type /Catalog /Pages 2 0 R >>");
                StringBuilder kids = new StringBuilder();
                for (int i = 0; i < pageCount; i++)
                {
                    if (i > 0) kids.Append(' ');
                    kids.Append((3 + i * 3).ToString(CultureInfo.InvariantCulture));
                    kids.Append(" 0 R");
                }
                WriteObject(fs, offsets, 2, "<< /Type /Pages /Kids [" + kids + "] /Count " + pageCount.ToString(CultureInfo.InvariantCulture) + " >>");

                for (int i = 0; i < pageCount; i++)
                {
                    PdfImagePage page = pages[i];
                    int pageId = 3 + i * 3;
                    int imageId = pageId + 1;
                    int contentId = pageId + 2;

                    double dpiX = page.DpiX > 50 ? page.DpiX : 300.0;
                    double dpiY = page.DpiY > 50 ? page.DpiY : 300.0;
                    double wPt = page.PixelWidth * 72.0 / dpiX;
                    double hPt = page.PixelHeight * 72.0 / dpiY;
                    if (wPt < 1.0) wPt = 1.0;
                    if (hPt < 1.0) hPt = 1.0;
                    string w = Num(wPt);
                    string h = Num(hPt);

                    string pageDict = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + w + " " + h + "] " +
                        "/Resources << /ProcSet [/PDF /ImageC] /XObject << /Im0 " + imageId.ToString(CultureInfo.InvariantCulture) + " 0 R >> >> " +
                        "/Contents " + contentId.ToString(CultureInfo.InvariantCulture) + " 0 R >>";
                    WriteObject(fs, offsets, pageId, pageDict);

                    offsets.Add(fs.Position);
                    WriteAscii(fs, imageId.ToString(CultureInfo.InvariantCulture) + " 0 obj\n");
                    WriteAscii(fs, "<< /Type /XObject /Subtype /Image /Width " + page.PixelWidth.ToString(CultureInfo.InvariantCulture) +
                        " /Height " + page.PixelHeight.ToString(CultureInfo.InvariantCulture) +
                        " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Interpolate true /Length " +
                        page.Jpeg.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
                    fs.Write(page.Jpeg, 0, page.Jpeg.Length);
                    WriteAscii(fs, "\nendstream\nendobj\n");

                    string content = "q\n" + w + " 0 0 " + h + " 0 0 cm\n/Im0 Do\nQ\n";
                    byte[] contentBytes = Encoding.ASCII.GetBytes(content);
                    offsets.Add(fs.Position);
                    WriteAscii(fs, contentId.ToString(CultureInfo.InvariantCulture) + " 0 obj\n<< /Length " + contentBytes.Length.ToString(CultureInfo.InvariantCulture) + " >>\nstream\n");
                    fs.Write(contentBytes, 0, contentBytes.Length);
                    WriteAscii(fs, "endstream\nendobj\n");
                }

                string date = DateTime.UtcNow.ToString("D:yyyyMMddHHmmss'Z'", CultureInfo.InvariantCulture);
                string info = "<< /Producer (Mini Scan) /Creator (Mini Scan) /CreationDate (" + date + ") >>";
                WriteObject(fs, offsets, infoId, info);

                long xrefPos = fs.Position;
                WriteAscii(fs, "xref\n0 " + objectCount.ToString(CultureInfo.InvariantCulture) + "\n");
                WriteAscii(fs, "0000000000 65535 f \n");
                for (int i = 1; i < objectCount; i++)
                {
                    long off = i < offsets.Count ? offsets[i] : 0L;
                    WriteAscii(fs, off.ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
                }
                WriteAscii(fs, "trailer\n<< /Size " + objectCount.ToString(CultureInfo.InvariantCulture) + " /Root 1 0 R /Info " + infoId.ToString(CultureInfo.InvariantCulture) + " 0 R >>\n");
                WriteAscii(fs, "startxref\n" + xrefPos.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");
            }
        }

        private static void WriteObject(FileStream fs, List<long> offsets, int id, string body)
        {
            offsets.Add(fs.Position);
            WriteAscii(fs, id.ToString(CultureInfo.InvariantCulture) + " 0 obj\n" + body + "\nendobj\n");
        }

        private static void WriteAscii(Stream s, string text)
        {
            byte[] b = Encoding.ASCII.GetBytes(text);
            s.Write(b, 0, b.Length);
        }

        private static string Num(double v)
        {
            return v.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}