using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        #region Image
        /// <summary>
        /// 插入图片元素。
        /// </summary>
        private async void BtnImageInsert_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg; *.jpeg; *.png; *.bmp)|*.jpg;*.jpeg;*.png;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                Image image = await CreateAndCompressImageAsync(filePath);

                if (image != null)
                {
                    string timestamp = "img_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
                    image.Name = timestamp;

                    CenterAndScaleElement(image);

                    InkCanvas.SetLeft(image, 0);
                    InkCanvas.SetTop(image, 0);
                    inkCanvas.Children.Add(image);

                    timeMachine.CommitElementInsertHistory(image);
                }
            }
        }

        /// <summary>
        /// 创建图片元素并按设置进行压缩与缓存复制。
        /// </summary>
        private async Task<Image> CreateAndCompressImageAsync(string filePath)
        {
            string savePath = Path.Combine(Settings.Automation.AutoSavedStrokesLocation, "File Dependency");
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            string fileExtension = Path.GetExtension(filePath);
            string timestamp = "img_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
            string newFilePath = Path.Combine(savePath, timestamp + fileExtension);

            await Task.Run(() => File.Copy(filePath, newFilePath, true));

            return await Dispatcher.InvokeAsync(() =>
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(newFilePath);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                int width = bitmapImage.PixelWidth;
                int height = bitmapImage.PixelHeight;

                Image image = new Image();
                if (isLoaded && Settings.Canvas.IsCompressPicturesUploaded && (width > 1920 || height > 1080))
                {
                    double scaleX = 1920.0 / width;
                    double scaleY = 1080.0 / height;
                    double scale = Math.Min(scaleX, scaleY);

                    TransformedBitmap transformedBitmap = new TransformedBitmap(bitmapImage, new ScaleTransform(scale, scale));

                    image.Source = transformedBitmap;
                    image.Width = transformedBitmap.PixelWidth;
                    image.Height = transformedBitmap.PixelHeight;
                }
                else
                {
                    image.Source = bitmapImage;
                    image.Width = width;
                    image.Height = height;
                }

                return image;
            });
        }


        /// <summary>
        /// 将传入位图插入白板层并记录历史。
        /// </summary>
        /// <param name="bitmap">要插入的截图位图，调用方保持其生命周期直到方法返回。</param>
        /// <returns>
        /// 当位图成功插入白板并提交历史时返回 <c>true</c>；
        /// 当位图为空或白板模式在重试后仍不可用时返回 <c>false</c>。
        /// </returns>
        private async Task<bool> AddBitmapToBoardAsync(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null) return false;

            // 若当前处于屏幕批注模式，先切换到黑板模式再插入图片
            if (!await EnsureBlackboardModeForBitmapInsertAsync())
            {
                ShowNotificationAsync("当前正处于模式切换中，请稍后重试");
                return false;
            }

            var image = new Image();
            image.Name = "img_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");

            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                image.Source = bitmapImage;
                image.Width = bitmapImage.PixelWidth;
                image.Height = bitmapImage.PixelHeight;
            }

            CenterAndScaleElement(image);
            InkCanvas.SetLeft(image, 0);
            InkCanvas.SetTop(image, 0);
            inkCanvas.Children.Add(image);
            timeMachine.CommitElementInsertHistory(image);
            return true;
        }

        /// <summary>
        /// 在截图插入前确保当前处于白板模式。
        /// </summary>
        /// <remarks>
        /// 该方法会在当前为屏幕批注模式时触发模式切换，并进行有限次延迟重试，
        /// 以覆盖 <c>ImageBlackboard_Click</c> 可能因切换防抖守卫被短暂拒绝的场景。
        /// </remarks>
        /// <returns>若最终检测到 <c>currentMode == 1</c> 则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
        private async Task<bool> EnsureBlackboardModeForBitmapInsertAsync()
        {
            if (currentMode == 1) return true;

            const int maxAttempts = 4;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                if (currentMode == 1) return true;
                ImageBlackboard_Click(null, null);
                await Task.Delay(80);
            }

            return currentMode == 1;
        }

        #endregion

        #region Media
        /// <summary>
        /// 插入媒体元素。
        /// </summary>
        private async void BtnMediaInsert_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Media files (*.mp4; *.avi; *.wmv)|*.mp4;*.avi;*.wmv";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                MediaElement mediaElement = await CreateMediaElementAsync(filePath);

                if (mediaElement != null)
                {
                    CenterAndScaleElement(mediaElement);

                    InkCanvas.SetLeft(mediaElement, 0);
                    InkCanvas.SetTop(mediaElement, 0);
                    inkCanvas.Children.Add(mediaElement);

                    mediaElement.LoadedBehavior = MediaState.Manual;
                    mediaElement.UnloadedBehavior = MediaState.Manual;
                    mediaElement.Loaded += async (_, args) =>
                    {
                        mediaElement.Play();
                        await Task.Delay(100);
                        mediaElement.Pause();
                    };

                    timeMachine.CommitElementInsertHistory(mediaElement);
                }
            }
        }

        /// <summary>
        /// 创建媒体元素并复制媒体文件到依赖目录。
        /// </summary>
        private async Task<MediaElement> CreateMediaElementAsync(string filePath)
        {
            string savePath = Path.Combine(Settings.Automation.AutoSavedStrokesLocation, "File Dependency");
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            return await Dispatcher.InvokeAsync(() =>
            {
                MediaElement mediaElement = new MediaElement();
                mediaElement.Source = new Uri(filePath);
                string timestamp = "media_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
                mediaElement.Name = timestamp;
                mediaElement.LoadedBehavior = MediaState.Manual;
                mediaElement.UnloadedBehavior = MediaState.Manual;

                mediaElement.Width = 256;
                mediaElement.Height = 256;

                string fileExtension = Path.GetExtension(filePath);
                string newFilePath = Path.Combine(savePath, mediaElement.Name + fileExtension);

                File.Copy(filePath, newFilePath, true);

                mediaElement.Source = new Uri(newFilePath);

                return mediaElement;
            });
        }
        #endregion

        /// <summary>
        /// 将元素缩放并居中到画布可视区域。
        /// </summary>
        private void CenterAndScaleElement(FrameworkElement element)
        {
            double maxWidth = SystemParameters.PrimaryScreenWidth / 2;
            double maxHeight = SystemParameters.PrimaryScreenHeight / 2;

            double scaleX = maxWidth / element.Width;
            double scaleY = maxHeight / element.Height;
            double scale = Math.Min(scaleX, scaleY);

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(scale, scale));

            double canvasWidth = inkCanvas.ActualWidth;
            double canvasHeight = inkCanvas.ActualHeight;
            double centerX = (canvasWidth - element.Width * scale) / 2;
            double centerY = (canvasHeight - element.Height * scale) / 2;

            transformGroup.Children.Add(new TranslateTransform(centerX, centerY));

            element.RenderTransform = transformGroup;
        }
    }
}
