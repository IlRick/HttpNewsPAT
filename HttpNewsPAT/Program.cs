using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpNewsPAT
{
    internal class Program
    {
        private static readonly HttpClientHandler handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };

        private static readonly HttpClient client = new HttpClient(handler);

        static void Main(string[] args)
        {
            Cookie token = SingIn("user", "user");
            GetContent(token);
            string name = "Новость дня!";
            string des = "В этот день (04.12.2025) ничго опять не произошло";
            string url = "https://permaviat.ru/_res/news_gallery/881pic.jpg";
            AddNewsAsync(token,url,name,des);
            ParseLentaRu();
            Console.Read();
        }


        public static async Task<bool> AddNewsAsync(Cookie token, string img, string name, string description)
        {
            if (token != null)
            {
                handler.CookieContainer.Add(new Uri("http://localhost/"), token);
            }

            string url = "http://localhost/ajax/add.php";

            var values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("name",name),
                new KeyValuePair<string, string>("description",description),
                new KeyValuePair<string, string>("src",img)
            };

            var content = new FormUrlEncodedContent(values);

            HttpResponseMessage response = await client.PostAsync(url, content);
            string reply = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Ответ сервера: " + reply);

            return response.IsSuccessStatusCode;
        }
        public static Cookie SingIn(string Login, string Password)
        {
            // Формируем данные для отправки
            var values = new Dictionary<string, string>
            {
                { "login", Login },
                { "password", Password }
            };
            var content = new FormUrlEncodedContent(values);

            // Выполняем POST-запрос синхронно
            HttpResponseMessage response = client.PostAsync("http://localhost/ajax/login.php", content)
                .GetAwaiter().GetResult();

            // Читаем тело ответа (для отладки)
            string responseFromServer = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Console.WriteLine(responseFromServer);

            // Извлекаем cookie "token" из глобального контейнера
            var cookies = handler.CookieContainer.GetCookies(new Uri("http://localhost/"));
            return cookies["token"]; // может быть null — как и раньше
        }
        /*public static Cookie SingIn(string Login, string Password)
        {
            Cookie token = null;
            string url = "http://localhost/ajax/login.php";
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
        }*/
        /*public static string GetContent(Cookie token)
        {
            string Content = null;
            string url = "http://localhost/main";
            Debug.WriteLine($"Выполняем запрос: {url}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(token);
            using(HttpWebResponse response=(HttpWebResponse)request.GetResponse())
            {
                Debug.WriteLine($"Статтус выполнения: {response.StatusCode}");
                Content=new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            return Content;
        }*/
        public static string GetContent(Cookie token)
        {
            // Убеждаемся, что cookie добавлены (на случай, если SingIn вернул null, но куки уже в контейнере)
            if (token != null && !handler.CookieContainer.GetCookies(new Uri("http://localhost/")).Cast<Cookie>().Any(c => c.Name == "token"))
            {
                handler.CookieContainer.Add(new Uri("http://localhost/"), token);
            }

            // Выполняем GET-запрос синхронно
            HttpResponseMessage response = client.GetAsync("http://localhost/main")
                .GetAwaiter().GetResult();

            Debug.WriteLine($"Статус выполнения: {response.StatusCode}");
            return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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
        public static async Task ParseLentaRu()
        {
            string url = "https://lenta.ru";

            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            string html;
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");

                html = await client.GetStringAsync(url);
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Основные новости на главной — в блоках .card-mini или .item (зависит от обновления)
            // Актуальный селектор (на 2025 г.): 
            var newsItems = doc.DocumentNode.SelectNodes("//a[contains(@class, 'card-mini') or contains(@class, 'item')]");

            if (newsItems == null || !newsItems.Any())
            {
                Console.WriteLine("Новости не найдены. Возможно, изменилась разметка.");
                return;
            }

            int count = 0;
            foreach (var item in newsItems)
            {
                if (count >= 10) break;

                string title = item.SelectSingleNode(".//h3")?.InnerText?.Trim() ??
                               item.SelectSingleNode(".//span")?.InnerText?.Trim() ??
                               "Без заголовка";

                string link = item.GetAttributeValue("href", "");
                if (link.StartsWith("/"))
                    link = "https://lenta.ru" + link;

                // Опционально: получаем краткое описание (если есть)
                string summary = item.SelectSingleNode(".//p")?.InnerText?.Trim() ?? "";

                Console.WriteLine($"Заголовок: {title}");
                if (!string.IsNullOrEmpty(summary))
                    Console.WriteLine($"Кратко: {summary}");
                Console.WriteLine($"Ссылка: {link}");
                Console.WriteLine(new string('-', 60));

                count++;
            }
        }
    }
}
