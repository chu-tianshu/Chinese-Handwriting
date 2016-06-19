using System;
using System.Collections.Generic;

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

        #endregion

        #region modifiers

        public void AppendPoint(SketchPoint p)
        {
            points.Add(p);
        }

        public void AppendTime(long t)
        {
            timeStamp.Add(t);
        }

        #endregion

        #region properties

        public List<SketchPoint> Points
        {
            get
            {
                return points;
            }
        }

        public List<long> TimeStamp
        {
            get
            {
                return timeStamp;
            }
            set
            {
                timeStamp = value;
            }
        }

        #endregion

        #region fields

        private List<SketchPoint> points;
        private List<long> timeStamp;

        #endregion
    }
}