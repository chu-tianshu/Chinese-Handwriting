namespace App2
{
    internal class SketchPoint
    {
        #region innitializers

        public SketchPoint() { }

        public SketchPoint(double xVal, double yVal)
        {
            x = xVal;
            y = yVal;
        }

        #endregion

        #region properties

        public double X
        {
            get { return x; }
            set { x = value; }
        }

        public double Y
        {
            get { return y; }
            set { y = value;}
        }

        #endregion

        #region fields

        double x;
        double y;

        #endregion
    }
}