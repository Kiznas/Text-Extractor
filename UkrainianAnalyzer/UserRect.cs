using System.Drawing;
using System.Drawing.Drawing2D;

namespace TextAnalyzer
{
    public class UserRect
    {
        private PictureBox mPictureBox;

        public Rectangle rectangle;
        public Rectangle selectedArea;

        public bool allowDeformingDuringMovement = false;
        private bool mIsClick = false;
        private bool mMove = false;

        private int oldX;
        private int oldY;

        private int buttonWidth = 27;
        private int buttonHeight = 27;
        private int sizeNodeRect = 8;

        private const int rotationHandleSize = 12;

        public Bitmap mBmp = null;
        public PosSizableRect nodeSelected = PosSizableRect.None;
        public float rotationAngle = 0;

        public event Action <String> ExtractText;
        public event Action <String> ExtractScreenshot;

        private Icon textIcon;
        private Icon screenshotIcon;

        private Color mainColor;
        private Color fillColor;

        public enum PosSizableRect
        {
            UpMiddle,
            LeftMiddle,
            LeftBottom,
            LeftUp,
            RightUp,
            RightMiddle,
            RightBottom,
            BottomMiddle,
            RotationHandle,
            TextConfirmHandle,
            ScreenshotConfirmHandle,
            None
        };

        public UserRect(Rectangle r)
        {
            rectangle = r;
            mIsClick = false;
            mainColor = Color.Cyan;
            fillColor = Color.Black;
            textIcon = new Icon("text.ico");
            screenshotIcon = new Icon("screenshot.ico");
        }
        public void Draw(Graphics g)
        {
            Pen pen = new Pen(mainColor, 1.5f);
            pen.DashStyle = DashStyle.Dash;

            PointF[] points = GetPoints();
            g.DrawPolygon(pen, points);

            foreach (PosSizableRect pos in Enum.GetValues(typeof(PosSizableRect)))
            {
                using Brush brush = new SolidBrush(mainColor);

                switch (pos)
                {
                    case PosSizableRect.RotationHandle:
                        g.FillEllipse(brush, GetRect(pos));
                        break;

                    case PosSizableRect.TextConfirmHandle:
                        g.DrawIcon(textIcon, GetRect(pos));
                        break;

                    case PosSizableRect.ScreenshotConfirmHandle:
                        g.DrawIcon(screenshotIcon, GetRect(pos));
                        break;

                    default:
                        g.FillRectangle(brush, GetRect(pos));
                        Rectangle handleRect = GetRect(pos);
                        int smallRectSize = sizeNodeRect - 2;
                        int smallRectX = handleRect.X + handleRect.Width / 2 - smallRectSize / 2;
                        int smallRectY = handleRect.Y + handleRect.Height / 2 - smallRectSize / 2;
                        Brush fillColorBrush = new SolidBrush(fillColor);
                        g.FillRectangle(fillColorBrush, smallRectX, smallRectY, smallRectSize, smallRectSize);
                        break;

                }
            }
        }
        public void SetPictureBox(PictureBox p, Color mainColorExternal, Color fillColorExternal)
        {
            mainColor = mainColorExternal;
            fillColor = fillColorExternal;
            this.mPictureBox = p;
            mPictureBox.MouseDown += new MouseEventHandler(mPictureBox_MouseDown);
            mPictureBox.MouseUp += new MouseEventHandler(mPictureBox_MouseUp);
            mPictureBox.MouseMove += new MouseEventHandler(mPictureBox_MouseMove);
            mPictureBox.Paint += new PaintEventHandler(mPictureBox_Paint);
        }
        private void mPictureBox_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                Draw(e.Graphics);
            }
            catch (Exception exp)
            {
                System.Console.WriteLine(exp.Message);
            }
        }

        private void mPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            mIsClick = true;

            nodeSelected = PosSizableRect.None;
            nodeSelected = GetNodeSelectable(e.Location);

            if(nodeSelected == PosSizableRect.TextConfirmHandle)
            {
                ExtractText.Invoke("text");
            }

            if (nodeSelected == PosSizableRect.ScreenshotConfirmHandle)
            {
                ExtractScreenshot.Invoke("screenshot");
            }

            if (rectangle.Contains(new Point(e.X, e.Y)))
            {
                mMove = true;
            }
            oldX = e.X;
            oldY = e.Y;
        }

        private void mPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            mIsClick = false;
            mMove = false;
        }

        private void mPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            ChangeCursor(e.Location);
            if (mIsClick == false)
            {
                return;
            }

            Rectangle backupRect = rectangle;

            switch (nodeSelected)
            {
                case PosSizableRect.LeftUp:
                    rectangle.X += e.X - oldX;
                    rectangle.Width -= e.X - oldX;
                    rectangle.Y += e.Y - oldY;
                    rectangle.Height -= e.Y - oldY;
                    break;
                case PosSizableRect.LeftMiddle:
                    rectangle.X += e.X - oldX;
                    rectangle.Width -= e.X - oldX;
                    break;
                case PosSizableRect.LeftBottom:
                    rectangle.Width -= e.X - oldX;
                    rectangle.X += e.X - oldX;
                    rectangle.Height += e.Y - oldY;
                    break;
                case PosSizableRect.BottomMiddle:
                    rectangle.Height += e.Y - oldY;
                    break;
                case PosSizableRect.RightUp:
                    rectangle.Width += e.X - oldX;
                    rectangle.Y += e.Y - oldY;
                    rectangle.Height -= e.Y - oldY;
                    break;
                case PosSizableRect.RightBottom:
                    rectangle.Width += e.X - oldX;
                    rectangle.Height += e.Y - oldY;
                    break;
                case PosSizableRect.RightMiddle:
                    rectangle.Width += e.X - oldX;
                    break;

                case PosSizableRect.UpMiddle:
                    rectangle.Y += e.Y - oldY;
                    rectangle.Height -= e.Y - oldY;
                    break;

                case PosSizableRect.RotationHandle:
                    int maxRotationAngle = 45;

                    float newRotationAngle = rotationAngle + (e.X - oldX) * 0.2f;

                    newRotationAngle = Math.Max(-maxRotationAngle, Math.Min(maxRotationAngle, newRotationAngle));

                    if (newRotationAngle >= -maxRotationAngle && newRotationAngle <= maxRotationAngle)
                    {
                        rotationAngle = newRotationAngle;
                    }

                    oldX = e.X;
                    break;

                default:
                    if (mMove)
                    {
                        rectangle.X = rectangle.X + e.X - oldX;
                        rectangle.Y = rectangle.Y + e.Y - oldY;
                    }
                    break;
            }
            oldX = e.X;
            oldY = e.Y;

            if (rectangle.Width < 5 || rectangle.Height < 5)
            {
                rectangle = backupRect;
            }

            TestIfRectInsideArea();

            mPictureBox.Invalidate();
        }

        private void TestIfRectInsideArea()
        {
            // Test if rectangle still inside the area.
            if (rectangle.X < 0) rectangle.X = 0;
            if (rectangle.Y < 0) rectangle.Y = 0;
            if (rectangle.Width <= 0) rectangle.Width = 1;
            if (rectangle.Height <= 0) rectangle.Height = 1;

            if (rectangle.X + rectangle.Width > mPictureBox.Width)
            {
                rectangle.Width = mPictureBox.Width - rectangle.X - 1; // -1 to be still show 
                if (allowDeformingDuringMovement == false)
                {
                    mIsClick = false;
                }
            }
            if (rectangle.Y + rectangle.Height > mPictureBox.Height)
            {
                rectangle.Height = mPictureBox.Height - rectangle.Y - 1;// -1 to be still show 
                if (allowDeformingDuringMovement == false)
                {
                    mIsClick = false;
                }
            }
        }

        public Rectangle CreateRectSizableNode(int x, int y)
        {
            return new Rectangle(x - sizeNodeRect / 2, y - sizeNodeRect / 2, sizeNodeRect, sizeNodeRect);
        }

        public PointF[] GetPoints()
        {
            int i = 0;
            PointF[] points = new PointF[4];
            foreach (PosSizableRect pos in Enum.GetValues(typeof(PosSizableRect)))
            {
                if(pos == PosSizableRect.LeftUp ||
                    pos == PosSizableRect.RightUp ||
                    pos == PosSizableRect.LeftBottom ||
                    pos == PosSizableRect.RightBottom)
                {
                    points[i] = (new PointF(GetRect(pos).Left + GetRect(pos).Width / 2, GetRect(pos).Top + GetRect(pos).Height / 2));
                    i++;
                }
            }

            return points;
        }
        private Rectangle GetRect(PosSizableRect p)
        {
            float centerX = rectangle.X + rectangle.Width / 2;
            float centerY = rectangle.Y + rectangle.Height / 2;
            float sin = (float)Math.Sin(rotationAngle * Math.PI / 180);
            float cos = (float)Math.Cos(rotationAngle * Math.PI / 180);

            switch (p)
            {
                case PosSizableRect.LeftUp:
                    return RotatePoint(CreateRectSizableNode(rectangle.X, rectangle.Y), centerX, centerY, sin, cos);

                case PosSizableRect.LeftMiddle:
                    return RotatePoint(CreateRectSizableNode(rectangle.X, rectangle.Y + rectangle.Height / 2), centerX, centerY, sin, cos);

                case PosSizableRect.LeftBottom:
                    return RotatePoint(CreateRectSizableNode(rectangle.X, rectangle.Y + rectangle.Height), centerX, centerY, sin, cos);

                case PosSizableRect.BottomMiddle:
                    return RotatePoint(CreateRectSizableNode(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height), centerX, centerY, sin, cos);

                case PosSizableRect.RightUp:
                    return RotatePoint(CreateRectSizableNode(rectangle.X + rectangle.Width, rectangle.Y), centerX, centerY, sin, cos);

                case PosSizableRect.RightBottom:
                    return RotatePoint(CreateRectSizableNode(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height), centerX, centerY, sin, cos);

                case PosSizableRect.RightMiddle:
                    return RotatePoint(CreateRectSizableNode(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height / 2), centerX, centerY, sin, cos);

                case PosSizableRect.UpMiddle:
                    return RotatePoint(CreateRectSizableNode(rectangle.X + rectangle.Width / 2, rectangle.Y), centerX, centerY, sin, cos);

                case PosSizableRect.RotationHandle:
                    Rectangle rotationHandleRect = new Rectangle((int)(centerX - rotationHandleSize / 2), (int)(rectangle.Y - rotationHandleSize - 10), rotationHandleSize, rotationHandleSize);
                    return RotatePoint(rotationHandleRect, centerX, centerY, sin, cos);

                case PosSizableRect.TextConfirmHandle:
                    int but1X = (int)(rectangle.X + rectangle.Width - buttonWidth); 
                    int but1Y = (int)(rectangle.Y + rectangle.Height + 6); 
                    Rectangle button1 = new Rectangle(but1X, but1Y, buttonWidth, buttonHeight);
                    return RotatePoint(button1, centerX, centerY, sin, cos);

                case PosSizableRect.ScreenshotConfirmHandle:
                    int but2X = (int)(rectangle.X + rectangle.Width - 2.2f * buttonWidth);
                    int but2Y = (int)(rectangle.Y + rectangle.Height + 6);
                    Rectangle button2 = new Rectangle(but2X, but2Y, buttonWidth, buttonHeight);
                    return RotatePoint(button2, centerX, centerY, sin, cos);

                default:
                    return new Rectangle();
            }
        }

        private Rectangle RotatePoint(Rectangle rect, float centerX, float centerY, float sin, float cos)
        {
            float x = rect.X - centerX;
            float y = rect.Y - centerY;

            // Rotate the point
            float rotatedX = x * cos - y * sin;
            float rotatedY = x * sin + y * cos;

            // Translate the point back to the original position
            int translatedX = (int)Math.Round(rotatedX + centerX);
            int translatedY = (int)Math.Round(rotatedY + centerY);

            return new Rectangle(translatedX, translatedY, rect.Width, rect.Height);
        }

        private PosSizableRect GetNodeSelectable(Point x)
        {
            foreach (PosSizableRect r in Enum.GetValues(typeof(PosSizableRect)))
            {
                if (r == PosSizableRect.RotationHandle && GetRect(r).Contains(x))
                    return r;
                else if (GetRect(r).Contains(x))
                    return r;
            }

            return PosSizableRect.None;
        }

        public void Dispose()
        {
            if (mPictureBox != null)
            {
                mPictureBox.MouseDown -= mPictureBox_MouseDown;
                mPictureBox.MouseUp -= mPictureBox_MouseUp;
                mPictureBox.MouseMove -= mPictureBox_MouseMove;
                mPictureBox.Paint -= mPictureBox_Paint;
                mPictureBox = null;
            }
            // Dispose any other disposable resources here, if applicable.

            // Set other fields to null or default values.
            rectangle = Rectangle.Empty;
            selectedArea = Rectangle.Empty;
            mBmp?.Dispose();
            mBmp = null;
            textIcon?.Dispose();
            textIcon = null;
        }

        public bool IsOverNode(Point x)
        {
            foreach (PosSizableRect r in Enum.GetValues(typeof(PosSizableRect)))
            {
                if (r == PosSizableRect.RotationHandle && GetRect(r).Contains(x))
                    return true;
                else if (GetRect(r).Contains(x))
                    return true;
            }

            return true;
        }
        private void ChangeCursor(Point p)
        {
            mPictureBox.Cursor = GetCursor(GetNodeSelectable(p));
        }

        /// <summary>
        /// Get cursor for the handle
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Cursor GetCursor(PosSizableRect p)
        {
            switch (p)
            {
                case PosSizableRect.LeftUp:
                    return Cursors.SizeNWSE;

                case PosSizableRect.LeftMiddle:
                    return Cursors.SizeWE;

                case PosSizableRect.LeftBottom:
                    return Cursors.SizeNESW;

                case PosSizableRect.BottomMiddle:
                    return Cursors.SizeNS;

                case PosSizableRect.RightUp:
                    return Cursors.SizeNESW;

                case PosSizableRect.RightBottom:
                    return Cursors.SizeNWSE;

                case PosSizableRect.RightMiddle:
                    return Cursors.SizeWE;

                case PosSizableRect.UpMiddle:
                    return Cursors.SizeNS;

                case PosSizableRect.RotationHandle:
                    return Cursors.SizeWE;

                case PosSizableRect.TextConfirmHandle:
                    return Cursors.Hand;

                case PosSizableRect.ScreenshotConfirmHandle:
                    return Cursors.Hand;

                default:
                    return Cursors.Default;
            }
        }
    }
}