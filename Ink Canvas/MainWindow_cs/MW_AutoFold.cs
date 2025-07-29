using Ink_Canvas.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        // 浮动栏是否已折叠、是否正在切换隐藏模式
        bool isFloatingBarFolded = false, isFloatingBarChangingHideMode = false;

        /// <summary>
        /// 折叠浮动栏按钮点击事件
        /// </summary>
        private async void FoldFloatingBar_Click(object sender, RoutedEventArgs e)
        {
            // 判断是否为用户操作
            if (sender == null)
            {
                foldFloatingBarByUser = false;
            }
            else
            {
                foldFloatingBarByUser = true;
            }
            unfoldFloatingBarByUser = false;

            // 若正在切换隐藏模式则直接返回
            if (isFloatingBarChangingHideMode) return;

            // UI线程：隐藏子面板，设置折叠状态，动画收起侧边栏
            await Dispatcher.InvokeAsync(() =>
            {
                HideSubPanelsImmediately();
                isFloatingBarChangingHideMode = true;
                isFloatingBarFolded = true;
                if (currentMode != 0) ImageBlackboard_Click(null, null);
                if (StackPanelCanvasControls.Visibility == Visibility.Visible)
                {
                    // 若用户主动折叠且墨迹数量大于2，提示清空墨迹
                    if (foldFloatingBarByUser && inkCanvas.Strokes.Count > 2)
                    {
                        ShowNotificationAsync("正在清空墨迹并收纳至侧边栏，可进入批注模式后通过【撤销】功能来恢复原先墨迹。");
                    }
                }
                CursorWithDelIcon_Click(null, null);
                SidePannelMarginAnimation(-16);
            });

            // 等待动画完成
            await Task.Delay(500);

            // UI线程：隐藏PPT导航，调整浮动栏动画
            await Dispatcher.InvokeAsync(() =>
            {
                PPTNavigationBottomLeft.Visibility = Visibility.Collapsed;
                PPTNavigationBottomRight.Visibility = Visibility.Collapsed;
                PPTNavigationSidesLeft.Visibility = Visibility.Collapsed;
                PPTNavigationSidesRight.Visibility = Visibility.Collapsed;
                ViewboxFloatingBarMarginAnimation();
                HideSubPanels("cursor");
                SidePannelMarginAnimation(-16);
            });
            isFloatingBarChangingHideMode = false;
        }

        /// <summary>
        /// 展开浮动栏鼠标弹起事件
        /// </summary>
        private async void UnFoldFloatingBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // 判断是否为用户操作
            if (sender == null || BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
            {
                unfoldFloatingBarByUser = false;
            }
            else
            {
                unfoldFloatingBarByUser = true;
            }
            foldFloatingBarByUser = false;

            // 若正在切换隐藏模式则直接返回
            if (isFloatingBarChangingHideMode) return;

            // UI线程：设置展开状态
            await Dispatcher.InvokeAsync(() =>
            {
                isFloatingBarChangingHideMode = true;
                isFloatingBarFolded = false;
            });

            // 等待动画完成
            await Task.Delay(500);

            // UI线程：显示PPT导航，调整浮动栏动画
            await Dispatcher.InvokeAsync(() =>
            {
                if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                {
                    if (Settings.PowerPointSettings.IsShowBottomPPTNavigationPanel)
                    {
                        AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomLeft);
                        AnimationsHelper.ShowWithScaleFromBottom(PPTNavigationBottomRight);
                    }
                    if (Settings.PowerPointSettings.IsShowSidePPTNavigationPanel)
                    {
                        AnimationsHelper.ShowWithScaleFromLeft(PPTNavigationSidesLeft);
                        AnimationsHelper.ShowWithScaleFromRight(PPTNavigationSidesRight);
                    }
                }
                ViewboxFloatingBarMarginAnimation();
                SidePannelMarginAnimation(-40);
            });

            isFloatingBarChangingHideMode = false;
        }

        /// <summary>
        /// 侧边栏边距动画
        /// </summary>
        /// <param name="MarginFromEdge">边距值，-40为收起，-16为展开</param>
        private async void SidePannelMarginAnimation(int MarginFromEdge) // Possible value: -40, -16
        {
            await Dispatcher.InvokeAsync(() =>
            {
                // 展开时显示左侧面板
                if (MarginFromEdge == -16) LeftSidePanel.Visibility = Visibility.Visible;

                // 左侧面板动画
                ThicknessAnimation LeftSidePanelmarginAnimation = new ThicknessAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.3),
                    From = LeftSidePanel.Margin,
                    To = new Thickness(MarginFromEdge, 0, 0, -150)
                };
                // 右侧面板动画
                ThicknessAnimation RightSidePanelmarginAnimation = new ThicknessAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.3),
                    From = RightSidePanel.Margin,
                    To = new Thickness(0, 0, MarginFromEdge, -150)
                };

                LeftSidePanel.BeginAnimation(FrameworkElement.MarginProperty, LeftSidePanelmarginAnimation);
                RightSidePanel.BeginAnimation(FrameworkElement.MarginProperty, RightSidePanelmarginAnimation);
            });

            // 等待动画完成
            await Task.Delay(600);

            await Dispatcher.InvokeAsync(() =>
            {
                // 动画结束后设置最终边距
                LeftSidePanel.Margin = new Thickness(MarginFromEdge, 0, 0, -150);
                RightSidePanel.Margin = new Thickness(0, 0, MarginFromEdge, -150);

                // 收起时隐藏左侧面板
                if (MarginFromEdge == -40) LeftSidePanel.Visibility = Visibility.Collapsed;
            });
            isFloatingBarChangingHideMode = false;
        }
    }
}