using System;
using System.Collections.Generic;

namespace App2
{
    public class VisionFeatureExtraction
    {
        public static bool[,] SketchToArray(List<SketchStroke> strokes, int length)
        {
            bool[,] array = new bool[length, length];

            foreach (SketchStroke stroke in strokes) foreach (SketchPoint point in stroke.Points) array[(int)point.X, (int)point.Y] = true;

            return array;
        }

        public static bool[,] Scale(bool[,] array, int scaledLength)
        {
            bool[,] scaled = new bool[scaledLength, scaledLength];

            int origLength = array.GetLength(0);

            double ratio = scaledLength * 1.0 / origLength;

            for (int i = 0; i < origLength; i++)
            {
                for (int j = 0; j < origLength; j++)
                {
                    int newI = (int)(i * ratio);
                    int newJ = (int)(j * ratio);

                    if (array[i, j]) scaled[newI, newJ] = true;
                }
            }

            return scaled;
        }

        public static SketchPoint Centroid(bool[,] array)
        {
            int pointCount = 0;
            double meanX = 0.0;
            double meanY = 0.0;

            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    if (array[i, j])
                    {
                        pointCount++;
                        meanX += j;
                        meanY += i;
                    }
                }
            }

            meanX /= pointCount;
            meanY /= pointCount;

            return new SketchPoint(meanX, meanY);
        }

        public static BoundingBox BoundingRectangle(bool[,] array)
        {
            double minX = Double.MaxValue;
            double maxX = Double.MinValue;
            double minY = Double.MaxValue;
            double maxY = Double.MinValue;

            for (int i = 0; i < array.GetLength(0); i++)
                for (int j = 0; j < array.GetLength(1); j++)
                    if (array[i, j])
                    {
                        minX = j < minX ? j : minX;
                        maxX = j > maxX ? j : maxX;
                        minY = i < minY ? i : minY;
                        maxY = i > maxY ? i : maxY;
                    }

            return new BoundingBox(minX, minY, maxX, maxY);
        }

        public static int[] HorizontalProjection(bool[,] array)
        {
            int h = array.GetLength(0);

            int[] projection = new int[h];

            for (int i = 0; i < h; i++) projection[i] = 0;

            for (int i = 0; i < array.GetLength(0); i++) for (int j = 0; j < array.GetLength(1); j++) if (array[i, j]) projection[i]++;

            return projection;
        }

        public static int[] VerticalProjection(bool[,] array)
        {
            int w = array.GetLength(1);

            int[] projection = new int[w];

            for (int i = 0; i < w; i++) projection[i] = 0;

            for (int i = 0; i < array.GetLength(0); i++) for (int j = 0; j < array.GetLength(1); j++) if (array[i, j]) projection[j]++;

            return projection;
        }

        public static double ProjectionDifference(int[] samplePro, int[] templatePro)
        {
            int sumOfDiff = 0;
            int sumOfSum = 0;

            for (int i = 0; i < samplePro.GetLength(0); i++)
            {
                sumOfDiff += Math.Abs(samplePro[i] - templatePro[i]);
                sumOfSum += Math.Abs(samplePro[i] + templatePro[i]);
            }

            return (sumOfDiff * 1.0 / sumOfSum);
        }

        public static double SymmetryFeature(int[] samplePro, int[] templatePro)
        {
            int spDiffSym = 0;
            int tpltDiffSym = 0;

            int width = samplePro.GetLength(0);

            for (int i = 0; i < width / 2 - 1; i++)
            {
                spDiffSym += (samplePro[i] - samplePro[width - 1 - i]);
                tpltDiffSym += (templatePro[i] - templatePro[width - 1 - i]);
            }

            return (Math.Abs(spDiffSym) - Math.Abs(tpltDiffSym)) / (width * 1.0);
        }
    }
}