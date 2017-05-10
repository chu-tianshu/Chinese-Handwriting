using System.Collections.Generic;

namespace App2
{
    public class Segmentation
    {
        #region initializers
        public Segmentation()
        {
            Segments = new List<SketchStroke>();
            StrokeSegmentations = new List<List<SketchStroke>>();
        }

        public Segmentation(List<SketchStroke> strokes)
        {
            Segments = new List<SketchStroke>();
            StrokeSegmentations = new List<List<SketchStroke>>();

            for (int i = 0; i < strokes.Count; i++)
            {
                List<SketchStroke> currSegments = ShortStraw.FindStrokeSegments(strokes[i]);
                Segments.AddRange(currSegments);
                StrokeSegmentations.Add(currSegments);
                for (int j = 0; j < currSegments.Count; j++) SegmentStrokeIndices.Add(i);
            }
        }
        #endregion

        #region properties
        public List<SketchStroke> Segments { get; private set; } // The list of stroke segments that form the sketch
        public List<List<SketchStroke>> StrokeSegmentations { get; private set; }  // The ith list in StrokeSegmentations contains the stroke segments that form the ith stroke in the sketch
        public List<int> SegmentStrokeIndices { get; private set; } // The ith element is the index of the stroke in the sketch that the ith segment belongs to
        #endregion
    }
}