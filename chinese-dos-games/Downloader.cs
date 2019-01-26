using System;
using System.ComponentModel;
using System.Net;
using Newtonsoft.Json.Linq;

namespace ChineseDosGames
{
    public class Downloader
    {
        public static event AsyncCompletedEventHandler DownloadFileCompleted;
        public static event DownloadProgressChangedEventHandler DownloadProgressChanged;
        public static void Download(string url, string location,string identifier,JToken token,int index,JObject root)
        {
            var webClient = new WebClient();
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            if (DownloadFileCompleted != null) webClient.DownloadFileCompleted += DownloadFileCompleted;
            if (DownloadProgressChanged != null) webClient.DownloadProgressChanged += DownloadProgressChanged;
            try
            {
                webClient.DownloadFileAsync(new Uri(url), location,new DownloadItem()
                {
                    Url=url,
                    Location=location,
                    Identifier= identifier,
                    Token= token,
                    Index=index,
                    Root=root
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Download Error :" + ex.Message.ToString());
            }
        }
    }

    public class DownloadItem
    {
        public string Url { get; set; }
        public  string Location { get; set; }
        public string Identifier { get; set; }
        public JToken Token { get; set; }
        public int Index { get; set; }
        public JObject Root { get; set; }
    }
}
