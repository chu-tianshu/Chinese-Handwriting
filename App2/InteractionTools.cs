using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

namespace App2
{
    public class InteractionTools
    {
        public static List<Storyboard> Animate(Canvas canvas, List<List<SketchPoint>> strokes, double pointSize)
        {
            List<Storyboard> storyboards = new List<Storyboard>();

            foreach (List<SketchPoint> points in strokes)
            {
                Ellipse animator = new Ellipse()
                {
                    Width = pointSize,
                    Height = pointSize,
                    Fill = new SolidColorBrush(Colors.Green),
                };

                Canvas.SetLeft(animator, -pointSize / 2);
                Canvas.SetTop(animator, -pointSize / 2);


            }

            return storyboards;
        }
    }
}