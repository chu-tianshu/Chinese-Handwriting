using System;
using System.Collections.Generic;

namespace App2
{
    public class DtwClassifier
    {
        public DtwClassifier(Dictionary<string, Sketch> templates, int frameLength)
        {
            this.Templates = templates;
            this.frameLength = frameLength;
        }

        public void run(List<SketchStroke> sample)
        {
            List<Tuple<string, double>> results = new List<Tuple<string, double>>();

            foreach (KeyValuePair<string, Sketch> pair in this.Templates)
            {
                bool[,] sampleImage = VisionFeatureExtraction.SketchToArray(sample, this.frameLength);
                bool[,] templateImage = VisionFeatureExtraction.SketchToArray(pair.Value.Strokes, this.frameLength);
                bool[,] sampleImageScaled = VisionFeatureExtraction.Scale(sampleImage, 100);
                bool[,] templateImageScaled = VisionFeatureExtraction.Scale(templateImage, 100);
                int[] sampleHorizontalProjection = VisionFeatureExtraction.TrimProjection(VisionFeatureExtraction.HorizontalProjection(sampleImageScaled));
                int[] sampleVerticalProjection = VisionFeatureExtraction.TrimProjection(VisionFeatureExtraction.VerticalProjection(sampleImageScaled));
                int[] templateHorizontalProjection = VisionFeatureExtraction.TrimProjection(VisionFeatureExtraction.HorizontalProjection(templateImageScaled));
                int[] templateVerticalProjection = VisionFeatureExtraction.TrimProjection(VisionFeatureExtraction.VerticalProjection(templateImageScaled));

                int distance = this.DtwDistance(sampleHorizontalProjection, templateHorizontalProjection) 
                             + this.DtwDistance(sampleVerticalProjection, templateVerticalProjection);

                results.Add(new Tuple<string, double>(pair.Key, distance));
            }

            results.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            this.labels = new List<string>();
            this.scores = new List<double>();

            for (int i = 0; i < results.Count; ++i)
            {
                string label = results[i].Item1;
                double score = results[i].Item2;

                this.labels.Add(label);
                this.scores.Add(score);
            }
        }

        private int DtwDistance(int[] sampleProj, int[] templateProj)
        {
            /*
             * Matlab code:    
            map = ones(sizeSamp + 1, sizeTemp + 1) * 10000;    
            map(1, 1) = 0;
    
            for i = 2 : sizeSamp + 1
                for j = 2 : sizeTemp + 1
                    cost = abs(proSamp(i - 1) - proTemp(j - 1));
                    map(i, j) = cost + min([map(i - 1, j), map(i - 1, j - 1), map(i, j - 1)]);
                end
            end
            */
            int[,] map = new int[sampleProj.Length + 1, templateProj.Length + 1];
            map[0, 0] = 0;
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    map[i, j] = int.MaxValue;
                }
            }
            for (int i = 0; i <= sampleProj.Length - 1; i++)
            {
                for (int j = 0; j <= templateProj.Length - 1; j++)
                {
                    int cost = Math.Abs(sampleProj[i] - templateProj[j]);
                    map[i + 1, j + 1] = cost + Math.Min(map[i, j], Math.Min(map[i, j + 1], map[i + 1, j]));
                }
            }

            return map[map.GetLength(0) - 1, map.GetLength(1) - 1];
        }

        #region properties
        public Dictionary<string, Sketch> Templates { get; private set; }
        public List<string> Labels { get { return labels; } }
        public List<double> Scores { get { return scores; } }
        #endregion

        #region fields
        private List<string> labels;
        private List<double> scores;
        private int frameLength;
        #endregion
    }
}