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
            this.IsCorrectStrokeCount = JudgeStrokeCount(sample, template);
            this.IsCorrectStrokeOrder = JudgeStrokeOrder(sample, template);
            this.IsCorrectStrokeDirection = JudgeStrokeDirection(sample, template);
            this.IsCorrectIntersection = JudgeIntersection(sample, template);
            this.IsCorrectOverall = IsCorrectStrokeCount && IsCorrectStrokeDirection && IsCorrectStrokeOrder && IsCorrectIntersection;
        }

        #endregion

        #region helper methods

        private bool JudgeStrokeCount(List<SketchStroke> sample, List<SketchStroke> template)
        {
            this.SampleStrokeCount = sample.Count;
            this.TemplateStrokeCount = template.Count;

            if (this.SampleStrokeCount == this.TemplateStrokeCount)
            {
                this.StrokeToStrokeCorrespondenceSameCount = SketchFeatureExtraction.StrokeToStrokeCorrespondenceSameCount(sample, template);
                return true;
            }
            else
            {
                if (sample.Count < template.Count) // Concatenating strokes
                {
                    this.ConcatenatingCorrespondence = SketchFeatureExtraction.StrokeToStrokeCorrespondenceConcatenating(sample, template);
                    foreach (List<int> templateIndices in this.ConcatenatingCorrespondence)
                    {
                        Debug.Write("Sample: ");
                        foreach (int templateIndex in templateIndices)
                        {
                            Debug.Write(templateIndex + ", ");
                        }
                        Debug.WriteLine("");
                    }
                }
                else // Broken strokes
                {
                    this.BrokenStrokeCorrespondence = SketchFeatureExtraction.StrokeToStrokeCorrespondenceBroken(sample, template);
                    foreach (List<int> sampleIndices in this.BrokenStrokeCorrespondence)
                    {
                        Debug.Write("Template: ");
                        foreach (int sampleIndex in sampleIndices)
                        {
                            Debug.Write(sampleIndex + ", ");
                        }
                        Debug.WriteLine("");
                    }
                }

                return false;
            }
        }

        private bool JudgeStrokeOrder(List<SketchStroke> sample, List<SketchStroke> template)
        {
            if (this.IsCorrectStrokeCount)
            {
                for (int i = 0; i < this.StrokeToStrokeCorrespondenceSameCount.Length; i++)
                {
                    if (this.StrokeToStrokeCorrespondenceSameCount[i] != i)
                    {
                        return false;
                    }
                }

                return true;
            }

            if (this.SampleStrokeCount < this.TemplateStrokeCount) // concatenating strokes
            {
                int lastMax = -1;
                for (int i = 0; i < this.ConcatenatingCorrespondence.Count; i++)
                {
                    int currMin = int.MaxValue;
                    int currMax = int.MinValue;
                    foreach (int tempIndex in this.ConcatenatingCorrespondence[i])
                    {
                        if (tempIndex < currMin)
                        {
                            currMin = tempIndex;
                        }
                        if (tempIndex > currMax)
                        {
                            currMax = tempIndex;
                        }
                    }

                    if (currMax - currMin != this.ConcatenatingCorrespondence[i].Count - 1 || currMin != lastMax + 1)
                    {
                        return false;
                    }

                    lastMax = currMax;
                }
            }
            else // broken strokes
            {
                int lastMax = -1;
                for (int i = 0; i < this.BrokenStrokeCorrespondence.Count; i++)
                {
                    int currMin = int.MaxValue;
                    int currMax = int.MinValue;
                    foreach (int sampleIndex in this.BrokenStrokeCorrespondence[i])
                    {
                        if (sampleIndex < currMin)
                        {
                            currMin = sampleIndex;
                        }
                        if (sampleIndex > currMax)
                        {
                            currMax = sampleIndex;
                        }
                    }

                    if (currMax - currMin != this.BrokenStrokeCorrespondence[i].Count - 1 || currMin != lastMax + 1)
                    {
                        return false;
                    }

                    lastMax = currMax;
                }
            }

            return true;
        }

        private bool JudgeStrokeDirection(List<SketchStroke> sample, List<SketchStroke> template)
        {
            if (!IsCorrectStrokeCount)
            {
                return false;
            }

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
        }

        private bool JudgeIntersection(List<SketchStroke> sample, List<SketchStroke> template)
        {
            if (!IsCorrectStrokeCount)
            {
                return false;
            }

            this.SampleIntersectionMatrix = SketchFeatureExtraction.IntersectionMatrix(sample, StrokeToStrokeCorrespondenceSameCount, WrongDirectionStrokeIndices);
            this.TemplateIntersectionMatrix = SketchFeatureExtraction.IntersectionMatrix(template);

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
        public int SampleStrokeCount { get; private set; }
        public int TemplateStrokeCount { get; private set; }
        public int[] StrokeToStrokeCorrespondenceSameCount { get; private set; }
        public List<List<int>> ConcatenatingCorrespondence { get; private set; }
        public List<List<int>> BrokenStrokeCorrespondence { get; private set; }
        public List<List<int>[]> StrokeToStrokeCorrespondenceDifferentCount { get; private set; }
        public Dictionary<int, SketchStroke> StrokeToSegmentCorrespondence { get; private set; }
        public HashSet<int> WrongDirectionStrokeIndices { get; private set; }
        public string[,] SampleIntersectionMatrix { get; private set; }
        public string[,] TemplateIntersectionMatrix { get; private set; }

        #endregion
    }
}