using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 墨迹图形识别辅助类。
    /// </summary>
    public class InkRecognizeHelper
    {
        /// <summary>
        /// 对给定笔迹集合进行图形识别。
        /// </summary>
        /// <param name="strokes">待识别笔迹集合。</param>
        /// <returns>识别结果；识别失败时返回默认值。</returns>
        public static ShapeRecognizeResult RecognizeShape(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0)
                return default;

            var analyzer = new InkAnalyzer();
            analyzer.AddStrokes(strokes);
            analyzer.SetStrokesType(strokes, System.Windows.Ink.StrokeType.Drawing);

            AnalysisAlternate analysisAlternate = null;
            int strokesCount = strokes.Count;
            var sfsaf = analyzer.Analyze();
            if (sfsaf.Successful)
            {
                var alternates = analyzer.GetAlternates();
                if (alternates.Count > 0)
                {
                    while ((!alternates[0].Strokes.Contains(strokes.Last()) ||
                        !IsContainShapeType(((InkDrawingNode)alternates[0].AlternateNodes[0]).GetShapeName()))
                        && strokesCount >= 2)
                    {
                        analyzer.RemoveStroke(strokes[strokes.Count - strokesCount]);
                        strokesCount--;
                        sfsaf = analyzer.Analyze();
                        if (sfsaf.Successful)
                        {
                            alternates = analyzer.GetAlternates();
                        }
                    }
                    analysisAlternate = alternates[0];
                }
            }

            analyzer.Dispose();

            if (analysisAlternate != null && analysisAlternate.AlternateNodes.Count > 0)
            {
                var node = analysisAlternate.AlternateNodes[0] as InkDrawingNode;
                return new ShapeRecognizeResult(node.Centroid, node.HotPoints, analysisAlternate, node);
            }

            return default;
        }

        /// <summary>
        /// 判断识别名称是否属于可处理的基础几何图形。
        /// </summary>
        /// <param name="name">图形名称。</param>
        /// <returns>属于受支持图形时返回 <c>true</c>。</returns>
        public static bool IsContainShapeType(string name)
        {
            if (name.Contains("Triangle") || name.Contains("Circle") ||
                name.Contains("Rectangle") || name.Contains("Diamond") ||
                name.Contains("Parallelogram") || name.Contains("Square")
                || name.Contains("Ellipse"))
            {
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 手写识别语言枚举。
    /// </summary>
    public enum RecognizeLanguage
    {
        SimplifiedChinese = 0x0804,
        TraditionalChinese = 0x7c03,
        English = 0x0809
    }

    /// <summary>
    /// 图形识别结果对象。
    /// </summary>
    public class ShapeRecognizeResult
    {
        /// <summary>
        /// 创建图形识别结果实例。
        /// </summary>
        public ShapeRecognizeResult(Point centroid, PointCollection hotPoints, AnalysisAlternate analysisAlternate, InkDrawingNode node)
        {
            Centroid = centroid;
            HotPoints = hotPoints;
            AnalysisAlternate = analysisAlternate;
            InkDrawingNode = node;
        }

        /// <summary>
        /// 识别候选结果对象。
        /// </summary>
        public AnalysisAlternate AnalysisAlternate { get; }

        /// <summary>
        /// 图形质心。
        /// </summary>
        public Point Centroid { get; set; }

        /// <summary>
        /// 图形关键点集合。
        /// </summary>
        public PointCollection HotPoints { get; }

        /// <summary>
        /// 图形绘制节点。
        /// </summary>
        public InkDrawingNode InkDrawingNode { get; }
    }

    /// <summary>
    /// 图形识别类
    /// </summary>
    //public class ShapeRecogniser
    //{
    //    public InkAnalyzer _inkAnalyzer = null;

    //    private ShapeRecogniser()
    //    {
    //        this._inkAnalyzer = new InkAnalyzer
    //        {
    //            AnalysisModes = AnalysisModes.AutomaticReconciliationEnabled
    //        };
    //    }

    //    /// <summary>
    //    /// 根据笔迹集合返回图形名称字符串
    //    /// </summary>
    //    /// <param name="strokeCollection"></param>
    //    /// <returns></returns>
    //    public InkDrawingNode Recognition(StrokeCollection strokeCollection)
    //    {
    //        if (strokeCollection == null)
    //        {
    //            //MessageBox.Show("dddddd");
    //            return null;
    //        }

    //        InkDrawingNode result = null;
    //        try
    //        {
    //            this._inkAnalyzer.AddStrokes(strokeCollection);
    //            if (this._inkAnalyzer.Analyze().Successful)
    //            {
    //                result = _internalAnalyzer(this._inkAnalyzer);
    //                this._inkAnalyzer.RemoveStrokes(strokeCollection);
    //            }
    //        }
    //        catch (System.Exception ex)
    //        {
    //            //result = ex.Message;
    //            System.Diagnostics.Debug.WriteLine(ex.Message);
    //        }

    //        return result;
    //    }

    //    /// <summary>
    //    /// 实现笔迹的分析，返回图形对应的字符串
    //    /// 你在实际的应用中根据返回的字符串来生成对应的Shape
    //    /// </summary>
    //    /// <param name="ink"></param>
    //    /// <returns></returns>
    //    private InkDrawingNode _internalAnalyzer(InkAnalyzer ink)
    //    {
    //        try
    //        {
    //            ContextNodeCollection nodecollections = ink.FindNodesOfType(ContextNodeType.InkDrawing);
    //            foreach (ContextNode node in nodecollections)
    //            {
    //                InkDrawingNode drawingNode = node as InkDrawingNode;
    //                if (drawingNode != null)
    //                {
    //                    return drawingNode;//.GetShapeName();
    //                }
    //            }
    //        }
    //        catch (System.Exception ex)
    //        {
    //            System.Diagnostics.Debug.WriteLine(ex.Message);
    //        }

    //        return null;
    //    }


    //    private static ShapeRecogniser instance = null;
    //    public static ShapeRecogniser Instance
    //    {
    //        get
    //        {
    //            return instance == null ? (instance = new ShapeRecogniser()) : instance;
    //        }
    //    }
    //}


    /// <summary>
    /// 圆形几何信息对象（用于图形相对定位计算）。
    /// </summary>
    public class Circle
    {
        /// <summary>
        /// 创建圆形信息对象。
        /// </summary>
        public Circle(Point centroid, double r, Stroke stroke)
        {
            Centroid = centroid;
            R = r;
            Stroke = stroke;
        }

        /// <summary>
        /// 圆心坐标。
        /// </summary>
        public Point Centroid { get; set; }

        /// <summary>
        /// 半径。
        /// </summary>
        public double R { get; set; }

        /// <summary>
        /// 对应笔迹对象。
        /// </summary>
        public Stroke Stroke { get; set; }
    }
}
