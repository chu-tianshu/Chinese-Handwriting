using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App2
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Initializers

        public MainPage()
        {
            InitializeComponent();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;

            myInkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse 
                | Windows.UI.Core.CoreInputDeviceTypes.Pen
                | Windows.UI.Core.CoreInputDeviceTypes.Touch;
        }

        #endregion

        #region button interations

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            myInkCanvas.InkPresenter.StrokeContainer.Clear();
            myTimeCollection = new List<List<long>>();
        }

        private void undoButton_Click(object sender, RoutedEventArgs e)
        {
            var strokes = myInkCanvas.InkPresenter.StrokeContainer.GetStrokes();

            if (strokes.Count != 0)
            {
                strokes[strokes.Count - 1].Selected = true;
                myInkCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                myTimeCollection.RemoveAt(strokes.Count - 1);
            }
        }

        private void finishButton_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region helper methods

        private async void ReadInTemplateXML(StorageFile file)
        {
            string label = "";
            List<SketchStroke> sketch = new List<SketchStroke>();

            // create a new XML document
            // get the text from the XML file
            // load the file's text into an XML document 
            string text = await FileIO.ReadTextAsync(file);
            XDocument document = XDocument.Parse(text);

            label = document.Root.Attribute("label").Value;

            // itereate through each stroke element
            foreach (XElement element in document.Root.Elements())
            {
                // initialize the stroke
                SketchStroke stroke = new SketchStroke();

                // iterate through each point element
                double x, y;
                long t;

                foreach (XElement pointElement in element.Elements())
                {
                    x = Double.Parse(pointElement.Attribute("x").Value);
                    y = Double.Parse(pointElement.Attribute("y").Value);
                    t = Int64.Parse(pointElement.Attribute("time").Value);

                    SketchPoint point = new SketchPoint(x, y, t);

                    stroke.AppendPoint(point);
                }

                sketch.Add(stroke);
            }

            templates.Add(label, sketch);
        }

        #endregion

        #region fields

        private List<long> myTimes;
        private List<List<long>> myTimeCollection;
        private Dictionary<string, List<SketchStroke>> templates;

        #endregion
    }
}
