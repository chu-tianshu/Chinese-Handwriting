using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace App2
{
    public sealed partial class MainPage : Page
    {
        #region Initializers

        public MainPage()
        {
            InitializeComponent();
            InitializeQuestions();
            InitializeTemplates();
            InitializeWritingInkCanvas();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
        }

        private void InitializeTemplates()
        {
            strokeTemplates = new Dictionary<string, List<SketchStroke>>();
            loadTemplates();
        }

        private void InitializeQuestions()
        {
            questions = new List<Question>();
            loadQuestions();
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            timeCollection = new List<List<long>>();
            sketchStrokes = new List<SketchStroke>();

            double writingBorderHeight = WritingBorder.ActualHeight;
            double writingBorderWidth = WritingBorder.ActualWidth;
            double writingBorderLength = writingBorderHeight < writingBorderWidth ? writingBorderHeight : writingBorderWidth;

            WritingBorder.Height = WritingBorder.Width = writingBorderLength;

            currentQuestionIndex = 0;

            LoadQuestion(currentQuestionIndex);
        }

        private async void loadQuestions()
        {
            StorageFolder localFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFolder questionsFolder = await localFolder.GetFolderAsync("Questions");

            var questionFiles = await questionsFolder.GetFilesAsync();
            foreach (var questionFile in questionFiles) ReadInQuestionXML(questionFile);
        }

        private async void loadTemplates()
        {
            StorageFolder localFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFolder templatesFolder = await localFolder.GetFolderAsync("Templates");
            StorageFolder strokeTemplateFolder = await templatesFolder.GetFolderAsync("StrokeData");
            StorageFolder imageTemplateFolder = await templatesFolder.GetFolderAsync("Images");

            var strokeTemplateFiles = await strokeTemplateFolder.GetFilesAsync();
            foreach (var strokeTemplateFile in strokeTemplateFiles) ReadInTemplateXML(strokeTemplateFile);
        }

        private void InitializeWritingInkCanvas()
        {
            WritingInkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse 
                | Windows.UI.Core.CoreInputDeviceTypes.Pen 
                | Windows.UI.Core.CoreInputDeviceTypes.Touch;

            WritingInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            WritingInkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued;
            WritingInkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded;

            StrokeVisuals = new InkDrawingAttributes();
            StrokeVisuals.Color = Colors.Black;
            StrokeVisuals.IgnorePressure = true;
            StrokeVisuals.PenTip = PenTipShape.Circle;
            StrokeVisuals.Size = new Size(20, 20);

            WritingInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);
        }

        #endregion

        #region button interations

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            WritingInkCanvas.InkPresenter.StrokeContainer.Clear();
            timeCollection = new List<List<long>>();
            sketchStrokes = new List<SketchStroke>();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            var strokes = WritingInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            if (strokes.Count != 0)
            {
                strokes[strokes.Count - 1].Selected = true;
                WritingInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                sketchStrokes.RemoveAt(sketchStrokes.Count - 1);
                timeCollection.RemoveAt(timeCollection.Count - 1);
            }
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            var strokes = WritingInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            List<List<SketchPoint>> cornersList = new List<List<SketchPoint>>();

            for (int i = 0; i < strokes.Count; i++)
            {
                SketchStroke curSketchStroke = new SketchStroke();

                curSketchStroke.TimeStamp = timeCollection.ElementAt(i);

                var curInkPoints = strokes.ElementAt(i).GetInkPoints();

                for (int j = 0; j < curInkPoints.Count; j++)
                {
                    var curInkPoint = curInkPoints.ElementAt(j);

                    var curX = curInkPoint.Position.X;
                    var curY = curInkPoint.Position.Y;

                    SketchPoint curSketchPoint = new SketchPoint(curX, curY);

                    curSketchStroke.AppendPoint(curSketchPoint);
                }

                sketchStrokes.Add(curSketchStroke);
            }

            #region recognizes using $P

            NumResampleForPDollar = 128;
            SizeScaleForPDollar = 500;
            PointTranslateForPDollar = new SketchPoint(0, 0);
            pDollarClassifier = new PDollarClassifier(NumResampleForPDollar, SizeScaleForPDollar, PointTranslateForPDollar, strokeTemplates);
            pDollarClassifier.run(sketchStrokes);
            List<string> resultLabels = pDollarClassifier.Labels;

            #endregion

            string answer = currentQuestion.Answer;

            if (answer == resultLabels[resultLabels.Count - 1] || 
                answer == resultLabels[resultLabels.Count - 2] || 
                answer == resultLabels[resultLabels.Count - 3])
            {
                currentTemplate = strokeTemplates[answer];
            }
            else
            {
                FeedbackTextBlock.Text = "Wrong answer";
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;

            if (currentQuestionIndex == 0) index = questions.Count - 1;
            else index = currentQuestionIndex - 1;

            LoadQuestion(index);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;

            if (currentQuestionIndex == questions.Count - 1) index = 0;
            else index = currentQuestionIndex + 1;

            LoadQuestion(index);
        }

        private void VisualFeedbackButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TechniqueFeedbackButton_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region stroke interaction methods

        private void StrokeInput_StrokeStarted(Windows.UI.Input.Inking.InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            UpdateTime(true, false);
        }

        private void StrokeInput_StrokeContinued(Windows.UI.Input.Inking.InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            UpdateTime(false, false);
        }

        private void StrokeInput_StrokeEnded(Windows.UI.Input.Inking.InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            UpdateTime(false, true);
        }

        #endregion

        #region helper methods

        private void LoadQuestion(int questionIndex)
        {
            currentQuestion = questions[questionIndex];
            InstructionTextBlock.Text = currentQuestion.Text;
        }

        private async void ReadInQuestionXML(StorageFile file)
        {
            string fileText = await FileIO.ReadTextAsync(file);
            XDocument document = XDocument.Parse(fileText);

            string id = document.Root.Attribute("id").Value;
            string text = document.Root.Attribute("text").Value;
            string answer = document.Root.Attribute("answer").Value;

            questions.Add(new Question(id, text, answer));
        }

        private async void ReadInTemplateXML(StorageFile file)
        {
            string label = "";
            List<SketchStroke> sketch = new List<SketchStroke>();
            
            /* Creates a new XML document
             * Gets the text from the XML file
             * Loads the file's text into an XML document 
             */
            string text = await FileIO.ReadTextAsync(file);
            XDocument document = XDocument.Parse(text);

            label = document.Root.Attribute("label").Value;

            // Itereates through each stroke element
            foreach (XElement element in document.Root.Elements())
            {
                // Initializes the stroke
                SketchStroke stroke = new SketchStroke();

                // Iterates through each point element
                double x, y;
                long t;

                foreach (XElement pointElement in element.Elements())
                {
                    x = Double.Parse(pointElement.Attribute("x").Value);
                    y = Double.Parse(pointElement.Attribute("y").Value);
                    t = Int64.Parse(pointElement.Attribute("time").Value);

                    SketchPoint point = new SketchPoint(x, y);

                    stroke.AppendPoint(point);
                    stroke.AppendTime(t);
                }

                sketch.Add(stroke);
            }

            strokeTemplates.Add(label, sketch);
        }

        private void UpdateTime(bool hasStarted, bool hasEnded)
        {
            if (hasStarted && hasEnded) { throw new Exception("Cannot start and end stroke at the same time."); }

            if (hasStarted) { times = new List<long>(); }

            long time = DateTime.Now.Ticks - DateTimeOffset;
            times.Add(time);

            if (hasEnded) { timeCollection.Add(times); }
        }

        #endregion

        #region properties

        private long DateTimeOffset { get; set; }
        private InkDrawingAttributes StrokeVisuals { get; set; }
        private int NumResampleForPDollar { get; set; }
        private double SizeScaleForPDollar { get; set; }
        private SketchPoint PointTranslateForPDollar { get; set; }

        #endregion

        #region fields

        private List<long> times;
        private List<List<long>> timeCollection;
        private Dictionary<string, List<SketchStroke>> strokeTemplates;
        private List<SketchStroke> sketchStrokes;
        private PDollarClassifier pDollarClassifier;
        private List<Question> questions;
        private Question currentQuestion;
        private List<SketchStroke> currentTemplate;
        private int currentQuestionIndex;

        #endregion
    }
}
