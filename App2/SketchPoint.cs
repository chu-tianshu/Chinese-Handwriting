using System;

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

        #endregion

        #region static methods

        public static double EuclideanDistance(SketchPoint p1, SketchPoint p2)
        {
            return (Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)));
        }

        public static double EuclideanDistance(double x1, double y1, double x2, double y2)
        {
            return (Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)));
        }

        #endregion

        #region properties

        public double X { get; set; }
        public double Y { get; set; }

        #endregion
    }
}