using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace App2
{
    public class TechniqueAssessor
    {
        #region initializers

        public TechniqueAssessor(List<SketchStroke> sample, List<SketchStroke> template)
        {
            IsCorrectStrokeCount = JudgeStrokeCount(sample, template);
            IsCorrectStrokeOrder = JudgeStrokeOrder(sample, template);
            IsCorrectStrokeDirection = JudgeStrokeDirection(sample, template);
            IsCorrectIntersection = JudgeIntersection(sample, template);
            IsCorrectOverall = IsCorrectStrokeCount && IsCorrectStrokeDirection && IsCorrectStrokeOrder;
        }

        #endregion

        #region helper methods

        private bool JudgeStrokeCount(List<SketchStroke> sample, List<SketchStroke> template)
        {
            return (sample.Count == template.Count);
        }

        private bool JudgeStrokeOrder(List<SketchStroke> sample, List<SketchStroke> template)
        {
            if (!IsCorrectStrokeCount) return false;

            bool result = true;

            List<SketchStroke> sampleNormalized = SketchPreprocessing.Normalize(sample, 128, 500, new SketchPoint(0.0, 0.0));
            List<SketchStroke> templateNormalized = SketchPreprocessing.Normalize(template, 128, 500, new SketchPoint(0.0, 0.0));

            int numStroke = template.Count;

            /**
             * Correspondance[i] denotes the mapped template stroke number of the ith stroke in the sample
             */

            Correspondance = new int[numStroke];
            bool[] hasCompared = new bool[numStroke];

            for (int i = 0; i < numStroke; i++)
            {
                double minDis = double.MaxValue;
                int matchedIdx = -1;

                for (int j = 0; j < numStroke; j++)
                {
                    if (hasCompared[j]) continue;

                    double dis = SketchTools.HausdorffDistance(sampleNormalized[i], templateNormalized[j]);

                    if (dis < minDis)
                    {
                        minDis = dis;
                        matchedIdx = j;
                    }
                }

                if (i != matchedIdx) result = false;

                Correspondance[i] = matchedIdx;
                hasCompared[matchedIdx] = true;
            }

            return result;
        }

        private bool JudgeStrokeDirection(List<SketchStroke> sample, List<SketchStroke> template)
        {
            if (!IsCorrectStrokeCount) return false;

            int numStroke = template.Count;

            IsCorrectStrokeDirection = true;
            WrongDirectionStrokeIndices = new HashSet<int>();

            for (int i = 0; i < numStroke; i++)
            {
                SketchStroke sampleStroke = sample[i];
                SketchStroke templateStroke = template[Correspondance[i]];

                Vector2 sampleStartToEndVector
                    = new Vector2((float)(sampleStroke.EndPoint.Y - sampleStroke.StartPoint.Y),
                                  (float)(sampleStroke.EndPoint.X - sampleStroke.StartPoint.X));
                Vector2 templateStartToEndVector
                    = new Vector2((float)(templateStroke.EndPoint.Y - templateStroke.StartPoint.Y),
                                  (float)(templateStroke.EndPoint.X - templateStroke.StartPoint.X));

                Vector2 sampleStartToEndVectorNormalized = Vector2.Normalize(sampleStartToEndVector);
                Vector2 templateStartToEndVectorNormalized = Vector2.Normalize(templateStartToEndVector);

                double cosBetweenSampleAndTemplateStrokes = Vector2.Dot(sampleStartToEndVectorNormalized, templateStartToEndVectorNormalized);

                if (cosBetweenSampleAndTemplateStrokes < 0) WrongDirectionStrokeIndices.Add(i);
            }

            return WrongDirectionStrokeIndices.Count == 0;
        }

        private bool JudgeIntersection(List<SketchStroke> sample, List<SketchStroke> template)
        {
            if (!IsCorrectStrokeCount) return false;

            SampleIntersectionMatrix = SketchFeatureExtraction.IntersectionMatrix(sample, Correspondance, WrongDirectionStrokeIndices);
            TemplateIntersectionMatrix = SketchFeatureExtraction.IntersectionMatrix(template);

            for (int i = 0; i < sample.Count; i++)
                for (int j = 0; j < sample.Count; j++)
                    if (SampleIntersectionMatrix[i, j] != TemplateIntersectionMatrix[i, j]) return false;

            return true;
        }

        #endregion

        #region properties

        public bool IsCorrectStrokeCount { get; private set; }
        public bool IsCorrectStrokeOrder { get; private set; }
        public bool IsCorrectStrokeDirection { get; private set; }
        public bool IsCorrectIntersection { get; private set; }
        public bool IsCorrectOverall { get; private set; }
        public int[] Correspondance { get; private set; }
        public HashSet<int> WrongDirectionStrokeIndices { get; private set; }
        public string[,] SampleIntersectionMatrix { get; private set; }
        public string[,] TemplateIntersectionMatrix { get; private set; }

        #endregion
    }
}