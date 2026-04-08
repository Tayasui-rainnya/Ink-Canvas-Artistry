using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 撤销/重做时间线管理器，用于维护笔迹与元素操作历史。
    /// </summary>
    public class TimeMachine
    {
        private readonly List<TimeMachineHistory> _currentStrokeHistory = new List<TimeMachineHistory>();

        private int _currentIndex = -1;

        /// <summary>
        /// 撤销状态变化回调委托。
        /// </summary>
        public delegate void OnUndoStateChange(bool status);

        /// <summary>
        /// 撤销可用状态变化事件。
        /// </summary>
        public event OnUndoStateChange OnUndoStateChanged;

        /// <summary>
        /// 重做状态变化回调委托。
        /// </summary>
        public delegate void OnRedoStateChange(bool status);

        /// <summary>
        /// 重做可用状态变化事件。
        /// </summary>
        public event OnRedoStateChange OnRedoStateChanged;

        /// <summary>
        /// 在新提交历史前，移除当前索引后的“未来历史”分支。
        /// </summary>
        private void CheckHistoryIndex()
        {
            if (_currentIndex + 1 < _currentStrokeHistory.Count)
            {
                _currentStrokeHistory.RemoveRange(_currentIndex + 1, (_currentStrokeHistory.Count - 1) - _currentIndex);
            }
        }

        /// <summary>
        /// 提交用户输入笔迹历史。
        /// </summary>
        public void CommitStrokeUserInputHistory(StrokeCollection stroke)
        {
            _currentStrokeHistory.Add(new TimeMachineHistory(stroke, TimeMachineHistoryType.UserInput, false));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        /// <summary>
        /// 提交图形识别替换历史。
        /// </summary>
        public void CommitStrokeShapeHistory(StrokeCollection strokeToBeReplaced, StrokeCollection generatedStroke)
        {
            CheckHistoryIndex();
            _currentStrokeHistory.Add(new TimeMachineHistory(generatedStroke, TimeMachineHistoryType.ShapeRecognition, false, strokeToBeReplaced));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        /// <summary>
        /// 提交笔迹/元素变换历史。
        /// </summary>
        public void CommitStrokeManipulationHistory(
            Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> stylusPointDictionary,
            Dictionary<string, Tuple<object, TransformGroup>> ElementsManipulationHistory)
        {
            CheckHistoryIndex();
            _currentStrokeHistory.Add(new TimeMachineHistory(stylusPointDictionary, ElementsManipulationHistory, TimeMachineHistoryType.Manipulation));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        /// <summary>
        /// 提交绘图属性变更历史。
        /// </summary>
        public void CommitStrokeDrawingAttributesHistory(Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> drawingAttributes)
        {
            CheckHistoryIndex();
            _currentStrokeHistory.Add(new TimeMachineHistory(drawingAttributes, TimeMachineHistoryType.DrawingAttributes));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        /// <summary>
        /// 提交擦除/清空相关历史。
        /// </summary>
        public void CommitStrokeEraseHistory(StrokeCollection stroke, StrokeCollection sourceStroke = null)
        {
            CheckHistoryIndex();
            _currentStrokeHistory.Add(new TimeMachineHistory(stroke, TimeMachineHistoryType.Clear, true, sourceStroke));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        /// <summary>
        /// 提交元素插入历史。
        /// </summary>
        public void CommitElementInsertHistory(UIElement element, bool strokeHasBeenCleared = false)
        {
            CheckHistoryIndex();
            _currentStrokeHistory.Add(new TimeMachineHistory(element, TimeMachineHistoryType.ElementInsert, strokeHasBeenCleared));
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        /// <summary>
        /// 清空全部历史并重置索引。
        /// </summary>
        public void ClearStrokeHistory()
        {
            _currentStrokeHistory.Clear();
            _currentIndex = -1;
            NotifyUndoRedoState();
        }

        /// <summary>
        /// 执行撤销并返回对应历史项。
        /// </summary>
        public TimeMachineHistory Undo()
        {
            var item = _currentStrokeHistory[_currentIndex];
            item.StrokeHasBeenCleared = !item.StrokeHasBeenCleared;
            _currentIndex--;
            NotifyUndoRedoState();
            return item;
        }

        /// <summary>
        /// 执行重做并返回对应历史项。
        /// </summary>
        public TimeMachineHistory Redo()
        {
            var item = _currentStrokeHistory[++_currentIndex];
            item.StrokeHasBeenCleared = !item.StrokeHasBeenCleared;
            NotifyUndoRedoState();
            return item;
        }

        /// <summary>
        /// 导出当前历史快照数组。
        /// </summary>
        public TimeMachineHistory[] ExportTimeMachineHistory()
        {
            CheckHistoryIndex();
            return _currentStrokeHistory.ToArray();
        }

        /// <summary>
        /// 导入历史快照并重建索引。
        /// </summary>
        public bool ImportTimeMachineHistory(TimeMachineHistory[] sourceHistory)
        {
            _currentStrokeHistory.Clear();
            _currentStrokeHistory.AddRange(sourceHistory);
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
            return true;
        }

        /// <summary>
        /// 通知外部当前撤销/重做可用状态。
        /// </summary>
        private void NotifyUndoRedoState()
        {
            OnUndoStateChanged?.Invoke(_currentIndex > -1);
            OnRedoStateChanged?.Invoke(_currentIndex < _currentStrokeHistory.Count - 1);
        }
    }

    /// <summary>
    /// 单条历史记录对象，按提交类型承载不同数据。
    /// </summary>
    public class TimeMachineHistory
    {
        public TimeMachineHistoryType CommitType;
        public bool StrokeHasBeenCleared = false;
        public StrokeCollection CurrentStroke;
        public StrokeCollection ReplacedStroke;
        public UIElement Element;
        // 这里说一下 Tuple 的 Value1 是初始值；Value2 是改变值。
        public Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> StylusPointDictionary;
        public Dictionary<string, Tuple<object, TransformGroup>> ElementsManipulationHistory;
        public Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> DrawingAttributes;
        /// <summary>
        /// 构造“用户输入/基础笔迹”历史。
        /// </summary>
        public TimeMachineHistory(StrokeCollection currentStroke, TimeMachineHistoryType commitType, bool strokeHasBeenCleared)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            StrokeHasBeenCleared = strokeHasBeenCleared;
            ReplacedStroke = null;
        }
        /// <summary>
        /// 构造“清除/替换”历史。
        /// </summary>
        public TimeMachineHistory(StrokeCollection currentStroke, TimeMachineHistoryType commitType, bool strokeHasBeenCleared, StrokeCollection replacedStroke)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            StrokeHasBeenCleared = strokeHasBeenCleared;
            ReplacedStroke = replacedStroke;
        }
        /// <summary>
        /// 构造“笔迹/元素变换”历史。
        /// </summary>
        public TimeMachineHistory(
            Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> stylusPointDictionary,
            Dictionary<string, Tuple<object, TransformGroup>> elementsManipulationHistory,
            TimeMachineHistoryType commitType)
        {
            CommitType = commitType;
            ElementsManipulationHistory = elementsManipulationHistory;
            StylusPointDictionary = stylusPointDictionary;
        }
        /// <summary>
        /// 构造“绘图属性变更”历史。
        /// </summary>
        public TimeMachineHistory(Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> drawingAttributes, TimeMachineHistoryType commitType)
        {
            CommitType = commitType;
            DrawingAttributes = drawingAttributes;
        }
        /// <summary>
        /// 构造“插入 UI 元素”历史。
        /// </summary>
        public TimeMachineHistory(UIElement element, TimeMachineHistoryType commitType, bool strokeHasBeenCleared)
        {
            CommitType = commitType;
            Element = element;
            StrokeHasBeenCleared = strokeHasBeenCleared;
        }
    }

    /// <summary>
    /// 历史记录类型枚举。
    /// </summary>
    public enum TimeMachineHistoryType
    {
        UserInput,
        ShapeRecognition,
        Clear,
        Manipulation,
        DrawingAttributes,
        ElementInsert
    }
}
