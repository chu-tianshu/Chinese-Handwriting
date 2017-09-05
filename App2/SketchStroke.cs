using System.Collections.Generic;
using Windows.UI.Input.Inking;
using System.Linq;

namespace App2
{
    public class SketchStroke
    {
        #region initializers
        public SketchStroke()
        {
            points = new List<SketchPoint>();
            timeStamp = new List<long>();
        }

        public SketchStroke(List<SketchPoint> ps, List<long> ts)
        {
            points = ps;
            timeStamp = ts;
        }

        public SketchStroke(InkStroke inkStroke, List<long> list)
        {
            points = new List<SketchPoint>();
            var inkPoints = inkStroke.GetInkPoints();
            for (int j = 0; j < inkPoints.Count; j++)
            {
                AppendPoint(new SketchPoint(inkPoints.ElementAt(j)));
            }
            timeStamp = list;
        }
        #endregion

        #region modifiers
        public void AppendPoint(SketchPoint p) { points.Add(p); }
        public void AppendTime(long t) { timeStamp.Add(t); }
        #endregion

        #region static methods
        public static SketchStroke Reverse(SketchStroke sketchStroke)
        {
            List<SketchPoint> reversedPoints = new List<SketchPoint>();
            for (int i = sketchStroke.Points.Count - 1; i >= 0; i--)
            {
                reversedPoints.Add(sketchStroke.Points[i]);
            }
            return new SketchStroke(reversedPoints, sketchStroke.TimeStamp);
        }

        public static SketchStroke Substroke(SketchStroke stroke, int start, int end)
        {
            return new SketchStroke(stroke.Points.GetRange(start, end - start + 1), stroke.TimeStamp.GetRange(start, end - start + 1));
        }

        /// <summary>
        /// Concatenate two strokes s1, s2
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns>s1 + s2</returns>
        public static SketchStroke ConcatenateStrokes(SketchStroke s1, SketchStroke s2)
        {
            SketchStroke result = new SketchStroke();

            for (int i = 0; i < s1.PointsCount; i++)
            {
                result.AppendPoint(s1.Points[i]);
                result.AppendTime(s1.TimeStamp[i]);
            }

            for (int i = 0; i < s2.PointsCount; i++)
            {
                result.AppendPoint(s2.Points[i]);
                result.AppendTime(s2.TimeStamp[i]);
            }

            return result;
        }
        #endregion

        #region properties
        public int PointsCount { get { return points.Count; } }
        public List<SketchPoint> Points { get { return points; } }
        public SketchPoint StartPoint { get { return points[0]; } }
        public SketchPoint EndPoint { get { return points[Points.Count - 1]; } }
        public List<long> TimeStamp { get { return timeStamp; } set { timeStamp = value; } }
        #endregion

        #region fields
        private List<SketchPoint> points;
        private List<long> timeStamp;
        #endregion
    }
}