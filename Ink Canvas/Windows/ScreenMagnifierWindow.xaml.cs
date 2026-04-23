using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Ink_Canvas.Windows
{
    public partial class ScreenMagnifierWindow : Window
    {
        private const uint WdaNone = 0x00000000;
        private const uint WdaExcludeFromCapture = 0x00000011;

        private readonly IntPtr _mainWindowHandle;
        private readonly DispatcherTimer _captureTimer;

        private bool _isResizing;
        private bool _isLeftHandle;
        private System.Windows.Point _resizeStartPoint;
        private double _resizeStartWidth;

        public event EventHandler RequestClose;

        private static bool IsExcludeFromCaptureSupported()
        {
            Version osVersion = Environment.OSVersion.Version;
            return osVersion.Major >= 10 && (osVersion.Build >= 19041 || osVersion.Major > 10);
        }

        private void ApplyCaptureExclusion(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero || !IsExcludeFromCaptureSupported()) return;

            try
            {
                SetWindowDisplayAffinity(windowHandle, WdaExcludeFromCapture);
            }
            catch
            {
                // 低版本系统或驱动环境下可能不支持，忽略并降级为普通采集。
            }
        }

        private void ClearCaptureExclusion(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero || !IsExcludeFromCaptureSupported()) return;

            try
            {
                SetWindowDisplayAffinity(windowHandle, WdaNone);
            }
            catch
            {
                // 忽略恢复失败，避免关闭流程抛异常。
            }
        }

        public ScreenMagnifierWindow(IntPtr mainWindowHandle)
        {
            _mainWindowHandle = mainWindowHandle;
            InitializeComponent();

            _captureTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _captureTimer.Tick += (_, __) => RenderMagnifiedFrame();

            Loaded += ScreenMagnifierWindow_Loaded;
            Closed += ScreenMagnifierWindow_Closed;
        }

        private void RefreshZoomLabel()
        {
            if (TxtZoom == null || ZoomSlider == null) return;
            TxtZoom.Text = $"{ZoomSlider.Value:F1}x";
        }

        private void ScreenMagnifierWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Left = (SystemParameters.WorkArea.Width - Width) / 2;
            Top = (SystemParameters.WorkArea.Height - Height) / 2;

            IntPtr magnifierHandle = new WindowInteropHelper(this).Handle;
            ApplyCaptureExclusion(magnifierHandle);
            ApplyCaptureExclusion(_mainWindowHandle);

            RefreshZoomLabel();
            _captureTimer.Start();
        }

        private void ScreenMagnifierWindow_Closed(object sender, EventArgs e)
        {
            _captureTimer.Stop();

            IntPtr magnifierHandle = new WindowInteropHelper(this).Handle;
            ClearCaptureExclusion(magnifierHandle);
            ClearCaptureExclusion(_mainWindowHandle);

            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void RenderMagnifiedFrame()
        {
            if (!IsLoaded || ImageViewport == null || ZoomSlider == null || ActualWidth < 40 || ActualHeight < 60) return;

            double zoom = ZoomSlider.Value;
            double captureWidth = ActualWidth / zoom;
            double captureHeight = (ActualHeight - 42) / zoom;

            if (captureWidth < 1 || captureHeight < 1) return;

            double centerX = Left + ActualWidth / 2;
            double centerY = Top + (ActualHeight - 42) / 2;

            int srcX = (int)Math.Round(centerX - captureWidth / 2);
            int srcY = (int)Math.Round(centerY - captureHeight / 2);
            int srcW = Math.Max(1, (int)Math.Round(captureWidth));
            int srcH = Math.Max(1, (int)Math.Round(captureHeight));

            using (var bitmap = new Bitmap(srcW, srcH, PixelFormat.Format32bppPArgb))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(srcX, srcY, 0, 0, new System.Drawing.Size(srcW, srcH), CopyPixelOperation.SourceCopy);
                }

                IntPtr hBitmap = bitmap.GetHbitmap();
                try
                {
                    BitmapSource source = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    source.Freeze();
                    ImageViewport.Source = source;
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
        }

        private void DragHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value = Math.Max(ZoomSlider.Minimum, Math.Round((ZoomSlider.Value - 0.5) * 10) / 10);
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value = Math.Min(ZoomSlider.Maximum, Math.Round((ZoomSlider.Value + 0.5) * 10) / 10);
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtZoom == null || ZoomSlider == null) return;

            RefreshZoomLabel();
            RenderMagnifiedFrame();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                _isResizing = true;
                _isLeftHandle = string.Equals(element.Tag as string, "Left", StringComparison.OrdinalIgnoreCase);
                _resizeStartPoint = PointToScreen(e.GetPosition(this));
                _resizeStartWidth = Width;
                element.CaptureMouse();
                e.Handled = true;
            }
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizing) return;

            System.Windows.Point nowPoint = PointToScreen(e.GetPosition(this));
            double delta = nowPoint.X - _resizeStartPoint.X;
            if (_isLeftHandle)
            {
                delta = -delta;
            }

            double newWidth = Math.Max(260, Math.Min(SystemParameters.WorkArea.Width * 0.95, _resizeStartWidth + delta));
            Width = newWidth;
            Height = Math.Max(180, Math.Min(SystemParameters.WorkArea.Height * 0.8, newWidth * 0.66));

            if (_isLeftHandle)
            {
                Left = Left + (_resizeStartWidth - newWidth);
            }
        }

        private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                _isResizing = false;
                element.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}
