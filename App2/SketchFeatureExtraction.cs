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
            return new BoundingBox(strokes).Height;
        }

        public static double Width(List<SketchStroke> strokes)
        {
            return new BoundingBox(strokes).Width;
        }

        public static string[,] IntersectionMatrix(List<SketchStroke> strokes)
        {
            string[,] intersectionMatrix = new string[strokes.Count, strokes.Count];

            for (int i = 0; i < strokes.Count; i++)
                for (int j = 0; j < strokes.Count; j++)
                    if (i == j) intersectionMatrix[i, j] = "none";
                    else intersectionMatrix[i, j] = SketchStrokeFeatureExtraction.IntersectionRelationship(strokes[i], strokes[j]);

            return intersectionMatrix;
        }

        public static string[,] IntersectionMatrix(List<SketchStroke> strokes, int[] correspondance, HashSet<int> wrongDirectionStrokeIndices)
        {
            List<SketchStroke> restored = new List<SketchStroke>();

            for (int i = 0; i < correspondance.Length; i++)
            {
                int strokeIndex = Array.IndexOf(correspondance, i);

                if (wrongDirectionStrokeIndices.Contains(strokeIndex)) restored.Add(SketchStroke.Reverse(strokes[strokeIndex]));
                else restored.Add(strokes[strokeIndex]);
            }

            return IntersectionMatrix(restored);
        }

        public static int[] StrokeToStrokeCorrespondence(List<SketchStroke> sample, List<SketchStroke> template)
        {
            int numStroke = sample.Count;
            int[] correspondence = new int[numStroke];
            bool[] hasCompared = new bool[numStroke];

            List<SketchStroke> sampleNormalized = SketchPreprocessing.Normalize(sample, 128, 500, new SketchPoint(0.0, 0.0));
            List<SketchStroke> templateNormalized = SketchPreprocessing.Normalize(template, 128, 500, new SketchPoint(0.0, 0.0));

            for (int i = 0; i < numStroke; i++)
            {
                double minDis = double.MaxValue;
                int matchedIdx = -1;

                for (int j = 0; j < numStroke; j++)
                {
                    if (hasCompared[j]) continue;

                    double dis = SketchTools.HausdorffDistance(sampleNormalized[i], templateNormalized[j]);

                    if (dis < minDis)
                    {
                        minDis = dis;
                        matchedIdx = j;
                    }
                }

                correspondence[i] = matchedIdx;
                hasCompared[matchedIdx] = true;
            }

            return correspondence;
        }

        public static Dictionary<int, SketchStroke> StrokeToSegmentCorrespondence(List<SketchStroke> sample, List<SketchStroke> template)
        {
            Dictionary<int, SketchStroke> result = new Dictionary<int, SketchStroke>();

            List<SketchStroke> sampleNormalized = SketchPreprocessing.Normalize(sample, 128, 500, new SketchPoint(0.0, 0.0));
            List<SketchStroke> templateNormalized = SketchPreprocessing.Normalize(template, 128, 500, new SketchPoint(0.0, 0.0));
            Segmentation templateSegmentation = new Segmentation(templateNormalized);
            Segmentation sampleSegmentation = new Segmentation(sampleNormalized);

            HashSet<int> sampleSegmentTaken = new HashSet<int>();

            for (int i = 0; i < templateSegmentation.Segments.Count; i++)
            {
                double minDistance = double.MaxValue;
                int matchedIndex = -1;
                SketchStroke matchedSegment = new SketchStroke();

                for (int j = 0; j < sampleSegmentation.Segments.Count; j++)
                {
                    if (sampleSegmentTaken.Contains(j)) continue;

                    double dis = SketchTools.HausdorffDistance(templateSegmentation.Segments[i], sampleSegmentation.Segments[j]);

                    if (dis < minDistance)
                    {
                        minDistance = dis;
                        matchedIndex = j;
                        matchedSegment = sampleSegmentation.Segments[j];
                    }
                }

                for (int j = matchedIndex - 1; j >= 0; j--)
                {
                    if (sampleSegmentTaken.Contains(j)) break;


                }
            }

            return result;
        }
    }
}