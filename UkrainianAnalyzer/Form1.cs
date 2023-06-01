using Tesseract;
using System.Runtime.InteropServices;
using System.Net.Http.Headers;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;

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

        UserRect rect;

        public Form1()
        {
            InitializeComponent();

            int id = 0;
            RegisterHotKey(this.Handle, id, (int)KeyModifier.Shift, Keys.A.GetHashCode());
        }

        private void pictureBoxMainPaint(object sender, PaintEventArgs e)
        {
            if (!rectangleExist)
            {
                ControlPaint.DrawReversibleFrame(selectedArea, Color.White, FrameStyle.Dashed);
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

        private string RemoveEmptyLines(string lines)
        {
            return Regex.Replace(lines, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline).TrimEnd();
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

            using (capturedImage = new Bitmap(screenWidth, screenHeight))
            {
                using (Graphics graphics = Graphics.FromImage(capturedImage))
                {
                    graphics.CopyFromScreen(screenBounds.Left, screenBounds.Top, 0, 0, capturedImage.Size);
                }

                using (Graphics overlayGraphics = Graphics.FromImage(capturedImage))
                {
                    using (Brush brush = new SolidBrush(Color.FromArgb(10, Color.Black))) // 50% alpha black
                    {
                        overlayGraphics.FillRectangle(brush, new Rectangle(0, 0, screenWidth, screenHeight));
                    }
                }

                pictureBoxMain.Image?.Dispose();

                pictureBoxMain.Image = new Bitmap(capturedImage, newWidth, newHeight);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

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

        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ocrEngine = new TesseractEngine("./tessdata", checkedListBox1.Text, EngineMode.Default);
            ocrEngine.SetVariable("tessedit_pageseg_mode", "9");
            ocrEngine.SetVariable("textord_min_linesize", "1");
            checkedListBox1.Visible = false;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Minimized;
            Hide();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                isSelecting = false;
                hotkeyPressed = false;

                capturedImage = new Bitmap(rect.rectangle.Width, rect.rectangle.Height);

                using (Graphics graphics = Graphics.FromImage(capturedImage))
                {
                    graphics.DrawImage(pictureBoxMain.Image, 0, 0, rect.rectangle, GraphicsUnit.Pixel);
                }

                string tempImagePath = Path.GetTempFileName();
                capturedImage.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.MemoryBmp);

                Pix pixImage = Pix.LoadFromFile(tempImagePath);

                File.Delete(tempImagePath);

                using (var page = ocrEngine.Process(pixImage))
                {
                    string extractedText = page.GetText();

                    extractedText = RemoveEmptyLines(extractedText);

                    if (extractedText.Length > 0)
                    {
                        Clipboard.SetText(extractedText);
                        //Clipboard.SetImage(capturedImage);
                    }

                    this.FormBorderStyle = FormBorderStyle.Sizable;
                    this.WindowState = FormWindowState.Minimized;
                    Hide();
                }
                rectangleExist = false;
                selectedArea = Rectangle.Empty;
                rect.rectangle = Rectangle.Empty;
                rect = null;

                pictureBoxMain.Refresh(); 
            }
        }
    }
}