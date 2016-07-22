using System.Collections.Generic;

namespace App2
{
    public class Sketch
    {
        public Sketch(int minX, int maxX, int minY, int maxY, string label, List<SketchStroke> strokes)
        {
            FrameMinX = minX;
            FrameMaxX = maxX;
            FrameMinY = minY;
            FrameMaxY = maxY;
            Label = label;
            Strokes = strokes;
        }

        #region properties

        public int FrameMinX { get; private set; }
        public int FrameMaxX { get; private set; }
        public int FrameMinY { get; private set; }
        public int FrameMaxY { get; private set; }
        public string Label { get; private set; }
        public List<SketchStroke> Strokes { get; private set; }

        #endregion
    }
}