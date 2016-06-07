using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
            this.InitializeComponent();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;

            myInkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse 
                | Windows.UI.Core.CoreInputDeviceTypes.Pen
                | Windows.UI.Core.CoreInputDeviceTypes.Touch;
        }

        #endregion

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            myInkCanvas.InkPresenter.StrokeContainer.Clear();
        }

        private void undoButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void finishButton_Click(object sender, RoutedEventArgs e)
        {

        }

        #region fields

        private List<long> myTimes;
        private List<List<long>> myTimeCollection;

        #endregion
    }
}
