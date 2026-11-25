using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MiniScan
{
    internal enum IconKind
    {
        None, Scanner, Sliders, ChevronDown, Plus, Check, Pdf, Image, Trash,
        RotateLeft, RotateRight, Folder, Refresh, Close, Warning, ArrowRight,
        Save, ZoomIn, ZoomOut, Fit, Eye, Gear
    }

    internal static class IconPainter
    {
        public static void Draw(Graphics g, IconKind kind, RectangleF rect, Color color, float iconSize, bool filled)
        {
            if (kind == IconKind.None || rect.Width <= 0f || rect.Height <= 0f) return;
            GraphicsState state = g.Save();
            try
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TranslateTransform(rect.X, rect.Y);
                g.ScaleTransform(rect.Width / 24f, rect.Height / 24f);
                float sw = Math.Max(1.35f, iconSize / 12f);
                using (Pen p = new Pen(color, sw))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    p.LineJoin = LineJoin.Round;
                    DrawCore(g, p, kind, color, filled);
                }
            }
            finally
            {
                g.Restore(state);
            }
        }

        private static void DrawCore(Graphics g, Pen p, IconKind kind, Color color, bool filled)
        {
            using (SolidBrush b = new SolidBrush(color))
            {
                switch (kind)
                {
                    case IconKind.Scanner:
                        using (GraphicsPath body = Theme.RoundedRect(new RectangleF(2.5f, 6.5f, 19f, 12.5f), 2.6f))
                        {
                            g.DrawPath(p, body);
                        }
                        g.DrawLine(p, 4.5f, 10.5f, 19.5f, 10.5f);
                        g.DrawRectangle(p, 8f, 2.5f, 8f, 7f);
                        g.DrawLine(p, 9.5f, 5f, 14.5f, 5f);
                        g.DrawLine(p, 9.5f, 7f, 13f, 7f);
                        g.FillEllipse(b, 16.2f, 14f, 2.1f, 2.1f);
                        break;
                    case IconKind.Sliders:
                        g.DrawLine(p, 4f, 6f, 20f, 6f);
                        g.DrawLine(p, 4f, 12f, 20f, 12f);
                        g.DrawLine(p, 4f, 18f, 20f, 18f);
                        g.FillEllipse(b, 7.2f, 4.3f, 3.4f, 3.4f);
                        g.FillEllipse(b, 13.4f, 10.3f, 3.4f, 3.4f);
                        g.FillEllipse(b, 8.7f, 16.3f, 3.4f, 3.4f);
                        break;
                    case IconKind.ChevronDown:
                        g.DrawLines(p, new PointF[] { new PointF(6f, 9.5f), new PointF(12f, 15.5f), new PointF(18f, 9.5f) });
                        break;
                    case IconKind.Plus:
                        g.DrawLine(p, 12f, 5f, 12f, 19f);
                        g.DrawLine(p, 5f, 12f, 19f, 12f);
                        break;
                    case IconKind.Check:
                        g.DrawLines(p, new PointF[] { new PointF(5f, 12.5f), new PointF(10f, 17.5f), new PointF(19f, 6.5f) });
                        break;
                    case IconKind.Pdf:
                    case IconKind.Image:
                        using (GraphicsPath page = PagePath())
                        {
                            g.DrawPath(p, page);
                        }
                        if (kind == IconKind.Image)
                        {
                            g.DrawEllipse(p, 7.5f, 8.5f, 2.2f, 2.2f);
                            g.DrawLines(p, new PointF[] { new PointF(6f, 18f), new PointF(10.5f, 13.5f), new PointF(13.5f, 16.5f), new PointF(15.5f, 14.3f), new PointF(18f, 17f) });
                        }
                        else
                        {
                            g.DrawLine(p, 7.5f, 13.5f, 16.5f, 13.5f);
                            g.DrawLine(p, 7.5f, 17f, 14f, 17f);
                        }
                        break;
                    case IconKind.Trash:
                        g.DrawLine(p, 4.5f, 7f, 19.5f, 7f);
                        g.DrawLine(p, 9.5f, 4f, 14.5f, 4f);
                        g.DrawLines(p, new PointF[] { new PointF(6.5f, 7f), new PointF(7.6f, 21f), new PointF(16.4f, 21f), new PointF(17.5f, 7f) });
                        g.DrawLine(p, 10f, 10.5f, 10.4f, 17.5f);
                        g.DrawLine(p, 14f, 10.5f, 13.6f, 17.5f);
                        break;
                    case IconKind.RotateLeft:
                        g.DrawArc(p, 4f, 5f, 16f, 16f, 200f, 245f);
                        g.DrawLines(p, new PointF[] { new PointF(4.2f, 9.5f), new PointF(4f, 4.5f), new PointF(9f, 5f) });
                        break;
                    case IconKind.RotateRight:
                        g.DrawArc(p, 4f, 5f, 16f, 16f, -85f, 245f);
                        g.DrawLines(p, new PointF[] { new PointF(19.8f, 9.5f), new PointF(20f, 4.5f), new PointF(15f, 5f) });
                        break;
                    case IconKind.Folder:
                        using (GraphicsPath f = new GraphicsPath())
                        {
                            f.AddLines(new PointF[] { new PointF(3f, 7f), new PointF(3f, 20f), new PointF(21f, 20f), new PointF(21f, 8.5f), new PointF(12f, 8.5f), new PointF(10f, 6f), new PointF(3f, 6f) });
                            f.CloseFigure();
                            g.DrawPath(p, f);
                        }
                        break;
                    case IconKind.Refresh:
                        g.DrawArc(p, 5f, 5f, 14f, 14f, 40f, 280f);
                        g.DrawLines(p, new PointF[] { new PointF(18.4f, 5.4f), new PointF(19.2f, 10f), new PointF(14.6f, 9.2f) });
                        break;
                    case IconKind.Close:
                        g.DrawLine(p, 6f, 6f, 18f, 18f);
                        g.DrawLine(p, 18f, 6f, 6f, 18f);
                        break;
                    case IconKind.Warning:
                        g.DrawLines(p, new PointF[] { new PointF(12f, 3.5f), new PointF(21f, 20f), new PointF(3f, 20f), new PointF(12f, 3.5f) });
                        g.DrawLine(p, 12f, 9f, 12f, 14f);
                        g.FillEllipse(b, 11.1f, 16.4f, 1.8f, 1.8f);
                        break;
                    case IconKind.ArrowRight:
                        g.DrawLine(p, 5f, 12f, 19f, 12f);
                        g.DrawLines(p, new PointF[] { new PointF(13f, 6f), new PointF(19f, 12f), new PointF(13f, 18f) });
                        break;
                    case IconKind.Save:
                        g.DrawLine(p, 12f, 4f, 12f, 15f);
                        g.DrawLines(p, new PointF[] { new PointF(7f, 10f), new PointF(12f, 15f), new PointF(17f, 10f) });
                        g.DrawLines(p, new PointF[] { new PointF(4.5f, 16.5f), new PointF(4.5f, 20f), new PointF(19.5f, 20f), new PointF(19.5f, 16.5f) });
                        break;
                    case IconKind.ZoomIn:
                    case IconKind.ZoomOut:
                        g.DrawEllipse(p, 5f, 5f, 12f, 12f);
                        g.DrawLine(p, 15.5f, 15.5f, 21f, 21f);
                        g.DrawLine(p, 8.5f, 11f, 13.5f, 11f);
                        if (kind == IconKind.ZoomIn) g.DrawLine(p, 11f, 8.5f, 11f, 13.5f);
                        break;
                    case IconKind.Fit:
                        g.DrawLines(p, new PointF[] { new PointF(9f, 4f), new PointF(4f, 4f), new PointF(4f, 9f) });
                        g.DrawLines(p, new PointF[] { new PointF(15f, 4f), new PointF(20f, 4f), new PointF(20f, 9f) });
                        g.DrawLines(p, new PointF[] { new PointF(20f, 15f), new PointF(20f, 20f), new PointF(15f, 20f) });
                        g.DrawLines(p, new PointF[] { new PointF(9f, 20f), new PointF(4f, 20f), new PointF(4f, 15f) });
                        break;
                    case IconKind.Eye:
                        g.DrawArc(p, 2.5f, 7f, 19f, 12f, 200f, 140f);
                        g.DrawArc(p, 2.5f, 5f, 19f, 14f, 20f, 140f);
                        g.DrawEllipse(p, 9.5f, 9.5f, 5f, 5f);
                        break;
                    case IconKind.Gear:
                        g.DrawEllipse(p, 8.5f, 8.5f, 7f, 7f);
                        for (int i = 0; i < 8; i++)
                        {
                            double a = Math.PI * i / 4.0;
                            float x1 = 12f + (float)Math.Cos(a) * 7.2f;
                            float y1 = 12f + (float)Math.Sin(a) * 7.2f;
                            float x2 = 12f + (float)Math.Cos(a) * 10.2f;
                            float y2 = 12f + (float)Math.Sin(a) * 10.2f;
                            g.DrawLine(p, x1, y1, x2, y2);
                        }
                        break;
                }
            }
        }

        private static GraphicsPath PagePath()
        {
            GraphicsPath page = new GraphicsPath();
            page.AddLines(new PointF[] {
                new PointF(5f, 2.5f), new PointF(14f, 2.5f), new PointF(19f, 7.5f),
                new PointF(19f, 21.5f), new PointF(5f, 21.5f), new PointF(5f, 2.5f)
            });
            page.CloseFigure();
            return page;
        }
    }
}