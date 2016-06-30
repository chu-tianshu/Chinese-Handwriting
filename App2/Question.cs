using System.Diagnostics;

namespace App2
{
    public class Question
    {
        #region initializers

        public Question(string id, string text, string answer)
        {
            Id = id;
            Text = text;
            Answer = answer;
        }

        #endregion

        #region properties

        public string Id { get; private set; }
        public string Text { get; private set; }
        public string Answer { get; private set; }

        #endregion
    }
}