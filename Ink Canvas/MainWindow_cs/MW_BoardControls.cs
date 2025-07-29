using Ink_Canvas.Helpers;
using System;
using System.Windows;
using System.Windows.Ink;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        // 用于存储每页白板的墨迹集合
        StrokeCollection[] strokeCollections = new StrokeCollection[101];
        // 用于记录最后一次触控按下时的墨迹集合
        StrokeCollection lastTouchDownStrokeCollection = new StrokeCollection();

        // 当前白板页码和总页数
        int CurrentWhiteboardIndex = 1, WhiteboardTotalCount = 1;
        // 用于存储每页的历史记录，最多99页，0号用于非白板时的墨迹
        TimeMachineHistory[][] TimeMachineHistories = new TimeMachineHistory[101][];

        /// <summary>
        /// 保存当前页的墨迹历史
        /// </summary>
        /// <param name="isBackupMain">是否备份主页面</param>
        private void SaveStrokes(bool isBackupMain = false)
        {
            if (isBackupMain)
            {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[0] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();

            }
            else
            {
                var timeMachineHistory = timeMachine.ExportTimeMachineHistory();
                TimeMachineHistories[CurrentWhiteboardIndex] = timeMachineHistory;
                timeMachine.ClearStrokeHistory();
            }
        }

        /// <summary>
        /// 清空当前白板的墨迹
        /// </summary>
        /// <param name="isErasedByCode">是否由代码触发清空</param>
        private void ClearStrokes(bool isErasedByCode)
        {
            _currentCommitType = CommitReason.ClearingCanvas;
            if (isErasedByCode) _currentCommitType = CommitReason.CodeInput;
            inkCanvas.Strokes.Clear();
            inkCanvas.Children.Clear();
            _currentCommitType = CommitReason.UserInput;
        }

        /// <summary>
        /// 恢复当前页的墨迹历史
        /// </summary>
        /// <param name="isBackupMain">是否恢复主页面</param>
        private void RestoreStrokes(bool isBackupMain = false)
        {
            try
            {
                if (TimeMachineHistories[CurrentWhiteboardIndex] == null) return; //防止白板打开后不居中
                if (isBackupMain)
                {
                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[0]);
                    foreach (var item in TimeMachineHistories[0])
                    {
                        ApplyHistoryToCanvas(item);
                    }
                }
                else
                {
                    timeMachine.ImportTimeMachineHistory(TimeMachineHistories[CurrentWhiteboardIndex]);
                    foreach (var item in TimeMachineHistories[CurrentWhiteboardIndex])
                    {
                        ApplyHistoryToCanvas(item);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 切换到上一页白板
        /// </summary>
        private void BtnWhiteBoardSwitchPrevious_Click(object sender, EventArgs e)
        {
            if (CurrentWhiteboardIndex <= 1) return;
            SaveStrokes();
            ClearStrokes(true);
            CurrentWhiteboardIndex--;
            RestoreStrokes();
            UpdateIndexInfoDisplay();
        }

        /// <summary>
        /// 切换到下一页白板，必要时自动新增白板页
        /// </summary>
        private void BtnWhiteBoardSwitchNext_Click(object sender, EventArgs e)
        {
            // 自动保存墨迹截图
            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
            {
                SaveScreenshot(true);
            }
            if (CurrentWhiteboardIndex >= WhiteboardTotalCount)
            {
                BtnWhiteBoardAdd_Click(sender, e);
                return;
            }
            SaveStrokes();
            ClearStrokes(true);
            CurrentWhiteboardIndex++;
            RestoreStrokes();
            UpdateIndexInfoDisplay();
        }

        /// <summary>
        /// 新增白板页
        /// </summary>
        private void BtnWhiteBoardAdd_Click(object sender, EventArgs e)
        {
            if (WhiteboardTotalCount >= 99) return;
            // 自动保存墨迹截图
            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
            {
                SaveScreenshot(true);
            }
            SaveStrokes();
            ClearStrokes(true);
            WhiteboardTotalCount++;
            CurrentWhiteboardIndex++;
            // 插入新页时，后面的历史向后移动
            if (CurrentWhiteboardIndex != WhiteboardTotalCount)
            {
                for (int i = WhiteboardTotalCount; i > CurrentWhiteboardIndex; i--)
                {
                    TimeMachineHistories[i] = TimeMachineHistories[i - 1];
                }
            }
            UpdateIndexInfoDisplay();
        }

        /// <summary>
        /// 删除当前白板页
        /// </summary>
        private void BtnWhiteBoardDelete_Click(object sender, RoutedEventArgs e)
        {
            ClearStrokes(true);
            // 删除当前页后，后面的历史向前移动
            if (CurrentWhiteboardIndex != WhiteboardTotalCount)
            {
                for (int i = CurrentWhiteboardIndex; i <= WhiteboardTotalCount; i++)
                {
                    TimeMachineHistories[i] = TimeMachineHistories[i + 1];
                }
            }
            else
            {
                CurrentWhiteboardIndex--;
            }
            WhiteboardTotalCount--;

            // 如果删除后没有白板页，则自动新建一页
            if (WhiteboardTotalCount == 0)
            {
                WhiteboardTotalCount = 1;
                CurrentWhiteboardIndex = 1;
                TimeMachineHistories[1] = null;
                strokeCollections[1] = new StrokeCollection();
                RestoreStrokes();
            }
            else
            {
                RestoreStrokes();
            }
            UpdateIndexInfoDisplay();
        }

        /// <summary>
        /// 更新白板页码及相关控件显示
        /// </summary>
        private void UpdateIndexInfoDisplay()
        {
            TextBlockWhiteBoardIndexInfo.Text = string.Format("{0} / {1}", CurrentWhiteboardIndex, WhiteboardTotalCount);

            // 控制“下一页/加页”按钮显示
            if (CurrentWhiteboardIndex == WhiteboardTotalCount)
            {
                BoardLeftPannelNextPage1.Width = 26;
                BoardLeftPannelNextPage2.Width = 0;
                BoardLeftPannelNextPageTextBlock.Text = "加页";
            }
            else
            {
                BoardLeftPannelNextPage1.Width = 0;
                BoardLeftPannelNextPage2.Width = 26;
                BoardLeftPannelNextPageTextBlock.Text = "下一页";
            }

            // 控制“上一页”按钮使能
            if (CurrentWhiteboardIndex == 1)
            {
                BtnWhiteBoardSwitchPrevious.IsEnabled = false;
            }
            else
            {
                BtnWhiteBoardSwitchPrevious.IsEnabled = true;
            }

            // 控制“下一页/加页”按钮使能
            if (CurrentWhiteboardIndex == 99)
            {
                BoardLeftPannelNextPage1.IsEnabled = false;
            }
            else
            {
                BoardLeftPannelNextPage1.IsEnabled = true;
            }

            // 控制“加页”按钮使能
            if (WhiteboardTotalCount == 99)
            {
                BtnBoardAddPage.IsEnabled = false;
            }
            else
            {
                BtnBoardAddPage.IsEnabled = true;
            }
            /*
            if (WhiteboardTotalCount == 1)
            {
                //BtnWhiteBoardDelete.IsEnabled = false;
            }
            else
            {
                //BtnWhiteBoardDelete.IsEnabled = true;
            }
            */
        }
    }
}