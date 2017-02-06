using System.Collections.Generic;

namespace App2
{
    public class BoundingBox
    {
        public BoundingBox(List<SketchStroke> strokes)
        {
            MinX = double.MaxValue;
            MinY = double.MaxValue;
            MaxX = double.MinValue;
            MaxY = double.MinValue;

            foreach (SketchStroke stroke in strokes)
            {
                foreach (SketchPoint point in stroke.Points)
                {
                    MinX = MinX < point.X ? MinX : point.X;
                    MinY = MinY < point.Y ? MinY : point.Y;
                    MaxX = MaxX > point.X ? MaxX : point.X;
                    MaxY = MaxY > point.Y ? MaxY : point.Y;
                }
            }

            CenterX = (MinX + MaxX) / 2.0;
            CenterY = (MinY + MaxY) / 2.0;
        }

        public BoundingBox(SketchStroke stroke)
        {
            MinX = double.MaxValue;
            MinY = double.MaxValue;
            MaxX = double.MinValue;
            MaxY = double.MinValue;

            foreach (SketchPoint point in stroke.Points)
            {
                MinX = MinX < point.X ? MinX : point.X;
                MinY = MinY < point.Y ? MinY : point.Y;
                MaxX = MaxX > point.X ? MaxX : point.X;
                MaxY = MaxY > point.Y ? MaxY : point.Y;
            }

            CenterX = (MinX + MaxX) / 2.0;
            CenterY = (MinY + MaxY) / 2.0;
        }

        public BoundingBox(double minX, double minY, double maxX, double maxY)
        {

            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;

            CenterX = (MinX + MaxX) / 2.0;
            CenterY = (MinY + MaxY) / 2.0;
        }

        public bool Contains(SketchPoint point)
        {
            double x = point.X;
            double y = point.Y;

            return (x < MaxX && x > MinX && y < MaxX && y > MinY);
        }

        #region properties

        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public double CenterX { get; }
        public double CenterY { get; }
        public SketchPoint Center { get { return (new SketchPoint(CenterX, CenterY)); } }
        public SketchPoint TopLeft { get { return new SketchPoint(MinX, MinY); } }
        public SketchPoint TopRight { get { return new SketchPoint(MaxX, MinY); } }
        public SketchPoint BottomLeft { get { return new SketchPoint(MinX, MaxY); } }
        public SketchPoint BottomRight { get { return new SketchPoint(MaxX, MaxY); } }
        public double Width { get { return (MaxX - MinX); } }
        public double Height { get { return (MaxY - MinY); } }
        public double Diagonal { get { return (MathHelpers.EuclideanDistance(TopLeft, BottomRight)); } }

        #endregion
    }
}