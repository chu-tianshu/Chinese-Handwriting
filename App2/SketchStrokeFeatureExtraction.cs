using System;

namespace App2
{
    public class SketchStrokeFeatureExtraction
    {
        public static SketchStroke ResampleForCornerFinding(SketchStroke stroke)
        {
            BoundingBox bb = new BoundingBox(stroke);
            double resamplingDistance = bb.Diagonal / DiagonalDividedFactor;
            double strokeLength = PathLength(stroke);
            int numOfSampledPoints = (int)(strokeLength / resamplingDistance + 1);

            SketchStroke resampledStroke = SketchStrokePreprocessor.ResampleStroke(stroke, numOfSampledPoints);

            return resampledStroke;
        }

        #region static methods

        private static double PathLength(SketchStroke stroke)
        {
            return PathLength(stroke, 0, stroke.Points.Count - 1);
        }

        private static double PathLength(SketchStroke stroke, int startIndex, int endIndex)
        {
            double length = 0;

            for (int i = startIndex; i <= endIndex - 1; i++)
                length += SketchPoint.EuclideanDistance(stroke.Points[i], stroke.Points[i + 1]);

            return length;
        }

        #endregion

        #region fields

        private static double DiagonalDividedFactor = 40;

        #endregion
    }
}