using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;

namespace baiduDownloader
{
    class v_ku6_com
    {
        private string dom1;
        private string url1;
        private string HTML1;
        public v_ku6_com(string dom, string url)
        {
            dom1 = dom;
            url1 = url; 
        }
        public string getlink()
        {
            string linka = "";
            int count = 0;
            lab1:
            try
            {
                count++;
               /* HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(@url1);
                myRequest.Method = "GET";
                myRequest.UserAgent = "MSIE 6.0";
                WebResponse myResponse = myRequest.GetResponse();
                StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);*/
                WebClient web = new WebClient();

                // MUST add a known browser user agent or else response encoding doen't return UTF-8 (WTF Google?)
                web.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0");
                web.Headers.Add(HttpRequestHeader.AcceptCharset, "UTF-8");

                // Make sure we have response encoding to UTF-8
                web.Encoding = Encoding.GetEncoding("gb2312");
                string html = web.DownloadString(url1);

                string HTML = HTML1 = html;
                //System.IO.File.WriteAllText(@"WriteText.txt", HTML);
                Regex newReg = new Regex("\"f\":\"(?<val>.*?)\",\"");
                MatchCollection matches = newReg.Matches(HTML);
                
                if (matches.Count > 0)
                {
                    //Console.WriteLine("++++++++++++++++");
                    foreach (Match mat in matches)
                    {
                        linka = mat.Groups["val"].Value;
                        break;
                    }
                }else
                {
                    Console.WriteLine("--" + url1 + "--"); Console.ReadKey();
                }
            }
            catch(Exception ex)
            {
                Thread.Sleep(25000);
                if (count < 6)
                {
                    goto lab1;
                }
            }

            return linka;
        }
        public string gettitle()
        {
            string title = "";
            Regex newReg = new Regex("<h1 title=\"(?<val>.*?)\">");
            MatchCollection matches = newReg.Matches(HTML1);
            if (matches.Count > 0)
            {
                //Console.WriteLine("++++++++++++++++");
                foreach (Match mat in matches)
                {
                    title = mat.Groups["val"].Value;
                    break;
                }
            }
            
            return title;
        }
    }
}
