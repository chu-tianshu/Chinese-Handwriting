using System.Collections.Generic;
using System.Diagnostics;

namespace App2
{
    public class TechniqueAssessor
    {
        #region initializers

        public TechniqueAssessor(List<SketchStroke> sample, List<SketchStroke> template)
        {
            IsCorrectStrokeCount = (sample.Count == template.Count);

            if (IsCorrectStrokeCount == false) IsCorrectStrokeOrder = false;
            else
            {
                IsCorrectStrokeOrder = true;

                List<SketchStroke> sampleNormalized = SketchPreprocessing.Normalize(sample, 128, 500, new SketchPoint(0.0, 0.0));
                List<SketchStroke> templateNormalized = SketchPreprocessing.Normalize(template, 128, 500, new SketchPoint(0.0, 0.0));

                int numStroke = template.Count;

                bool[] hasCompared = new bool[numStroke];

                for (int i = 0; i < numStroke; i++)
                {
                    double minDis = double.MaxValue;
                    int matchedIdx = -1;

                    for (int j = 0; j < numStroke; j++)
                    {
                        if (hasCompared[j]) continue;

                        double dis = SketchTools.HausdorffDistance(sampleNormalized[i], templateNormalized[j]);

                        Debug.WriteLine("i = " + i + ", j = " + j + ", minDis = " + minDis + ", dis = " + dis);

                        if (dis < minDis)
                        {
                            minDis = dis;
                            matchedIdx = j;
                        }
                    }

                    Debug.WriteLine("i = " + i + ", matched = " + matchedIdx);

                    if (i != matchedIdx)
                    {
                        IsCorrectStrokeOrder = false;

                        break;
                    }

                    hasCompared[matchedIdx] = true;
                }
            }
        }

        #endregion

        #region properties

        public bool IsCorrectStrokeCount { get; private set; }
        public bool IsCorrectStrokeOrder { get; private set; }

        #endregion
    }
}