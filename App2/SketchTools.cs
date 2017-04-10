using System;
using System.Collections.Generic;

namespace App2
{
    public class SketchTools
    {
        #region distance metrics

        public static double Distance(List<SketchStroke> alphaStrokeList, List<SketchStroke> betaStrokeList)
        {
            List<SketchPoint> alphaPoints = new List<SketchPoint>();
            List<SketchPoint> betaPoints = new List<SketchPoint>();

            foreach (SketchStroke alphaStroke in alphaStrokeList) alphaPoints.AddRange(alphaStroke.Points);
            foreach (SketchStroke betaStroke in betaStrokeList) betaPoints.AddRange(betaStroke.Points);

            return Distance(alphaPoints, betaPoints);
        }

        public static double Distance(SketchStroke alphaStroke, SketchStroke betaStroke)
        {
            List<SketchPoint> alphaPoints = new List<SketchPoint>();
            List<SketchPoint> betaPoints = new List<SketchPoint>();

            alphaPoints.AddRange(alphaStroke.Points);
            betaPoints.AddRange(betaStroke.Points);

            return Distance(alphaPoints, betaPoints);
        }

        public static double Distance(List<SketchPoint> alphaPoints, List<SketchPoint> betaPoints)
        {
            double distances = 0.0;

            var pairs = new List<Tuple<SketchPoint, SketchPoint>>();

            double minDistance, weight, distance;
            int index;

            SketchPoint minPoint = betaPoints[0];

            foreach (SketchPoint alphaPoint in alphaPoints)
            {
                minDistance = Double.MaxValue;

                // iterate through each beta point to find the min beta point to the alpha point
                index = 1;

                foreach (SketchPoint betaPoint in betaPoints)
                {
                    distance = MathHelpers.EuclideanDistance(alphaPoint, betaPoint);

                    // update the min distance and min point
                    if (minDistance > distance)
                    {
                        minDistance = distance;
                        minPoint = betaPoint;
                    }
                }

                // update distance between alpha and beta point lists
                weight = 1 - ((index - 1) / alphaPoints.Count);
                distances += minDistance * weight;

                // pair the alpha point to the min beta point and remove min beta point from list of beta points
                pairs.Add(new Tuple<SketchPoint, SketchPoint>(alphaPoint, minPoint));
                betaPoints.Remove(minPoint);
            }

            return distances;
        }

        public static double HausdorffDistance(SketchStroke strokeA, SketchStroke strokeB)
        {
            return (HausdorffDistance(strokeA.Points, strokeB.Points));
        }

        public static double HausdorffDistance(List<SketchPoint> pointSetA, List<SketchPoint> pointSetB)
        {
            return (Math.Max(DirectedHausdorffDistance(pointSetA, pointSetB), DirectedHausdorffDistance(pointSetB, pointSetA)));
        }

        public static double DirectedHausdorffDistance(List<SketchPoint> pointSetA, List<SketchPoint> pointSetB)
        {
            double maxDis = double.MinValue;

            foreach(SketchPoint pa in pointSetA)
            {
                double minDis = double.MaxValue;

                foreach(SketchPoint pb in pointSetB)
                {
                    double currentDistance = MathHelpers.EuclideanDistance(pa, pb);
                    if (currentDistance < minDis) minDis = currentDistance;
                }

                if (minDis > maxDis) maxDis = minDis;
            }

            return maxDis;
        }

        #endregion
    }
}