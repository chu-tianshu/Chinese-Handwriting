using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace App2
{
    public sealed partial class MainPage : Page
    {
        #region Initializers

        public MainPage()
        {
            InitializeComponent();
            InitializeWritingInkCanvas();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
        }

        private void InitializeTemplates()
        {
            strokeTemplates = new Dictionary<string, Sketch>();
            templateImageFiles = new Dictionary<string, StorageFile>();

            LoadTemplates();
        }


        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            double writingBorderHeight = WritingBorder.ActualHeight;
            double writingBorderWidth = WritingBorder.ActualWidth;
            writingFrameLength = writingBorderHeight < writingBorderWidth ? writingBorderHeight : writingBorderWidth;

            WritingBorder.Height = WritingBorder.Width = writingFrameLength;

            LoadQuestions(out questionFiles);
            questions = new List<Question>();
            foreach (StorageFile questionFile in questionFiles)
            {
                Question question = null;
                Task task = Task.Run(async () => question = await ReadInQuestionXML(questionFile));
                task.Wait();

                questions.Add(question);
            }

            InitializeTemplates();

            timeCollection = new List<List<long>>();
            sketchStrokes = new List<SketchStroke>();

            currentQuestionIndex = 0;

            LoadQuestion(0);

            HidePlayButtons();
        }

        private void LoadQuestions(out List<StorageFile> targetFiles)
        {
            Task task;

            StorageFolder folder = null;
            task = Task.Run(async () => folder = await Package.Current.InstalledLocation.GetFolderAsync("Questions"));
            task.Wait();

            IReadOnlyList<StorageFile> files = null;
            task = Task.Run(async () => files = await folder.GetFilesAsync());
            task.Wait();

            targetFiles = new List<StorageFile>();
            foreach (StorageFile file in files) targetFiles.Add(file);
        }

        private async void LoadTemplates()
        {
            StorageFolder localFolder = Package.Current.InstalledLocation;
            StorageFolder templatesFolder = await localFolder.GetFolderAsync("Templates");
            StorageFolder strokeTemplateFolder = await templatesFolder.GetFolderAsync("StrokeData");
            StorageFolder imageTemplateFolder = await templatesFolder.GetFolderAsync("Images");

            var strokeTemplateFileList = await strokeTemplateFolder.GetFilesAsync();
            foreach (var strokeTemplateFile in strokeTemplateFileList) ReadInTemplateXML(strokeTemplateFile);

            var templateImageFileList = await imageTemplateFolder.GetFilesAsync();
            foreach (var templateImageFile in templateImageFileList) templateImageFiles.Add(RemoveExtension(templateImageFile.Name), templateImageFile);
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
            StrokeVisuals.PenTip = PenTipShape.Rectangle;
            StrokeVisuals.Size = new Size(20, 20);

            WritingInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);
        }

        #endregion

        #region button interations

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Clear();
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

            PointTranslateForPDollar = new SketchPoint(0, 0);
            pDollarClassifier = new PDollarClassifier(NumResampleForPDollar, SizeScaleForPDollar, PointTranslateForPDollar, strokeTemplates);
            pDollarClassifier.run(sketchStrokes);
            List<string> resultLabels = pDollarClassifier.Labels;

            #endregion

            if (answer == resultLabels[resultLabels.Count - 1] || 
                answer == resultLabels[resultLabels.Count - 2] || 
                answer == resultLabels[resultLabels.Count - 3])
            {
                currentTemplateSketch = strokeTemplates[answer];

                techAssessor = new TechniqueAssessor(sketchStrokes, currentTemplateSketch.Strokes);

                isWrittenCorrectly = techAssessor.IsCorrectOverall;

                LoadFeedback("technique");

                if (isWrittenCorrectly)
                {
                    LoadTemplateImage(answer);

                    visAssessor = new VisionAssessor(sketchStrokes, (int) writingFrameLength, currentTemplateSketch.Strokes, currentTemplateSketch.FrameMaxX - currentTemplateSketch.FrameMinX);
                }
            }
            else
            {
                LoadFeedback("wrong");
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            currentQuestionIndex = currentQuestionIndex == 0 ? questions.Count - 1 : currentQuestionIndex - 1;
            LoadQuestion(currentQuestionIndex);

            Clear();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            currentQuestionIndex = currentQuestionIndex == questions.Count - 1 ? 0 : currentQuestionIndex + 1;
            LoadQuestion(currentQuestionIndex);

            Clear();
        }

        private void VisualFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFeedback("visual");
        }

        private void TechniqueFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFeedback("technique");
        }

        // Stroke count button
        private void FeedbackPlayButton1_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Button 1 pressed");
        }

        // Stroke order button
        private void FeedbackPlayButton2_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Button 2 pressed");
        }

        // Stroke direction button
        private void FeedbackPlayButton3_Click(object sender, RoutedEventArgs e)
        {
            List<int> wrongStrokeIndices = techAssessor.WrongDirectionStrokeIndices;

            Debug.WriteLine("Number of wrong direction strokes: " + wrongStrokeIndices.Count);

            List<List<SketchPoint>> solutionStrokeTraces = new List<List<SketchPoint>>();

            foreach (int index in wrongStrokeIndices)
            {
                Debug.WriteLine(index);

                List<SketchPoint> origPoints = sketchStrokes[index].Points;
                List<SketchPoint> reversed = new List<SketchPoint>();
                for (int i = origPoints.Count - 1; i >= 0; i--) reversed.Add(origPoints[i]);
                solutionStrokeTraces.Add(reversed);
            }

            List<Storyboard> storyboards = InteractionTools.Animate(AnimationCanvas, solutionStrokeTraces, AnimationPointSize, DirectionAnimationDuration);

            foreach (var sb in storyboards) sb.Begin();
        }

        // Stroke intersection button
        private void FeedbackPlayButton4_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Button 4 pressed");

            int[] correspondance = techAssessor.Correspondance;

            string[,] sampleIntersections = techAssessor.SampleIntersectionMatrix;
            string[,] templateIntersections = techAssessor.TemplateIntersectionMatrix;

            for (int i = 0; i < sampleIntersections.GetLength(0); i++)
            {
                for (int j = 0; j < sampleIntersections.GetLength(0); j++)
                {
                    if (i == j) continue;

                    if (sampleIntersections[i, j] != templateIntersections[i, j])
                    {
                        int realI = 0;
                        int realJ = 0;

                        for (int index = 0; index < correspondance.GetLength(0); index++)
                        {
                            if (correspondance[index] == i) realI = index;
                            if (correspondance[index] == j) realJ = index;
                        }

                        SketchPoint intersection = SketchStrokeFeatureExtraction.Intersection(sketchStrokes[realI], sketchStrokes[realJ]);

                        if (intersection != null)
                        {
                            // Highlights the wrong intersection
                            
                            Ellipse circle = new Ellipse()
                            {
                                Height = 50,
                                Width = 50,
                                Stroke = new SolidColorBrush(Colors.Red),
                                StrokeThickness = 10,
                            };

                            Canvas.SetLeft(circle, intersection.X - circle.Width / 2);
                            Canvas.SetTop(circle, intersection.Y - circle.Height / 2);

                            AnimationCanvas.Children.Add(circle);
                        }
                        else
                        {
                            // Highlights the location where the intersection should be

                            
                        }
                    }
                }
            }
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
                    FeedbackTextBlock1.Text = "Wrong answer";

                    break;

                case "technique":
                    ShowPlayButtons();

                    FeedbackTextBlock1.Text = ("Stroke count: " + "\n" + (techAssessor.IsCorrectStrokeCount ? "Correct" : "Incorrect") + "\n");
                    FeedbackTextBlock2.Text = ("Stroke order: " + "\n" + (techAssessor.IsCorrectStrokeOrder ? "Correct" : "Incorrect") + "\n");
                    FeedbackTextBlock3.Text = ("Stroke directions: " + "\n" + (techAssessor.IsCorrectStrokeDirection ? "Correct" : "Incorrect") + "\n");
                    FeedbackTextBlock4.Text = ("Stroke intersections: " + "\n" + (techAssessor.IsCorrectIntersection ? "Correct" : "Incorrect") + "\n");

                    break;

                case "visual":
                    HidePlayButtons();

                    if (!isWrittenCorrectly)
                    {
                        FeedbackTextBlock1.Text = "Please try to write the character correct";
                        FeedbackTextBlock2.Text = "";
                        FeedbackTextBlock3.Text = "";
                        FeedbackTextBlock4.Text = "";
                    }
                    else
                    {
                        FeedbackTextBlock1.Text = "Location: " + visAssessor.LocationFeedback + "\n";
                        FeedbackTextBlock2.Text = "Shape: " + visAssessor.ShapeFeedback + "\n";
                        FeedbackTextBlock3.Text = "Distribution: " + visAssessor.ProjectionFeedback + "\n";
                        FeedbackTextBlock4.Text = "";
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

        private async void LoadTemplateImage(string answer)
        {
            currentImageTemplate = new BitmapImage();

            StorageFile currentImageTemplateFile = templateImageFiles[answer];

            FileRandomAccessStream stream = (FileRandomAccessStream)await currentImageTemplateFile.OpenAsync(FileAccessMode.Read);

            currentImageTemplate.SetSource(stream);
        }

        private async Task<Question> ReadInQuestionXML(StorageFile file)
        {
            string fileText = await FileIO.ReadTextAsync(file);
            XDocument document = XDocument.Parse(fileText);

            string id = document.Root.Attribute("id").Value;
            string text = document.Root.Attribute("text").Value;
            string answer = document.Root.Attribute("answer").Value;

            return new Question(id, text, answer);
        }

        private async void ReadInTemplateXML(StorageFile file)
        {
            /**
             * Creates a new XML document
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

        private string RemoveExtension(string fileName)
        {
            int dotLocation = 0;

            for (int i = fileName.Length - 1; i >= 0; i--) if (fileName[i] == '.') dotLocation = i;

            return fileName.Substring(0, dotLocation);
        }

        private void ShowPlayButtons()
        {
            FeedbackPlayButton1.Visibility = Visibility.Visible;
            FeedbackPlayButton2.Visibility = Visibility.Visible;
            FeedbackPlayButton3.Visibility = Visibility.Visible;
            FeedbackPlayButton4.Visibility = Visibility.Visible;
        }

        private void HidePlayButtons()
        {
            FeedbackPlayButton1.Visibility = Visibility.Collapsed;
            FeedbackPlayButton2.Visibility = Visibility.Collapsed;
            FeedbackPlayButton3.Visibility = Visibility.Collapsed;
            FeedbackPlayButton4.Visibility = Visibility.Collapsed;
        }

        private void Clear()
        {
            WritingInkCanvas.InkPresenter.StrokeContainer.Clear();
            timeCollection = new List<List<long>>();
            sketchStrokes = new List<SketchStroke>();
            isWrittenCorrectly = false;
            FeedbackTextBlock1.Text = "";
            FeedbackTextBlock2.Text = "";
            FeedbackTextBlock3.Text = "";
            FeedbackTextBlock4.Text = "";
            HidePlayButtons();
            AnimationCanvas.Children.Clear();
        }

        #endregion

        #region properties

        private long DateTimeOffset { get; set; }
        private InkDrawingAttributes StrokeVisuals { get; set; }
        private SketchPoint PointTranslateForPDollar { get; set; }

        #endregion

        #region fields

        private List<long> times;
        private List<List<long>> timeCollection;
        private double writingFrameLength;
        private Dictionary<string, Sketch> strokeTemplates;
        private Dictionary<string, StorageFile> templateImageFiles;
        private List<SketchStroke> sketchStrokes;
        private PDollarClassifier pDollarClassifier;
        private List<StorageFile> questionFiles;
        private List<Question> questions;
        private Question currentQuestion;
        private Sketch currentTemplateSketch;
        private BitmapImage currentImageTemplate;
        private bool isWrittenCorrectly;
        private int currentQuestionIndex;
        private TechniqueAssessor techAssessor;
        private VisionAssessor visAssessor;

        #endregion

        #region read only fields

        private readonly int NumResampleForPDollar = 128;
        private readonly double SizeScaleForPDollar = 500;
        private readonly double AnimationPointSize = 30;
        private readonly long DirectionAnimationDuration = 200000;

        #endregion
    }
}
