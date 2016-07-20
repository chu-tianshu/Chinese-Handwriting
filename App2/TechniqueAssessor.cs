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
            IsCorrectStrokeCount = (sample.Count == template.Count);

            if (IsCorrectStrokeCount == false)
            {
                IsCorrectStrokeOrder = false;
                IsCorrectStrokeDirection = false;
            }
            else
            {
                #region stroke order correctness check

                IsCorrectStrokeOrder = true;

                List<SketchStroke> sampleNormalized = SketchPreprocessing.Normalize(sample, 128, 500, new SketchPoint(0.0, 0.0));
                List<SketchStroke> templateNormalized = SketchPreprocessing.Normalize(template, 128, 500, new SketchPoint(0.0, 0.0));

                int numStroke = template.Count;

                int[] correspondance = new int[numStroke];
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

                    if (i != matchedIdx) IsCorrectStrokeOrder = false;

                    correspondance[i] = matchedIdx;
                    hasCompared[matchedIdx] = true;
                }

                #endregion

                #region stroke direction correctness check

                IsCorrectStrokeDirection = true;
                wrongDirectionStrokeIndices = new List<int>();

                for (int i = 0; i < numStroke; i++)
                {
                    SketchStroke sampleStroke = sample[i];
                    SketchStroke templateStroke = template[correspondance[i]];

                    Vector2 sampleStartToEndVector 
                        = new Vector2((float) (sampleStroke.EndPoint.Y - sampleStroke.StartPoint.Y), 
                                      (float) (sampleStroke.EndPoint.X - sampleStroke.StartPoint.X));
                    Vector2 templateStartToEndVector
                        = new Vector2((float)(templateStroke.EndPoint.Y - templateStroke.StartPoint.Y),
                                      (float)(templateStroke.EndPoint.X - templateStroke.StartPoint.X));

                    Vector2 sampleStartToEndVectorNormalized = Vector2.Normalize(sampleStartToEndVector);
                    Vector2 templateStartToEndVectorNormalized = Vector2.Normalize(templateStartToEndVector);

                    double cosBetweenSampleAndTemplateStrokes = Vector2.Dot(sampleStartToEndVectorNormalized, templateStartToEndVectorNormalized);

                    if (cosBetweenSampleAndTemplateStrokes < 0)
                    {
                        wrongDirectionStrokeIndices.Add(i);

                        IsCorrectStrokeDirection = false;
                    }
                }

                #endregion
            }

            IsCorrectOverall = IsCorrectStrokeCount && IsCorrectStrokeDirection && IsCorrectStrokeOrder;
        }

        #endregion

        #region properties

        public bool IsCorrectStrokeCount { get; private set; }
        public bool IsCorrectStrokeOrder { get; private set; }
        public bool IsCorrectStrokeDirection { get; private set; }
        public bool IsCorrectOverall { get; private set; }
        public List<int> wrongDirectionStrokeIndices { get; private set; }

        #endregion
    }
}