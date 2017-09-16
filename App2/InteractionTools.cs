using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Inking;
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
            foreach (SketchStroke stroke in correctStrokes)
            {
                solutionStrokeTraces.Add(stroke.Points);
            }
            List<Storyboard> storyboards = GenerateStoryBoards(canvas, solutionStrokeTraces, AnimationPointSize, AnimationPointDuration);
            foreach (var sb in storyboards)
            {
                sb.Begin();
            }
        }

        /*
        /// <summary>
        /// Demos correct stroke counts. For one-to-many errors, we concatenate the broken strokes and demo a smooth stroke. For extra strokes, we
        /// highlight them. For many-to-one errors, we try to break those too-long strokes and demo the correct parts one by one.
        /// </summary>
        /// <param name="canvas">animation canvas</param>
        /// <param name="inkCanvas">ink canvas</param>
        /// <param name="strokes">sketchstrokes</param>
        /// <param name="correspondence">template to sample stroke correspondence</param>
        public static void DemoStrokeCount(Canvas canvas, InkCanvas inkCanvas, List<SketchStroke> strokes, List<List<int>[]> correspondence)
        {
            // Make extra strokes red
            HashSet<int> extraStrokeIndices = new HashSet<int>();
            for (int i = 0; i < strokes.Count; i++)
            {
                extraStrokeIndices.Add(i);
            }
            foreach (List<int>[] pair in correspondence)
            {
                foreach (int index in pair[1])
                {
                    extraStrokeIndices.Remove(index);
                }
            }
            foreach (int extraIndex in extraStrokeIndices)
            {
                InkStroke extraStroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokes()[extraIndex];
                InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
                drawingAttributes.Color = Windows.UI.Colors.Red;
                drawingAttributes.PenTip = PenTipShape.Circle;
                drawingAttributes.Size = new Size(20, 20);
                extraStroke.DrawingAttributes = drawingAttributes;
            }

            // Feedback for broken strokes (one-to-many errors)
            List<SketchStroke> concatenatedStrokes = new List<SketchStroke>();
            foreach (List<int>[] pair in correspondence)
            {
                if (pair[1].Count > 1)
                {
                    List<SketchStroke> brokenStrokes = new List<SketchStroke>();
                    foreach (int index in pair[1])
                    {
                        brokenStrokes.Add(strokes[index]);
                    }
                    SketchStroke concatenatedStroke = SketchStroke.ConcatenateStrokes(brokenStrokes);
                    concatenatedStrokes.Add(concatenatedStroke);
                }
            }
            List<List<SketchPoint>> concatenatedPointLists = new List<List<SketchPoint>>();
            foreach (SketchStroke stroke in concatenatedStrokes)
            {
                concatenatedPointLists.Add(stroke.Points);
            }
            List<Storyboard> storyboards = InteractionTools.GenerateStoryBoards(canvas, concatenatedPointLists, AnimationPointSize, AnimationPointDuration);
            foreach (var sb in storyboards)
            {
                sb.Begin();
            }

            // Feedback for sticky strokes
            HashSet<int> stickyStrokes = new HashSet<int>();
            foreach (List<int>[] pair in correspondence)
            {
                if (pair[0].Count > 1)
                {
                    stickyStrokes.Add(pair[1][0]);
                }
            }

            foreach (int stickyIndex in stickyStrokes)
            {
                InkStroke stickyStroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokes()[stickyIndex];
                InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
                drawingAttributes.Color = Windows.UI.Colors.Yellow;
                drawingAttributes.PenTip = PenTipShape.Circle;
                drawingAttributes.Size = new Size(20, 20);
                stickyStroke.DrawingAttributes = drawingAttributes;
            }
        }
        */

        /// <summary>
        /// demos either concatenating strokes or broken strokes
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="inkCanvas"></param>
        /// <param name="strokes"></param>
        /// <param name="correspondence"></param>
        /// <param name="errorType"></param>
        public static void DemoStrokeCount(Canvas canvas, InkCanvas inkCanvas, List<List<int>> correspondence, string errorType)
        {
            if (errorType == "concatenating")
            {
                for (int i = 0; i < correspondence.Count; i++)
                {
                    if (correspondence[i].Count == 0) // extra stroke
                    {
                        InkStroke extraStroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokes()[i];
                        InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
                        drawingAttributes.Color = Windows.UI.Colors.Red;
                        drawingAttributes.PenTip = PenTipShape.Circle;
                        drawingAttributes.Size = new Size(20, 20);
                        extraStroke.DrawingAttributes = drawingAttributes;
                    }
                    else if (correspondence[i].Count > 1) // concatenating stroke
                    {
                        InkStroke concatenatingStroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokes()[i];
                        InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
                        drawingAttributes.Color = Windows.UI.Colors.Yellow;
                        drawingAttributes.PenTip = PenTipShape.Circle;
                        drawingAttributes.Size = new Size(20, 20);
                        concatenatingStroke.DrawingAttributes = drawingAttributes;
                    }
                }

                return;
            }

            if (errorType == "broken")
            {
                for (int i = 0; i < correspondence.Count; i++)
                {
                    if (correspondence[i].Count > 1) // broken stroke
                    {
                        foreach (int sampleIndex in correspondence[i])
                        {
                            InkStroke concatenatingStroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokes()[sampleIndex];
                            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
                            drawingAttributes.Color = Windows.UI.Colors.Yellow;
                            drawingAttributes.PenTip = PenTipShape.Circle;
                            drawingAttributes.Size = new Size(20, 20);
                            concatenatingStroke.DrawingAttributes = drawingAttributes;
                        }
                    }
                }

                return;
            }

            return;
        }

        public static void DemoStrokeDirection(Canvas canvas, List<SketchStroke> strokes, HashSet<int> wrongDirectionStrokeIndices)
        {
            List<List<SketchPoint>> solutionStrokeTraces = new List<List<SketchPoint>>();

            foreach (int index in wrongDirectionStrokeIndices)
            {
                List<SketchPoint> origPoints = strokes[index].Points;
                List<SketchPoint> reversed = new List<SketchPoint>();
                for (int i = origPoints.Count - 1; i >= 0; i--)
                {
                    reversed.Add(origPoints[i]);
                }
                solutionStrokeTraces.Add(reversed);
            }

            List<Storyboard> storyboards = InteractionTools.GenerateStoryBoards(canvas, solutionStrokeTraces, AnimationPointSize, AnimationPointDuration);
            foreach (var sb in storyboards)
            {
                sb.Begin();
            }
        }

        public static void DemoStrokeOrder(Canvas canvas, InkCanvas inkCanvas, int[] correspondence)
        {
            for (int i = 0; i < correspondence.Length; i++)
            {
                if (correspondence[i] != i)
                {
                    InkStroke wrongOrderStroke = inkCanvas.InkPresenter.StrokeContainer.GetStrokes()[i];
                    InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
                    drawingAttributes.Color = Windows.UI.Colors.Orange;
                    drawingAttributes.PenTip = PenTipShape.Circle;
                    drawingAttributes.Size = new Size(20, 20);
                    wrongOrderStroke.DrawingAttributes = drawingAttributes;
                }
            }
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

        public static void ShowStroke(Canvas canvas, SketchStroke stroke)
        {
            for (int i = 0; i < stroke.PointsCount - 1; i++)
            {
                Line line = new Line();
                line.X1 = stroke.Points[i].X;
                line.Y1 = stroke.Points[i].Y;
                line.X2 = stroke.Points[i + 1].X;
                line.Y2 = stroke.Points[i + 1].Y;
                line.StrokeThickness = 40;
                line.Stroke = new SolidColorBrush(Colors.Blue);
                canvas.Children.Add(line);
            }
        }

        public static void ShowTemplateImage(Image templateImage, BitmapImage currentImageTemplate)
        {
            templateImage.Source = currentImageTemplate;
        }

        public static void HighlightStrokes(Canvas canvas, InkCanvas inkCanvas, int realI, int realJ)
        {
            InkStroke strokeI = inkCanvas.InkPresenter.StrokeContainer.GetStrokes()[realI];
            InkStroke strokeJ = inkCanvas.InkPresenter.StrokeContainer.GetStrokes()[realJ];
            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            drawingAttributes.Color = Windows.UI.Colors.Blue;
            drawingAttributes.PenTip = PenTipShape.Circle;
            drawingAttributes.Size = new Size(20, 20);
            strokeI.DrawingAttributes = drawingAttributes;
            strokeJ.DrawingAttributes = drawingAttributes;
        }

        #region readonly fields

        private static readonly double AnimationPointSize = 30;
        private static readonly long AnimationPointDuration = 200000;

        #endregion
    }
}