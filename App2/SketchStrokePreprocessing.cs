using System.Collections.Generic;

namespace App2
{
    public class SketchStrokePreprocessing
    {
        public static SketchStroke ResampleStroke(SketchStroke stroke, int n)
        {
            SketchStroke resampledStroke = new SketchStroke();

            List<SketchPoint> points = new List<SketchPoint>();
            List<long> timeStamp = new List<long>();

            // Copies the points and timeStamps list of the initial stroke
            foreach (SketchPoint p in stroke.Points) points.Add(p);
            foreach (long t in stroke.TimeStamp) timeStamp.Add(t);

            double totalLength = SketchStrokeFeatureExtraction.PathLength(stroke);
            double increment = totalLength / (n - 1);
            double dist = 0;

            resampledStroke.AppendPoint(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                SketchPoint cur = points[i];
                SketchPoint pre = points[i - 1];

                double d = MathHelpers.EuclideanDistance(cur, pre);

                if (dist + d >= increment)
                {

                    double qx = pre.X + ((increment - dist) / d) * (cur.X - pre.X);
                    double qy = pre.Y + ((increment - dist) / d) * (cur.Y - pre.Y);

                    SketchPoint q = new SketchPoint(qx, qy);

                    resampledStroke.AppendPoint(q);
                    points.Insert(i, q);

                    dist = 0.0;
                }
                else
                {
                    dist = dist + d;
                }
            }

            int oldTimeCount = timeStamp.Count;
            int newTimeCount = resampledStroke.Points.Count;

            for (int j = 0; j < newTimeCount; ++j)
            {
                int index = (int) ((double) j / newTimeCount * oldTimeCount);
                resampledStroke.AppendTime(timeStamp[index]);
            }

            return resampledStroke;
        }
    }
}