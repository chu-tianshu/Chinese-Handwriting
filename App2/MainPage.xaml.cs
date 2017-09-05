using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

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

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Adjust canvas size
            double writingBorderHeight = WritingBorder.ActualHeight;
            double writingBorderWidth = WritingBorder.ActualWidth;
            writingFrameLength = writingBorderHeight < writingBorderWidth ? writingBorderHeight : writingBorderWidth;
            WritingBorder.Height = WritingBorder.Width = writingFrameLength;

            // Load questions
            LoadQuestions(out questionFiles);
            questions = new List<Question>();
            foreach (StorageFile questionFile in questionFiles)
            {
                Question question = null;
                Task task = Task.Run(async () => question = await XMLHelpers.XMLToQuestion(questionFile));
                task.Wait();

                questions.Add(question);
            }

            currentQuestionIndex = 0;
            LoadQuestion(0);

            // Load templates
            strokeTemplates = new Dictionary<string, Sketch>();
            templateImageFiles = new Dictionary<string, StorageFile>();
            LoadTemplates();

            // Initialize user input
            timeCollection = new List<List<long>>();
            sketchStrokes = new List<SketchStroke>();

            HidePlayButtons();
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
            StrokeVisuals.Color = Colors.Purple;
            StrokeVisuals.IgnorePressure = true;
            StrokeVisuals.PenTip = PenTipShape.Circle;
            StrokeVisuals.Size = new Size(20, 20);

            WritingInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);
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
            /*
             * Loads both sketch and image templates
             **/
            StorageFolder localFolder = Package.Current.InstalledLocation;
            StorageFolder templatesFolder = await localFolder.GetFolderAsync("Templates");
            StorageFolder strokeTemplateFolder = await templatesFolder.GetFolderAsync("StrokeData");
            StorageFolder imageTemplateFolder = await templatesFolder.GetFolderAsync("Images");

            var strokeTemplateFileList = await strokeTemplateFolder.GetFilesAsync();
            foreach (var strokeTemplateFile in strokeTemplateFileList)
            {
                ReadInTemplateXML(strokeTemplateFile);
            }

            var templateImageFileList = await imageTemplateFolder.GetFilesAsync();
            foreach (var templateImageFile in templateImageFileList)
            {
                templateImageFiles.Add(XMLHelpers.RemoveExtension(templateImageFile.Name), templateImageFile);
            }
        }

        #endregion

        #region button interations

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

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            AnimationCanvas.Children.Clear();

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
            CaptureSketchStrokes();

            string answer = currentQuestion.Answer;
            LoadTemplateImage(answer);

            #region recognizes using $P

            pDollarClassifier = new PDollarClassifier(NumResampleForPDollar, SizeScaleForPDollar, new SketchPoint(0, 0), strokeTemplates);
            pDollarClassifier.run(sketchStrokes);
            List<string> resultLabels = pDollarClassifier.Labels;

            #endregion

            #region finds corners with shortstraw

            foreach (var stroke in sketchStrokes)
            {
                var corners = ShortStraw.FindCorners(stroke);

                foreach (var point in corners)
                {
                    InteractionTools.HighlightWrongIntersection(AnimationCanvas, point);
                }
            }

            #endregion

            if (answer == resultLabels[resultLabels.Count - 1] || 
                answer == resultLabels[resultLabels.Count - 2] || 
                answer == resultLabels[resultLabels.Count - 3])
            {
                currentTemplateSketch = strokeTemplates[answer];
                currentTemplateSketchStrokes = SketchPreprocessing.ScaleToFrame(currentTemplateSketch, writingFrameLength);

                techAssessor = new TechniqueAssessor(sketchStrokes, currentTemplateSketchStrokes);
                isWrittenCorrectly = techAssessor.IsCorrectOverall;

                LoadFeedback("technique");

                if (isWrittenCorrectly) visAssessor = new VisionAssessor(sketchStrokes, (int) writingFrameLength, currentTemplateSketch.Strokes, currentTemplateSketch.FrameMaxX - currentTemplateSketch.FrameMinX);
            }
            else
            {
                LoadFeedback("wrong");
            }
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
        private void StrokeCountPlayButton_Click(object sender, RoutedEventArgs e)
        {
            AnimationCanvas.Children.Clear();

            InteractionTools.ShowTemplateImage(TemplateImage, currentImageTemplate);
            InteractionTools.DemoTemplate(AnimationCanvas, currentTemplateSketchStrokes);
        }

        private void StrokeOrderPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!techAssessor.IsCorrectStrokeCount)
            {
                return;
            }

            AnimationCanvas.Children.Clear();
        }

        private void StrokeDirectionPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!techAssessor.IsCorrectStrokeCount)
            {
                return;
            }

            AnimationCanvas.Children.Clear();
            DisablePlayButtons();
            InteractionTools.DemoCorrectStrokes(AnimationCanvas, sketchStrokes, techAssessor.WrongDirectionStrokeIndices);
            EnablePlayButtons();
        }

        private void StrokeIntersectionPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!techAssessor.IsCorrectStrokeCount)
            {
                return;
            }

            AnimationCanvas.Children.Clear();

            int[] correspondance = techAssessor.StrokeToStrokeCorrespondenceSameCount;

            string[,] sampleIntersections = techAssessor.SampleIntersectionMatrix;
            string[,] templateIntersections = techAssessor.TemplateIntersectionMatrix;

            for (int i = 0; i < sampleIntersections.GetLength(0); i++)
            {
                for (int j = 0; j < sampleIntersections.GetLength(0); j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

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
                            InteractionTools.HighlightWrongIntersection(AnimationCanvas, intersection);
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

        private void StrokeInput_StrokeStarted(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args) { UpdateTime(true, false); }
        private void StrokeInput_StrokeContinued(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args) { UpdateTime(false, false); }
        private void StrokeInput_StrokeEnded(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args) { UpdateTime(false, true); }

        #endregion

        #region helper methods

        private void CaptureSketchStrokes()
        {
            var strokes = WritingInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            for (int i = 0; i < strokes.Count; i++)
            {
                SketchStroke curSketchStroke = new SketchStroke(strokes.ElementAt(i), timeCollection.ElementAt(i));
                sketchStrokes.Add(curSketchStroke);
            }
        }

        private void LoadFeedback(string option)
        {
            switch (option)
            {
                case "wrong":
                    ShowWrongAnswerWarning();
                    break;
                case "technique":
                    ShowPlayButtons();
                    ShowTechniqueFeedback();
                    break;
                case "visual":
                    HidePlayButtons();
                    if (!isWrittenCorrectly)
                    {
                        ShowIncorrectWritingWarning();
                    }
                    else
                    {
                        ShowVisualFeedback();
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
            FileRandomAccessStream stream = (FileRandomAccessStream) await currentImageTemplateFile.OpenAsync(FileAccessMode.Read);
            currentImageTemplate.SetSource(stream);
        }

        private async void ReadInTemplateXML(StorageFile file)
        {
            Sketch sketch = await XMLHelpers.XMLToSketch(file);
            strokeTemplates.Add(sketch.Label, sketch);
        }

        private void UpdateTime(bool hasStarted, bool hasEnded)
        {
            if (hasStarted && hasEnded)
            {
                throw new Exception("Cannot start and end stroke at the same time.");
            }

            if (hasStarted)
            {
                times = new List<long>();
            }

            long time = DateTime.Now.Ticks - DateTimeOffset;
            times.Add(time);

            if (hasEnded)
            {
                timeCollection.Add(times);
            }
        }

        private void ShowTechniqueFeedback()
        {
            StrokeCountFeedbackTextBlock.Text = ("Stroke count: " + "\n" + (techAssessor.IsCorrectStrokeCount ? "Correct" : "Incorrect") + "\n");
            StrokeOrderFeedbackTextBlock.Text = ("Stroke order: " + "\n" + (techAssessor.IsCorrectStrokeOrder ? "Correct" : "Incorrect") + "\n");
            StrokeDirectionFeedbackTextBlock.Text = ("Stroke directions: " + "\n" + (techAssessor.IsCorrectStrokeDirection ? "Correct" : "Incorrect") + "\n");
            StrokeIntersectionFeedbackTextBlock.Text = ("Stroke intersections: " + "\n" + (techAssessor.IsCorrectIntersection ? "Correct" : "Incorrect") + "\n");
        }

        private void ShowVisualFeedback()
        {
            InteractionTools.ShowTemplateImage(TemplateImage, currentImageTemplate);

            StrokeCountFeedbackTextBlock.Text = "Location: " + visAssessor.LocationFeedback + "\n";
            StrokeOrderFeedbackTextBlock.Text = "Shape: " + visAssessor.ShapeFeedback + "\n";
            StrokeDirectionFeedbackTextBlock.Text = "Distribution: " + visAssessor.ProjectionFeedback + "\n";
            StrokeIntersectionFeedbackTextBlock.Text = "";
        }

        private void ShowPlayButtons()
        {
            StrokeCountPlayButton.Visibility = Visibility.Visible;
            StrokeOrderPlayButton.Visibility = Visibility.Visible;
            StrokeDirectionPlayButton.Visibility = Visibility.Visible;
            StrokeIntersectionPlayButton.Visibility = Visibility.Visible;
        }

        private void HidePlayButtons()
        {
            StrokeCountPlayButton.Visibility = Visibility.Collapsed;
            StrokeOrderPlayButton.Visibility = Visibility.Collapsed;
            StrokeDirectionPlayButton.Visibility = Visibility.Collapsed;
            StrokeIntersectionPlayButton.Visibility = Visibility.Collapsed;
        }

        private void EnablePlayButtons()
        {
            StrokeCountPlayButton.IsEnabled = true;
            StrokeOrderPlayButton.IsEnabled = true;
            StrokeDirectionPlayButton.IsEnabled = true;
            StrokeIntersectionPlayButton.IsEnabled = true;
        }

        private void DisablePlayButtons()
        {
            StrokeCountPlayButton.IsEnabled = false;
            StrokeOrderPlayButton.IsEnabled = false;
            StrokeDirectionPlayButton.IsEnabled = false;
            StrokeIntersectionPlayButton.IsEnabled = false;
        }

        private void ClearFeedbackTextBlocks()
        {
            StrokeCountFeedbackTextBlock.Text = "";
            StrokeOrderFeedbackTextBlock.Text = "";
            StrokeDirectionFeedbackTextBlock.Text = "";
            StrokeIntersectionFeedbackTextBlock.Text = "";
        }

        private void Clear()
        {
            timeCollection = new List<List<long>>();
            sketchStrokes = new List<SketchStroke>();
            isWrittenCorrectly = false;
            ClearFeedbackTextBlocks();
            HidePlayButtons();
            WritingInkCanvas.InkPresenter.StrokeContainer.Clear();
            AnimationCanvas.Children.Clear();
            TemplateImage.Source = null;
        }

        private async void ShowWrongAnswerWarning()
        {
            var wrongAnswerWarning = new MessageDialog("Your answer is wrong");

            var retryCommand = new UICommand("Retry") { Id = 0 };
            var showAnswerCommand = new UICommand("Show answer") { Id = 1 };

            wrongAnswerWarning.Commands.Add(retryCommand);
            wrongAnswerWarning.Commands.Add(showAnswerCommand);

            var result = await wrongAnswerWarning.ShowAsync();
            if (result == retryCommand)
            {
                Clear();
            }
            if (result == showAnswerCommand)
            {
                InteractionTools.ShowTemplateImage(TemplateImage, currentImageTemplate);
            }
        }

        private async void ShowIncorrectWritingWarning()
        {
            var incorrectWritingWarning = new MessageDialog("Please try to write the character correctly.");
            incorrectWritingWarning.Commands.Add(new UICommand("Ok") { Id = 0 });
            incorrectWritingWarning.DefaultCommandIndex = 0;
            await incorrectWritingWarning.ShowAsync();
        }

        #endregion

        #region properties

        private long DateTimeOffset { get; set; }
        private InkDrawingAttributes StrokeVisuals { get; set; }

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
        private List<SketchStroke> currentTemplateSketchStrokes;
        private BitmapImage currentImageTemplate;
        private bool isWrittenCorrectly;
        private int currentQuestionIndex;
        private TechniqueAssessor techAssessor;
        private VisionAssessor visAssessor;

        #endregion

        #region read only fields

        private readonly int NumResampleForPDollar = 128;
        private readonly double SizeScaleForPDollar = 500;

        #endregion
    }
}
