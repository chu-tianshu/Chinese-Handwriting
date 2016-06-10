using System;
using System.Collections.Generic;

namespace App2
{
    internal class SketchStroke
    {
        #region initializers

        public SketchStroke() { }

        #endregion

        #region setters

        public void AppendPoint(SketchPoint p)
        {
            points.Add(p);
        }

        #endregion

        #region getters

        public List<SketchPoint> GetPoints()
        {
            return points;
        }

        public SketchPoint GetFirstPoint()
        {
            if (points.Count == 0) throw new Exception("Current stroke is empty.");

            return (points[0]);
        }

        public SketchPoint GetLastPoint()
        {
            if (points.Count == 0) throw new Exception("Current stroke is empty.");

            return (points[points.Count - 1]);
        }

        #endregion

        #region fields

        private List<SketchPoint> points;

        #endregion
    }
}