using System;
using System.Collections.Generic;

namespace App2
{
    public class BopoNotoClassifier
    {
        public BopoNotoClassifier(int n, double size, SketchPoint point, Dictionary<string, Sketch> templates)
        {
            this.N = n;
            this.Size = size;
            this.Origin = point;
            this.Templates = templates;
        }

        public void run(List<SketchStroke> sample)
        {
            List<Tuple<string, double>> results = new List<Tuple<string, double>>();

            List<SketchStroke> normalizedSample = SketchPreprocessing.Normalize(sample, N, Size, Origin);

            foreach (KeyValuePair<string, Sketch> entry in this.Templates)
            {
                List<SketchStroke> templateStrokes = entry.Value.Strokes;
                List<SketchStroke> normalizedTemplate = SketchPreprocessing.Normalize(templateStrokes, N, Size, Origin);

                double d1 = SketchTools.HausdorffDistance(normalizedSample, normalizedTemplate);
                double d2 = SketchTools.Distance(normalizedTemplate, normalizedSample);
                double distance = d1 < d2 ? d1 : d2;

                results.Add(new Tuple<string, double>(entry.Key, distance));
            }

            results.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            labels = new List<string>();
            scores = new List<double>();

            for (int i = 0; i < results.Count; i++)
            {
                string label = results[i].Item1;
                double score = results[i].Item2;

                labels.Add(label);
                scores.Add(score);
            }
        }

        #region properties
        public int N { get; private set; }
        public double Size { get; private set; }
        public SketchPoint Origin { get; private set; }
        public Dictionary<string, Sketch> Templates { get; private set; }
        public List<string> Labels { get { return labels; } }
        public List<double> Scores { get { return scores; } }
        #endregion

        #region fields
        List<string> labels;
        List<double> scores;
        #endregion
    }
}