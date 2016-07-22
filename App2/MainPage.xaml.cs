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
            strokeTemplates = new Dictionary<string, Sketch>();
            loadTemplates();
        }

        private void InitializeQuestions()
        {
            questions = new List<Question>();
            loadQuestions();
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            double writingBorderHeight = WritingBorder.ActualHeight;
            double writingBorderWidth = WritingBorder.ActualWidth;
            writingFrameLength = writingBorderHeight < writingBorderWidth ? writingBorderHeight : writingBorderWidth;

            WritingBorder.Height = WritingBorder.Width = writingFrameLength;

            timeCollection = new List<List<long>>();
            sketchStrokes = new List<SketchStroke>();
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
            isWrittenCorrectly = false;
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            var strokes = WritingInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            if (strokes.Count != 0)
            {
                strokes[strokes.Count - 1].Selected = true;
                WritingInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                timeCollection.RemoveAt(timeCollection.Count - 1);
            }

            isWrittenCorrectly = false;
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            string answer = currentQuestion.Answer;

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

            if (answer == resultLabels[resultLabels.Count - 1] || 
                answer == resultLabels[resultLabels.Count - 2] || 
                answer == resultLabels[resultLabels.Count - 3])
            {
                currentTemplate = strokeTemplates[answer];

                techAssessor = new TechniqueAssessor(sketchStrokes, currentTemplate.Strokes);

                isWrittenCorrectly = techAssessor.IsCorrectOverall;

                LoadFeedback("technique");

                if (isWrittenCorrectly)
                {
                    visAssessor = new VisionAssessor(sketchStrokes, (int) writingFrameLength, currentTemplate.Strokes, currentTemplate.FrameMaxX - currentTemplate.FrameMinX);
                }
            }
            else
            {
                LoadFeedback("wrong");
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;

            if (currentQuestionIndex == 0) index = questions.Count - 1;
            else index = currentQuestionIndex - 1;

            LoadQuestion(index);

            WritingInkCanvas.InkPresenter.StrokeContainer.Clear();
            timeCollection = new List<List<long>>();
            sketchStrokes = new List<SketchStroke>();
            FeedbackTextBlock.Text = "";
            isWrittenCorrectly = false;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            int index = 0;

            if (currentQuestionIndex == questions.Count - 1) index = 0;
            else index = currentQuestionIndex + 1;

            LoadQuestion(index);

            WritingInkCanvas.InkPresenter.StrokeContainer.Clear();
            timeCollection = new List<List<long>>();
            sketchStrokes = new List<SketchStroke>();
            FeedbackTextBlock.Text = "";
            isWrittenCorrectly = false;
        }

        private void VisualFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFeedback("visual");
        }

        private void TechniqueFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFeedback("technique");
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

        private void LoadFeedback(string option)
        {
            switch(option)
            {
                case "wrong":

                    FeedbackTextBlock.Text = "Wrong answer";

                    break;

                case "technique":

                    FeedbackTextBlock.FontSize = 38;

                    FeedbackTextBlock.Text = "";
                    FeedbackTextBlock.Text += ("Stroke count: " + (techAssessor.IsCorrectStrokeCount ? "Correct" : "Incorrect") + "\n");
                    FeedbackTextBlock.Text += ("Stroke order: " + (techAssessor.IsCorrectStrokeOrder ? "Correct" : "Incorrect") + "\n");
                    FeedbackTextBlock.Text += ("Stroke directions: " + (techAssessor.IsCorrectStrokeDirection ? "Correct" : "Incorrect") + "\n");

                    if (techAssessor.IsCorrectStrokeCount == true && techAssessor.IsCorrectStrokeDirection == false)
                    {
                        Debug.Write("Wrong stroke: " + techAssessor.wrongDirectionStrokeIndices[0]);

                        var strokes = WritingInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

                        foreach (int wrongStrokeIndex in techAssessor.wrongDirectionStrokeIndices)
                        {
                            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
                            drawingAttributes.Color = Colors.Red;
                            drawingAttributes.PenTip = PenTipShape.Circle;
                            drawingAttributes.Size = new Size(20, 20);

                            strokes[wrongStrokeIndex].DrawingAttributes = drawingAttributes;
                        }
                    }

                    break;

                case "visual":

                    if (!isWrittenCorrectly) FeedbackTextBlock.Text = "Please try to write the character correctly first";
                    else
                    {
                        FeedbackTextBlock.Text = "";
                    }

                    break;

                default:

                    break;
            }
        }

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
            /* Creates a new XML document
             * Gets the text from the XML file
             * Loads the file's text into an XML document 
             */
            string text = await FileIO.ReadTextAsync(file);
            XDocument document = XDocument.Parse(text);

            int minX = Int32.Parse(document.Root.Attribute("frameMinX").Value);
            int maxX = Int32.Parse(document.Root.Attribute("frameMaxX").Value);
            int minY = Int32.Parse(document.Root.Attribute("frameMinY").Value);
            int maxY = Int32.Parse(document.Root.Attribute("frameMaxY").Value);
            string label = document.Root.Attribute("label").Value;
            List<SketchStroke> strokes = new List<SketchStroke>();

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

                strokes.Add(stroke);
            }

            Sketch sketch = new Sketch(minX, maxX, minY, maxY, label, strokes);

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
        private double writingFrameLength;
        private Dictionary<string, Sketch> strokeTemplates;
        private List<SketchStroke> sketchStrokes;
        private PDollarClassifier pDollarClassifier;
        private List<Question> questions;
        private Question currentQuestion;
        private Sketch currentTemplate;
        private bool isWrittenCorrectly;
        private int currentQuestionIndex;
        private TechniqueAssessor techAssessor;
        private VisionAssessor visAssessor;

        #endregion
    }
}
