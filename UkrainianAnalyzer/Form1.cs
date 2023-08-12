using Tesseract;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace TextAnalyzer
{
    public partial class Form1 : Form
    {
        private Point startPoint;
        private Bitmap capturedImage;
        public Bitmap overlayImage;
        public Rectangle selectedArea;
        private SettingsForm settings;
        private TesseractEngine ocrEngine;

        private bool hotkeyPressed;
        private bool isSelecting = false;
        private bool rectangleExist = false;

        private Color mainColor;
        private Color fillColor;

        private int regularHotkey;
        private int specialHotkey;

        private string filePath = "save.txt";

        private UserRect rect;

        Dictionary<string, string> languageMapping = new Dictionary<string, string>()
        {
            { "en-US", "eng" },
            { "ru-RU", "rus" },
            { "uk-UA", "ukr" },
            { "pl-PL", "pol" }
        };


        public Form1()
        {
            if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1) System.Diagnostics.Process.GetCurrentProcess().Kill();
            InitializeComponent();

            if(File.Exists(filePath))
            {
                LoadVariablesFromJson(filePath, out regularHotkey, out specialHotkey, out mainColor, out fillColor);
            }
            else
            {
                specialHotkey = 1; // ALT
                regularHotkey = 65; // A
                mainColor = Color.White;
                fillColor = Color.Black;
            }

            int id = 0;
            RegisterHotKey(this.Handle, id, specialHotkey, regularHotkey);

            settings = new SettingsForm(mainColor, fillColor, regularHotkey, specialHotkey);
            settings.MainColorChanged += MainColorChanged;
            settings.FillColorChanged += FillColorChanged;
            settings.RegularKeyChanged += RegularKeyChanged;
            settings.SpecialKeyChanged += SpecialKeyChanged;

            notifyIcon1.ContextMenuStrip = new ContextMenuStrip();

            notifyIcon1.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("Settings", null, new EventHandler(Settings)),
                new ToolStripMenuItem("Exit", null, new EventHandler(Exit))
            });

            this.AutoScaleMode= AutoScaleMode.Dpi;

            this.WindowState = FormWindowState.Minimized;
        }
        private void pictureBoxMainPaint(object sender, PaintEventArgs e)
        {
            if (!rectangleExist)
            {
                using (Pen pen = new Pen(mainColor, 2f))
                {
                    pen.DashStyle = DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, selectedArea);
                }
            }

            if (capturedImage != null)
            {
                if (rectangleExist)
                {
                    rect.Draw(e.Graphics);
                    e.Graphics.DrawImage(capturedImage, 0, 0);

                    using (TextureBrush brush = new TextureBrush(overlayImage))
                    {
                        brush.TranslateTransform(0, 0);
                        brush.WrapMode = WrapMode.Tile;
                        PointF[] points = rect.GetPoints();
                        GraphicsPath graphicsPath = new GraphicsPath();
                        graphicsPath.AddPolygon(points);
                        Region region = new Region(new RectangleF(0, 0, overlayImage.Width, overlayImage.Height));
                        region.Exclude(graphicsPath);
                        e.Graphics.FillRegion(brush, region);

                    }
                }
                else
                {
                    using (Pen pen = new Pen(mainColor, 2))
                    {
                        pen.DashStyle = DashStyle.Dash;
                        e.Graphics.DrawRectangle(pen, selectedArea);
                    }
                    using (TextureBrush brush = new TextureBrush(overlayImage))
                    {
                        brush.TranslateTransform(0, 0);
                        brush.WrapMode = WrapMode.Tile;
                        Region region = new Region(new RectangleF(0, 0, overlayImage.Width, overlayImage.Height));
                        region.Exclude(selectedArea);
                        e.Graphics.FillRegion(brush, region);
                    }
                }
            }
        }

        private void pictureBoxMain_Up(object sender, MouseEventArgs e)
        {
            if (!rectangleExist && selectedArea.Width >= 5 && selectedArea.Height >= 5)
            {
                rect = new UserRect(selectedArea);
                rect.SetPictureBox(this.pictureBoxMain, mainColor, fillColor);
                rect.ExtractText += (string mode) => Extract(mode);
                rect.ExtractScreenshot += (string mode) => Extract(mode);
                pictureBoxMain.Invalidate();
                rectangleExist = true;
                isSelecting = false;
            }
            else
            {
                selectedArea = Rectangle.Empty;
                isSelecting = false;
            }
        }

        private void pictureBoxMain_Down(object sender, MouseEventArgs e)
        {
            if (rectangleExist)
            {
                isSelecting = false;
                selectedArea = rect.rectangle;
            }
            else if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
            }
        }

        private void pictureBoxMain_Move(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                Point currentPoint = e.Location;

                int x = Math.Min(startPoint.X, currentPoint.X);
                int y = Math.Min(startPoint.Y, currentPoint.Y);
                int width = Math.Abs(startPoint.X - currentPoint.X);
                int height = Math.Abs(startPoint.Y - currentPoint.Y);
                selectedArea = new Rectangle(x, y, width, height);

                pictureBoxMain.Refresh();
            }
            else if (rect != null && rect.IsOverNode(e.Location))
            {
                isSelecting = false;
                selectedArea = rect.rectangle;
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                hotkeyPressed = false;
                rectangleExist = false;
                selectedArea = Rectangle.Empty;
                if (rect != null)
                {
                    rect.rectangle = Rectangle.Empty;
                }
                pictureBoxMain.Refresh();

                capturedImage?.Dispose();
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Minimized;
            }
        }

        private void Extract(string mode)
        {
            isSelecting = false;
            hotkeyPressed = false;

            Bitmap rotatedImage = new Bitmap((int)capturedImage.Width, (int)capturedImage.Height);

            using (Graphics graphics = Graphics.FromImage(rotatedImage))
            {
                graphics.TranslateTransform(capturedImage.Width / 2, capturedImage.Height / 2);
                graphics.RotateTransform(-rect.rotationAngle);
                graphics.TranslateTransform(-capturedImage.Width / 2, -capturedImage.Height / 2);
                graphics.DrawImage(pictureBoxMain.Image, 0, 0);
            }

            Bitmap croppedBitmap = new Bitmap(selectedArea.Width, selectedArea.Height);

            using (Graphics graphics = Graphics.FromImage(croppedBitmap))
            {
                graphics.DrawImage(rotatedImage, 0, 0, selectedArea, GraphicsUnit.Pixel);
            }

            string tempImagePath = Path.GetTempFileName();
            croppedBitmap.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.MemoryBmp);

            Pix pixImage = Pix.LoadFromFile(tempImagePath);

            File.Delete(tempImagePath);

            if(mode == "screenshot")
            {
                Clipboard.SetImage(croppedBitmap);
            }
            else
            {
                string myCurrentLanguage = InputLanguage.CurrentInputLanguage.Culture.IetfLanguageTag;
                ChangeOCRLanguage(languageMapping[myCurrentLanguage]);
                using (var page = ocrEngine.Process(pixImage))
                {
                    string extractedText = page.GetText();

                    extractedText = RemoveEmptyLines(extractedText);

                    if (extractedText.Length > 0)
                    {
                        Clipboard.SetText(extractedText);
                    }
                }
            }

            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Minimized;

            rectangleExist = false;
            selectedArea = Rectangle.Empty;
            
            rect?.Dispose();
            capturedImage?.Dispose();
            overlayImage?.Dispose();
            rotatedImage?.Dispose();
            croppedBitmap?.Dispose();
            pixImage?.Dispose();

            GC.Collect();

            pictureBoxMain.Refresh();
        }
        
        private string RemoveEmptyLines(string lines)
        {
            return Regex.Replace(lines, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline).TrimEnd();
        }

        private void CaptureScreenshot()
        {
            Point cursorPosition = Cursor.Position;
            Screen screen = Screen.FromPoint(cursorPosition);
            Rectangle screenBounds = screen.Bounds;
            this.Location = screen.Bounds.Location;

            pictureBoxMain.Cursor = Cursors.Cross;

            int screenWidth = screenBounds.Width;
            int screenHeight = screenBounds.Height;

            capturedImage = new Bitmap(screenWidth, screenHeight);
            using (Graphics graphics = Graphics.FromImage(capturedImage))
            {
                graphics.CopyFromScreen(screenBounds.Left, screenBounds.Top, 0, 0, capturedImage.Size);
            }

            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.None;

            pictureBoxMain.Image?.Dispose();

            this.WindowState = FormWindowState.Maximized;

            double widthRatio = (double)pictureBoxMain.Width / screenWidth;
            double heightRatio = (double)pictureBoxMain.Height / screenHeight;

            double scaleFactor = Math.Min(widthRatio, heightRatio);

            int newWidth = (int)(screenWidth * scaleFactor);
            int newHeight = (int)(screenHeight * scaleFactor);

            pictureBoxMain.Image = new Bitmap(capturedImage, newWidth, newHeight);

            overlayImage = new Bitmap(capturedImage.Width, capturedImage.Height);
            using (Graphics g = Graphics.FromImage(overlayImage))
            {
                g.Clear(Color.FromArgb(128, Color.Black));
            }
        }
            

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if(hotkeyPressed != true)
            {
                if (m.Msg == 0x0312)
                {
                    _ = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                    _ = (KeyModifier)((int)m.LParam & 0xFFFF);
                    _ = m.WParam.ToInt32();

                    Show();

                    CaptureScreenshot();
                    hotkeyPressed = true;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, 0);
            notifyIcon1.Visible = false;
                this.WindowState = FormWindowState.Normal;
                this.Show();
            
        }

        public enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        public void ChangeOCRLanguage(string lang)
        {
            ocrEngine = new TesseractEngine("./tessdata", lang, EngineMode.Default);
            ocrEngine.SetVariable("tessedit_pageseg_mode", "9");
            ocrEngine.SetVariable("textord_min_linesize", "1");
        }

        private void MainColorChanged(Color color)
        {
            mainColor = color;
        }
        private void FillColorChanged(Color color)
        {
            fillColor = color;
        }
        private void SpecialKeyChanged(int newSpecialHotkey)
        {
            specialHotkey = newSpecialHotkey;
            UnregisterHotKey(this.Handle, 0);
            RegisterHotKey(this.Handle, 0, specialHotkey, regularHotkey);
        }

        private void RegularKeyChanged(int newRegularHotkey)
        {
            regularHotkey = newRegularHotkey;
            UnregisterHotKey(this.Handle, 0);
            RegisterHotKey(this.Handle, 0, specialHotkey, regularHotkey);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.Visible = true;
            }
            else if (FormWindowState.Normal == this.WindowState)
            { notifyIcon1.Visible = false; }
        }
        private void Settings(object? sender, EventArgs e)
        {
            settings.Show();
            settings.WindowState = FormWindowState.Normal;
        }

        private void Exit(object? sender, EventArgs e)
        {
            StoreVariablesToJson("save.txt", regularHotkey, specialHotkey, mainColor, fillColor);
            Application.Exit();
        }

        public static void StoreVariablesToJson(string filePath, int hotkeyNormal, int hotkeySpecial, Color mainColor, Color fillColor)
        {
            Variables variables = new Variables
            {
                HotkeyNormal = hotkeyNormal,
                HotkeySpecial = hotkeySpecial,
                MainColorArgb = mainColor.ToArgb(),
                FillColorArgb = fillColor.ToArgb()
            };

            string json = JsonSerializer.Serialize(variables, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(filePath, json);
        }

        public static void LoadVariablesFromJson(string filePath, out int hotkeyNormal, out int hotkeySpecial, out Color mainColor, out Color fillColor)
        {
            string json = File.ReadAllText(filePath);

            Variables variables = JsonSerializer.Deserialize<Variables>(json);

            hotkeyNormal = variables.HotkeyNormal;
            hotkeySpecial = variables.HotkeySpecial;
            mainColor = Color.FromArgb(variables.MainColorArgb);
            fillColor = Color.FromArgb(variables.FillColorArgb);
        }
    }
    public class Variables
    {
        public int HotkeyNormal { get; set; }
        public int HotkeySpecial { get; set; }
        public int MainColorArgb { get; set; }
        public int FillColorArgb { get; set; }
    }
}