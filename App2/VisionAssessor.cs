namespace App2
{
    public class VisionAssessor
    {
        public VisionAssessor(List<SketchStroke> sample, List<SketchStroke> template)
        {
            SketchPoint sampleCentroid = SketchFeatureExtraction.Centroid(sample);
            SketchPoint templateCentroid = SketchFeatureExtraction.Centroid(template);

            double fc = Math.sqrt();
        }

        #region properties

        public double LocationScore { get; private set; }
        public double SizeScore { get; private set; }
        public double ProjectionScore { get; private set; }

        #endregion
    }
}