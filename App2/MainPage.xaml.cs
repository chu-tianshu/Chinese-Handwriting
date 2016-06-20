using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
            timeCollection = new List<List<long>>();
            strokeTemplates = new Dictionary<string, List<SketchStroke>>();
            sketchStrokes = new List<SketchStroke>();

            loadTemplates();
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

            List<SketchStroke> sketchStrokesResampledForCornerFinding = new List<SketchStroke>();

            foreach(SketchStroke curSketchStroke in sketchStrokes)
            {
                SketchStroke curResampledForCornerFinding = SketchStrokeFeatureExtraction.ResampleForCornerFinding(curSketchStroke);
                List<SketchPoint> corners = SketchStrokeFeatureExtraction.FindCorners(curResampledForCornerFinding);

                cornersList.Add(corners);
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
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

        #endregion

        #region fields

        private List<long> times;
        private List<List<long>> timeCollection;
        private Dictionary<string, List<SketchStroke>> strokeTemplates;
        private List<SketchStroke> sketchStrokes;

        #endregion
    }
}
