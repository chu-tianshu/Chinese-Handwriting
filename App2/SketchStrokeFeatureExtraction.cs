using System;
using System.Collections.Generic;

namespace App2
{
    public class SketchStrokeFeatureExtraction
    {
        #region ShortStraw corner finding algorithm

        public static SketchStroke ResampleForCornerFinding(SketchStroke stroke)
        {
            BoundingBox bb = new BoundingBox(stroke);
            double resamplingDistance = bb.Diagonal / DiagonalDividedFactor;
            double strokeLength = PathLength(stroke);
            int numOfSampledPoints = (int) (strokeLength / resamplingDistance + 1);

            SketchStroke resampledStroke = SketchStrokePreprocessing.ResampleStroke(stroke, numOfSampledPoints);

            return resampledStroke;
        }

        public static List<SketchPoint> FindCorners(SketchStroke resampledStroke)
        {
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

            for (int i = WindowRadius; i < resampledStroke.Points.Count - WindowRadius; i++)
                straws[i] = SketchPoint.EuclideanDistance(resampledStroke.Points[i - WindowRadius], resampledStroke.Points[i + WindowRadius]);

            /*
             * Get the median of the straw lengths
             */
            double[] strawsCopy = new double[resampledStroke.Points.Count - 2 * WindowRadius];

            for (int i = WindowRadius; i < resampledStroke.Points.Count - WindowRadius; i++)
                strawsCopy[i - WindowRadius] = straws[i];

            double strawLengthMedian = Median(strawsCopy);

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

                        i = i + 1;
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
                    if (!IsLine(resampledStroke, i - 1, i))
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
                    i = i - 1;
                }
            }
        }

        private static int HalfwayCornerIndex(double[] straws, int index1, int index2)
        {
            double quarter = (index2 - index1) / 4.0;

            double minValue = Double.MaxValue;
            int minValueIndex = (int) (index1 + quarter);

            for (int i = (int) (index1 + quarter); i <= index2 - quarter; i++)
            {
                if (straws[i] < minValue)
                {
                    minValue = straws[i];
                    minValueIndex = i;
                }
            }

            return minValueIndex;
        }

        #endregion

        #region static methods

        public static double StartToEndSlope(SketchStroke stroke)
        {
            SketchPoint start = stroke.Points[0];
            SketchPoint end = stroke.Points[stroke.Points.Count - 1];

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

            for (int i = startIndex; i <= endIndex - 1; i++)
                length += SketchPoint.EuclideanDistance(stroke.Points[i], stroke.Points[i + 1]);

            return length;
        }

        public static bool IsLine(SketchStroke stroke, int i1, int i2)
        {

            SketchPoint p1 = stroke.Points[i1];
            SketchPoint p2 = stroke.Points[i2];

            return (SketchPoint.EuclideanDistance(p1, p2) / PathLength(stroke, i1, i2) > LineValidationThreshold);
        }

        #region helper methods

        private static double Median(double[] array)
        {
            Array.Sort(array);

            if (array.Length % 2 == 0) return (array[array.Length / 2] + array[array.Length / 2 - 1]) / 2.0;
            else return (double)array[array.Length / 2];
        }

        #endregion

        #endregion

        #region fields

        private static double DiagonalDividedFactor = 40;
        private static int WindowRadius = 3;
        private static double MedianStrawLengthMultipliedFactor = 0.95;
        private static double LineValidationThreshold = 0.95;

        #endregion
    }
}