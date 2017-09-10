using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace App2
{
    public class SketchFeatureExtraction
    {
        public static SketchPoint Centroid(List<SketchStroke> strokes)
        {
            List<SketchPoint> points = new List<SketchPoint>();
            foreach (SketchStroke stroke in strokes) points.AddRange(stroke.Points);

            double meanX = 0.0;
            double meanY = 0.0;

            foreach (SketchPoint point in points)
            {
                meanX += point.X;
                meanY += point.Y;
            }

            meanX /= points.Count;
            meanY /= points.Count;

            return new SketchPoint(meanX, meanY);
        }

        public static BoundingBox BoundingRectangle(List<SketchStroke> strokes)
        {
            return new BoundingBox(strokes);
        }

        public static double Height(List<SketchStroke> strokes)
        {
            return new BoundingBox(strokes).Height;
        }

        public static double Width(List<SketchStroke> strokes)
        {
            return new BoundingBox(strokes).Width;
        }

        public static string[,] IntersectionMatrix(List<SketchStroke> strokes)
        {
            string[,] intersectionMatrix = new string[strokes.Count, strokes.Count];

            for (int i = 0; i < strokes.Count; i++)
            {
                for (int j = 0; j < strokes.Count; j++)
                {
                    if (i == j)
                    {
                        intersectionMatrix[i, j] = "none";
                    }
                    else
                    {
                        intersectionMatrix[i, j] = SketchStrokeFeatureExtraction.IntersectionRelationship(strokes[i], strokes[j]);
                    }
                }
            }

            return intersectionMatrix;
        }

        public static string[,] IntersectionMatrix(List<SketchStroke> strokes, int[] correspondance, HashSet<int> wrongDirectionStrokeIndices)
        {
            List<SketchStroke> restored = new List<SketchStroke>();

            for (int i = 0; i < correspondance.Length; i++)
            {
                int strokeIndex = Array.IndexOf(correspondance, i);

                if (wrongDirectionStrokeIndices.Contains(strokeIndex))
                {
                    restored.Add(SketchStroke.Reverse(strokes[strokeIndex]));
                }
                else
                {
                    restored.Add(strokes[strokeIndex]);
                }
            }

            return IntersectionMatrix(restored);
        }

        /// <summary>
        /// When sample and template have same numbers of strokes
        /// </summary>
        /// <param name="sample">sample strokes</param>
        /// <param name="template">template strokes</param>
        /// <returns>An array where corr[i] = j denotes the ith stroke from the sample corresponds to the jth stroke from the template.</returns>
        public static int[] StrokeToStrokeCorrespondenceSameCount(List<SketchStroke> sample, List<SketchStroke> template)
        {
            int numStroke = sample.Count;
            int[] correspondence = new int[numStroke];
            bool[] hasCompared = new bool[numStroke];

            List<SketchStroke> sampleNormalized = SketchPreprocessing.Normalize(sample, 128, 500, new SketchPoint(0.0, 0.0));
            List<SketchStroke> templateNormalized = SketchPreprocessing.Normalize(template, 128, 500, new SketchPoint(0.0, 0.0));

            for (int i = 0; i < numStroke; i++)
            {
                double minDis = double.MaxValue;
                int matchedIdx = -1;

                for (int j = 0; j < numStroke; j++)
                {
                    if (hasCompared[j])
                    {
                        continue;
                    }

                    double dis = SketchTools.HausdorffDistance(sampleNormalized[i], templateNormalized[j]);

                    if (dis < minDis)
                    {
                        minDis = dis;
                        matchedIdx = j;
                    }
                }

                correspondence[i] = matchedIdx;
                hasCompared[matchedIdx] = true;
            }

            return correspondence;
        }

        /// <summary>
        /// When sample and template have difference numbers of strokes
        /// </summary>
        /// <param name="sample">sample strokes</param>
        /// <param name="template">template strokes</param>
        /// <returns>The ith element in the result list is an array of two lists, which looks like {{ti, tj, ... ,tk}, {sa}} or 
        /// {{ta}, {si, sj, ..., sk}} which denotes the a correspondence between a set of strokes from the template with a set of
        /// strokes from the sample. Note that it's either one-to-one, or one-to-many, or many-to-one.</returns>
        public static List<List<int>[]> StrokeToStrokeCorrespondenceDifferentCount(List<SketchStroke> sample, List<SketchStroke> template)
        {
            List<List<int>[]> result = new List<List<int>[]>();
            HashSet<int> usedSampleIndices = new HashSet<int>();
            HashSet<int> usedTemplateIndices = new HashSet<int>();

            List<SketchStroke> sampleNormalized = SketchPreprocessing.Normalize(sample, 128, 500, new SketchPoint(0.0, 0.0));
            List<SketchStroke> templateNormalized = SketchPreprocessing.Normalize(template, 128, 500, new SketchPoint(0.0, 0.0));

            int ti = 0;
            while (ti < template.Count && usedSampleIndices.Count < sample.Count)
            {
                if (usedTemplateIndices.Contains(ti))
                {
                    ti++;
                    continue;
                }

                double oneToOneDis = double.MaxValue;
                int matchedIdx = -1;
                for (int si = 0; si < sample.Count; si++)
                {
                    if (usedSampleIndices.Contains(si))
                    {
                        continue;
                    }

                    double dis = SketchTools.HausdorffDistance(sampleNormalized[si], templateNormalized[ti]);

                    if (dis < oneToOneDis)
                    {
                        oneToOneDis = dis;
                        matchedIdx = si;
                    }
                }

                usedTemplateIndices.Add(ti);
                usedSampleIndices.Add(matchedIdx);

                List<int> oneToManyIndices = new List<int>();
                oneToManyIndices.Add(matchedIdx);
                List<int> manyToOneIndices = new List<int>();
                manyToOneIndices.Add(ti);

                SketchStroke currSampleStroke = sampleNormalized[matchedIdx];
                double oneToManyDis = oneToOneDis;

                int preSample = matchedIdx - 1;
                int postSample = matchedIdx + 1;
                while (preSample >= 0)
                {
                    if (usedSampleIndices.Contains(preSample))
                    {
                        break;
                    }

                    SketchStroke concatenatedStroke = SketchStroke.ConcatenateStrokes(sampleNormalized[preSample], currSampleStroke);
                    double dis = SketchTools.HausdorffDistance(concatenatedStroke, templateNormalized[ti]);

                    if (dis < oneToManyDis)
                    {
                        oneToManyDis = dis;
                        oneToManyIndices.Insert(0, preSample);
                        currSampleStroke = concatenatedStroke;
                    }
                    else
                    {
                        break;
                    }

                    preSample--;
                }
                while (postSample < sample.Count)
                {
                    if (usedSampleIndices.Contains(postSample))
                    {
                        break;
                    }

                    SketchStroke concatenatedStroke = SketchStroke.ConcatenateStrokes(currSampleStroke, sampleNormalized[postSample]);
                    double dis = SketchTools.HausdorffDistance(concatenatedStroke, templateNormalized[ti]);

                    if (dis < oneToManyDis)
                    {
                        oneToManyDis = dis;
                        oneToManyIndices.Add(postSample);
                        currSampleStroke = concatenatedStroke;
                    }
                    else
                    {
                        break;
                    }

                    postSample++;
                }

                SketchStroke currTemplateStroke = templateNormalized[ti];
                double manyToOneDis = oneToOneDis;

                int preTemplate = ti - 1;
                int postTemplate = ti + 1;
                while (preTemplate >= 0)
                {
                    if (usedTemplateIndices.Contains(preTemplate))
                    {
                        break;
                    }

                    SketchStroke concatenatedStroke = SketchStroke.ConcatenateStrokes(templateNormalized[preTemplate], currTemplateStroke);
                    double dis = SketchTools.HausdorffDistance(concatenatedStroke, sampleNormalized[matchedIdx]);

                    if (dis < manyToOneDis)
                    {
                        manyToOneDis = dis;
                        manyToOneIndices.Insert(0, preTemplate);
                        currTemplateStroke = concatenatedStroke;
                    }
                    else
                    {
                        break;
                    }

                    preTemplate--;
                }
                while (postTemplate < template.Count)
                {
                    if (usedTemplateIndices.Contains(postTemplate))
                    {
                        break;
                    }

                    SketchStroke concatenatedStroke = SketchStroke.ConcatenateStrokes(currTemplateStroke, templateNormalized[postTemplate]);
                    double dis = SketchTools.HausdorffDistance(concatenatedStroke, sampleNormalized[matchedIdx]);

                    if (dis < manyToOneDis)
                    {
                        manyToOneDis = dis;
                        manyToOneIndices.Add(postTemplate);
                        currTemplateStroke = concatenatedStroke;
                    }
                    else
                    {
                        break;
                    }

                    postTemplate++;
                }

                int flag = -2; // -1 for oneToMany, 0 for oneToOne, 1 for manyToOne
                if (oneToManyDis < oneToOneDis && manyToOneDis < oneToOneDis)
                {
                    if (oneToManyDis < manyToOneDis)
                    {
                        flag = -1;
                    }
                    else
                    {
                        flag = 1;
                    }
                }
                else if (oneToManyDis < oneToOneDis)
                {
                    flag = -1;
                }
                else if (manyToOneDis < oneToOneDis)
                {
                    flag = 1;
                }
                else
                {
                    flag = 0;
                }

                if (flag == 0)
                {
                    List<int> oneTemplate = new List<int>();
                    List<int> oneSample = new List<int>();
                    oneTemplate.Add(ti);
                    oneSample.Add(matchedIdx);
                    result.Add(new List<int>[] { oneTemplate, oneSample });
                    usedTemplateIndices.Add(ti);
                    usedSampleIndices.Add(matchedIdx);
                    ti++;
                }
                else if (flag == -1) // one to many
                {
                    List<int> one = new List<int>();
                    List<int> many = new List<int>();
                    one.Add(ti);
                    usedTemplateIndices.Add(ti);
                    foreach (int index in oneToManyIndices)
                    {
                        usedSampleIndices.Add(index);
                        many.Add(index);
                    }
                    result.Add(new List<int>[] { one, many });
                    ti++;

                    Debug.WriteLine("count = " + many.Count);
                }
                else if (flag == 1) // many to one
                {
                    List<int> many = new List<int>();
                    List<int> one = new List<int>();
                    foreach (int index in manyToOneIndices)
                    {
                        usedTemplateIndices.Add(index);
                        many.Add(index);
                    }
                    one.Add(matchedIdx);
                    result.Add(new List<int>[] { many, one });
                    usedSampleIndices.Add(matchedIdx);
                    ti = many[many.Count - 1] + 1;
                }
            }

            return result;
        }
        /// <summary>
        /// one to one, one to many, or many to one, result is from template to sample strokes
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static List<List<int>[]> StrokeToStrokeCorrespondenceDifferentCountStartFromSample(List<SketchStroke> sample, List<SketchStroke> template)
        {
            List<List<int>[]> result = new List<List<int>[]>();
            HashSet<int> usedSampleIndices = new HashSet<int>();
            HashSet<int> usedTemplateIndices = new HashSet<int>();

            List<SketchStroke> sampleNormalized = SketchPreprocessing.Normalize(sample, 128, 500, new SketchPoint(0.0, 0.0));
            List<SketchStroke> templateNormalized = SketchPreprocessing.Normalize(template, 128, 500, new SketchPoint(0.0, 0.0));

            int si = 0;
            while (si < sample.Count && usedTemplateIndices.Count < template.Count)
            {
                Debug.WriteLine("si = " + si);

                if (usedSampleIndices.Contains(si))
                {
                    si++;
                    continue;
                }

                double oneToOneDis = double.MaxValue;
                int matchedIdx = -1;
                for (int ti = 0; ti < template.Count; ti++)
                {
                    if (usedTemplateIndices.Contains(ti))
                    {
                        continue;
                    }

                    double dis = SketchTools.HausdorffDistance(sampleNormalized[si], templateNormalized[ti]);

                    if (dis < oneToOneDis)
                    {
                        oneToOneDis = dis;
                        matchedIdx = ti;
                    }
                }

                usedSampleIndices.Add(si);
                usedTemplateIndices.Add(matchedIdx);

                List<int> oneToManyIndices = new List<int>();
                oneToManyIndices.Add(matchedIdx);
                List<int> manyToOneIndices = new List<int>();
                manyToOneIndices.Add(si);

                SketchStroke currSampleStroke = sampleNormalized[si];
                double oneToManyDis = oneToOneDis;

                int preSample = si - 1;
                int postSample = si + 1;
                while (preSample >= 0)
                {
                    if (usedSampleIndices.Contains(preSample))
                    {
                        break;
                    }

                    SketchStroke concatenatedStroke = SketchStroke.ConcatenateStrokes(sampleNormalized[preSample], currSampleStroke);
                    double dis = SketchTools.HausdorffDistance(concatenatedStroke, templateNormalized[matchedIdx]);

                    if (dis < oneToManyDis)
                    {
                        oneToManyDis = dis;
                        oneToManyIndices.Insert(0, preSample);
                        currSampleStroke = concatenatedStroke;
                    }
                    else
                    {
                        break;
                    }

                    preSample--;
                }
                while (postSample < sample.Count)
                {
                    if (usedSampleIndices.Contains(postSample))
                    {
                        break;
                    }

                    SketchStroke concatenatedStroke = SketchStroke.ConcatenateStrokes(currSampleStroke, sampleNormalized[postSample]);
                    double dis = SketchTools.HausdorffDistance(concatenatedStroke, templateNormalized[matchedIdx]);

                    if (dis < oneToManyDis)
                    {
                        oneToManyDis = dis;
                        oneToManyIndices.Add(postSample);
                        currSampleStroke = concatenatedStroke;
                    }
                    else
                    {
                        break;
                    }

                    postSample++;
                }

                SketchStroke currTemplateStroke = templateNormalized[matchedIdx];
                double manyToOneDis = oneToOneDis;

                int preTemplate = matchedIdx - 1;
                int postTemplate = matchedIdx + 1;
                while (preTemplate >= 0)
                {
                    if (usedTemplateIndices.Contains(preTemplate))
                    {
                        break;
                    }

                    SketchStroke concatenatedStroke = SketchStroke.ConcatenateStrokes(templateNormalized[preTemplate], currTemplateStroke);
                    double dis = SketchTools.HausdorffDistance(concatenatedStroke, sampleNormalized[si]);

                    if (dis < manyToOneDis)
                    {
                        manyToOneDis = dis;
                        manyToOneIndices.Insert(0, preTemplate);
                        currTemplateStroke = concatenatedStroke;
                    }
                    else
                    {
                        break;
                    }

                    preTemplate--;
                }
                while (postTemplate < template.Count)
                {
                    if (usedTemplateIndices.Contains(postTemplate))
                    {
                        break;
                    }

                    SketchStroke concatenatedStroke = SketchStroke.ConcatenateStrokes(currTemplateStroke, templateNormalized[postTemplate]);
                    double dis = SketchTools.HausdorffDistance(concatenatedStroke, sampleNormalized[si]);

                    if (dis < manyToOneDis)
                    {
                        manyToOneDis = dis;
                        manyToOneIndices.Add(postTemplate);
                        currTemplateStroke = concatenatedStroke;
                    }
                    else
                    {
                        break;
                    }

                    postTemplate++;
                }

                Debug.WriteLine("matched index = " + matchedIdx);

                int flag = -2; // -1 for oneToMany, 0 for oneToOne, 1 for manyToOne
                if (oneToManyDis < oneToOneDis && manyToOneDis < oneToOneDis)
                {
                    if (oneToManyDis < manyToOneDis)
                    {
                        flag = -1;
                    }
                    else
                    {
                        flag = 1;
                    }
                }
                else if (oneToManyDis < oneToOneDis)
                {
                    flag = -1;
                }
                else if (manyToOneDis < oneToOneDis)
                {
                    flag = 1;
                }
                else
                {
                    flag = 0;
                }

                if (flag == 0)
                {
                    List<int> oneTemplate = new List<int>();
                    List<int> oneSample = new List<int>();
                    oneTemplate.Add(matchedIdx);
                    oneSample.Add(si);
                    result.Add(new List<int>[] { oneTemplate, oneSample });
                    usedTemplateIndices.Add(matchedIdx);
                    usedSampleIndices.Add(matchedIdx);
                    si++;
                }
                else if (flag == -1) // one to many
                {
                    List<int> one = new List<int>();
                    List<int> many = new List<int>();
                    one.Add(matchedIdx);
                    usedTemplateIndices.Add(matchedIdx);
                    foreach (int index in oneToManyIndices)
                    {
                        usedSampleIndices.Add(index);
                        many.Add(index);
                    }
                    result.Add(new List<int>[] { one, many });
                    si = many[many.Count - 1] + 1;
                }
                else if (flag == 1) // many to one
                {
                    List<int> many = new List<int>();
                    List<int> one = new List<int>();
                    foreach (int index in manyToOneIndices)
                    {
                        usedTemplateIndices.Add(index);
                        many.Add(index);
                    }
                    one.Add(si);
                    result.Add(new List<int>[] { many, one });
                    usedSampleIndices.Add(matchedIdx);
                    si++;
                }
            }

            return result;
        }

        /*
        public static Dictionary<int, SketchStroke> StrokeToSegmentCorrespondence(List<SketchStroke> sample, List<SketchStroke> template)
        {
            Dictionary<int, SketchStroke> result = new Dictionary<int, SketchStroke>();

            List<SketchStroke> sampleNormalized = SketchPreprocessing.Normalize(sample, 128, 500, new SketchPoint(0.0, 0.0));
            List<SketchStroke> templateNormalized = SketchPreprocessing.Normalize(template, 128, 500, new SketchPoint(0.0, 0.0));
            Segmentation templateSegmentation = new Segmentation(templateNormalized);
            Segmentation sampleSegmentation = new Segmentation(sampleNormalized);

            HashSet<int> sampleSegmentTaken = new HashSet<int>();

            for (int i = 0; i < templateSegmentation.Segments.Count; i++)
            {
                double minDistance = double.MaxValue;
                int matchedIndex = -1;
                SketchStroke matchedSegment = new SketchStroke();

                for (int j = 0; j < sampleSegmentation.Segments.Count; j++)
                {
                    if (sampleSegmentTaken.Contains(j)) continue;

                    double dis = SketchTools.HausdorffDistance(templateSegmentation.Segments[i], sampleSegmentation.Segments[j]);

                    if (dis < minDistance)
                    {
                        minDistance = dis;
                        matchedIndex = j;
                        matchedSegment = sampleSegmentation.Segments[j];
                    }
                }

                for (int j = matchedIndex - 1; j >= 0; j--)
                {
                    if (sampleSegmentTaken.Contains(j)) break;


                }
            }

            return result;
        }
        */
    }
}