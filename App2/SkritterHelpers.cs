using System.Diagnostics;

namespace App2
{
    public class SkritterHelpers
    {
        public static bool ValidateStroke(SketchStroke sampleStroke, SketchStroke templateStroke)
        {
            Debug.WriteLine("distance: " + SketchTools.HausdorffDistance(sampleStroke, templateStroke));
            return SketchTools.HausdorffDistance(sampleStroke, templateStroke) < SkritterHelpers.StrokeValidationDistanceThreshold;
        }

        private static readonly int StrokeValidationDistanceThreshold = 100;
    }
}