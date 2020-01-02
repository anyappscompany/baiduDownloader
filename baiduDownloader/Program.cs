using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Web;
using System.Web.Script;
using System.Web.Script.Serialization;

namespace baiduDownloader
{
    class Program
    {
        struct odinvideo
        {
            public string url;
            public string domain;
        }
        
        static Queue<odinvideo> videos = new Queue<odinvideo>();
        static object URLlocker = new object();
        static object HTMLlocker = new object();
        static object errorlocker = new object();
        static void Main(string[] args)
        {
            string line;
            StreamReader file = new StreamReader(@"result.txt");

            if (File.Exists(@"errors.txt"))
            {
                File.Delete(@"errors.txt");
            }
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "video/upload.csv"))
            {
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "video/upload.csv");
            }

            while ((line = file.ReadLine()) != null)
            {
                Console.WriteLine(line);
                odinvideo vid = new odinvideo();
                string[] lines = Regex.Split(line, @"5544"); 
                vid.url = lines[0]; 
                vid.domain = lines[1];
                videos.Enqueue(vid);
            }
            //создаем и запускаем 3 потока
            for (int i = 0; i < 10; i++)
                (new Thread(new ThreadStart(Download))).Start();

            Console.WriteLine(videos.Count);            
        }
        public static void Download()
        {            
            while (true)
            {
                odinvideo URL;                
                lock (URLlocker)
                {
                    if (videos.Count == 0)
                        break;
                    else
                    {
                        Console.WriteLine("************");
                        Console.WriteLine("****  " + videos.Count);
                        Console.WriteLine("************");
                        URL = videos.Dequeue();
                    }
                }
                Console.WriteLine(URL.domain + ": - " + URL.url + " - start downloading ...");

                videoinfo vidn = new videoinfo();
                vidn = getVideoLink(URL.domain, URL.url);

                string vidLink = vidn.videourl;
                string vidTitl = vidn.title;
                DownloadFile(vidLink, vidTitl, vidTitl);
            }
        }
        public static videoinfo getVideoLink(string domain2, string url2)
        {            
            videoinfo vid1 = new videoinfo();
            switch(domain2)
            {
                case "v_ku6_com":
                    {
                        v_ku6_com vkub = new v_ku6_com(domain2, url2);
                        try
                        {
                            vid1.videourl = System.Text.RegularExpressions.Regex.Unescape(vkub.getlink());
                            vid1.title = vkub.gettitle();
                        }
                        catch { }
                        break;
                    }
                default:
                    break;
            }
            return vid1;
        }
        public static System.String GetRandomString(System.Int32 length)
        {
            System.Byte[] seedBuffer = new System.Byte[4];
            using (var rngCryptoServiceProvider = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                rngCryptoServiceProvider.GetBytes(seedBuffer);
                System.String chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                System.Random random = new System.Random(System.BitConverter.ToInt32(seedBuffer, 0));
                return new System.String(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
            }
        }
        public static int Download(String remoteFilename,
                               String localFilename)
        {
            // Function will return the number of bytes processed
            // to the caller. Initialize to 0 here.
            int bytesProcessed = 0;

            // Assign values to these objects here so that they can
            // be referenced in the finally block
            Stream remoteStream = null;
            Stream localStream = null;
            WebResponse response = null;

            // Use a try/catch/finally block as both the WebRequest and Stream
            // classes throw exceptions upon error

            // Create a request for the specified remote file name
            WebRequest request = WebRequest.Create(remoteFilename);
            if (request != null)
            {
                // Send the request to the server and retrieve the
                // WebResponse object 
                response = request.GetResponse();
                if (response != null)
                {
                    // Once the WebResponse object has been retrieved,
                    // get the stream object associated with the response's data
                    remoteStream = response.GetResponseStream();

                    // Create the local file
                    if (File.Exists(localFilename))
                    {
                        localStream = File.Create(localFilename.Replace(".mp4", "-" + GetRandomString(8) + ".mp4"));
                    }
                    else
                    {
                        localStream = File.Create(localFilename);
                    }

                    // Allocate a 1k buffer
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    // Simple do/while loop to read from stream until
                    // no bytes are returned
                    do
                    {
                        // Read data (up to 1k) from the stream
                        bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

                        // Write the data to the local file
                        localStream.Write(buffer, 0, bytesRead);

                        // Increment total bytes processed
                        bytesProcessed += bytesRead;
                    } while (bytesRead > 0);
                }

            }

            // Return total bytes processed to caller.
            return bytesProcessed;
        }
        
        public static string TranslateGoogle(string text, string fromCulture, string toCulture)
        {
            fromCulture = fromCulture.ToLower();
            toCulture = toCulture.ToLower();

            // normalize the culture in case something like en-us was passed 
            // retrieve only en since Google doesn't support sub-locales
            string[] tokens = fromCulture.Split('-');
            if (tokens.Length > 1)
                fromCulture = tokens[0];

            // normalize ToCulture
            tokens = toCulture.Split('-');
            if (tokens.Length > 1)
                toCulture = tokens[0];

            string url = string.Format(@"http://translate.google.com/translate_a/t?client=j&text={0}&hl=en&sl={1}&tl={2}",
                                       HttpUtility.UrlEncode(text), fromCulture, toCulture);

            // Retrieve Translation with HTTP GET call
            string html = null;
            try
            {
                WebClient web = new WebClient();

                // MUST add a known browser user agent or else response encoding doen't return UTF-8 (WTF Google?)
                web.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0");
                web.Headers.Add(HttpRequestHeader.AcceptCharset, "UTF-8");

                // Make sure we have response encoding to UTF-8
                web.Encoding = Encoding.UTF8;
                html = web.DownloadString(url);
            }
            catch (Exception ex)
            {
                //this.ErrorMessage = Westwind.Globalization.Resources.Resources.ConnectionFailed + ": " +
                //                    ex.GetBaseException().Message;
                return null;
            }

            // Extract out trans":"...[Extracted]...","from the JSON string
            string result = Regex.Match(html, "trans\":(\".*?\"),\"", RegexOptions.IgnoreCase).Groups[1].Value;

            if (string.IsNullOrEmpty(result))
            {
                //this.ErrorMessage = Westwind.Globalization.Resources.Resources.InvalidSearchResult;
                return null;
            }

            //return WebUtils.DecodeJsString(result);

            // Result is a JavaScript string so we need to deserialize it properly
            JavaScriptSerializer ser = new JavaScriptSerializer();
            try
            {
                return ser.Deserialize(result, typeof(string)) as string;
            }
            catch(Exception ex)
            {
                return "";
            }
        }
        
        public static int DownloadFile(String remoteFilename,
                               String localFilename, string title)
        {

            localFilename = TranslateGoogle(localFilename, "zh-CN", "en").Replace(@"\", "").Replace(@"/", "").Replace(@":", "").Replace(@"*", "").Replace(@"?", "").Replace("\"", "").Replace(@"<", "").Replace(@">", "").Replace(@"|", "").Replace(@".", "") + ".mp4";

            string rustitle = TranslateGoogle(title, "zh-CN", "ru").Replace(@"\", "").Replace(@"/", "").Replace(@":", "").Replace(@"*", "").Replace(@"?", "").Replace("\"", "").Replace(@"<", "").Replace(@">", "").Replace(@"|", "").Replace(@".", "");
            title = rustitle + " - " + localFilename.Replace(".mp4", "");
            title = System.Text.RegularExpressions.Regex.Unescape(title);

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "video\\" + localFilename))
            {
                DateTime now = DateTime.Now;
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(now.ToString("u"));
                localFilename = localFilename.Replace(".mp4", "(" + System.Convert.ToBase64String(plainTextBytes) + ").mp4");
                
            }
            
            lock (HTMLlocker)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "video\\upload.csv", true))
                {
                    file.WriteLine(title + "05452" + title + "05452" + title + "05452" + "Comedy" + "05452" + "TRUE" + "05452" + rustitle + " - " + localFilename);
                }
            }

            int count = 0;
        lab2:
            try
            {
                /*using (WebClient webClient = new WebClient())
                {
                    webClient.DownloadFile(remoteFilename, localFilename);
                }*/
                Download(remoteFilename, AppDomain.CurrentDomain.BaseDirectory + "video\\" + rustitle + " - " + localFilename);
            }
            catch (Exception ex)
            {
                if (ex.Message.IndexOf("403") > -1) { return 403; }

                count++;
                Console.WriteLine("--3" + " " + ex.Message + "::" + remoteFilename);

                lock (errorlocker)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"errors.txt", true))
                    {
                        //file.WriteLine("--3" + ex.Message + "::" + remoteFilename);
                        file.WriteLine("--3 " + ";;;;;" + title + ";;;;;" + ex.Message);

                    }
                }

                if (count < 6)
                {
                    Console.WriteLine("Ожидание 5 с");
                    Thread.Sleep(25000);
                    goto lab2;
                }
                else
                {

                }
            }
            return 0;

        }
    }
}
