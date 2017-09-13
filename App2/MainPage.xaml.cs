using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            this.pdollarRecCount = 0;
            this.bpntRecCount = 0;
            this.total = 0;

            // this.ShowUserInformationDialog();

            InitializeComponent();
            InitializeWritingInkCanvas();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
        }

        private void MyPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Adjust canvas size
            double writingBorderHeight = WritingBorder.ActualHeight;
            double writingBorderWidth = WritingBorder.ActualWidth;
            this.writingFrameLength = writingBorderHeight < writingBorderWidth ? writingBorderHeight : writingBorderWidth;
            this.WritingBorder.Height = WritingBorder.Width = writingFrameLength;

            // Load questions
            this.LoadQuestions(out questionFiles);
            this.questions = new List<Question>();
            foreach (StorageFile questionFile in questionFiles)
            {
                Question question = null;
                Task task = Task.Run(async () => question = await XMLHelpers.XMLToQuestion(questionFile));
                task.Wait();

                this.questions.Add(question);
            }

            this.currentQuestionIndex = 0;
            this.LoadQuestion(0);

            // Load templates
            this.strokeTemplates = new Dictionary<string, Sketch>();
            this.templateImageFiles = new Dictionary<string, StorageFile>();
            this.LoadTemplates();

            // Initialize user input
            this.timeCollection = new List<List<long>>();
            this.sketchStrokes = new List<SketchStroke>();

            this.HidePlayButtons();
        }

        private async void ShowUserInformationDialog()
        {
            UserInformationDialog dialog = new UserInformationDialog();
            ContentDialogResult result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                this.UserName = dialog.UserName;
                this.UserMotherLanguage = dialog.UserMotherLanguage;
                this.UserFluency = dialog.UserFluency;
            }
        }

        private void InitializeWritingInkCanvas()
        {
            this.WritingInkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse 
                | Windows.UI.Core.CoreInputDeviceTypes.Pen 
                | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            this.WritingInkCanvas.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            this.WritingInkCanvas.InkPresenter.StrokeInput.StrokeContinued += StrokeInput_StrokeContinued;
            this.WritingInkCanvas.InkPresenter.StrokeInput.StrokeEnded += StrokeInput_StrokeEnded;

            this.StrokeVisuals = new InkDrawingAttributes();
            this.StrokeVisuals.Color = Colors.Purple;
            this.StrokeVisuals.IgnorePressure = true;
            this.StrokeVisuals.PenTip = PenTipShape.Circle;
            this.StrokeVisuals.Size = new Size(20, 20);
            this.WritingInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(StrokeVisuals);
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
            foreach (StorageFile file in files)
            {
                targetFiles.Add(file);
            }
        }

        /// <summary>
        /// Load both sketch and image templates
        /// </summary>
        private async void LoadTemplates()
        {
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
            this.currentQuestionIndex = this.currentQuestionIndex == 0 ? this.questions.Count - 1 : this.currentQuestionIndex - 1;
            this.LoadQuestion(this.currentQuestionIndex);
            this.Clear();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            this.currentQuestionIndex = this.currentQuestionIndex == this.questions.Count - 1 ? 0 : this.currentQuestionIndex + 1;
            this.LoadQuestion(currentQuestionIndex);
            this.Clear();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            this.Clear();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            this.AnimationCanvas.Children.Clear();

            var strokes = this.WritingInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            if (strokes.Count != 0)
            {
                strokes[strokes.Count - 1].Selected = true;
                this.WritingInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                this.timeCollection.RemoveAt(timeCollection.Count - 1);
            }

            this.isWrittenCorrectly = false;
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WritingInkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count == 0)
            {
                this.ShowWrongAnswerWarning();
            }

            this.CaptureSketchStrokes();
            // this.WriteSampleXml();

            string answer = currentQuestion.Answer;
            this.LoadTemplateImage(answer);

            // recognizes with $P
            PDollarClassifier pDollarClassifier = new PDollarClassifier(NumResampleForPDollar, SizeScaleForPDollar, new SketchPoint(0, 0), this.strokeTemplates);
            pDollarClassifier.run(this.sketchStrokes);
            List<string> resultLabels = pDollarClassifier.Labels;
            string result = resultLabels[resultLabels.Count - 1];

            // recognizes with BopoNoto
            BopoNotoClassifier bpntClassifier = new BopoNotoClassifier(NumResampleForPDollar, SizeScaleForPDollar, new SketchPoint(0, 0), this.strokeTemplates);
            bpntClassifier.run(this.sketchStrokes);
            List<string> bpntResultLabels = bpntClassifier.Labels;
            string bpntResult = bpntResultLabels[bpntResultLabels.Count - 1];

            //Debug.WriteLine("bpnt recognition result: " + bpntResultLabels[bpntResultLabels.Count - 1]);
            //Debug.WriteLine("recognition result: " + resultLabels[resultLabels.Count - 1]);

            this.total++;

            if (result == answer)
            {
                this.pdollarRecCount++;
            }

            if (bpntResult == answer)
            {
                this.bpntRecCount++;
            }

            Debug.Write("P dollar success: " + this.pdollarRecCount + " ");
            Debug.Write("bpnt success: " + this.bpntRecCount + " ");
            Debug.Write("total: " + this.total + "\n");

            if (answer == resultLabels[resultLabels.Count - 1] || 
                answer == resultLabels[resultLabels.Count - 2] || 
                answer == resultLabels[resultLabels.Count - 3])
            {
                this.currentTemplateSketch = strokeTemplates[answer];
                this.currentTemplateSketchStrokes = SketchPreprocessing.ScaleToFrame(currentTemplateSketch, writingFrameLength);

                this.bpntTechAssessor = new BoponotoTechniqueAssessor(sketchStrokes, currentTemplateSketchStrokes);

                this.techAssessor = new TechniqueAssessor(sketchStrokes, currentTemplateSketchStrokes);
                this.isWrittenCorrectly = techAssessor.IsCorrectOverall;

                this.LoadFeedback("technique");

                if (this.isWrittenCorrectly)
                {
                    this.visAssessor = new VisionAssessor(sketchStrokes, (int)writingFrameLength, currentTemplateSketch.Strokes, currentTemplateSketch.FrameMaxX - currentTemplateSketch.FrameMinX);
                }
            }
            else
            {
                this.LoadFeedback("wrong");
            }
        }

        private void VisualFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            this.LoadFeedback("visual");
        }

        private void TechniqueFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            this.LoadFeedback("technique");
        }

        private void StrokeCountPlayButton_Click(object sender, RoutedEventArgs e)
        {
            this.AnimationCanvas.Children.Clear();

            if (!this.techAssessor.IsCorrectStrokeCount)
            {
                this.DisablePlayButtons();
                InteractionTools.DemoStrokeCount(this.AnimationCanvas, this.WritingInkCanvas, this.sketchStrokes, this.techAssessor.StrokeToStrokeCorrespondenceDifferentCount);
                this.EnablePlayButtons();
            }
        }

        private void StrokeOrderPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!techAssessor.IsCorrectStrokeCount)
            {
                return;
            }

            this.AnimationCanvas.Children.Clear();
        }

        private void StrokeDirectionPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!techAssessor.IsCorrectStrokeCount)
            {
                return;
            }

            AnimationCanvas.Children.Clear();
            DisablePlayButtons();
            InteractionTools.DemoStrokeDirection(this.AnimationCanvas, this.sketchStrokes, this.techAssessor.WrongDirectionStrokeIndices);
            EnablePlayButtons();
        }

        private void StrokeIntersectionPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.techAssessor.IsCorrectStrokeCount)
            {
                return;
            }

            this.AnimationCanvas.Children.Clear();

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
                            if (correspondance[index] == i)
                            {
                                realI = index;
                            }
                            if (correspondance[index] == j)
                            {
                                realJ = index;
                            }
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

        /// <summary>
        /// Store stroke data in variable this.sketchStokes
        /// </summary>
        private void CaptureSketchStrokes()
        {
            var strokes = WritingInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            for (int i = 0; i < strokes.Count; i++)
            {
                SketchStroke curSketchStroke = new SketchStroke(strokes.ElementAt(i), timeCollection.ElementAt(i));
                this.sketchStrokes.Add(curSketchStroke);
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

        private async void WriteSampleXml()
        {
            StorageFolder saveFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFolder fluencyFolder = await saveFolder.CreateFolderAsync(this.UserFluency, CreationCollisionOption.OpenIfExists);

            Debug.WriteLine(fluencyFolder.Path);

            string fileName = this.UserName + "_" + this.currentQuestion.Answer + "_" + DateTime.Now.Ticks + ".xml";
            StorageFile userFile = await fluencyFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            XMLHelpers.SketchToXml(userFile, this.currentQuestion.Answer, this.UserName, this.UserMotherLanguage, this.UserFluency, 0, 0, this.WritingInkCanvas.ActualWidth, this.WritingInkCanvas.ActualHeight, this.sketchStrokes);
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
        private List<StorageFile> questionFiles;
        private List<Question> questions;
        private Question currentQuestion;
        private Sketch currentTemplateSketch;
        private List<SketchStroke> currentTemplateSketchStrokes;
        private BitmapImage currentImageTemplate;
        private bool isWrittenCorrectly;
        private int currentQuestionIndex;
        private BoponotoTechniqueAssessor bpntTechAssessor;
        private TechniqueAssessor techAssessor;
        private VisionAssessor visAssessor;
        private string UserName;
        private string UserMotherLanguage;
        private string UserFluency;

        private int pdollarRecCount;
        private int bpntRecCount;
        private int total;

        #endregion

        #region read only fields

        private readonly int NumResampleForPDollar = 128;
        private readonly double SizeScaleForPDollar = 500;

        #endregion
    }
}
