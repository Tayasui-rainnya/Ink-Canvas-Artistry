using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Ink_Canvas_Magnifier
{
    public partial class MagnifierWindow : Window
    {
        private const double MinLensWidth = 240;
        private const double MaxLensWidth = 1100;

        private readonly DispatcherTimer _refreshTimer;

        private bool _isDragging;
        private Point _dragStartMousePosition;
        private Point _dragStartWindowPosition;

        public MagnifierWindow()
        {
            InitializeComponent();

            Left = (SystemParameters.WorkArea.Width - Width) / 2;
            Top = (SystemParameters.WorkArea.Height - Height) / 2;

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            Loaded += (_, __) =>
            {
                _refreshTimer.Start();
                RefreshMagnifiedImage();
            };

            Closed += (_, __) =>
            {
                _refreshTimer.Stop();
            };
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshMagnifiedImage();
        }

        private void RefreshMagnifiedImage()
        {
            double lensWidth = Math.Max(1, ActualWidth - 10);
            double lensHeight = Math.Max(1, ActualHeight - 54);
            double zoom = Math.Max(1.5, Math.Min(5.0, ZoomSlider.Value));

            double centerX = Left + ActualWidth / 2;
            double centerY = Top + lensHeight / 2;

            int captureWidth = Math.Max(1, (int)Math.Round(lensWidth / zoom));
            int captureHeight = Math.Max(1, (int)Math.Round(lensHeight / zoom));

            var virtualScreen = Forms.SystemInformation.VirtualScreen;

            int captureX = (int)Math.Round(centerX - captureWidth / 2.0);
            int captureY = (int)Math.Round(centerY - captureHeight / 2.0);

            if (captureX < virtualScreen.Left) captureX = virtualScreen.Left;
            if (captureY < virtualScreen.Top) captureY = virtualScreen.Top;
            if (captureX + captureWidth > virtualScreen.Right) captureX = virtualScreen.Right - captureWidth;
            if (captureY + captureHeight > virtualScreen.Bottom) captureY = virtualScreen.Bottom - captureHeight;

            if (captureX < virtualScreen.Left) captureX = virtualScreen.Left;
            if (captureY < virtualScreen.Top) captureY = virtualScreen.Top;

            using (var bitmap = new Bitmap(captureWidth, captureHeight, PixelFormat.Format32bppPArgb))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(captureX, captureY, 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);
                }

                IntPtr hBitmap = bitmap.GetHbitmap();
                try
                {
                    var source = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    MagnifiedImage.Source = source;
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ZoomTextBlock.Text = $"{ZoomSlider.Value:F1}x";
            RefreshMagnifiedImage();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value = Math.Max(ZoomSlider.Minimum, ZoomSlider.Value - 0.5);
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value = Math.Min(ZoomSlider.Maximum, ZoomSlider.Value + 0.5);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DragHandleArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStartMousePosition = PointToScreen(e.GetPosition(this));
            _dragStartWindowPosition = new Point(Left, Top);
            DragHandleArea.CaptureMouse();
        }

        private void DragHandleArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            Point currentMousePos = PointToScreen(e.GetPosition(this));
            Vector delta = currentMousePos - _dragStartMousePosition;

            Left = _dragStartWindowPosition.X + delta.X;
            Top = _dragStartWindowPosition.Y + delta.Y;
        }

        private void DragHandleArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            DragHandleArea.ReleaseMouseCapture();
        }

        private void LeftResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double delta = e.HorizontalChange;
            double newWidth = Math.Max(MinLensWidth, Math.Min(MaxLensWidth, Width - delta));

            double adjustedLeft = Left + (Width - newWidth);
            Width = newWidth;
            Left = adjustedLeft;
        }

        private void RightResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double delta = e.HorizontalChange;
            Width = Math.Max(MinLensWidth, Math.Min(MaxLensWidth, Width + delta));
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
