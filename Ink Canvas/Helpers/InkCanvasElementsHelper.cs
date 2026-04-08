using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// InkCanvas 元素操作辅助类：选中、克隆、添加与边界计算。
    /// </summary>
    public static class InkCanvasElementsHelper
    {
        /// <summary>
        /// 获取当前选区边界中心点。
        /// </summary>
        public static Point GetAllElementsBoundsCenterPoint(InkCanvas inkCanvas)
        {
            Rect bounds = inkCanvas.GetSelectionBounds();
            return new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        }

        /// <summary>
        /// 判断当前是否没有任何笔迹或元素被选中。
        /// </summary>
        public static bool IsNotCanvasElementSelected(InkCanvas inkCanvas)
        {
            return (inkCanvas.GetSelectedStrokes().Count == 0 && inkCanvas.GetSelectedElements().Count == 0);
        }

        /// <summary>
        /// 获取画布中的全部 UI 元素。
        /// </summary>
        public static List<UIElement> GetAllElements(InkCanvas inkCanvas)
        {
            List<UIElement> canvasElements = new List<UIElement>();
            foreach (UIElement element in inkCanvas.Children)
            {
                canvasElements.Add(element);
            }
            return canvasElements;
        }

        /// <summary>
        /// 获取当前已选中的 UI 元素集合。
        /// </summary>
        public static List<UIElement> GetSelectedElements(InkCanvas inkCanvas)
        {
            List<UIElement> selectedImages = new List<UIElement>();
            foreach (UIElement element in inkCanvas.GetSelectedElements())
            {
                selectedImages.Add(element);
            }
            return selectedImages;
        }

        /// <summary>
        /// 元素初始位置信息（用于历史记录）。
        /// </summary>
        public class ElementData
        {
            public double SetLeftData { get; set; }
            public double SetTopData { get; set; }
            public FrameworkElement FrameworkElement { get; set; }
        }

        /// <summary>
        /// 克隆当前选中元素并直接添加到画布，同时记录元素初始历史。
        /// </summary>
        public static List<UIElement> CloneSelectedElements(InkCanvas inkCanvas, ref Dictionary<string, object> ElementsInitialHistory)
        {
            List<UIElement> clonedElements = new List<UIElement>();
            int key = 0;
            foreach (UIElement element in inkCanvas.GetSelectedElements())
            {
                UIElement clonedElement = CloneUIElement(element);
                if (clonedElement != null)
                {
                    FrameworkElement frameworkElement = clonedElement as FrameworkElement;
                    string timestamp = "ele_" + DateTime.Now.ToString("ddHHmmssfff") + key.ToString();
                    frameworkElement.Name = timestamp;
                    ++key;
                    InkCanvas.SetLeft(frameworkElement, InkCanvas.GetLeft(element));
                    InkCanvas.SetTop(frameworkElement, InkCanvas.GetTop(element));
                    inkCanvas.Children.Add(frameworkElement);
                    clonedElements.Add(frameworkElement);
                    ElementsInitialHistory[frameworkElement.Name] = new ElementData
                    {
                        SetLeftData = InkCanvas.GetLeft(element),
                        SetTopData = InkCanvas.GetTop(element),
                        FrameworkElement = frameworkElement
                    };
                }
            }
            return clonedElements;
        }

        /// <summary>
        /// 克隆当前选中元素（不加入画布）。
        /// </summary>
        public static List<UIElement> GetSelectedElementsCloned(InkCanvas inkCanvas)
        {
            List<UIElement> clonedElements = new List<UIElement>();
            int key = 0;
            foreach (UIElement element in inkCanvas.GetSelectedElements())
            {
                UIElement clonedElement = CloneUIElement(element);
                if (clonedElement != null)
                {
                    FrameworkElement frameworkElement = clonedElement as FrameworkElement;
                    string timestamp = "ele_" + DateTime.Now.ToString("ddHHmmssfff") + key.ToString();
                    frameworkElement.Name = timestamp;
                    ++key;
                    InkCanvas.SetLeft(frameworkElement, InkCanvas.GetLeft(element));
                    InkCanvas.SetTop(frameworkElement, InkCanvas.GetTop(element));
                    clonedElements.Add(frameworkElement);
                }
            }
            return clonedElements;
        }

        /// <summary>
        /// 将元素批量加入画布，并写入 TimeMachine 插入历史。
        /// </summary>
        public static void AddElements(InkCanvas inkCanvas, List<UIElement> elements, TimeMachine timeMachine)
        {
            foreach (UIElement element in elements)
            {
                inkCanvas.Children.Add(element);
                timeMachine.CommitElementInsertHistory(element);
            }
        }

        /// <summary>
        /// 根据元素类型创建浅层克隆对象。
        /// </summary>
        private static UIElement CloneUIElement(UIElement element)
        {
            if (element == null) return null;

            if (element is Image originalImage)
            {
                return CloneImage(originalImage);
            }
            
            if (element is MediaElement originalMediaElement)
            {
                return CloneMediaElement(originalMediaElement);
            }

            if (element is FrameworkElement frameworkElement)
            {
                var clonedElement = (UIElement)Activator.CreateInstance(element.GetType());
                if (clonedElement is FrameworkElement clonedFrameworkElement)
                {
                    clonedFrameworkElement.Width = frameworkElement.Width;
                    clonedFrameworkElement.Height = frameworkElement.Height;
                    clonedFrameworkElement.Margin = frameworkElement.Margin;
                    clonedFrameworkElement.HorizontalAlignment = frameworkElement.HorizontalAlignment;
                    clonedFrameworkElement.VerticalAlignment = frameworkElement.VerticalAlignment;
                    clonedFrameworkElement.DataContext = frameworkElement.DataContext;
                }
                return clonedElement;
            }

            return null;
        }

        /// <summary>
        /// 克隆 Image 元素。
        /// </summary>
        private static Image CloneImage(Image originalImage)
        {
            Image clonedImage = new Image
            {
                Source = originalImage.Source,
                Width = originalImage.Width,
                Height = originalImage.Height,
                Stretch = originalImage.Stretch,
                Opacity = originalImage.Opacity,
                RenderTransform = originalImage.RenderTransform.Clone()
            };
            return clonedImage;
        }

        /// <summary>
        /// 克隆 MediaElement 元素，并保持基础媒体属性。
        /// </summary>
        private static MediaElement CloneMediaElement(MediaElement originalMediaElement)
        {
            MediaElement clonedMediaElement = new MediaElement
            {
                Source = originalMediaElement.Source,
                Width = originalMediaElement.Width,
                Height = originalMediaElement.Height,
                Stretch = originalMediaElement.Stretch,
                Opacity = originalMediaElement.Opacity,
                RenderTransform = originalMediaElement.RenderTransform.Clone(),
                LoadedBehavior = originalMediaElement.LoadedBehavior,
                UnloadedBehavior = originalMediaElement.UnloadedBehavior,
                Volume = originalMediaElement.Volume,
                Balance = originalMediaElement.Balance,
                IsMuted = originalMediaElement.IsMuted,
                ScrubbingEnabled = originalMediaElement.ScrubbingEnabled
            };
            clonedMediaElement.Loaded += async (sender, args) =>
            {
                clonedMediaElement.Play();
                await Task.Delay(100);
                clonedMediaElement.Pause();
            };
            return clonedMediaElement;
        }
    }
}
