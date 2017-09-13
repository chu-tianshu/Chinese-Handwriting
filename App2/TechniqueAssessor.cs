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
            // this.StrokeToStrokeCorrespondenceDifferentCount = SketchFeatureExtraction.StrokeToStrokeCorrespondenceDifferentCountStartFromSample(sample, template);

            List<List<int>> concatenatingCorrespondence = SketchFeatureExtraction.StrokeToStrokeCorrespondenceConcatenating(sample, template);
            foreach (List<int> templateIndices in concatenatingCorrespondence)
            {
                Debug.Write("Sample: ");
                foreach (int templateIndex in templateIndices)
                {
                    Debug.Write(templateIndex + ", ");
                }
                Debug.WriteLine("");
            }

            if (sample.Count == template.Count)
            {
                this.StrokeToStrokeCorrespondenceSameCount = SketchFeatureExtraction.StrokeToStrokeCorrespondenceSameCount(sample, template);
            }
            else
            {
                this.StrokeToStrokeCorrespondenceDifferentCount = SketchFeatureExtraction.StrokeToStrokeCorrespondenceDifferentCount(sample, template);
            }

            IsCorrectStrokeCount = JudgeStrokeCount(sample, template);
            IsCorrectStrokeOrder = JudgeStrokeOrder(sample, template);
            IsCorrectStrokeDirection = JudgeStrokeDirection(sample, template);
            IsCorrectIntersection = JudgeIntersection(sample, template);
            IsCorrectOverall = IsCorrectStrokeCount && IsCorrectStrokeDirection && IsCorrectStrokeOrder && IsCorrectIntersection;
        }

        #endregion

        #region helper methods

        private bool JudgeStrokeCount(List<SketchStroke> sample, List<SketchStroke> template)
        {
            return (sample.Count == template.Count);

            /*
            if (this.StrokeToStrokeCorrespondenceDifferentCount.Count != template.Count)
            {
                return false;
            }

            foreach (List<int>[] corr in this.StrokeToStrokeCorrespondenceDifferentCount)
            {
                if (corr[0].Count != 1 || corr[1].Count != 1)
                {
                    return false;
                }
            }

            return true;*/
        }

        private bool JudgeStrokeOrder(List<SketchStroke> sample, List<SketchStroke> template)
        {
            if (IsCorrectStrokeCount)
            {
                for (int i = 0; i < this.StrokeToStrokeCorrespondenceSameCount.Length; i++)
                {
                    if (StrokeToStrokeCorrespondenceSameCount[i] != i)
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                // this.PrintCorrespondence();

                int prev = -1;
                foreach (List<int>[] corr in this.StrokeToStrokeCorrespondenceDifferentCount)
                {
                    foreach (int curr in corr[1])
                    {
                        if (prev != -1)
                        {
                            if (curr != prev + 1)
                            {
                                return false;
                            }
                        }

                        prev = curr;
                    }
                }

                return prev == sample.Count - 1;
            }
        }

        private bool JudgeStrokeDirection(List<SketchStroke> sample, List<SketchStroke> template)
        {
            /*
            if (!IsCorrectStrokeCount) return false;

            int numStroke = template.Count;

            IsCorrectStrokeDirection = true;
            WrongDirectionStrokeIndices = new HashSet<int>();

            for (int i = 0; i < numStroke; i++)
            {
                SketchStroke sampleStroke = sample[i];
                SketchStroke templateStroke = template[StrokeToStrokeCorrespondenceSameCount[i]];

                Vector2 sampleStartToEndVector = new Vector2((float)(sampleStroke.EndPoint.Y - sampleStroke.StartPoint.Y), (float)(sampleStroke.EndPoint.X - sampleStroke.StartPoint.X));
                Vector2 templateStartToEndVector = new Vector2((float)(templateStroke.EndPoint.Y - templateStroke.StartPoint.Y), (float)(templateStroke.EndPoint.X - templateStroke.StartPoint.X));

                Vector2 sampleStartToEndVectorNormalized = Vector2.Normalize(sampleStartToEndVector);
                Vector2 templateStartToEndVectorNormalized = Vector2.Normalize(templateStartToEndVector);

                double cosBetweenSampleAndTemplateStrokes = Vector2.Dot(sampleStartToEndVectorNormalized, templateStartToEndVectorNormalized);

                if (cosBetweenSampleAndTemplateStrokes < 0)
                {
                    WrongDirectionStrokeIndices.Add(i);
                }
            }

            return WrongDirectionStrokeIndices.Count == 0;
            */

            return true;
        }

        private bool JudgeIntersection(List<SketchStroke> sample, List<SketchStroke> template)
        {
            if (!IsCorrectStrokeCount)
            {
                return false;
            }
            /*
            SampleIntersectionMatrix = SketchFeatureExtraction.IntersectionMatrix(sample, StrokeToStrokeCorrespondenceSameCount, WrongDirectionStrokeIndices);
            TemplateIntersectionMatrix = SketchFeatureExtraction.IntersectionMatrix(template);

            for (int i = 0; i < sample.Count; i++)
            {
                for (int j = 0; j < sample.Count; j++)
                {
                    if (SampleIntersectionMatrix[i, j] != TemplateIntersectionMatrix[i, j])
                    {
                        return false;
                    }
                }
            }
            */
            return true;
        }

        #endregion

        private void PrintCorrespondence()
        {
            foreach (List<int>[] corr in this.StrokeToStrokeCorrespondenceDifferentCount)
            {
                Debug.WriteLine("");
                Debug.Write("Template: ");
                foreach (int tempIndex in corr[0])
                {
                    Debug.Write(tempIndex + ", ");
                }
                Debug.Write(".........Sample: ");
                foreach (int sampIndex in corr[1])
                {
                    Debug.Write(sampIndex + ", ");
                }
            }
            Debug.WriteLine("");
        }

        #region properties

        public bool IsCorrectStrokeCount { get; private set; }
        public bool IsCorrectStrokeOrder { get; private set; }
        public bool IsCorrectStrokeDirection { get; private set; }
        public bool IsCorrectIntersection { get; private set; }
        public bool IsCorrectOverall { get; private set; }
        public int[] StrokeToStrokeCorrespondenceSameCount { get; private set; }
        public List<List<int>[]> StrokeToStrokeCorrespondenceDifferentCount { get; private set; }
        public Dictionary<int, SketchStroke> StrokeToSegmentCorrespondence { get; private set; }
        public HashSet<int> WrongDirectionStrokeIndices { get; private set; }
        public string[,] SampleIntersectionMatrix { get; private set; }
        public string[,] TemplateIntersectionMatrix { get; private set; }

        #endregion
    }
}