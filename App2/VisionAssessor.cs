using System;
using System.Collections.Generic;

namespace App2
{
    public class VisionAssessor
    {
        public VisionAssessor(List<SketchStroke> sample, int sampleFrameLength, List<SketchStroke> template, int templateFrameLength)
        {
            SketchPoint sampleCentroid = SketchFeatureExtraction.Centroid(sample);
            SketchPoint templateCentroid = SketchFeatureExtraction.Centroid(template);

            BoundingBox sampleBoundingBox = SketchFeatureExtraction.BoundingRectangle(sample);
            BoundingBox templateBoundingBox = SketchFeatureExtraction.BoundingRectangle(template);

            double sampleHeight = sampleBoundingBox.Height;
            double templateHeight = templateBoundingBox.Height;

            double sampleWidth = sampleBoundingBox.Width;
            double templateWidth = templateBoundingBox.Width;

            double fc = Math.Sqrt(Math.Pow(sampleCentroid.X - templateCentroid.X, 2) 
                      + Math.Pow(sampleCentroid.Y - templateCentroid.Y, 2)) 
                      / (0.5 * sampleWidth);
            double fa = (sampleWidth * sampleHeight - templateWidth * templateHeight) / (templateWidth * templateHeight);
            double fr = (sampleWidth / sampleHeight - templateWidth / templateHeight);

            double muonNear = Fuzzy.Gamma(fc, 0.1, 0.9);
            double muonFar = Fuzzy.Lambda(fc, 0.1, 0.9);
            double muonSmall = Fuzzy.Gamma(fa, -1, 0);
            double muonProperSize = Fuzzy.Delta(fa, -0.4, 0, 0.4);
            double muonLarge = Fuzzy.Lambda(fa, 0, 1);
            double muonTall = Fuzzy.Gamma(fr, -0.5, 0);
            double muonProperRatio = Fuzzy.Delta(fr, -0.166667, 0, 0.166667);
            double muonShort = Fuzzy.Lambda(fr, 0, 0.5);

            double sc, sa, sr;

            sc = muonNear > muonFar ? Fuzzy.CenterOfAreaHigh(0.1, 0.9, muonNear) : Fuzzy.CenterOfAreaLow(0.1, 0.9, muonFar);
            sa = (muonProperSize > Math.Max(muonSmall, muonLarge)) ? 
                Fuzzy.CenterOfAreaHigh(0.6, 0.9, muonProperSize) : Fuzzy.CenterOfAreaLow(0.1, 0.9, Math.Max(muonSmall, muonLarge));
            sr = (muonProperRatio > Math.Max(muonTall, muonShort)) ? 
                Fuzzy.CenterOfAreaHigh(0.8, 0.9, muonProperRatio) : Fuzzy.CenterOfAreaLow(0.5, 0.9, Math.Max(muonTall, muonShort));
        }

        #region properties

        public double LocationScore { get; private set; }
        public double SizeScore { get; private set; }
        public double ProjectionScore { get; private set; }

        #endregion

        #region fields

        private readonly int ScaledFrameSize = 200;

        #endregion
    }
}