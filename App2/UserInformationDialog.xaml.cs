using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace App2
{
    public sealed partial class UserInformationDialog : ContentDialog
    {
        public UserInformationDialog()
        {
            this.InitializeComponent();
            this.comboBoxFluency.Items.Add("novice");
            this.comboBoxFluency.Items.Add("learner");
            this.comboBoxFluency.Items.Add("expert");

            this.UserName = string.Empty;
            this.UserMotherLanguage = string.Empty;
            this.UserFluency = (string)this.comboBoxFluency.SelectedItem;
        }

        private void textBoxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UserName = this.textBoxName.Text.Trim();
        }

        private void textBoxMotherLanguage_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UserMotherLanguage = this.textBoxMotherLanguage.Text.Trim();
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.UserFluency = (string)this.comboBoxFluency.SelectedItem;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        public string UserName { get; private set; }
        public string UserMotherLanguage { get; private set; }
        public string UserFluency { get; private set; }
    }
}
