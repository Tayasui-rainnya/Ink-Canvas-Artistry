using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Ink_Canvas.Windows
{
    public partial class ScreenMagnifierWindow : Window
    {
        private readonly DispatcherTimer _refreshTimer;
        private System.Windows.Point _dragStartPoint;
        private bool _isDragging;
        private bool _isCaptureInProgress;
        private bool _excludeFromCaptureEnabled;

        public ScreenMagnifierWindow()
        {
            InitializeComponent();

            Left = SystemParameters.WorkArea.Left + 120;
            Top = SystemParameters.WorkArea.Top + 100;

            DragHandleBorder.MouseLeftButtonDown += DragHandleBorder_MouseLeftButtonDown;
            DragHandleBorder.MouseLeftButtonUp += DragHandleBorder_MouseLeftButtonUp;
            DragHandleBorder.MouseMove += DragHandleBorder_MouseMove;

            LeftResizeThumb.DragDelta += LeftResizeThumb_DragDelta;
            RightResizeThumb.DragDelta += RightResizeThumb_DragDelta;
            TopResizeThumb.DragDelta += TopResizeThumb_DragDelta;

            _refreshTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            SourceInitialized += (_, __) => TryEnableExcludeFromCapture();
            Loaded += (_, __) => _refreshTimer.Start();
            Closed += (_, __) => _refreshTimer.Stop();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            UpdateMagnifiedView();
        }

        private void UpdateMagnifiedView()
        {
            if (_isCaptureInProgress) return;

            int viewportWidth = Math.Max(1, (int)Math.Round(ActualWidth));
            int viewportHeight = Math.Max(1, (int)Math.Round(ActualHeight - 40));
            double zoom = ZoomSlider.Value;

            int captureWidth = Math.Max(1, (int)Math.Round(viewportWidth / zoom));
            int captureHeight = Math.Max(1, (int)Math.Round(viewportHeight / zoom));

            double centerX = Left + viewportWidth / 2.0;
            double centerY = Top + viewportHeight / 2.0;

            int sourceX = (int)Math.Round(centerX - captureWidth / 2.0);
            int sourceY = (int)Math.Round(centerY - captureHeight / 2.0);

            using (Bitmap source = _excludeFromCaptureEnabled
                ? CaptureScreen(sourceX, sourceY, captureWidth, captureHeight)
                : CaptureScreenWithoutSelf(sourceX, sourceY, captureWidth, captureHeight))
            using (Bitmap scaled = new Bitmap(source, viewportWidth, viewportHeight))
            {
                IntPtr hBitmap = scaled.GetHbitmap();
                try
                {
                    BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    bitmapSource.Freeze();
                    MagnifierImage.Source = bitmapSource;
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
        }

        private void TryEnableExcludeFromCapture()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero) return;
            _excludeFromCaptureEnabled = SetWindowDisplayAffinity(handle, WDA_EXCLUDEFROMCAPTURE);
        }

        private Bitmap CaptureScreenWithoutSelf(int sourceX, int sourceY, int width, int height)
        {
            _isCaptureInProgress = true;
            double originalOpacity = Opacity;
            try
            {
                Opacity = 0;
                Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));
                return CaptureScreen(sourceX, sourceY, width, height);
            }
            finally
            {
                Opacity = originalOpacity;
                _isCaptureInProgress = false;
            }
        }

        private static Bitmap CaptureScreen(int sourceX, int sourceY, int width, int height)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);

            using (Graphics destinationGraphics = Graphics.FromImage(bitmap))
            {
                IntPtr destinationHdc = destinationGraphics.GetHdc();
                IntPtr sourceHdc = GetDC(IntPtr.Zero);
                try
                {
                    BitBlt(destinationHdc, 0, 0, width, height, sourceHdc, sourceX, sourceY, SRCCOPY);
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, sourceHdc);
                    destinationGraphics.ReleaseHdc(destinationHdc);
                }
            }

            return bitmap;
        }

        private void DragHandleBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            DragHandleBorder.CaptureMouse();
        }

        private void DragHandleBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            System.Windows.Point screenPoint = PointToScreen(e.GetPosition(this));
            Left = screenPoint.X - _dragStartPoint.X;
            Top = screenPoint.Y - _dragStartPoint.Y;
        }

        private void DragHandleBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            DragHandleBorder.ReleaseMouseCapture();
        }

        private void LeftResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double delta = e.HorizontalChange;
            double newWidth = Math.Max(MinWidth, Width - delta);
            double widthDiff = Width - newWidth;
            Width = newWidth;
            Left += widthDiff;
        }

        private void RightResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Width = Math.Max(MinWidth, Width + e.HorizontalChange);
        }

        private void TopResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double delta = e.VerticalChange;
            double newHeight = Math.Max(MinHeight, Height - delta);
            double heightDiff = Height - newHeight;
            Height = newHeight;
            Top += heightDiff;
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ZoomText == null || MagnifierImage == null) return;
            ZoomText.Text = $"{ZoomSlider.Value:F1}x";
            UpdateMagnifiedView();
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

        private const int SRCCOPY = 0x00CC0020;

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BitBlt(
            IntPtr hdcDest,
            int nXDest,
            int nYDest,
            int nWidth,
            int nHeight,
            IntPtr hdcSrc,
            int nXSrc,
            int nYSrc,
            int dwRop);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
    }
}
