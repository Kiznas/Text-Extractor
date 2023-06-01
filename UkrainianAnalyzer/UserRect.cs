using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UkrainianAnalyzer
{
    public class UserRect
    {
        private PictureBox mPictureBox;
        public Rectangle rectangle;
        public bool allowDeformingDuringMovement = false;
        private bool mIsClick = false;
        private bool mMove = false;
        private int oldX;
        private int oldY;
        private int sizeNodeRect = 10;
        private Bitmap mBmp = null;
        public PosSizableRect nodeSelected = PosSizableRect.None;
        private int angle = 30;

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
            None

        };

        public UserRect(Rectangle r)
        {
            rectangle = r;
            mIsClick = false;
        }

        public void Draw(Graphics g)
        {
            Pen pen = new Pen(Color.White);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            g.DrawRectangle(pen, rectangle);

            foreach (PosSizableRect pos in Enum.GetValues(typeof(PosSizableRect)))
            {
                using Brush brush = new SolidBrush(Color.White);
                g.FillRectangle(brush, GetRect(pos));
            }
        }

        public void SetBitmapFile(string filename)
        {
            this.mBmp = new Bitmap(filename);
        }

        public void SetBitmap(Bitmap bmp)
        {
            this.mBmp = bmp;
        }

        public void SetPictureBox(PictureBox p)
        {
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

        private Rectangle GetRect(PosSizableRect p)
        {
            switch (p)
            {
                case PosSizableRect.LeftUp:
                    return CreateRectSizableNode(rectangle.X, rectangle.Y);

                case PosSizableRect.LeftMiddle:
                    return CreateRectSizableNode(rectangle.X, rectangle.Y + +rectangle.Height / 2);

                case PosSizableRect.LeftBottom:
                    return CreateRectSizableNode(rectangle.X, rectangle.Y + rectangle.Height);

                case PosSizableRect.BottomMiddle:
                    return CreateRectSizableNode(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height);

                case PosSizableRect.RightUp:
                    return CreateRectSizableNode(rectangle.X + rectangle.Width, rectangle.Y);

                case PosSizableRect.RightBottom:
                    return CreateRectSizableNode(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height);

                case PosSizableRect.RightMiddle:
                    return CreateRectSizableNode(rectangle.X + rectangle.Width, rectangle.Y + rectangle.Height / 2);

                case PosSizableRect.UpMiddle:
                    return CreateRectSizableNode(rectangle.X + rectangle.Width / 2, rectangle.Y);
                default:
                    return new Rectangle();
            }
        }

        public PosSizableRect GetNodeSelectable(Point p)
        {
            foreach (PosSizableRect r in Enum.GetValues(typeof(PosSizableRect)))
            {
                if (GetRect(r).Contains(p))
                {
                    return r;
                }
            }
            return PosSizableRect.None;
        }

        public bool IsOverNode(Point p)
        {
            foreach (PosSizableRect r in Enum.GetValues(typeof(PosSizableRect)))
            {
                if (GetRect(r).Contains(p))
                {
                    return true;
                }
            }
            return false;
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
                default:
                    return Cursors.Default;
            }
        }

    }
}