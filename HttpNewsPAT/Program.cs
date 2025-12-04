using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpNewsPAT
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Cookie token = SingIn("user", "user");
            GetContent(token);
            Console.Read();
        }

        public static Cookie SingIn(string Login, string Password)
        {
            Cookie token = null;
            string url = "http://127.0.0.1/ajax/login.php";
            Debug.WriteLine($"Выполняю запрос: {url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "Post";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer = new CookieContainer();
            string postData = $"login={Login}&password={Password}";
            byte[] Data = Encoding.ASCII.GetBytes(postData);
            request.ContentLength = Data.Length;
            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(Data, 0, Data.Length);
            }
            using (HttpWebResponse Response = (HttpWebResponse)request.GetResponse())
            {
                Debug.WriteLine($"Статус выполенения: {Response.StatusCode}");
                string ResponseFromServer = new StreamReader(Response.GetResponseStream()).ReadToEnd();
                Console.WriteLine(ResponseFromServer);
                token = Response.Cookies["token"];
            }
            return token;
        }

        public static void GetContent(Cookie Token)
        {
            string url = "http://localhost/ajax/main.php";
            Debug.WriteLine($"Выполняем запрос: {url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(Token);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Debug.WriteLine($"Статус выполнения: {response.StatusCode}");
            string responseFromServer = new StreamReader(response.GetResponseStream()).ReadToEnd();
            Console.WriteLine(responseFromServer);
        }
        public static void ParsingHtml(string htmlCode)
        {
            var html = new HtmlDocument();
            html.LoadHtml(htmlCode);
            var Document = html.DocumentNode;
            IEnumerable<HtmlNode> divsNews = Document.Descendants(0).Where(n => n.HasClass("news"));
            foreach (var DivsNews in divsNews)
            {
                var src = DivsNews.ChildNodes[1].GetAttributeValue("src", "none");
                var name = DivsNews.ChildNodes[3].InnerText;
                var Description = DivsNews.ChildNodes[5].InnerText;
                Console.WriteLine(name + "\n" + "Изображение: " + src + "\n" + "Описание: " + Description + "\n");
            }
        }
    }
}
