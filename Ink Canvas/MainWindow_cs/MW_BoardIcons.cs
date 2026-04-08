using Ink_Canvas.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using Application = System.Windows.Application;
using System.Diagnostics;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 切换白板背景（黑板/白板）并同步主题与工具栏配色。
        /// </summary>
        private void BoardChangeBackgroundColorBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;
            Settings.Canvas.UsingWhiteboard = !Settings.Canvas.UsingWhiteboard;
            SaveSettingsToFile();
            if (Settings.Canvas.UsingWhiteboard)
            {
                if (inkColor == 5) lastBoardInkColor = 0;
            }
            else
            {
                if (inkColor == 0) lastBoardInkColor = 5;
            }
            ComboBoxTheme_SelectionChanged(null, null);
            CheckColorTheme(true);
            if (BoardPen.Opacity == 1)
            {
                BoardPen.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
            }
            if (BoardEraser.Opacity == 1)
            {
                BoardEraser.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
            }
            if (BoardSelect.Opacity == 1)
            {
                BoardSelect.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
            }
            if (BoardEraserByStrokes.Opacity == 1)
            {
                BoardEraserByStrokes.Background = (Brush)Application.Current.FindResource("BoardBarBackground");
            }
        }

        /// <summary>
        /// 板擦按钮行为：未激活时展开面板，激活时切换为点擦模式。
        /// </summary>
        private void BoardEraserIcon_Click(object sender, RoutedEventArgs e)
        {
            if (BoardEraser.Opacity != 1)
            {
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardDeleteIcon);
            }
            else
            {
                forceEraser = true;
                forcePointEraser = true;
                double k = 1;
                switch (Settings.Canvas.EraserSize)
                {
                    case 0:
                        k = 0.5;
                        break;
                    case 1:
                        k = 0.8;
                        break;
                    case 3:
                        k = 1.25;
                        break;
                    case 4:
                        k = 1.8;
                        break;
                }
                inkCanvas.EraserShape = new EllipseStylusShape(k * 90, k * 90);
                inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                drawingShapeMode = 0;

                inkCanvas_EditingModeChanged(inkCanvas, null);
                CancelSingleFingerDragMode();

                HideSubPanels("eraser");
            }
        }

        /// <summary>
        /// 笔划擦除按钮行为：未激活时展开面板，激活时切换为笔划擦模式。
        /// </summary>
        private void BoardEraserIconByStrokes_Click(object sender, RoutedEventArgs e)
        {
            if (BoardEraserByStrokes.Opacity != 1)
            {
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardDeleteIcon);
            }
            else
            {
                forceEraser = true;
                forcePointEraser = false;

                inkCanvas.EraserShape = new EllipseStylusShape(5, 5);
                inkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                drawingShapeMode = 0;

                inkCanvas_EditingModeChanged(inkCanvas, null);
                CancelSingleFingerDragMode();

                HideSubPanels("eraserByStrokes");
            }
        }

        /// <summary>
        /// 删除图标入口（先切换到画笔工具后执行删除逻辑）。
        /// </summary>
        private void BoardSymbolIconDelete_Click(object sender, RoutedEventArgs e)
        {
            PenIcon_Click(null, null);
            SymbolIconDelete_MouseUp(sender, e);
        }

        /// <summary>
        /// 启动希沃视频展台。
        /// </summary>
        private void BoardLaunchEasiCamera_Click(object sender, RoutedEventArgs e)
        {
            ImageBlackboard_Click(null, null);
            SoftwareLauncher.LaunchEasiCamera("希沃视频展台");
        }

        /// <summary>
        /// 打开 Desmos 在线计算器。
        /// </summary>
        private void BoardLaunchDesmos_Click(object sender, RoutedEventArgs e)
        {
            HideSubPanelsImmediately();
            ImageBlackboard_Click(null, null);
            Process.Start("https://www.desmos.com/calculator?lang=zh-CN");
        }

    }
}
