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

        #endregion

        public void run(List<SketchStroke> sample)
        {
            List<Tuple<string, double>> results = new List<Tuple<string, double>>();

            List<SketchStroke> normalizedSample = Normalize(sample);

            foreach(KeyValuePair<string, List<SketchStroke>> entry in Templates)
            {
                List<SketchStroke> normalizedTemplate = Normalize(entry.Value);

                double d1 = Distance(normalizedSample, normalizedTemplate);
                double d2 = Distance(normalizedTemplate, normalizedSample);
                double distance = d1 < d2 ? d1 : d2;

                double score = ToScore(distance);

                results.Add(new Tuple<string, double>(entry.Key, distance));
            }

            results.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            labels = new List<string>();
            scores = new List<double>();

            for (int i = 0; i < results.Count; ++i)
            {
                string label = results[i].Item1;
                double score = results[i].Item2;

                labels.Add(label);
                scores.Add(score);
            }
        }

        #region point cloud distance

        public double Distance(List<SketchStroke> alphaStrokeList, List<SketchStroke> betaStrokeList)
        {
            List<SketchPoint> alphaPoints = new List<SketchPoint>();
            List<SketchPoint> betaPoints = new List<SketchPoint>();

            foreach (SketchStroke alphaStroke in alphaStrokeList) alphaPoints.AddRange(alphaStroke.Points);
            foreach (SketchStroke betaStroke in betaStrokeList) betaPoints.AddRange(betaStroke.Points);

            return Distance(alphaPoints, betaPoints);
        }

        public double Distance(List<SketchPoint> alphaPoints, List<SketchPoint> betaPoints)
        {
            double distances = 0.0;

            var pairs = new List<Tuple<SketchPoint, SketchPoint>>();

            double minDistance, weight, distance;
            int index;

            SketchPoint minPoint = betaPoints[0];

            foreach (SketchPoint alphaPoint in alphaPoints)
            {
                minDistance = Double.MaxValue;

                // iterate through each beta point to find the min beta point to the alpha point
                index = 1;

                foreach (SketchPoint betaPoint in betaPoints)
                {
                    distance = SketchPoint.EuclideanDistance(alphaPoint, betaPoint);

                    // update the min distance and min point
                    if (minDistance > distance)
                    {
                        minDistance = distance;
                        minPoint = betaPoint;
                    }
                }

                // update distance between alpha and beta point lists
                weight = 1 - ((index - 1) / alphaPoints.Count);
                distances += minDistance * weight;

                // pair the alpha point to the min beta point and remove min beta point from list of beta points
                pairs.Add(new Tuple<SketchPoint, SketchPoint>(alphaPoint, minPoint));
                betaPoints.Remove(minPoint);
            }

            return distances;
        }

        #endregion

        #region preprocessing

        List<SketchStroke> Normalize(List<SketchStroke> strokes)
        {
            List<SketchStroke> resampled = SketchPreprocessing.Resample(strokes, N);
            List<SketchStroke> scaled = SketchPreprocessing.ScaleSquare(resampled, Size);
            List<SketchStroke> translated = SketchPreprocessing.TranslateCentroid(scaled, Origin);

            return translated;
        }

        #endregion

        #region helper methods

        private double ToScore(double distance)
        {
            return (100.0 - (distance / (0.5 * (Math.Sqrt(Size * Size + Size * Size)))));
        }

        #endregion

        #region properties

        public int N { get; private set; }
        public double Size { get; private set; }
        public SketchPoint Origin { get; private set; }
        public Dictionary<string, List<SketchStroke>> Templates { get; private set; }
        public List<string> Labels { get { return labels; } }
        public List<double> Scores { get { return scores; } }

        #endregion

        #region fields

        List<string> labels;
        List<double> scores;

        #endregion
    }
}