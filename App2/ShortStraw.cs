using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace App2
{
    public class ShortStraw
    {
        public static List<SketchStroke> FindStrokeSegments(SketchStroke stroke)
        {
            List<SketchStroke> result = new List<SketchStroke>();

            List<int> cornerIndices = FindCornerIndices(stroke);
            for (int i = 0; i < cornerIndices.Count - 1; i++) result.Add(SketchStroke.Substroke(stroke, cornerIndices[i], cornerIndices[i + 1]));

            return result;
        }

        public static List<SketchPoint> FindCorners(SketchStroke stroke)
        {
            var resampledStroke = ResampleForCornerFinding(stroke);

            List<SketchPoint> corners = new List<SketchPoint>();
            List<int> cornerIndices = FindCornerIndices(resampledStroke);

            foreach (int index in cornerIndices) corners.Add(resampledStroke.Points[index]);

            return corners;
        }

        public static List<int> FindCornerIndices(SketchStroke resampledStroke)
        {
            List<int> cornerIndices = new List<int>();

            GetCornerIndices(resampledStroke, cornerIndices);

            return cornerIndices;
        }

        private static void GetCornerIndices(SketchStroke resampledStroke, List<int> cornerIndices)
        {
            cornerIndices.Add(0);

            if (resampledStroke.Points.Count <= WindowRadius * 2) return;

            double[] straws = new double[resampledStroke.Points.Count];
            for (int i = WindowRadius; i < resampledStroke.Points.Count - WindowRadius; i++) straws[i] = MathHelpers.EuclideanDistance(resampledStroke.Points[i - WindowRadius], resampledStroke.Points[i + WindowRadius]);

            double[] strawsCopy = new double[resampledStroke.Points.Count - 2 * WindowRadius];
            for (int i = WindowRadius; i < resampledStroke.Points.Count - WindowRadius; i++) strawsCopy[i - WindowRadius] = straws[i];

            double strawLengthMedian = MathHelpers.Median(strawsCopy);
            double strawLengthThreshold = strawLengthMedian * MedianStrawLengthMultipliedFactor;

            for (int i = WindowRadius; i < resampledStroke.Points.Count - WindowRadius; i++)
            {
                if (straws[i] < strawLengthThreshold)
                {
                    double localMin = Double.MaxValue;
                    int localMinIndex = i;

                    while (i < resampledStroke.Points.Count - WindowRadius && straws[i] < strawLengthThreshold)
                    {
                        if (straws[i] < localMin)
                        {
                            localMin = straws[i];
                            localMinIndex = i;
                        }

                        i++;
                    }

                    cornerIndices.Add(localMinIndex);
                }
            }

            cornerIndices.Add(resampledStroke.Points.Count - 1);

            postProcessCorners(resampledStroke, cornerIndices, straws);
        }

        /*
         * Checks the corner candidates to see if any corners can be removed or added based on higher-level polyline rules
         */
        private static void postProcessCorners(SketchStroke resampledStroke, List<int> cornerIndices, double[] straws)
        {
            bool shouldContinue = false;

            while (!shouldContinue)
            {
                shouldContinue = true;

                for (int i = 1; i < cornerIndices.Count; i++)
                {
                    if (cornerIndices[i] - cornerIndices[i - 1] > 3 && !IsLine(resampledStroke, cornerIndices[i - 1], cornerIndices[i]))
                    {
                        cornerIndices.Insert(i, HalfwayCornerIndex(straws, cornerIndices[i - 1], cornerIndices[i]));
                        shouldContinue = false;
                    }
                }
            }

            for (int i = 1; i <= cornerIndices.Count - 2; i++)
            {
                if (IsLine(resampledStroke, cornerIndices[i - 1], cornerIndices[i + 1]))
                {
                    cornerIndices.RemoveAt(i);
                    i--;
                }
            }
        }

        private static int HalfwayCornerIndex(double[] straws, int index1, int index2)
        {
            double quarter = (index2 - index1) / 4.0;

            double minValue = Double.MaxValue;
            int minValueIndex = (int)(index1 + quarter);

            for (int i = (int)(index1 + quarter); i <= index2 - quarter; i++)
            {
                if (straws[i] < minValue)
                {
                    minValue = straws[i];
                    minValueIndex = i;
                }
            }

            return minValueIndex;
        }

        private static SketchStroke ResampleForCornerFinding(SketchStroke stroke)
        {
            BoundingBox bb = new BoundingBox(stroke);
            double resamplingDistance = bb.Diagonal / DiagonalDividedFactor;
            double strokeLength = SketchStrokeFeatureExtraction.PathLength(stroke);
            int numOfSampledPoints = (int)(strokeLength / resamplingDistance + 1);

            SketchStroke resampledStroke = SketchStrokePreprocessing.ResampleStroke(stroke, numOfSampledPoints);

            return resampledStroke;
        }

        private static bool IsLine(SketchStroke stroke)
        {
            return IsLine(stroke, 0, stroke.Points.Count - 1);
        }

        private static bool IsLine(SketchStroke stroke, int i1, int i2)
        {
            SketchPoint p1 = stroke.Points[i1];
            SketchPoint p2 = stroke.Points[i2];

            return (MathHelpers.EuclideanDistance(p1, p2) / SketchStrokeFeatureExtraction.PathLength(stroke, i1, i2) > LineValidationThreshold);
        }

        private static readonly double DiagonalDividedFactor = 40;
        private static readonly int WindowRadius = 3;
        private static readonly double MedianStrawLengthMultipliedFactor = 0.95;
        private static readonly double LineValidationThreshold = 0.95;
    }
}