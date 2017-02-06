using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;

namespace App2
{
    public class XMLHelpers
    {
        public static async Task<Sketch> XMLToSketch(StorageFile file)
        {
            string text = await FileIO.ReadTextAsync(file);
            XDocument document = XDocument.Parse(text);

            int minX = Int32.Parse(document.Root.Attribute("frameMinX").Value);
            int maxX = Int32.Parse(document.Root.Attribute("frameMaxX").Value);
            int minY = Int32.Parse(document.Root.Attribute("frameMinY").Value);
            int maxY = Int32.Parse(document.Root.Attribute("frameMaxY").Value);
            string label = document.Root.Attribute("label").Value;
            List<SketchStroke> strokes = new List<SketchStroke>();

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

                strokes.Add(stroke);
            }

            return new Sketch(minX, maxX, minY, maxY, label, strokes);
        }

        public static async Task<Question> XMLToQuestion(StorageFile file)
        {
            string fileText = await FileIO.ReadTextAsync(file);
            XDocument document = XDocument.Parse(fileText);

            string id = document.Root.Attribute("id").Value;
            string text = document.Root.Attribute("text").Value;
            string answer = document.Root.Attribute("answer").Value;

            return new Question(id, text, answer);
        }

        public static string RemoveExtension(string fileName)
        {
            int dotLocation = 0;
            for (int i = fileName.Length - 1; i >= 0; i--) if (fileName[i] == '.') dotLocation = i;
            return fileName.Substring(0, dotLocation);
        }
    }
}