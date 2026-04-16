using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        private readonly DispatcherTimer magnifierRefreshTimer = new DispatcherTimer();
        private bool isMagnifierEnabled;
        private bool isMagnifierDragging;
        private Point magnifierDragStart;
        private double magnifierStartLeft;
        private double magnifierStartTop;

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private bool CanUseMagnifier()
        {
            return currentMode == 0 && StackPanelCanvasControls.Visibility != Visibility.Visible;
        }

        private void RefreshMagnifierButtonState()
        {
            if (BtnToolMagnifier == null) return;

            BtnToolMagnifier.Visibility = currentMode == 0 ? Visibility.Visible : Visibility.Collapsed;
            BtnToolMagnifier.IsEnabled = CanUseMagnifier();
            BtnToolMagnifier.Opacity = BtnToolMagnifier.IsEnabled ? 1 : 0.45;

            if (!BtnToolMagnifier.IsEnabled && isMagnifierEnabled)
            {
                DisableMagnifier();
            }
        }

        private void InitializeMagnifierIfNeeded()
        {
            if (magnifierRefreshTimer.Interval != TimeSpan.Zero) return;

            magnifierRefreshTimer.Interval = TimeSpan.FromMilliseconds(33);
            magnifierRefreshTimer.Tick += (_, __) => RefreshMagnifierFrame();

            if (double.IsNaN(Canvas.GetLeft(BorderMagnifierViewport)))
            {
                Canvas.SetLeft(BorderMagnifierViewport, 80);
                Canvas.SetTop(BorderMagnifierViewport, 80);
            }
        }

        private void BtnToolMagnifier_Click(object sender, RoutedEventArgs e)
        {
            InitializeMagnifierIfNeeded();

            if (!CanUseMagnifier())
            {
                DisableMagnifier();
                RefreshMagnifierButtonState();
                return;
            }

            if (isMagnifierEnabled) DisableMagnifier();
            else EnableMagnifier();
        }

        private void EnableMagnifier()
        {
            isMagnifierEnabled = true;
            CanvasMagnifierLayer.Visibility = Visibility.Visible;

            if (double.IsNaN(Canvas.GetLeft(BorderMagnifierViewport)))
            {
                Canvas.SetLeft(BorderMagnifierViewport, Math.Max(40, (ActualWidth - BorderMagnifierViewport.Width) / 2));
                Canvas.SetTop(BorderMagnifierViewport, Math.Max(40, (ActualHeight - BorderMagnifierViewport.Height) / 2));
            }

            magnifierRefreshTimer.Start();
            RefreshMagnifierScaleText();
            RefreshMagnifierFrame();
        }

        private void DisableMagnifier()
        {
            isMagnifierEnabled = false;
            magnifierRefreshTimer.Stop();
            CanvasMagnifierLayer.Visibility = Visibility.Collapsed;
            ImageMagnifierContent.Source = null;
        }

        private void RefreshMagnifierScaleText()
        {
            if (TextMagnifierScale != null)
            {
                TextMagnifierScale.Text = $"{SliderMagnifierScale.Value:F1}x";
            }
        }

        private void RefreshMagnifierFrame()
        {
            if (!isMagnifierEnabled || !IsVisible) return;

            double left = Canvas.GetLeft(BorderMagnifierViewport);
            double top = Canvas.GetTop(BorderMagnifierViewport);
            if (double.IsNaN(left) || double.IsNaN(top)) return;

            double viewportWidth = BorderMagnifierContent.ActualWidth;
            double viewportHeight = BorderMagnifierContent.ActualHeight;
            if (viewportWidth <= 0 || viewportHeight <= 0) return;

            double scale = SliderMagnifierScale.Value;
            double sourceWidth = viewportWidth / scale;
            double sourceHeight = viewportHeight / scale;

            Point center = new Point(left + viewportWidth / 2, top + viewportHeight / 2);
            Point sourceTopLeft = PointToScreen(new Point(center.X - sourceWidth / 2, center.Y - sourceHeight / 2));

            int captureX = (int)Math.Round(sourceTopLeft.X);
            int captureY = (int)Math.Round(sourceTopLeft.Y);
            int captureWidth = Math.Max(1, (int)Math.Round(sourceWidth));
            int captureHeight = Math.Max(1, (int)Math.Round(sourceHeight));

            CanvasMagnifierLayer.Visibility = Visibility.Hidden;
            try
            {
                using (var bitmap = new Bitmap(captureWidth, captureHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(captureX, captureY, 0, 0, new System.Drawing.Size(captureWidth, captureHeight), CopyPixelOperation.SourceCopy);
                    }

                    IntPtr hBitmap = bitmap.GetHbitmap();
                    try
                    {
                        var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        bitmapSource.Freeze();
                        ImageMagnifierContent.Source = bitmapSource;
                    }
                    finally
                    {
                        DeleteObject(hBitmap);
                    }
                }
            }
            catch
            {
                // 忽略抓屏异常，避免打断主界面操作。
            }
            finally
            {
                if (isMagnifierEnabled)
                {
                    CanvasMagnifierLayer.Visibility = Visibility.Visible;
                }
            }
        }

        private void BorderMagnifierHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isMagnifierEnabled) return;

            isMagnifierDragging = true;
            magnifierDragStart = e.GetPosition(this);
            magnifierStartLeft = Canvas.GetLeft(BorderMagnifierViewport);
            magnifierStartTop = Canvas.GetTop(BorderMagnifierViewport);
            BorderMagnifierHandle.CaptureMouse();
        }

        private void BorderMagnifierHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isMagnifierDragging) return;

            Point current = e.GetPosition(this);
            double targetLeft = magnifierStartLeft + current.X - magnifierDragStart.X;
            double targetTop = magnifierStartTop + current.Y - magnifierDragStart.Y;

            Canvas.SetLeft(BorderMagnifierViewport, Math.Max(0, Math.Min(ActualWidth - BorderMagnifierViewport.ActualWidth, targetLeft)));
            Canvas.SetTop(BorderMagnifierViewport, Math.Max(0, Math.Min(ActualHeight - BorderMagnifierViewport.ActualHeight, targetTop)));

            RefreshMagnifierFrame();
        }

        private void BorderMagnifierHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isMagnifierDragging = false;
            BorderMagnifierHandle.ReleaseMouseCapture();
        }

        private void SliderMagnifierScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RefreshMagnifierScaleText();
            if (isMagnifierEnabled) RefreshMagnifierFrame();
        }

        private void BtnMagnifierZoomOut_Click(object sender, RoutedEventArgs e)
        {
            SliderMagnifierScale.Value = Math.Max(SliderMagnifierScale.Minimum, SliderMagnifierScale.Value - 0.5);
        }

        private void BtnMagnifierZoomIn_Click(object sender, RoutedEventArgs e)
        {
            SliderMagnifierScale.Value = Math.Min(SliderMagnifierScale.Maximum, SliderMagnifierScale.Value + 0.5);
        }
    }
}
