using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace App2
{
    public class VisionAssessor
    {
        public VisionAssessor(List<SketchStroke> sample, int sampleFrameLength, List<SketchStroke> template, int templateFrameLength)
        {
            bool[,] sampleArray = VisionFeatureExtraction.SketchToArray(sample, sampleFrameLength);
            bool[,] templateArray = VisionFeatureExtraction.SketchToArray(template, templateFrameLength);

            bool[,] sampleArrayScaled = VisionFeatureExtraction.Scale(sampleArray, ScaledFrameSize);
            bool[,] templateArrayScaled = VisionFeatureExtraction.Scale(templateArray, ScaledFrameSize);

            SketchPoint sampleCentroid = VisionFeatureExtraction.Centroid(sampleArrayScaled);
            SketchPoint templateCentroid = VisionFeatureExtraction.Centroid(templateArrayScaled);

            BoundingBox sampleBoundingBox = VisionFeatureExtraction.BoundingRectangle(sampleArrayScaled);
            BoundingBox templateBoundingBox = VisionFeatureExtraction.BoundingRectangle(templateArrayScaled);

            double sampleHeight = sampleBoundingBox.Height;
            double templateHeight = templateBoundingBox.Height;
            double sampleWidth = sampleBoundingBox.Width;
            double templateWidth = templateBoundingBox.Width;

            int[] sampleProjectionX = VisionFeatureExtraction.VerticalProjection(sampleArrayScaled);
            int[] sampleProjectionY = VisionFeatureExtraction.HorizontalProjection(sampleArrayScaled);
            int[] templateProjectionX = VisionFeatureExtraction.VerticalProjection(templateArrayScaled);
            int[] templateProjectionY = VisionFeatureExtraction.HorizontalProjection(templateArrayScaled);

            double fc = Math.Sqrt(Math.Pow(sampleCentroid.X - templateCentroid.X, 2) 
                      + Math.Pow(sampleCentroid.Y - templateCentroid.Y, 2)) 
                      / (0.5 * sampleArrayScaled.GetLength(1));
            double fa = (sampleWidth * sampleHeight - templateWidth * templateHeight) / (templateWidth * templateHeight);
            double fr = (sampleWidth / sampleHeight - templateWidth / templateHeight);
            double dx = VisionFeatureExtraction.ProjectionDifference(sampleProjectionX, templateProjectionX);
            double dy = VisionFeatureExtraction.ProjectionDifference(sampleProjectionY, templateProjectionY);
            double fs = VisionFeatureExtraction.SymmetryFeature(sampleProjectionX, templateProjectionX);

            double muonNear = Fuzzy.Gamma(fc, 0.1, 0.9);
            double muonFar = Fuzzy.Lambda(fc, 0.1, 0.9);
            double muonSmall = Fuzzy.Gamma(fa, -1, 0);
            double muonProperSize = Fuzzy.Delta(fa, -0.4, 0, 0.4);
            double muonLarge = Fuzzy.Lambda(fa, 0, 1);
            double muonTall = Fuzzy.Gamma(fr, -0.5, 0);
            double muonProperRatio = Fuzzy.Delta(fr, -0.166667, 0, 0.166667);
            double muonShort = Fuzzy.Lambda(fr, 0, 0.5);
            double muonLessX = Fuzzy.Gamma(dx, 0.1, 0.9);
            double muonMuchX = Fuzzy.Lambda(dx, 0.1, 0.9);
            double muonLessY = Fuzzy.Gamma(dy, 0.1, 0.9);
            double muonMuchY = Fuzzy.Lambda(dy, 0.1, 0.9);
            double muonLeft = Fuzzy.Gamma(fs, -1, 0);
            double muonProperSymmetry = Fuzzy.Delta(fs, -0.4, 0, 0.4);
            double muonRight = Fuzzy.Lambda(fs, 0, 1);

            double sc = muonNear > muonFar ? Fuzzy.CenterOfAreaHigh(0.1, 0.9, muonNear) : Fuzzy.CenterOfAreaLow(0.1, 0.9, muonFar);
            double sa = muonProperSize > Math.Max(muonSmall, muonLarge) ? 
                Fuzzy.CenterOfAreaHigh(0.6, 0.9, muonProperSize) : Fuzzy.CenterOfAreaLow(0.1, 0.9, Math.Max(muonSmall, muonLarge));
            double sr = muonProperRatio > Math.Max(muonTall, muonShort) ? 
                Fuzzy.CenterOfAreaHigh(0.8, 0.9, muonProperRatio) : Fuzzy.CenterOfAreaLow(0.5, 0.9, Math.Max(muonTall, muonShort));
            double sdx = muonLessX > muonMuchX ? Fuzzy.CenterOfAreaHigh(0.1, 0.9, muonLessX) : Fuzzy.CenterOfAreaLow(0.1, 0.9, muonMuchX);
            double sdy = muonLessY > muonMuchY ? Fuzzy.CenterOfAreaHigh(0.1, 0.9, muonMuchY) : Fuzzy.CenterOfAreaLow(0.1, 0.9, muonMuchY);
            double ss = muonProperSymmetry > Math.Max(muonLeft, muonRight) ? 
                Fuzzy.CenterOfAreaHigh(0.6, 0.9, muonProperSymmetry) : Fuzzy.CenterOfAreaLow(0.1, 0.9, Math.Max(muonLeft, muonRight));

            double s1 = sc;
            double s2 = (sa + sr) / 2.0;
            double s3 = (sdx + sdy) / 2.0;

            LocationScore = s1;
            ShapeScore = s2;
            ProjectionScore = s3;

            Debug.WriteLine("fc = " + fc);
            Debug.WriteLine("fa = " + fa);
            Debug.WriteLine("dx = " + dx);
            Debug.WriteLine("dy = " + dy);
            Debug.WriteLine("fs = " + fs);
            Debug.WriteLine("sc = " + sc);
            Debug.WriteLine("sa = " + sa);
            Debug.WriteLine("sdx = " + sdx);
            Debug.WriteLine("sdy = " + sdy);
        }

        #region properties

        public double LocationScore { get; private set; }
        public double ShapeScore { get; private set; }
        public double ProjectionScore { get; private set; }

        #endregion

        #region fields

        private readonly int ScaledFrameSize = 200;

        #endregion
    }
}