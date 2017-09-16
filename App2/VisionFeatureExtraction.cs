using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace App2
{
    public class VisionFeatureExtraction
    {
        public static bool[,] SketchToArray(List<SketchStroke> strokes, int length)
        {
            bool[,] array = new bool[length, length];

            foreach (SketchStroke stroke in strokes)
            {
                foreach (SketchPoint point in stroke.Points)
                {
                    int currX = (int)point.X;
                    int currY = (int)point.Y;

                    for (int x = currX - 10; x < currX + 10; x++)
                    {
                        for (int y = currY - 10; y < currY + 10; y++)
                        {
                            if (IsInsideBoard(x, y, length))
                            {
                                array[x, y] = true;
                            }
                        }
                    }
                }
            }

            return array;
        }

        private static bool IsInsideBoard(SketchPoint point, int length)
        {
            return IsInsideBoard((int)point.X, (int)point.Y, length);
        }

        private static bool IsInsideBoard(int x, int y, int length)
        {
            return (x >= 0 && x <= length - 1 && y >= 0 && y <= length - 1);
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
                    if (array[i, j])
                    {
                        scaled[(int)(i * ratio), (int)(j * ratio)] = true;
                    }
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
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    if (array[i, j])
                    {
                        minX = j < minX ? j : minX;
                        maxX = j > maxX ? j : maxX;
                        minY = i < minY ? i : minY;
                        maxY = i > maxY ? i : maxY;
                    }
                }
            }

            return new BoundingBox(minX, minY, maxX, maxY);
        }

        public static int[] TrimProjection(int[] projection)
        {
            int startIndex = 0;
            while (projection[startIndex++] == 0) { }
            int endIndex = projection.Length - 1;
            while (projection[endIndex--] == 0) { }
            int[] trimmedProjection = new int[endIndex - startIndex + 1];
            for (int i = 0; i < trimmedProjection.Length; i++)
            {
                trimmedProjection[i] = projection[i + startIndex];
            }
            return trimmedProjection;
        }

        public static int[] HorizontalProjection(bool[,] array)
        {
            int h = array.GetLength(0);
            int[] projection = new int[h];
            for (int i = 0; i < h; i++)
            {
                projection[i] = 0;
            }
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    if (array[i, j])
                    {
                        projection[i]++;
                    }
                }
            }
            return projection;
        }

        public static int[] VerticalProjection(bool[,] array)
        {
            int w = array.GetLength(1);
            int[] projection = new int[w];
            for (int i = 0; i < w; i++)
            {
                projection[i] = 0;
            }
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    if (array[i, j])
                    {
                        projection[j]++;
                    }
                }
            }
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