using System;
using System.IO;
using System.Net;
using System.Windows;

namespace WPFLauncher
{
    public partial class BreadWindow : Window
    {
        public BreadWindow()
        {
            InitializeComponent();
            GetBread();
        }

        private void GetBread()
        {
            try
            {
                var webRequest = WebRequest.Create("https://api.atlasfreeshard.com/bread") as HttpWebRequest;

                webRequest.ContentType = "application/json";
                webRequest.UserAgent = "Nothing";

                var response = webRequest.GetResponse();

                using (var s = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(s))
                    {
                        breadLabel.Content = sr.ReadToEnd();
                    }
                }
            }
            catch (Exception e)
            {
                breadLabel.Content = "No bread";
            }
        }
    }
}