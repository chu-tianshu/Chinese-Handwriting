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

        public static int[] GetStrokeCorrespondence(List<SketchStroke> sample, List<SketchStroke> template)
        {
            int[] correspondence = new int[template.Count];

            List<SketchStroke> sampleNormalized = SketchPreprocessing.Normalize(sample, 128, 500, new SketchPoint(0.0, 0.0));
            List<SketchStroke> templateNormalized = SketchPreprocessing.Normalize(template, 128, 500, new SketchPoint(0.0, 0.0));

            if (sample.Count == template.Count)
            {
                int numStroke = sample.Count;

                bool[] hasCompared = new bool[numStroke];

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
            }
            else
            {
                List<SketchStroke> templateSegments = new List<SketchStroke>();
                List<SketchStroke> sampleSegments = new List<SketchStroke>();
                List<int> templateSegmentStrokeIndex = new List<int>(); // the ith element in the array ai denotes the index of the stroke in the template that the ith segment belongs to
                List<int> sampleSegmentStrokeIndex = new List<int>();

                FindSegmentsAndStrokeIndices(templateNormalized, templateSegments, templateSegmentStrokeIndex);
                FindSegmentsAndStrokeIndices(sampleNormalized, sampleSegments, sampleSegmentStrokeIndex);

                int[] segmentCorrespondence = new int[templateSegments.Count];

                HashSet<int> sampleSegmentTaken = new HashSet<int>();

                for (int i = 0; i < templateSegments.Count; i++)
                {

                }
            }

            return correspondence;
        }

        public static void FindSegmentsAndStrokeIndices(List<SketchStroke> strokes, List<SketchStroke> segments, List<int> indices)
        {
            for (int i = 0; i < strokes.Count; i++)
            {
                var stroke = strokes[i];
                var currSegments = ShortStraw.FindStrokeSegments(stroke);
                foreach (var segment in currSegments) segments.Add(segment);
                for (int j = 0; j < currSegments.Count; j++) indices.Add(i); 
            }
        }
    }
}