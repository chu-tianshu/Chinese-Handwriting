using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace QuestionCollection
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            string question = questionText.Text;
            string answer = answerText.Text;
            string id = idText.Text;

            CreateQuestion(id, question, answer);
        }

        private async void CreateQuestion(string id, string question, string answer)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFolder assetsFolder = await localFolder.GetFolderAsync("Assets");
            StorageFolder questionFolder = await assetsFolder.GetFolderAsync("Questions");
            StorageFile file = await questionFolder.CreateFileAsync(id, CreationCollisionOption.ReplaceExisting);

            string output = "";

            using (StringWriter stringWriter = new StringWriter())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter))
                {
                    xmlWriter.WriteStartDocument();

                    xmlWriter.WriteStartElement("question");
                    xmlWriter.WriteAttributeString("id", id);
                    xmlWriter.WriteAttributeString("text", question);
                    xmlWriter.WriteAttributeString("answer", answer);

                    xmlWriter.WriteEndElement();
                }

                output = stringWriter.ToString();
            }

            await FileIO.WriteTextAsync(file, output);
        }
    }
}
