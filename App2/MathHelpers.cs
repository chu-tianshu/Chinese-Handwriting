using System;

namespace App2
{
    public class MathHelpers
    {
        public static double EuclideanDistance(SketchPoint p1, SketchPoint p2)
        {
            return (Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)));
        }

        public static double EuclideanDistance(double x1, double y1, double x2, double y2)
        {
            return (Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)));
        }

        public static double Median(double[] array)
        {
            Array.Sort(array);

            if (array.Length % 2 == 0) return (array[array.Length / 2] + array[array.Length / 2 - 1]) / 2.0;
            else return (double)array[array.Length / 2];
        }
    }
}