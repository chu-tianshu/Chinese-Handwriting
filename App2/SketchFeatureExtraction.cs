using System;
using System.Collections.Generic;

namespace App2
{
    public class SketchFeatureExtraction
    {
        public static SketchPoint Centroid(List<SketchStroke> strokes)
        {
            List<SketchPoint> points = new List<SketchPoint>();

            foreach (SketchStroke stroke in strokes) points.AddRange(stroke.Points);

            double meanX = 0.0;
            double meanY = 0.0;

            foreach (SketchPoint point in points)
            {
                meanX += point.X;
                meanY += point.Y;
            }

            meanX /= points.Count;
            meanY /= points.Count;

            return new SketchPoint(meanX, meanY);
        }

        public static BoundingBox BoundingRectangle(List<SketchStroke> strokes)
        {
            return new BoundingBox(strokes);
        }

        public static double Height(List<SketchStroke> strokes)
        {
            BoundingBox bb = new BoundingBox(strokes);

            return bb.Height;
        }

        public static double Width(List<SketchStroke> strokes)
        {
            BoundingBox bb = new BoundingBox(strokes);

            return bb.Width;
        }

        public static string[,] IntersectionMatrix(List<SketchStroke> strokes)
        {
            string[,] intersectionMatrix = new string[strokes.Count, strokes.Count];

            for (int i = 0; i < strokes.Count; i++)
                for (int j = 0; j < strokes.Count; j++)
                    intersectionMatrix[i, j] = SketchStrokeFeatureExtraction.IntersectionRelationship(strokes[i], strokes[j]);

            return intersectionMatrix;
        }

        public static string[,] IntersectionMatrix(List<SketchStroke> strokes, int[] correspondance)
        {
            List<SketchStroke> reordered = new List<SketchStroke>();

            for (int i = 0; i < correspondance.Length; i++)
            {
                int strokeIndex = Array.IndexOf(correspondance, i);

                reordered.Add(strokes[strokeIndex]);
            }

            return IntersectionMatrix(reordered);
        }
    }
}