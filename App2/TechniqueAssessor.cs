using System.Collections.Generic;

namespace App2
{
    public class TechniqueAssessor
    {
        #region initializers

        public TechniqueAssessor(List<SketchStroke> sample, List<SketchStroke> template)
        {
            IsCorrectStrokeCount = (sample.Count == template.Count);

            if (IsCorrectStrokeCount == false) IsCorrectStrokeOrder = false;
            else
            {

            }
        }

        #endregion

        #region properties

        public bool IsCorrectStrokeCount { get; private set; }
        public bool IsCorrectStrokeOrder { get; private set; }

        #endregion
    }
}