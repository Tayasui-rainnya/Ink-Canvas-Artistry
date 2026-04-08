using System;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 用于承载单个 <see cref="DrawingVisual"/> 的轻量可视化容器。
    /// </summary>
    public class VisualCanvas : FrameworkElement
    {
        /// <summary>
        /// 返回指定索引的可视子元素。
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            return Visual;
        }

        /// <summary>
        /// 当前可视子元素数量（固定为 1）。
        /// </summary>
        protected override int VisualChildrenCount => 1;

        /// <summary>
        /// 初始化可视化容器并挂载目标 <see cref="DrawingVisual"/>。
        /// </summary>
        public VisualCanvas(DrawingVisual visual)
        {
            Visual = visual;
            AddVisualChild(visual);
        }

        /// <summary>
        /// 被承载的可视化对象。
        /// </summary>
        public DrawingVisual Visual { get; }
    }

    /// <summary>
    ///     用于显示笔迹的类
    /// </summary>
    public class StrokeVisual : DrawingVisual
    {
        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        public StrokeVisual() : this(new DrawingAttributes()
        {
            Color = Colors.Red,
            //FitToCurve = true,
            Width = 3,
            Height = 3
        })
        {
        }

        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        /// <param name="drawingAttributes">笔迹绘制属性。</param>
        public StrokeVisual(DrawingAttributes drawingAttributes)
        {
            _drawingAttributes = drawingAttributes;
        }

        /// <summary>
        ///     设置或获取显示的笔迹
        /// </summary>
        public Stroke Stroke { set; get; }

        /// <summary>
        ///     在笔迹中添加点
        /// </summary>
        /// <param name="point">要追加的触控点。</param>
        public void Add(StylusPoint point)
        {
            if (Stroke == null)
            {
                var collection = new StylusPointCollection { point };
                Stroke = new Stroke(collection) { DrawingAttributes = _drawingAttributes };
            }
            else
            {
                Stroke.StylusPoints.Add(point);
            }
        }

        /// <summary>
        ///     重新画出笔迹
        /// </summary>
        public void Redraw()
        {
            try
            {
                using (var dc = RenderOpen())
                {
                    Stroke.Draw(dc);
                }
            }
            catch { }
        }

        private readonly DrawingAttributes _drawingAttributes;

        /// <summary>
        /// 隐式转换占位（当前未实现）。
        /// </summary>
        public static implicit operator Stroke(StrokeVisual v)
        {
            throw new NotImplementedException();
        }
    }
}
