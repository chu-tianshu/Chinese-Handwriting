using System;
using System.Collections.Generic;

namespace App2
{
    public class PixelDataAnalysis
    {
        public static bool[,] CalculatePixelData(List<SketchStroke> sketchStrokes, int height, int width, double radius)
        {
            bool[,] pixelArray = new bool[height, width];

            foreach (SketchStroke stroke in sketchStrokes)
            {
                foreach (SketchPoint point in stroke.Points)
                {
                    int nRow = (int)point.Y;
                    int nCol = (int)point.X;

                    pixelArray[nRow, nCol] = true;

                    for (int i = (0 > nRow - radius - 1 ? 0 : nRow - (int) radius - 1); i < (height < nRow + radius + 1 ? height : nRow + radius + 1); i++)
                    {
                        for (int j = (0 > nCol - radius - 1 ? 0 : nCol - (int) radius - 1); j < (width < nCol + radius + 1 ? width : nCol + radius + 1); j++)
                        {
                            if ()
                        }
                    }
                }
            }

            return pixelArray;
        }
    }
}