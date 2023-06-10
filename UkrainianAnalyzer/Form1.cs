using Tesseract;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.DataFormats;

namespace UkrainianAnalyzer
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
        private bool textMode = true;
        private bool isSelecting = false;
        private bool rectangleExist = false;

        private ToolStripMenuItem positionXItem;
        private ToolStripMenuItem positionYItem;

        private UserRect rect;

        public Form1()
        {
            InitializeComponent();

            int id = 0;
            RegisterHotKey(this.Handle, id, (int)KeyModifier.Shift, Keys.A.GetHashCode());

            settings = new SettingsForm();
            settings.StringChanged += ChangeOCRLanguage;

            notifyIcon1.ContextMenuStrip = new ContextMenuStrip();

            ToolStripMenuItem switchItem = new ToolStripMenuItem("Mode");

            positionXItem = new ToolStripMenuItem("TextMode");
            positionXItem.Click += PositionXItem_Click;
            switchItem.DropDownItems.Add(positionXItem);

            positionYItem = new ToolStripMenuItem("ScreenshotMode");
            positionYItem.Click += PositionYItem_Click;
            switchItem.DropDownItems.Add(positionYItem);

            notifyIcon1.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                switchItem,
                new ToolStripMenuItem("SETTINGS", null, new EventHandler(Settings)),
                new ToolStripMenuItem("EXIT", null, new EventHandler(Exit))
            });

            positionXItem.Checked = true;
        }

        private void pictureBoxMainPaint(object sender, PaintEventArgs e)
        {
            if (!rectangleExist)
            {
                using (Pen pen = new Pen(Color.Cyan, 2))
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
                }
                if (!rectangleExist)
                {
                    using (Pen pen = new Pen(Color.Cyan, 2))
                    {
                        pen.DashStyle = DashStyle.Dash;
                        e.Graphics.DrawRectangle(pen, selectedArea);
                    }
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

        private void pictureBoxMain_Up(object sender, MouseEventArgs e)
        {
            if (!rectangleExist)
            {
                rect = new UserRect(selectedArea);
                rect.SetPictureBox(this.pictureBoxMain, this);
                pictureBoxMain.Invalidate();
                rectangleExist = true;
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
            if (e.KeyCode == Keys.Enter)
            {
                isSelecting = false;
                hotkeyPressed = false;

                Bitmap rotatedImage = new Bitmap((int) capturedImage.Width, (int)capturedImage.Height);

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
                    graphics.DrawImage(rotatedImage, 0,0, selectedArea, GraphicsUnit.Pixel);
                }

                string tempImagePath = Path.GetTempFileName();
                croppedBitmap.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.MemoryBmp);

                Pix pixImage = Pix.LoadFromFile(tempImagePath);

                File.Delete(tempImagePath);

                using (var page = ocrEngine.Process(pixImage))
                {
                    string extractedText = page.GetText();

                    extractedText = RemoveEmptyLines(extractedText);

                    if (textMode)
                    {
                        if (extractedText.Length > 0)
                        {
                            Clipboard.SetText(extractedText);
                        }
                    }
                    else { Clipboard.SetImage(croppedBitmap); }

                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.WindowState = FormWindowState.Minimized;
                }

                rectangleExist = false;
                selectedArea = Rectangle.Empty;
                rect.rectangle = Rectangle.Empty;

                pictureBoxMain.Refresh();

                capturedImage?.Dispose();
                rotatedImage.Dispose();
                pixImage.Dispose();
            }
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

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ChangeOCRLanguage(checkedListBox1.Text);
            checkedListBox1.Visible = false;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Minimized;
        }

        public void ChangeOCRLanguage(string lang)
        {
            ocrEngine = new TesseractEngine("./tessdata", lang, EngineMode.Default);
            ocrEngine.SetVariable("tessedit_pageseg_mode", "9");
            ocrEngine.SetVariable("textord_min_linesize", "1");
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

        private void PositionXItem_Click(object? sender, EventArgs e)
        {
            positionXItem.Checked = true;
            positionYItem.Checked = false;
            textMode = true;
        }
        private void PositionYItem_Click(object? sender, EventArgs e)
        {
            positionXItem.Checked = false;
            positionYItem.Checked = true;
            textMode = false;
        }

        private void Settings(object? sender, EventArgs e)
        {
            settings.Show();
            settings.WindowState = FormWindowState.Normal;
        }

        private void Exit(object? sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}