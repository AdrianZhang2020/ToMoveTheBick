using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ToMoveTheBick
{
    class Program
    {
        public static List<Html_a> html_As = new List<Html_a>();

        /// <summary>
        /// 网络请求:请求方式为Get
        /// </summary>
        /// <param name="Url"> 请求地址</param>
        /// <returns>返回结果</returns>
        //public static string HttpGet(string Url)
        //{
        //    try
        //    {
        //        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
        //        request.Method = "GET";
        //        request.ContentType = "text/html;charset=GB2312";
        //        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        //        Stream myResponseStream = response.GetResponseStream();
        //        StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("GBK"));
        //        string retString = myStreamReader.ReadToEnd();
        //        myStreamReader.Close();
        //        myResponseStream.Close();
        //        return retString;
        //    }
        //    catch(Exception ex)
        //    {
        //        Thread.Sleep(100);
        //       return  HttpGet(Url);
        //    }

        //}
        private static string HttpGet(string url)
        {
            try
            {
                string htmlCode;
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                HttpWebRequest webRequest = (HttpWebRequest)System.Net.WebRequest.Create(url);
                webRequest.Method = "GET";
                webRequest.ContentType = "text/html;charset=GB2312";
                webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.72 Safari/537.36";
                webRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
                if (webResponse.ContentEncoding.ToLower() == "gzip")//如果使用了GZip则先解压
                {
                    using (System.IO.Stream streamReceive = webResponse.GetResponseStream())
                    {
                        using (var zipStream =
                            new System.IO.Compression.GZipStream(streamReceive, System.IO.Compression.CompressionMode.Decompress))
                        {
                            using (StreamReader sr = new StreamReader(zipStream, Encoding.GetEncoding("GB2312")))
                            {
                                htmlCode = sr.ReadToEnd();
                            }
                        }
                    }
                }
                else
                {
                    using (System.IO.Stream streamReceive = webResponse.GetResponseStream())
                    {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(streamReceive, Encoding.GetEncoding("GB2312")))
                        {
                            htmlCode = sr.ReadToEnd();
                        }
                    }
                }
                return htmlCode;
            }
            catch
            {
                Thread.Sleep(100);
                return HttpGet(url);
            }
        }
        static void Main(string[] args)
        {
            GetMsg("http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2020/");
            //GetMsg("https://xingzhengquhua.bmcx.com/");
        }

        public static void GetMsg(string Url)
        {

            var shenarr = GetShen(HttpGet(Url + "index.html"));
            //var shenarr = GetShen(HttpGet(Url));

            for (var shen_i=0;shen_i<shenarr.Count;shen_i++)
            {
                Html_a sen = shenarr[shen_i];
                Wreiterl(sen);
                if (sen.href == null)
                {
                    continue;
                }
                var shiarr = GetShi(HttpGet(Url + sen.href));
                for (var shi_i= 0; shi_i < shiarr.Count; shi_i++)
                {
                    Html_a shi = shiarr[shi_i];
                    shi.sjcode = sen.code;
                    Wreiterl(shi);
                    if (shi.href == null)
                    {
                        continue;
                    }
                    var quarr = GetQu(HttpGet(Url + shi.href));
                    for (var qu_i=0;qu_i< quarr.Count;qu_i++)
                    {
                        Html_a qu = quarr[qu_i];
                        qu.sjcode = shi.code;
                        Wreiterl(qu);

                        var strarr = shi.href.Split("/");
                        if (qu.href == null)
                        {
                            continue;
                        }
                        var qurl = strarr[0];
                        var xianarr = GetXian(HttpGet(Url + qurl + "/" + qu.href));
                        for (var xian_i=0;xian_i< xianarr.Count;xian_i++)
                        {
                            Html_a xian = xianarr[xian_i];
                            xian.sjcode = qu.code;
                            Wreiterl(xian);
                            if (xian.href == null)
                            {
                                continue;
                            }
                            strarr = qu.href.Split("/");
                            var jdrl = strarr[0];
                            var jiedao = Getjiedao(HttpGet(Url + qurl + "/" + jdrl + "/" + xian.href));

                            foreach (var jd in jiedao)
                            {

                                jd.sjcode = xian.code;
                                Wreiterl(jd);
                            }
                        }
                    }
                }
            }
        }

        public static void Wreiterl(Html_a html_A)
        {
            html_As.Add(html_A);
            var Msg = html_A.code + "\t\t" + (html_A.cxtype == null ? "Null" : html_A.cxtype) + "\t\t" + (html_A.sjcode == null ? "Null" : html_A.sjcode) + "\t\t" + html_A.name;
            Console.WriteLine(Msg);
            string Folder = ".\\data\\";

            if (!System.IO.Directory.Exists(Folder))
                System.IO.Directory.CreateDirectory(Folder);
            string FilePath = $"{ Folder }Msg.txt";
            using (TextWriter fs = new StreamWriter(FilePath, true))
            {
                fs.WriteLine(Msg);
                fs.Close();
                fs.Dispose();
            }
        }


        public static Html_a GetA(string html)
        {
            Html_a a = new Html_a();
            string regex = "href=[\\\"\\\'](http:\\/\\/|\\.\\/|\\/)?\\w+(\\.\\w+)*(\\/\\w+(\\.\\w+)?)*(\\/|\\?\\w*=\\w*(&\\w*=\\w*)*)?[\\\"\\\']";
            Regex re = new Regex(regex);
            MatchCollection matches = re.Matches(html);
            var href = matches[0].ToString().Replace("href=\'", "").Replace("\'", "");
            a.code = href.Replace(".html", "");
            a.href = href;
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var ass = htmlDoc.DocumentNode.SelectSingleNode("//a");
            a.name = ass.InnerText;
            return a;
        }


        /// <summary>
        /// 省
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<Html_a> GetShen(string str)
        {
            List<Html_a> aArr = new List<Html_a>();
            //string regex = "href=[\\\"\\\'](http:\\/\\/|\\.\\/|\\/)?\\w+(\\.\\w+)*(\\/\\w+(\\.\\w+)?)*(\\/|\\?\\w*=\\w*(&\\w*=\\w*)*)?[\\\"\\\']";
            string regex = "<tr class='provincetr'>(.*?)</tr>";
            Regex re = new Regex(regex);
            MatchCollection matches = re.Matches(str);

            foreach (var a in matches)
            {
                string agx = "<a href='(.*?)'>(.*?)</a>";
                Regex are = new Regex(agx);
                MatchCollection mc_a = are.Matches(a.ToString());
                foreach (var aitem in mc_a)
                {
                    aArr.Add(GetA(aitem.ToString()));
                }
            }
            return aArr;
        }
        /// <summary>
        /// 获取a标签
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static MatchCollection Get_A(string html)
        {
            string agx = "<a href='(.*?)'>(.*?)</a>";
            Regex are = new Regex(agx);
            MatchCollection mc_a = are.Matches(html);
            return mc_a;
        }

        /// <summary>
        /// 市
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<Html_a> GetShi(string str)
        {
            string regex = "<tr class='citytr'>(.*?)</tr>";
            Regex re = new Regex(regex);
            MatchCollection matches = re.Matches(str);
            return GetHtmlaArr(re, matches);
        }
        /// <summary>
        /// 区
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<Html_a> GetQu(string str)
        {
            string regex = "<tr class='countytr'>(.*?)</tr>";
            Regex re = new Regex(regex);
            MatchCollection matches = re.Matches(str);

            return GetHtmlaArr(re, matches);
        }
        public static List<Html_a> GetTowntr(string str)
        {
            string regex = "<tr class='towntr'>(.*?)</tr>";
            Regex re = new Regex(regex);
            MatchCollection matches = re.Matches(str);

            return GetHtmlaArr(re, matches);
        }

        private static List<Html_a> GetHtmlaArr(Regex re, MatchCollection matches)
        {
            List<Html_a> aArr = new List<Html_a>();
            foreach (var ma in matches)
            {
                string rema = "<td>(.*?)</td>";
                Regex ma2 = new Regex(rema);
                MatchCollection matches2 = re.Matches(ma.ToString());
                foreach (var td in matches2)
                {
                    var a = Get_A(td.ToString());
                    if (a.Count == 2)
                    {
                        var ca0 = GetA(a[0].ToString());
                        var ca1 = GetA(a[1].ToString());
                        Html_a html_A = new Html_a();
                        html_A.code = ca0.name;
                        html_A.href = ca0.href;
                        html_A.name = ca1.name;
                        aArr.Add(html_A);
                    }
                    else
                    {
                        var msc = ma2.Matches(td.ToString());
                        if (msc.Count == 2)
                        {
                            Html_a html_A = new Html_a();
                            var htmlDoc = new HtmlDocument();
                            htmlDoc.LoadHtml(msc[0].ToString());
                            var ass = htmlDoc.DocumentNode.SelectSingleNode("//td");
                            html_A.code = ass.InnerText;
                            htmlDoc.LoadHtml(msc[1].ToString());
                            var ass2 = htmlDoc.DocumentNode.SelectSingleNode("//td");
                            html_A.name = ass2.InnerText;
                            aArr.Add(html_A);
                        }

                        if (msc.Count == 3)
                        {
                            Html_a html_A = new Html_a();
                            var htmlDoc = new HtmlDocument();
                            htmlDoc.LoadHtml(msc[0].ToString());
                            var ass = htmlDoc.DocumentNode.SelectSingleNode("//td");
                            html_A.code = ass.InnerText;
                            htmlDoc.LoadHtml(msc[1].ToString());
                            var ass2 = htmlDoc.DocumentNode.SelectSingleNode("//td");
                            html_A.cxtype = ass2.InnerText;

                            htmlDoc.LoadHtml(msc[2].ToString());
                            var ass3 = htmlDoc.DocumentNode.SelectSingleNode("//td");
                            html_A.name = ass3.InnerText;
                            aArr.Add(html_A);
                        }
                    }

                }
            }
            return aArr;
        }

        /// <summary>
        /// 县 镇
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<Html_a> GetXian(string str)
        {
            string regex = "<tr class='towntr'>(.*?)</tr>";
            Regex re = new Regex(regex);
            MatchCollection matches = re.Matches(str);

            return GetHtmlaArr(re, matches);
        }

        /// <summary>
        /// 街道
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<Html_a> Getjiedao(string str)
        {
            string regex = "<tr class='villagetr'>(.*?)</tr>";
            Regex re = new Regex(regex);
            MatchCollection matches = re.Matches(str);

            return GetHtmlaArr(re, matches);
        }
    }


    class Html_a
    {
        public string code { get; set; }
        public string href { get; set; }
        public string cxtype { get; set; }
        public string name { get; set; }

        public string sjcode { get; set; }
    }
}
