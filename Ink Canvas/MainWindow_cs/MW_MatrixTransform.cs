using Ink_Canvas.Helpers;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 根据设置获取矩阵变换中心点。
        /// </summary>
        /// <param name="gestureOperationCenterPoint">手势操作中心点。</param>
        /// <param name="fe">参考框架元素。</param>
        /// <returns>最终采用的变换中心点。</returns>
        private Point GetMatrixTransformCenterPoint(Point gestureOperationCenterPoint, FrameworkElement fe)
        {
            Point canvasCenterPoint = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            if (!isLoaded) return canvasCenterPoint;
            if (Settings.Gesture.MatrixTransformCenterPoint == MatrixTransformCenterPointOptions.CanvasCenterPoint)
            {
                return canvasCenterPoint;
            }
            else if (Settings.Gesture.MatrixTransformCenterPoint == MatrixTransformCenterPointOptions.GestureOperationCenterPoint)
            {
                return gestureOperationCenterPoint;
            }
            else if (Settings.Gesture.MatrixTransformCenterPoint == MatrixTransformCenterPointOptions.SelectedElementsCenterPoint)
            {
                return InkCanvasElementsHelper.GetAllElementsBoundsCenterPoint(inkCanvas);
            }
            return canvasCenterPoint;
        }
    }
}
