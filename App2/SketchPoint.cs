using System;
using Windows.UI.Input.Inking;

namespace App2
{
    public class SketchPoint
    {
        #region innitializers

        public SketchPoint() { }

        public SketchPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public SketchPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public SketchPoint(InkPoint inkPoint)
        {
            X = inkPoint.Position.X;
            Y = inkPoint.Position.Y;
        }

        #endregion

        #region properties

        public double X { get; set; }
        public double Y { get; set; }

        #endregion
    }
}