using System;
using System.Collections.Generic;

namespace App2
{
    public class SketchPreprocessing
    {
        #region sketch preprocessing methods

        public static List<SketchStroke> Normalize(List<SketchStroke> strokes, int n, double size, SketchPoint origin)
        {
            List<SketchStroke> resampled = Resample(strokes, n);
            List<SketchStroke> scaled = ScaleSquare(resampled, size);
            List<SketchStroke> translated = TranslateCentroid(scaled, origin);

            return translated;
        }

        public static List<SketchStroke> Resample(List<SketchStroke> strokes, int n)
        {
            double totalLength = 0;

            foreach (SketchStroke stroke in strokes) totalLength += SketchStrokeFeatureExtraction.PathLength(stroke);

            double I = totalLength / (n - 1);
            double D = 0.0;

            List<SketchStroke> newStrokes = new List<SketchStroke>();

            // Iterates through each stroke points in a list of strokes
            int pointCount = 0;

            foreach (SketchStroke stroke in strokes)
            {
                /*
                 * Resamples point locations
                 */
                List<SketchPoint> points = stroke.Points;
                List<SketchPoint> newPoints = new List<SketchPoint>();

                newPoints.Add(new SketchPoint(points[0].X, points[0].Y));
                ++pointCount;

                bool isDone = false;

                for (int i = 1; i < points.Count; ++i)
                {
                    double d = MathHelpers.EuclideanDistance(points[i - 1], points[i]);

                    if (D + d >= I)
                    {
                        double qx = points[i - 1].X + ((I - D) / d) * (points[i].X - points[i - 1].X);
                        double qy = points[i - 1].Y + ((I - D) / d) * (points[i].Y - points[i - 1].Y);

                        if (pointCount < n - 1)
                        {
                            newPoints.Add(new SketchPoint(qx, qy));
                            ++pointCount;

                            points.Insert(i, new SketchPoint(qx, qy));
                            D = 0.0;
                        }
                        else
                        {
                            isDone = true;
                        }
                    }
                    else
                    {
                        D += d;
                    }

                    if (isDone) break;
                }

                D = 0.0;

                /*
                 * Resamples time stamps
                 */
                List<long> timeStamp = stroke.TimeStamp;
                List<long> newTimeStamp = new List<long>();

                int oldCount = timeStamp.Count;
                int newCount = newPoints.Count;

                for (int j = 0; j < newCount; ++j)
                {
                    int index = (int) ((double) j / newCount * oldCount);
                    newTimeStamp.Add(timeStamp[index]);
                }

                SketchStroke newStroke = new SketchStroke(newPoints, newTimeStamp);

                newStrokes.Add(newStroke);
            }

            return newStrokes;
        }

        public static List<SketchStroke> ScaleToFrame(Sketch sketch, double frameLength)
        {
            List<SketchStroke> result = new List<SketchStroke>();

            double templateFrameWidth = sketch.FrameMaxX - sketch.FrameMinX;
            double templateFrameHeight = sketch.FrameMaxY - sketch.FrameMinY;

            foreach (SketchStroke origStroke in sketch.Strokes)
            {
                List<SketchPoint> origPoints = origStroke.Points;
                List<SketchPoint> scaledPoints = new List<SketchPoint>();

                foreach (SketchPoint origPoint in origPoints)
                {
                    double scaledX = origPoint.X / templateFrameWidth * frameLength;
                    double scaledY = origPoint.Y / templateFrameHeight * frameLength;
                    SketchPoint scaledPoint = new SketchPoint(scaledX, scaledY);
                    scaledPoints.Add(scaledPoint);
                }

                SketchStroke scaledStroke = new SketchStroke(scaledPoints, origStroke.TimeStamp);
                result.Add(scaledStroke);
            }

            return result;
        }

        public static List<SketchStroke> ScaleSquare(List<SketchStroke> strokes, double size)
        {
            double width = SketchFeatureExtraction.Width(strokes);
            double height = SketchFeatureExtraction.Height(strokes);

            List<SketchStroke> newStrokes = new List<SketchStroke>();

            foreach (SketchStroke stroke in strokes)
            {
                List<SketchPoint> newPoints = new List<SketchPoint>();
                foreach (SketchPoint point in stroke.Points)
                {
                    newPoints.Add(new SketchPoint(point.X * size / width, point.Y * size / height));
                }
                SketchStroke newStroke = new SketchStroke(newPoints, stroke.TimeStamp);
                newStrokes.Add(newStroke);
            }

            return newStrokes;
        }

        public static List<SketchStroke> TranslateCentroid(List<SketchStroke> strokes, SketchPoint origin)
        {
            return Translate(strokes, origin, SketchFeatureExtraction.Centroid(strokes));
        }

        public static List<SketchStroke> Translate(List<SketchStroke> strokes, SketchPoint origin, SketchPoint centroid)
        {
            List<SketchStroke> newStrokes = new List<SketchStroke>();

            foreach (SketchStroke stroke in strokes)
            {
                List<SketchPoint> newPoints = new List<SketchPoint>();
                foreach (SketchPoint point in stroke.Points)
                {
                    newPoints.Add(new SketchPoint(point.X + origin.X - centroid.X, point.Y + origin.Y - centroid.Y));
                }
                SketchStroke newStroke = new SketchStroke(newPoints, stroke.TimeStamp);
                newStrokes.Add(newStroke);
            }

            return newStrokes;
        }

        #endregion

        #region read only fields

        private readonly int MergeDistanceThreshold = 10;

        #endregion
    }
}