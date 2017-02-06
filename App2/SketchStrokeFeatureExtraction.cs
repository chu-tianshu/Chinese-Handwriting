using System;
using System.Collections.Generic;

namespace App2
{
    public class SketchStrokeFeatureExtraction
    {
        public static List<SketchPoint> FindCorners(SketchStroke stroke)
        {
            return ShortStraw.FindCorners(stroke);
        }

        public static double StartToEndSlope(SketchStroke stroke)
        {
            SketchPoint start = stroke.StartPoint;
            SketchPoint end = stroke.EndPoint;

            if (start.X == end.X)
            {
                if (end.Y >= start.Y) return double.PositiveInfinity;
                else return double.NegativeInfinity;
            }
            else
            {
                return (end.Y - start.Y) / (end.X - start.X);
            }
        }

        public static double PathLength(SketchStroke stroke)
        {
            return PathLength(stroke, 0, stroke.Points.Count - 1);
        }

        public static double PathLength(SketchStroke stroke, int startIndex, int endIndex)
        {
            double length = 0;
            for (int i = startIndex; i <= endIndex - 1; i++) length += MathHelpers.EuclideanDistance(stroke.Points[i], stroke.Points[i + 1]);
            return length;
        }

        public static SketchPoint Intersection(SketchStroke sketchStroke1, SketchStroke sketchStroke2)
        {
            double minDistance = double.MaxValue;
            int minDisIndex1 = 0;
            int minDisIndex2 = 0;

            List<SketchPoint> points1 = sketchStroke1.Points;
            List<SketchPoint> points2 = sketchStroke2.Points;

            for (int i = 0; i < points1.Count; i++)
            {
                SketchPoint point1 = points1[i];

                for (int j = 0; j < points2.Count; j++)
                {
                    SketchPoint point2 = points2[j];

                    double distance = MathHelpers.EuclideanDistance(point1, point2);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        minDisIndex1 = i;
                        minDisIndex2 = j;
                    }
                }
            }

            if (minDistance > 30) return null;

            return new SketchPoint((points1[minDisIndex1].X + points2[minDisIndex2].X) / 2.0, (points1[minDisIndex1].Y + points2[minDisIndex2].Y) / 2.0);
        }

        public static string IntersectionRelationship(SketchStroke sketchStroke1, SketchStroke sketchStroke2)
        {
            SketchPoint closestPoint = Intersection(sketchStroke1, sketchStroke2);

            if (closestPoint == null) return "none";
            if (MathHelpers.EuclideanDistance(closestPoint, sketchStroke1.StartPoint) < 40) return "touch head";
            if (MathHelpers.EuclideanDistance(closestPoint, sketchStroke1.EndPoint) < 40) return "touch tail";
            return "cross";
        }
    }
}