﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Tesseract;

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
        private int sizeNodeRect = 12;
        private const int rotationHandleSize = 15;
        private Bitmap mBmp = null;
        public PosSizableRect nodeSelected = PosSizableRect.None;
        public float rotationAngle = 0;

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
            None
        };

        public UserRect(Rectangle r)
        {
            rectangle = r;
            mIsClick = false;
        }
        public void Draw(Graphics g)
        {
            Pen pen = new Pen(Color.Cyan, 2f);
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

            Matrix transform = g.Transform;

            g.TranslateTransform(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);

            g.RotateTransform(rotationAngle);

            g.TranslateTransform(-(rectangle.X + rectangle.Width / 2), -(rectangle.Y + rectangle.Height / 2));

            g.DrawRectangle(pen, rectangle);

            g.Transform = transform;

            // Draw the handles
            foreach (PosSizableRect pos in Enum.GetValues(typeof(PosSizableRect)))
            {
                using Brush brush = new SolidBrush(Color.Gray);
                if (pos == PosSizableRect.RotationHandle)
                {
                    g.FillEllipse(brush, GetRect(pos));
                }
                else
                {
                    g.FillRectangle(brush, GetRect(pos));
                }
            }
        }

        public Bitmap RotateImage(Bitmap image, float angle)
        {
            Bitmap rotatedImage = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                g.TranslateTransform(image.Width / 2, image.Height / 2);
                g.RotateTransform(angle);
                g.TranslateTransform(-image.Width / 2, -image.Height / 2);
                g.DrawImage(image, new Point(0, 0));
            }
            return rotatedImage;
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
            SetBitmap(mBmp);
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

        // Update the GetRect method to include the position of the rotation handle
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

        public void Rotate(int mouseX, int mouseY)
        {
            int centerX = rectangle.X + rectangle.Width / 2;
            int centerY = rectangle.Y + rectangle.Height / 2;

            // Calculate the angle between the center of the rectangle and the mouse position
            double dx = mouseX - centerX;
            double dy = mouseY - centerY;
            double angle = Math.Atan2(dy, dx) * 180 / Math.PI;

            rotationAngle = (int)angle;
            mPictureBox.Invalidate();
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
                default:
                    return Cursors.Default;
            }
        }
    }
}