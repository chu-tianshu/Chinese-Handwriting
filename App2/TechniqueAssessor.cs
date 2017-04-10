using System.Collections.Generic;
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
            Correspondence = SketchFeatureExtraction.GetStrokeCorrespondence(sample, template);

            for (int i = 0; i < Correspondence.Length; i++)
                if (Correspondence[i] != i) return false;

            return true;
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
                SketchStroke templateStroke = template[Correspondence[i]];

                Vector2 sampleStartToEndVector = new Vector2((float)(sampleStroke.EndPoint.Y - sampleStroke.StartPoint.Y), (float)(sampleStroke.EndPoint.X - sampleStroke.StartPoint.X));
                Vector2 templateStartToEndVector = new Vector2((float)(templateStroke.EndPoint.Y - templateStroke.StartPoint.Y), (float)(templateStroke.EndPoint.X - templateStroke.StartPoint.X));

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
        public int[] Correspondence { get; private set; }
        public HashSet<int> WrongDirectionStrokeIndices { get; private set; }
        public string[,] SampleIntersectionMatrix { get; private set; }
        public string[,] TemplateIntersectionMatrix { get; private set; }

        #endregion
    }
}