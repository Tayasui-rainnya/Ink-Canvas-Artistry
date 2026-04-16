using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace Ink_Canvas
{
    /// <summary>
    /// 放大镜窗口 - 提供屏幕局部放大功能
    /// </summary>
    public partial class MagnifierWindow : Window
    {
        // 屏幕捕获相关 API
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);
        
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);
        
        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);
        
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        
        private const int SRCCOPY = 0x00CC0020;

        private double _currentZoomRatio = 2.5;
        private double _currentWidth = 320;
        private double _currentHeight = 320;
        
        private bool _isDragging = false;
        private Point _dragStartPoint;
        
        private bool _isResizingLeft = false;
        private bool _isResizingRight = false;
        private double _resizeStartWidth;
        private Point _resizeStartPoint;

        private System.Timers.Timer _captureTimer;

        public MagnifierWindow()
        {
            InitializeComponent();
            
            // 初始化定时器用于定时捕获屏幕
            _captureTimer = new System.Timers.Timer(50); // 20 FPS
            _captureTimer.Elapsed += CaptureTimer_Elapsed;
            _captureTimer.Start();
            
            // 初始设置
            UpdateZoomRatio(_currentZoomRatio);
            SetMagnifierSize(_currentWidth, _currentHeight);
            
            // 居中显示
            CenterOnScreen();
        }

        /// <summary>
        /// 定时器事件 - 定时捕获并更新屏幕内容
        /// </summary>
        private void CaptureTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CaptureScreen();
            });
        }

        /// <summary>
        /// 捕获屏幕区域
        /// </summary>
        private void CaptureScreen()
        {
            try
            {
                // 获取当前放大镜在屏幕上的位置（相对于主窗口）
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null) return;

                // 计算捕获区域（相对于屏幕）
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;
                
                // 获取放大镜相对于屏幕的位置
                Point screenPos = this.PointToScreen(new Point(0, 0));
                
                // 计算要捕获的源区域（根据缩放比例反向计算）
                double sourceWidth = _currentWidth / _currentZoomRatio;
                double sourceHeight = _currentHeight / _currentZoomRatio;
                
                // 确保不超出屏幕范围
                double captureX = Math.Max(0, Math.Min(screenPos.X, screenWidth - sourceWidth));
                double captureY = Math.Max(0, Math.Min(screenPos.Y, screenHeight - sourceHeight));
                
                int width = (int)_currentWidth;
                int height = (int)_currentHeight;
                
                if (width <= 0 || height <= 0) return;

                // 创建位图
                BitmapSource bitmap = CaptureRegion(
                    (int)captureX, 
                    (int)captureY, 
                    (int)sourceWidth, 
                    (int)sourceHeight);
                
                if (bitmap != null)
                {
                    CapturedImage.Source = bitmap;
                    
                    // 更新 Viewbox 的缩放
                    MagnifierViewbox.StretchDirection = StretchDirection.Both;
                }
            }
            catch (Exception ex)
            {
                // 静默失败，避免频繁弹出错误
                System.Diagnostics.Debug.WriteLine($"Capture error: {ex.Message}");
            }
        }

        /// <summary>
        /// 捕获指定屏幕区域
        /// </summary>
        private BitmapSource CaptureRegion(int x, int y, int width, int height)
        {
            try
            {
                IntPtr hdcScreen = GetWindowDC(GetDesktopWindow());
                IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
                IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
                IntPtr hOld = SelectObject(hdcMem, hBitmap);

                BitBlt(hdcMem, 0, 0, width, height, hdcScreen, x, y, SRCCOPY);

                SelectObject(hdcMem, hOld);
                DeleteDC(hdcMem);
                DeleteDC(hdcScreen);

                BitmapSource bitmap = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(hBitmap);

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 更新缩放比例
        /// </summary>
        private void UpdateZoomRatio(double ratio)
        {
            _currentZoomRatio = ratio;
            ZoomSlider.Value = ratio;
            ZoomRatioText.Text = $"{ratio:F1}x";
            
            // 更新 Viewbox 的缩放变换
            MagnifierViewbox.LayoutTransform = new ScaleTransform(ratio, ratio);
            
            // 重新捕获屏幕
            CaptureScreen();
        }

        /// <summary>
        /// 设置放大镜尺寸
        /// </summary>
        private void SetMagnifierSize(double width, double height)
        {
            _currentWidth = Math.Max(100, Math.Min(width, 800));
            _currentHeight = Math.Max(100, Math.Min(height, 600));
            
            this.Width = _currentWidth;
            this.Height = _currentHeight + ControlBar.ActualHeight;
            
            CapturedContentGrid.Width = _currentWidth;
            CapturedContentGrid.Height = _currentHeight;
            
            CaptureScreen();
        }

        /// <summary>
        /// 居中显示
        /// </summary>
        private void CenterOnScreen()
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            
            this.Left = (screenWidth - this.Width) / 2;
            this.Top = (screenHeight - this.Height) / 2;
        }

        #region 拖拽控制栏
        
        private void DragHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this);
                DragHandle.CaptureMouse();
            }
        }

        private void DragHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition(this);
                double deltaX = currentPos.X - _dragStartPoint.X;
                double deltaY = currentPos.Y - _dragStartPoint.Y;
                
                this.Left += deltaX;
                this.Top += deltaY;
                
                _dragStartPoint = currentPos;
            }
        }

        private void DragHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            DragHandle.ReleaseMouseCapture();
        }

        #endregion

        #region 缩放控制
        
        private void BtnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            double newRatio = Math.Max(1.5, _currentZoomRatio - 0.5);
            UpdateZoomRatio(newRatio);
        }

        private void BtnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            double newRatio = Math.Min(5.0, _currentZoomRatio + 0.5);
            UpdateZoomRatio(newRatio);
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                UpdateZoomRatio(e.NewValue);
            }
        }

        #endregion

        #region 大小调节 Handle
        
        private void LeftResizeHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isResizingLeft = true;
                _resizeStartWidth = this.Width;
                _resizeStartPoint = e.GetPosition(this);
                LeftResizeHandle.CaptureMouse();
            }
        }

        private void LeftResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizingLeft && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition(this);
                double deltaX = _resizeStartPoint.X - currentPos.X;
                
                double newWidth = _resizeStartWidth + deltaX;
                if (newWidth >= 100 && newWidth <= 800)
                {
                    this.Left += deltaX;
                    SetMagnifierSize(newWidth, _currentHeight);
                }
            }
        }

        private void LeftResizeHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isResizingLeft = false;
            LeftResizeHandle.ReleaseMouseCapture();
        }

        private void RightResizeHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isResizingRight = true;
                _resizeStartWidth = this.Width;
                _resizeStartPoint = e.GetPosition(this);
                RightResizeHandle.CaptureMouse();
            }
        }

        private void RightResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizingRight && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPos = e.GetPosition(this);
                double deltaX = currentPos.X - _resizeStartPoint.X;
                
                double newWidth = _resizeStartWidth + deltaX;
                if (newWidth >= 100 && newWidth <= 800)
                {
                    SetMagnifierSize(newWidth, _currentHeight);
                }
            }
        }

        private void RightResizeHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isResizingRight = false;
            RightResizeHandle.ReleaseMouseCapture();
        }

        #endregion

        #region 关闭
        
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            _captureTimer?.Stop();
            _captureTimer?.Dispose();
            this.Close();
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _captureTimer?.Stop();
            _captureTimer?.Dispose();
        }
    }
}
