namespace App2
{
    public class VisionAssessor
    {
        public VisionAssessor(List<SketchStroke> sample, List<SketchStroke> template)
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
        }

        #region properties

        public double LocationScore { get; private set; }
        public double SizeScore { get; private set; }
        public double ProjectionScore { get; private set; }

        #endregion
    }
}