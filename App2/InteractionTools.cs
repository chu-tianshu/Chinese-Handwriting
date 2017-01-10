using System;
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
        public static List<Storyboard> Animate(Canvas canvas, List<List<SketchPoint>> strokes, double pointSize, int duration)
        {
            List<Storyboard> storyboards = new List<Storyboard>();

            int currentTime = 0;

            foreach (List<SketchPoint> points in strokes)
            {
                Storyboard sb = new Storyboard();

                Ellipse animator = new Ellipse()
                {
                    Width = pointSize,
                    Height = pointSize,
                    Fill = new SolidColorBrush(Colors.Green),
                };

                Canvas.SetLeft(animator, -pointSize / 2);
                Canvas.SetTop(animator, -pointSize / 2);
                canvas.Children.Add(animator);

                animator.RenderTransform = new CompositeTransform();

                DoubleAnimationUsingKeyFrames translateXAnimation = new DoubleAnimationUsingKeyFrames();
                DoubleAnimationUsingKeyFrames translateYAnimation = new DoubleAnimationUsingKeyFrames();

                for (int i = 0; i < points.Count; i++)
                {
                    KeyTime keyTime = new TimeSpan(currentTime);
                    EasingDoubleKeyFrame xFrame = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = points[i].X };
                    EasingDoubleKeyFrame yFrame = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = points[i].Y };

                    translateXAnimation.KeyFrames.Add(xFrame);
                    translateYAnimation.KeyFrames.Add(yFrame);

                    currentTime += duration;
                }

                Storyboard.SetTarget(translateXAnimation, animator);
                Storyboard.SetTarget(translateYAnimation, animator);
                Storyboard.SetTargetProperty(translateXAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
                Storyboard.SetTargetProperty(translateYAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");

                sb.Children.Add(translateXAnimation);
                sb.Children.Add(translateYAnimation);

                storyboards.Add(sb);
            }

            return storyboards;
        }
    }
}