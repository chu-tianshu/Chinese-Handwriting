namespace App2
{
    internal class SketchPoint
    {
        #region innitializers

        public SketchPoint() { }

        public SketchPoint(double xVal, double yVal, long tVal)
        {
            x = xVal;
            y = yVal;
            t = tVal;
        }

        #endregion

        #region setters

        public void SetLocation(double xVal, double yVal)
        {
            x = xVal;
            y = yVal;
        }

        public void SetTime(long tVal)
        {
            t = tVal;
        }

        #endregion

        #region fields

        double x;
        double y;
        long t;

        #endregion
    }
}