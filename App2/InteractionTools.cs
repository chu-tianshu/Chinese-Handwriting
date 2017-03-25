using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace App2
{
    public class InteractionTools
    {
        public static void DemoTemplate(Canvas canvas, List<SketchStroke> template)
        {
            DemoCorrectStrokes(canvas, template);
        }

        public static void DemoCorrectStrokes(Canvas canvas, List<SketchStroke> correctStrokes)
        {
            List<List<SketchPoint>> solutionStrokeTraces = new List<List<SketchPoint>>();
            foreach (SketchStroke stroke in correctStrokes) solutionStrokeTraces.Add(stroke.Points);
            List<Storyboard> storyboards = GenerateStoryBoards(canvas, solutionStrokeTraces, AnimationPointSize, AnimationPointDuration);
            foreach (var sb in storyboards) sb.Begin();
        }

        public static void DemoCorrectStrokes(Canvas canvas, List<SketchStroke> strokes, HashSet<int> wrongDirectionStrokeIndices)
        {
            List<List<SketchPoint>> solutionStrokeTraces = new List<List<SketchPoint>>();

            foreach (int index in wrongDirectionStrokeIndices)
            {
                List<SketchPoint> origPoints = strokes[index].Points;
                List<SketchPoint> reversed = new List<SketchPoint>();
                for (int i = origPoints.Count - 1; i >= 0; i--) reversed.Add(origPoints[i]);
                solutionStrokeTraces.Add(reversed);
            }

            List<Storyboard> storyboards = GenerateStoryBoards(canvas, solutionStrokeTraces, AnimationPointSize, AnimationPointDuration);

            foreach (var sb in storyboards) sb.Begin();
        }

        public static void HighlightWrongIntersection(Canvas animationCanvas, SketchPoint intersection)
        {
            Ellipse circle = new Ellipse()
            {
                Height = 50,
                Width = 50,
                Stroke = new SolidColorBrush(Colors.Red),
                StrokeThickness = 10,
            };

            Canvas.SetLeft(circle, intersection.X - circle.Width / 2);
            Canvas.SetTop(circle, intersection.Y - circle.Height / 2);

            animationCanvas.Children.Add(circle);
        }

        public static List<Storyboard> GenerateStoryBoards(Canvas canvas, List<List<SketchPoint>> strokes, double pointSize, long pointDuration)
        {
            List<Storyboard> storyboards = new List<Storyboard>();

            long currentTime = 0;

            foreach (List<SketchPoint> points in strokes)
            {
                Storyboard sb = new Storyboard();

                long currStrokeStartTime, currStrokeEndTime;

                currStrokeStartTime = currentTime;

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
                DoubleAnimationUsingKeyFrames fadeAnimation = new DoubleAnimationUsingKeyFrames();

                for (int i = 0; i < points.Count; i++)
                {
                    KeyTime keyTime = new TimeSpan(currentTime);
                    EasingDoubleKeyFrame xFrame = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = points[i].X };
                    EasingDoubleKeyFrame yFrame = new EasingDoubleKeyFrame() { KeyTime = keyTime, Value = points[i].Y };

                    translateXAnimation.KeyFrames.Add(xFrame);
                    translateYAnimation.KeyFrames.Add(yFrame);

                    currentTime += pointDuration;
                }

                currStrokeEndTime = currentTime;

                // fade animations
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(0), Value = 0 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(currStrokeStartTime), Value = 0 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(currStrokeStartTime), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(currStrokeEndTime), Value = 1 });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame() { KeyTime = new TimeSpan(currStrokeEndTime), Value = 0 });

                Storyboard.SetTarget(translateXAnimation, animator);
                Storyboard.SetTarget(translateYAnimation, animator);
                Storyboard.SetTarget(fadeAnimation, animator);

                Storyboard.SetTargetProperty(translateXAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
                Storyboard.SetTargetProperty(translateYAnimation, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");
                Storyboard.SetTargetProperty(fadeAnimation, "(UIElement.Opacity)");

                sb.Children.Add(translateXAnimation);
                sb.Children.Add(translateYAnimation);
                sb.Children.Add(fadeAnimation);

                storyboards.Add(sb);
            }

            return storyboards;
        }

        public static void ShowTemplateImage(Image templateImage, BitmapImage currentImageTemplate)
        {
            templateImage.Source = currentImageTemplate;
        }

        #region readonly fields

        private static readonly double AnimationPointSize = 30;
        private static readonly long AnimationPointDuration = 200000;

        #endregion
    }
}