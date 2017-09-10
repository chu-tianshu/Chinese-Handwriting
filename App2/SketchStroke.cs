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
            this.points = new List<SketchPoint>();
            this.timeStamp = new List<long>();
        }

        public SketchStroke(List<SketchPoint> ps, List<long> ts)
        {
            this.points = ps;
            this.timeStamp = ts;
        }

        public SketchStroke(InkStroke inkStroke, List<long> list)
        {
            this.points = new List<SketchPoint>();
            var inkPoints = inkStroke.GetInkPoints();
            for (int j = 0; j < inkPoints.Count; j++)
            {
                AppendPoint(new SketchPoint(inkPoints.ElementAt(j)));
            }
            this.timeStamp = list;
        }
        #endregion

        #region modifiers
        public void AppendPoint(SketchPoint p) { this.points.Add(p); }
        public void AppendTime(long t) { this.timeStamp.Add(t); }
        #endregion

        #region static methods
        public static SketchStroke Copy(SketchStroke orig)
        {
            List<SketchPoint> newPoints = new List<SketchPoint>();
            List<long> newTimestamps = new List<long>();
            for (int i = 0; i < orig.PointsCount; i++)
            {
                newPoints.Add(orig.Points[i]);
            }
            for (int i = 0; i < orig.TimesCount; i++)
            {
                newTimestamps.Add(orig.TimeStamp[i]);
            }
            return new SketchStroke(newPoints, newTimestamps);
        }

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

        public static SketchStroke ConcatenateStrokes(List<SketchStroke> strokes)
        {
            SketchStroke result = new SketchStroke();

            foreach (SketchStroke stroke in strokes)
            {
                for (int i = 0; i < stroke.PointsCount; i++)
                {
                    result.AppendPoint(stroke.Points[i]);
                    result.AppendTime(stroke.TimeStamp[i]);
                }
            }

            return result;
        }
        #endregion

        #region properties
        public int PointsCount { get { return this.points.Count; } }
        public int TimesCount { get { return this.timeStamp.Count; } }
        public List<SketchPoint> Points { get { return this.points; } }
        public SketchPoint StartPoint { get { return this.points[0]; } }
        public SketchPoint MidPoint { get { return this.points[this.points.Count / 2]; } }
        public SketchPoint EndPoint { get { return this.points[this.points.Count - 1]; } }
        public List<long> TimeStamp { get { return this.timeStamp; } set { this.timeStamp = value; } }
        #endregion

        #region fields
        private List<SketchPoint> points;
        private List<long> timeStamp;
        #endregion
    }
}