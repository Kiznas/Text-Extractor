using Tesseract;
using System.Runtime.InteropServices;
using System.Net.Http.Headers;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using static System.Windows.Forms.DataFormats;
using System;

namespace UkrainianAnalyzer
{
    public partial class Form1 : Form
    {
        private TesseractEngine ocrEngine;
        private bool isSelecting = false;
        private bool rectangleExist = false;
        private Rectangle selectedArea;
        private Bitmap capturedImage;
        private Point startPoint;
        private bool hotkeyPressed;
        private SettingsForm settings;

        UserRect rect;

        public Form1()
        {
            InitializeComponent();

            int id = 0;
            RegisterHotKey(this.Handle, id, (int)KeyModifier.Shift, Keys.A.GetHashCode());

            settings = new SettingsForm();
            settings.StringChanged += ChangeOCRLanguage;

            notifyIcon1.ContextMenuStrip = new ContextMenuStrip();

            notifyIcon1.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("SETTINGS", null, new EventHandler(Settings), "SETTINGS"),
                new ToolStripMenuItem("EXIT", null, new EventHandler(Exit), "EXIT")
            });
        }
        private void pictureBoxMainPaint(object sender, PaintEventArgs e)
        {
            if (!rectangleExist)
            {
                using (Pen pen = new Pen(Color.Cyan, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, selectedArea);
                }
            }
        }

        private void pictureBoxMain_Up(object sender, MouseEventArgs e)
        {
            if (!rectangleExist)
            {
                rect = new UserRect(selectedArea);
                rect.SetPictureBox(this.pictureBoxMain);
                pictureBoxMain.Invalidate();
                rectangleExist = true;
            }
        }

        private void pictureBoxMain_Down(object sender, MouseEventArgs e)
        {
            if (rectangleExist)
            {
                isSelecting = false;
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

                // Calculate the selected area
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
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                isSelecting = false;
                hotkeyPressed = false;


                GraphicsPath path = new GraphicsPath();
                path.AddRectangle(rect.rectangle);

                Matrix matrix = new Matrix();
                matrix.RotateAt(rect.rotationAngle, new PointF(rect.rectangle.Left + rect.rectangle.Width / 2, rect.rectangle.Top + rect.rectangle.Height / 2));

                path.Transform(matrix);

                RectangleF rotatedRect = path.GetBounds();

                Bitmap rotatedImage = new Bitmap((int)rotatedRect.Width, (int)rotatedRect.Height);

                using (Graphics graphics = Graphics.FromImage(rotatedImage))
                {
                    graphics.TranslateTransform(rotatedImage.Width / 2, rotatedImage.Height / 2);
                    graphics.RotateTransform(-rect.rotationAngle);
                    graphics.TranslateTransform(-rotatedImage.Width / 2, -rotatedImage.Height / 2);
                    graphics.DrawImage(pictureBoxMain.Image, 0, 0, rotatedRect, GraphicsUnit.Pixel);
                }

                string tempImagePath = Path.GetTempFileName();
                rotatedImage.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.MemoryBmp);

                Pix pixImage = Pix.LoadFromFile(tempImagePath);

                File.Delete(tempImagePath);

                using (var page = ocrEngine.Process(pixImage))
                {
                    string extractedText = page.GetText();

                    extractedText = RemoveEmptyLines(extractedText);

                    if (extractedText.Length > 0)
                    {
                        Clipboard.SetText(extractedText);
                    }
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.WindowState = FormWindowState.Minimized;
                }
                
                rectangleExist = false;
                selectedArea = Rectangle.Empty;
                rect.rectangle = Rectangle.Empty;
                rect = null;

                pictureBoxMain.Refresh();
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

            int screenWidth = screenBounds.Width;
            int screenHeight = screenBounds.Height;

            double widthRatio = (double)pictureBoxMain.Width / screenWidth;
            double heightRatio = (double)pictureBoxMain.Height / screenHeight;

            double scaleFactor = Math.Min(widthRatio, heightRatio);

            int newWidth = (int)(screenWidth * scaleFactor);
            int newHeight = (int)(screenHeight * scaleFactor);

            capturedImage = new Bitmap(screenWidth, screenHeight);
            using (Graphics graphics = Graphics.FromImage(capturedImage))
            {
                graphics.CopyFromScreen(screenBounds.Left, screenBounds.Top, 0, 0, capturedImage.Size);
            }

            pictureBoxMain.Image?.Dispose();

            pictureBoxMain.Image = new Bitmap(capturedImage, newWidth, newHeight);

            //Clipboard.SetImage(capturedImage);
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

                    this.notifyIcon1.Visible = false;

                    this.WindowState = FormWindowState.Normal;
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.Location = MousePosition;

                    this.WindowState = FormWindowState.Maximized;
                    this.TopMost = true;

                    hotkeyPressed = true;

                    CaptureScreenshot();
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, 0);
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