using System.Collections.Generic;

namespace App2
{
    public class BoponotoTechniqueAssessor
    {
        public BoponotoTechniqueAssessor(List<SketchStroke> sample, List<SketchStroke> template)
        {
            this.IsCorrectStrokeCount = this.JudgeStrokeCount(sample, template);
            this.IsCorrectStrokeOrder = this.JudgeStrokeOrder(sample, template);
            // this.IsCorrectStrokeDirection = this.JudgeStrokeDirection(sample, template);
            this.IsCorrectOverall = this.IsCorrectStrokeCount && this.IsCorrectStrokeOrder && this.IsCorrectStrokeDirection;
        }

        public bool JudgeStrokeCount(List<SketchStroke> sample, List<SketchStroke> template)
        {
            return sample.Count == template.Count;
        }

        public bool JudgeStrokeOrder(List<SketchStroke> sample, List<SketchStroke> template)
        {
            if (this.IsCorrectStrokeCount == false)
            {
                return false;
            }

            // the ith element is the corresponding 
            this.StrokeToStrokeCorrespondenceSameCount = new int[sample.Count];
            HashSet<int> unusedTemplates = new HashSet<int>();
            for (int i = 0; i < template.Count; i++)
            {
                unusedTemplates.Add(i);
            }
            for (int i = 0; i < sample.Count; i++)
            {
                int matchedIndex = -1;
                double minDistance = double.MaxValue;

                foreach (int tempIdx in unusedTemplates)
                {
                    double currDistance = SketchTools.TrioDistance(sample[i], template[tempIdx]);

                    if (currDistance < minDistance)
                    {
                        minDistance = currDistance;
                        matchedIndex = tempIdx;
                    }
                }

                this.StrokeToStrokeCorrespondenceSameCount[i] = matchedIndex;
                unusedTemplates.Remove(matchedIndex);
            }

            for (int i = 0; i < sample.Count; i++)
            {
                if (this.StrokeToStrokeCorrespondenceSameCount[i] != i)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsCorrectStrokeCount { get; private set; }
        public bool IsCorrectStrokeOrder { get; private set; }
        public bool IsCorrectStrokeDirection { get; private set; }
        public bool IsCorrectOverall { get; private set; }
        public int[] StrokeToStrokeCorrespondenceSameCount { get; private set; }
    }
}