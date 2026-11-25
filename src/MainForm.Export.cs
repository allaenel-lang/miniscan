using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace MiniScan
{
    internal sealed partial class MainForm
    {
        private void BuildMenus()
        {
            _pageMenu = CreateMenu();
            _pageMenu.Items.Add(MenuItem("Сохранить в PDF…", IconKind.Pdf, Theme.Text, delegate { ExportPdf(); }));
            _pageMenu.Items.Add(MenuItem("Сохранить в JPG…", IconKind.Image, Theme.Text, delegate { ExportJpg(); }));
            _pageMenu.Items.Add(new ToolStripSeparator());
            _pageMenu.Items.Add(MenuItem("Повернуть влево", IconKind.RotateLeft, Theme.Text, delegate { RotatePages(-90); }));
            _pageMenu.Items.Add(MenuItem("Повернуть вправо", IconKind.RotateRight, Theme.Text, delegate { RotatePages(90); }));
            _pageMenu.Items.Add(new ToolStripSeparator());
            _pageMenu.Items.Add(MenuItem("Удалить", IconKind.Trash, Theme.Red, delegate { DeleteSelectedPages(); }));

            _saveMenu = CreateMenu();
            _saveMenu.Items.Add(MenuItem("Сохранить в PDF…", IconKind.Pdf, Theme.Text, delegate { ExportPdf(); }));
            _saveMenu.Items.Add(MenuItem("Сохранить в JPG…", IconKind.Image, Theme.Text, delegate { ExportJpg(); }));
        }

        private ContextMenuStrip CreateMenu()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            m.Font = Theme.Body;
            m.BackColor = Theme.Surface;
            m.ForeColor = Theme.Text;
            m.ShowImageMargin = true;
            m.ImageScalingSize = new Size(Theme.S(this, 16), Theme.S(this, 16));
            m.Renderer = new ToolStripProfessionalRenderer(new AppColorTable());
            m.Padding = new Padding(Theme.S(this, 2), Theme.S(this, 4), Theme.S(this, 2), Theme.S(this, 4));
            return m;
        }

        private ToolStripMenuItem MenuItem(string text, IconKind icon, Color fore, EventHandler click)
        {
            ToolStripMenuItem item = new ToolStripMenuItem();
            item.Text = text;
            item.ForeColor = fore;
            item.Image = MakeMenuIcon(icon, Theme.S(this, 16), fore);
            item.Padding = new Padding(0, Theme.S(this, 4), 0, Theme.S(this, 4));
            item.Click += click;
            return item;
        }

        private Bitmap MakeMenuIcon(IconKind kind, int size, Color color)
        {
            Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);
                IconPainter.Draw(g, kind, new RectangleF(0, 0, size, size), color, size, false);
            }
            return bmp;
        }

        private void ShowSaveMenu()
        {
            if (_busy || _pages.Count == 0 || _saveMenu == null) return;
            _saveMenu.Show(_saveButton, new Point(0, _saveButton.Height + Theme.S(this, 4)));
        }

        private List<PageItem> GetExportPages()
        {
            List<PageItem> list = new List<PageItem>();
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_selected.Contains(_pages[i])) list.Add(_pages[i]);
            }
            if (list.Count == 0 && _current != null) list.Add(_current);
            if (list.Count == 0) list.AddRange(_pages);
            return list;
        }

        private string InitialExportFolder()
        {
            if (_app.AlwaysSave && !string.IsNullOrEmpty(_app.AlwaysSaveFolder) && Directory.Exists(_app.AlwaysSaveFolder)) return _app.AlwaysSaveFolder;
            if (!string.IsNullOrEmpty(_app.LastExportFolder) && Directory.Exists(_app.LastExportFolder)) return _app.LastExportFolder;
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(docs)) return docs;
            return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        private void ExportPdf()
        {
            if (_busy || _pages.Count == 0) return;
            List<PageItem> pages = GetExportPages();
            if (pages.Count == 0) return;
            string path;
            string folder;
            bool perPage;
            bool alwaysSave;
            using (ExportPdfDialog dlg = new ExportPdfDialog(pages.Count, InitialExportFolder(), _app.AlwaysSave))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                path = dlg.OutputPath;
                folder = dlg.OutputFolder;
                perPage = dlg.PerPage;
                alwaysSave = dlg.AlwaysSave;
            }
            if (string.IsNullOrEmpty(path)) return;
            _app.AlwaysSave = alwaysSave;
            if (alwaysSave && !string.IsNullOrEmpty(folder)) _app.AlwaysSaveFolder = folder;
            _app.Save();

            if (!perPage)
            {
                if (Directory.Exists(path)) path = Path.Combine(path, "Скан.pdf");
                if (!path.ToLowerInvariant().EndsWith(".pdf")) path += ".pdf";
                if (File.Exists(path))
                {
                    if (MessageBox.Show(this, "Файл уже существует. Перезаписать?\r\n\r\n" + path, "Перезапись", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                }
            }
            else
            {
                if (!Directory.Exists(path))
                {
                    try { Directory.CreateDirectory(path); }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Не удалось создать папку: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                List<string> existing = new List<string>();
                for (int i = 0; i < pages.Count; i++)
                {
                    string f = Path.Combine(path, "Скан_" + (i + 1).ToString("000") + ".pdf");
                    if (File.Exists(f)) existing.Add(f);
                }
                if (existing.Count > 0)
                {
                    if (MessageBox.Show(this, "В папке уже есть PDF-файлы с такими именами (" + existing.Count + "). Перезаписать?", "Перезапись", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                }
            }

            BeginWork("Сохранение PDF…", perPage ? "Создаются отдельные файлы" : "Формируется многостраничный документ");
            int done = 0;
            try
            {
                List<PdfImagePage> data = new List<PdfImagePage>();
                for (int i = 0; i < pages.Count; i++)
                {
                    ExportImageData img = pages[i].GetExportImage(false);
                    PdfImagePage pp = new PdfImagePage();
                    pp.Jpeg = img.Jpeg;
                    pp.PixelWidth = img.Width;
                    pp.PixelHeight = img.Height;
                    pp.DpiX = img.DpiX;
                    pp.DpiY = img.DpiY;
                    data.Add(pp);
                }
                if (perPage)
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        string file = Path.Combine(path, "Скан_" + (i + 1).ToString("000") + ".pdf");
                        List<PdfImagePage> one = new List<PdfImagePage>();
                        one.Add(data[i]);
                        PdfWriter.Write(file, one);
                        pages[i].Saved = true;
                        done++;
                    }
                }
                else
                {
                    PdfWriter.Write(path, data);
                    for (int i = 0; i < pages.Count; i++) pages[i].Saved = true;
                    done = pages.Count;
                }
                _app.LastExportFolder = perPage ? path : Path.GetDirectoryName(path);
                _app.Save();
                if (perPage)
                {
                    MessageBox.Show(this, "Сохранено PDF-файлов: " + done + "\r\n\r\n" + path, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this, "PDF сохранён.\r\n\r\n" + path + "\r\nСтраниц: " + done, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                string tail = done > 0 ? "\r\nСохранено файлов: " + done + " из " + pages.Count + "." : "";
                MessageBox.Show(this, "Не удалось сохранить PDF: " + ex.Message + tail, "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                EndWork();
            }
        }

        private void ExportJpg()
        {
            if (_busy || _pages.Count == 0) return;
            List<PageItem> pages = GetExportPages();
            if (pages.Count == 0) return;

            string path = null;
            string folder = null;
            bool multi = false;
            bool alwaysSave = false;
            using (ExportJpgDialog dlg = new ExportJpgDialog(pages.Count, InitialExportFolder(), _app.AlwaysSave))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                path = dlg.OutputPath;
                folder = dlg.OutputFolder;
                multi = dlg.Multi;
                alwaysSave = dlg.AlwaysSave;
            }
            if (string.IsNullOrEmpty(path)) return;
            _app.AlwaysSave = alwaysSave;
            if (alwaysSave && !string.IsNullOrEmpty(folder)) _app.AlwaysSaveFolder = folder;
            _app.Save();

            if (!multi)
            {
                if (Directory.Exists(path)) path = Path.Combine(path, "Скан.jpg");
                if (!path.ToLowerInvariant().EndsWith(".jpg") && !path.ToLowerInvariant().EndsWith(".jpeg")) path += ".jpg";
                if (File.Exists(path))
                {
                    if (MessageBox.Show(this, "Файл уже существует. Перезаписать?\r\n\r\n" + path, "Перезапись", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                }
                BeginWork("Сохранение JPG…", "Подождите, изображение сохраняется");
                try
                {
                    pages[0].SaveJpeg(path);
                    pages[0].Saved = true;
                    _app.LastExportFolder = Path.GetDirectoryName(path);
                    _app.Save();
                    MessageBox.Show(this, "JPG сохранён.\r\n\r\n" + path, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Не удалось сохранить JPG: " + ex.Message, "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { EndWork(); }
                return;
            }

            if (!Directory.Exists(path))
            {
                try { Directory.CreateDirectory(path); }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Не удалось создать папку: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            List<string> files = new List<string>();
            for (int i = 0; i < pages.Count; i++) files.Add(Path.Combine(path, "Скан_" + (i + 1).ToString("000") + ".jpg"));
            List<string> existing = new List<string>();
            for (int i = 0; i < files.Count; i++) if (File.Exists(files[i])) existing.Add(files[i]);
            if (existing.Count > 0)
            {
                if (MessageBox.Show(this, "В папке уже есть JPG-файлы с такими именами (" + existing.Count + "). Перезаписать?", "Перезапись", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            }
            BeginWork("Сохранение JPG…", "Сохраняется " + pages.Count + " изображений");
            int done = 0;
            try
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    pages[i].SaveJpeg(files[i]);
                    pages[i].Saved = true;
                    done++;
                }
                _app.LastExportFolder = path;
                _app.Save();
                MessageBox.Show(this, "Сохранено изображений: " + done + "\r\n\r\n" + path, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось сохранить JPG: " + ex.Message + "\r\nСохранено: " + done + " из " + pages.Count + ".", "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { EndWork(); }
        }

        private void RotatePages(int delta)
        {
            if (_busy || _current == null) return;
            List<PageItem> targets = new List<PageItem>();
            if (_selected.Count > 0 && _selected.Contains(_current))
            {
                for (int i = 0; i < _pages.Count; i++) if (_selected.Contains(_pages[i])) targets.Add(_pages[i]);
            }
            else targets.Add(_current);
            for (int i = 0; i < targets.Count; i++) targets[i].Rotate(delta);
            for (int i = 0; i < _thumbs.Count; i++) _thumbs[i].Invalidate();
            _preview.RefreshImage();
            SetStatus(targets.Count > 1 ? "Повёрнуто страниц: " + targets.Count : "Страница повёрнута", StatusKind.Ready);
            UpdateUi();
        }

        private void DeleteSelectedPages()
        {
            if (_busy || _pages.Count == 0) return;
            List<PageItem> targets = new List<PageItem>();
            if (_selected.Count > 0 && (_current == null || _selected.Contains(_current)))
            {
                for (int i = 0; i < _pages.Count; i++) if (_selected.Contains(_pages[i])) targets.Add(_pages[i]);
            }
            else if (_current != null) targets.Add(_current);
            if (targets.Count == 0) return;
            string question;
            if (targets.Count == 1) question = "Удалить страницу " + (_pages.IndexOf(targets[0]) + 1) + "?";
            else question = "Удалить выбранные страницы (" + targets.Count + ")?";
            if (MessageBox.Show(this, question, "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            int firstIndex = _pages.IndexOf(targets[0]);
            for (int i = 0; i < targets.Count; i++)
            {
                PageItem p = targets[i];
                _selected.Remove(p);
                _pages.Remove(p);
                PageThumbnail thumb = null;
                for (int j = 0; j < _thumbs.Count; j++)
                {
                    if (_thumbs[j].Page == p) { thumb = _thumbs[j]; break; }
                }
                if (thumb != null)
                {
                    _thumbs.Remove(thumb);
                    _pagesFlow.Controls.Remove(thumb);
                    thumb.Dispose();
                }
                p.Dispose();
            }
            if (_pages.Count == 0)
            {
                _current = null;
                _anchor = -1;
                _scanActive = false;
            }
            else
            {
                int idx = Math.Min(firstIndex, _pages.Count - 1);
                if (idx < 0) idx = 0;
                _current = _pages[idx];
                _selected.Clear();
                _selected.Add(_current);
                _anchor = idx;
            }
            for (int i = 0; i < _thumbs.Count; i++) _thumbs[i].Index = i;
            _preview.SetPage(_current);
            SetStatus(targets.Count > 1 ? "Удалено страниц: " + targets.Count : "Страница удалена", StatusKind.Ready);
            UpdateUi();
        }

        private void BeginWork(string message, string sub)
        {
            _busyOverlay.SetMessage(message, sub);
            _busyOverlay.Visible = true;
            _busyOverlay.BringToFront();
            Cursor = Cursors.WaitCursor;
            _busyOverlay.Update();
        }

        private void EndWork()
        {
            Cursor = Cursors.Default;
            _busyOverlay.Visible = false;
            UpdateUi();
        }
    }
}