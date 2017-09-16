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
            this.dtwRecCount = 0;
            this.total = 0;

            this.ShowUserInformationDialog();

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

            // Load templates
            this.strokeTemplates = new Dictionary<string, Sketch>();
            this.templateImageFiles = new Dictionary<string, StorageFile>();
            Task taskLoad = Task.Run(async () => await this.LoadTemplates());
            taskLoad.Wait();

            Debug.WriteLine(this.strokeTemplates.Count);

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
                this.userName = dialog.UserName;
                this.userMotherLanguage = dialog.UserMotherLanguage;
                this.userGender = dialog.UserGender;
                this.userFluency = dialog.UserFluency;
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
            this.WritingInkCanvas.InkPresenter.StrokesCollected += StrokeInput_StrokesCollected;

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
        private async Task LoadTemplates()
        {
            StorageFolder localFolder = Package.Current.InstalledLocation;
            StorageFolder templatesFolder = await localFolder.GetFolderAsync("Templates");
            // StorageFolder strokeTemplateFolder = await templatesFolder.GetFolderAsync("StrokeData");
            StorageFolder strokeTemplateFolder = await templatesFolder.GetFolderAsync("ExpertStrokes");
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
            if (this.WritingInkCanvas.InkPresenter.StrokeContainer.GetStrokes().Count == 0 && this.IsSkritter == false)
            {
                this.LoadFeedback("wrong");
                return;
            }

            if (this.IsSkritter == false)
            {
                this.CaptureSketchStrokes();
            }

            this.WriteSampleXml();

            string answer = this.currentQuestion.Answer;

            // recognizes with $P
            PDollarClassifier pDollarClassifier = new PDollarClassifier(NumResampleForPDollar, SizeScaleForPDollar, new SketchPoint(0, 0), this.strokeTemplates);
            pDollarClassifier.run(this.sketchStrokes);
            List<string> resultLabels = pDollarClassifier.Labels;
            string result = resultLabels[resultLabels.Count - 1];

            // recognizes with BopoNoto
            BoponotoClassifier bpntClassifier = new BoponotoClassifier(NumResampleForPDollar, SizeScaleForPDollar, new SketchPoint(0, 0), this.strokeTemplates);
            bpntClassifier.run(this.sketchStrokes);
            List<string> bpntResultLabels = bpntClassifier.Labels;
            string bpntResult = bpntResultLabels[bpntResultLabels.Count - 1];

            // recognizes with dynamic time warping
            DtwClassifier dtwClassifier = new DtwClassifier(this.strokeTemplates, (int)this.WritingInkCanvas.ActualHeight);
            dtwClassifier.run(this.sketchStrokes);
            List<string> dtwResultLabels = dtwClassifier.Labels;
            string dtwResult = dtwResultLabels[dtwResultLabels.Count - 1];

            Debug.WriteLine("bpnt recognition result: " + bpntResultLabels[bpntResultLabels.Count - 1]);
            Debug.WriteLine("$p recognition result: " + resultLabels[resultLabels.Count - 1]);
            Debug.WriteLine("dtw recognition result: " + dtwResultLabels[dtwResultLabels.Count - 1]);

            //for (int i = dtwResultLabels.Count - 1; i >= 0; i--)
            //{
            //    Debug.Write(dtwResultLabels[i] + ", ");
            //}

            this.total++;

            if (result == answer)
            {
                this.pdollarRecCount++;
            }

            if (bpntResult == answer)
            {
                this.bpntRecCount++;
            }

            if (dtwResult == answer)
            {
                this.dtwRecCount++;
            }

            Debug.Write("P dollar success: " + this.pdollarRecCount + " ");
            Debug.Write("bpnt success: " + this.bpntRecCount + " ");
            Debug.Write("dtw success: " + this.dtwRecCount + " ");
            Debug.Write("total: " + this.total + "\n");

            if (answer == resultLabels[resultLabels.Count - 1] || 
                answer == resultLabels[resultLabels.Count - 2] || 
                answer == resultLabels[resultLabels.Count - 3])
            {
                this.bpntTechAssessor = new BoponotoTechniqueAssessor(sketchStrokes, currentTemplateSketchStrokes);

                this.techAssessor = new TechniqueAssessor(sketchStrokes, currentTemplateSketchStrokes);
                this.isWrittenCorrectly = techAssessor.IsCorrectOverall;

                this.EnablePlayButtons();
                this.LoadFeedback("technique");

                if (this.isWrittenCorrectly)
                {
                    this.visAssessor = new VisionAssessor(sketchStrokes, (int)this.WritingInkCanvas.ActualHeight, currentTemplateSketch.Strokes, currentTemplateSketch.FrameMaxX - currentTemplateSketch.FrameMinX);
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

            if (this.techAssessor.ConcatenatingCorrespondence != null)
            {
                this.DisablePlayButtons();
                InteractionTools.DemoStrokeCount(this.AnimationCanvas, this.WritingInkCanvas, this.techAssessor.ConcatenatingCorrespondence, "concatenating");
                this.EnablePlayButtons();
            }

            if (this.techAssessor.BrokenStrokeCorrespondence != null)
            {
                this.DisablePlayButtons();
                InteractionTools.DemoStrokeCount(this.AnimationCanvas, this.WritingInkCanvas, this.techAssessor.BrokenStrokeCorrespondence, "broken");
                this.EnablePlayButtons();
            }
        }

        private void StrokeOrderPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!techAssessor.IsCorrectStrokeCount)
            {
                return;
            }

            InteractionTools.DemoStrokeOrder(this.AnimationCanvas, this.WritingInkCanvas, this.techAssessor.StrokeToStrokeCorrespondenceSameCount);
            this.AnimationCanvas.Children.Clear();
        }

        private void StrokeDirectionPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!techAssessor.IsCorrectStrokeCount)
            {
                return;
            }

            this.AnimationCanvas.Children.Clear();
            this.DisablePlayButtons();
            InteractionTools.DemoStrokeDirection(this.AnimationCanvas, this.sketchStrokes, this.techAssessor.WrongDirectionStrokeIndices);
            this.EnablePlayButtons();
        }

        private void StrokeIntersectionPlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this.techAssessor.IsCorrectStrokeCount)
            {
                return;
            }

            this.AnimationCanvas.Children.Clear();

            int[] correspondance = this.techAssessor.StrokeToStrokeCorrespondenceSameCount;
            string[,] sampleIntersections = this.techAssessor.SampleIntersectionMatrix;
            string[,] templateIntersections = this.techAssessor.TemplateIntersectionMatrix;

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

                        SketchPoint intersection = SketchStrokeFeatureExtraction.Intersection(this.sketchStrokes[realI], this.sketchStrokes[realJ]);
                        if (intersection != null)
                        {
                            InteractionTools.HighlightWrongIntersection(this.AnimationCanvas, intersection);
                        }
                        else
                        {
                            InteractionTools.HighlightStrokes(this.AnimationCanvas, this.WritingInkCanvas, realI, realJ);
                        }
                    }
                }
            }
        }

        #endregion

        #region stroke interaction methods

        private void StrokeInput_StrokeStarted(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            UpdateTime(true, false);
        }

        private void StrokeInput_StrokeContinued(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            UpdateTime(false, false);
        }

        private void StrokeInput_StrokeEnded(InkStrokeInput sender, Windows.UI.Core.PointerEventArgs args)
        {
            UpdateTime(false, true);
        }

        private void StrokeInput_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            if (this.IsSkritter == true)
            {
                var strokes = this.WritingInkCanvas.InkPresenter.StrokeContainer.GetStrokes();
                SketchStroke lastStroke = new SketchStroke(strokes[strokes.Count - 1], this.timeCollection[this.timeCollection.Count - 1]);

                if (SkritterHelpers.ValidateStroke(lastStroke, this.currentTemplateSketchStrokes[this.sketchStrokes.Count]) == true)
                {
                    this.sketchStrokes.Add(lastStroke);
                    InteractionTools.ShowStroke(this.AnimationCanvas, this.currentTemplateSketchStrokes[this.sketchStrokes.Count - 1]);
                }
                else
                {
                    this.timeCollection.RemoveAt(this.timeCollection.Count - 1);
                }

                strokes[strokes.Count - 1].Selected = true;
                this.WritingInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
            }
        }

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
                    this.ShowWrongAnswerWarning();
                    break;
                case "technique":
                    this.ShowPlayButtons();
                    this.ShowTechniqueFeedback();
                    break;
                case "visual":
                    HidePlayButtons();
                    if (!isWrittenCorrectly)
                    {
                        this.ShowIncorrectWritingWarning();
                    }
                    else
                    {
                        this.ShowVisualFeedback();
                    }
                    break;
                default:
                    break;
            }
        }

        private void LoadQuestion(int questionIndex)
        {
            this.currentQuestion = this.questions[questionIndex];
            this.InstructionTextBlock.Text = this.currentQuestion.Text;
            this.currentTemplateSketch = this.strokeTemplates[this.currentQuestion.Answer];
            this.currentTemplateSketchStrokes = SketchPreprocessing.ScaleToFrame(currentTemplateSketch, writingFrameLength);
            this.LoadTemplateImage(this.currentQuestion.Answer);
            InteractionTools.ShowTemplateImage(this.SmallTemplateImage, this.currentImageTemplate);
        }

        private async void LoadTemplateImage(string answer)
        {
            this.currentImageTemplate = new BitmapImage();
            StorageFile currentImageTemplateFile = templateImageFiles[answer];
            FileRandomAccessStream stream = (FileRandomAccessStream) await currentImageTemplateFile.OpenAsync(FileAccessMode.Read);
            this.currentImageTemplate.SetSource(stream);
        }

        private async void ReadInTemplateXML(StorageFile file)
        {
            Sketch sketch = await XMLHelpers.XMLToSketch(file);
            strokeTemplates.Add(sketch.Label, sketch);
        }

        private async void WriteSampleXml()
        {
            StorageFolder saveFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFolder fluencyFolder = await saveFolder.CreateFolderAsync(this.userFluency, CreationCollisionOption.OpenIfExists);

            // Debug.WriteLine(fluencyFolder.Path);

            string fileName = this.userName + "_" + this.userGender + "_" + this.currentQuestion.Answer + "_" + DateTime.Now.Ticks + (this.IsSkritter ? "_skritter" : string.Empty) + ".xml";
            StorageFile userFile = await fluencyFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            XMLHelpers.SketchToXml(userFile, this.currentQuestion.Answer, this.userName, this.userMotherLanguage, this.userFluency, 0, 0, this.WritingInkCanvas.ActualWidth, this.WritingInkCanvas.ActualHeight, this.sketchStrokes);
        }

        private void UpdateTime(bool hasStarted, bool hasEnded)
        {
            if (hasStarted && hasEnded)
            {
                throw new Exception("Cannot start and end stroke at the same time.");
            }

            if (hasStarted)
            {
                this.times = new List<long>();
            }

            long time = DateTime.Now.Ticks - DateTimeOffset;
            this.times.Add(time);

            if (hasEnded)
            {
                this.timeCollection.Add(times);
            }
        }

        private void ShowTechniqueFeedback()
        {
            this.StrokeCountFeedbackTextBlock.Text = ("Stroke count: \n" + (techAssessor.IsCorrectStrokeCount ? "Correct" : "Incorrect") + "\n");
            this.StrokeOrderFeedbackTextBlock.Text = ("Stroke order: \n" + (techAssessor.IsCorrectStrokeOrder ? "Correct" : "Incorrect") + "\n");

            if (this.techAssessor.IsCorrectStrokeCount == true)
            {
                this.StrokeDirectionFeedbackTextBlock.Text = ("Stroke directions: " + "\n" + (techAssessor.IsCorrectStrokeDirection ? "Correct" : "Incorrect") + "\n");
                this.StrokeIntersectionFeedbackTextBlock.Text = ("Stroke intersections: " + "\n" + (techAssessor.IsCorrectIntersection ? "Correct" : "Incorrect") + "\n");
            }
            else
            {
                this.StrokeDirectionFeedbackTextBlock.Text = "Stroke directions: \nNA\n";
                this.StrokeIntersectionFeedbackTextBlock.Text = "Stroke intersections: \nNA\n";
                this.StrokeDirectionPlayButton.IsEnabled = false;
                this.StrokeIntersectionPlayButton.IsEnabled = false;
            }
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
                InteractionTools.ShowTemplateImage(this.TemplateImage, this.currentImageTemplate);
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

        private string userName;
        private string userGender;
        private string userMotherLanguage;
        private string userFluency;

        private int pdollarRecCount;
        private int bpntRecCount;
        private int dtwRecCount;
        private int total;

        #endregion

        #region read only fields

        private readonly bool IsSkritter = false;
        private readonly int NumResampleForPDollar = 128;
        private readonly double SizeScaleForPDollar = 500;

        #endregion
    }
}
