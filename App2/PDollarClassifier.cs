using System;
using System.Collections.Generic;

namespace App2
{
    class PDollarClassifier
    {
        #region initializers

        public PDollarClassifier(int n, double size, SketchPoint point, Dictionary<string, List<SketchStroke>> templates)
        {
            N = n;
            Size = size;
            Origin = point;
            Templates = templates;
        }

        public void run(List<SketchStroke> sample)
        {
            List<Tuple<string, double>> results = new List<Tuple<string, double>>();

            List<SketchStroke> normalizedSample = Normalize(sample);
        }

        #endregion

        #region helper methods

        List<SketchStroke> Normalize(List<SketchStroke> strokes)
        {
            List<SketchStroke> resampled = SketchPreprocessing.Resample(strokes, N);
            List<SketchStroke> scaled = SketchPreprocessing.ScaleSquare(resampled, Size);
            List<SketchStroke> translated = SketchPreprocessing.TranslateCentroid(scaled, Origin);

            return translated;
        }

        #endregion

        #region properties

        public int N { get; private set; }
        public double Size { get; private set; }
        public SketchPoint Origin { get; private set; }
        public Dictionary<string, List<SketchStroke>> Templates { get; private set; }
        public List<string> Lables { get { return labels; } }
        public List<double> Scores { get { return scores; } }

        #endregion

        #region fields

        List<string> labels;
        List<double> scores;

        #endregion
    }
}