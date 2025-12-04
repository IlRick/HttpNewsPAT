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

        static async Task Main(string[] args)
        {
            /* Cookie token = SingIn("user", "user");
             GetContent(token);
             //await ParseAvito();*/
            //Console.Read();


            Console.WriteLine("=== Авторизация ===");
            Console.Write("Логин: ");
            string login = Console.ReadLine();

            Console.Write("Пароль: ");
            string password = Console.ReadLine();

            Cookie token = await SingInAsync(login, password);

            if (token == null)
            {
                Console.WriteLine("Ошибка авторизации!");
                return;
            }

            Console.WriteLine("Авторизация успешна!\n");
            Console.WriteLine("=== Получение контента ===");
            string content = await GetContentAsync(token);
            ParsingHtml(content);
            Console.WriteLine("=== Добавление новости ===");

            Console.Write("URL изображения: ");
            string img = Console.ReadLine();

            Console.Write("Название новости: ");
            string name = Console.ReadLine();

            Console.Write("Описание: ");
            string description = Console.ReadLine();

            bool result = await AddNewsAsync(token, img, name, description);

            Console.WriteLine(result ? "Новость добавлена!" : "Ошибка при добавлении новости!");
        }
        

        public static async Task<bool> AddNewsAsync(Cookie token, string img, string name, string description)
         {
            if (token != null)
            {
                handler.CookieContainer.Add(new Uri("http://news.permaviat.ru"), token);
            }

            string url = "http://news.permaviat.ru/add";

            var values = new Dictionary<string, string>
            {
                { "img", img },
                { "name", name },
                { "description", description }
            };

            var content = new FormUrlEncodedContent(values);

            HttpResponseMessage response = await client.PostAsync(url, content);
            string reply = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Ответ сервера: " + reply);

            return response.IsSuccessStatusCode;
         }

        public static async Task<Cookie> SingInAsync(string login, string password)
        {
            string url = "http://127.0.0.1/ajax/login.php";

            var values = new Dictionary<string, string>
            {
                { "login", login },
                { "password", password }
            };

            var content = new FormUrlEncodedContent(values);

            HttpResponseMessage response = await client.PostAsync(url, content);

            string responseText = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Ответ сервера при входе: " + responseText);

            Uri uri = new Uri("http://127.0.0.1");
            Cookie token = handler.CookieContainer.GetCookies(uri)["token"];
            return token;
        }

        public static async Task<string> GetContentAsync(Cookie token)
        {
            if (token != null)
            {
                handler.CookieContainer.Add(new Uri("http://127.0.0.1"), token);
            }

            string url = "http://127.0.0.1/ajax/main.php";
            HttpResponseMessage response = await client.GetAsync(url);

            string responseText = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Ответ сервера:\n{responseText}");
            return responseText;
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

       /* public static async Task ParseAvito()
        {
            string url = "https://www.avito.ru";

            // скачиваем страницу
            HttpClientHandler handler = new HttpClientHandler()
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };

            string html;

            using (HttpClient client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");

                html = await client.GetStringAsync(url);
            }

            // парсим HTML
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var items = doc.DocumentNode.SelectNodes("//div[@itemtype='http://schema.org/Product']");

            if (items == null)
            {
                Console.WriteLine("Объявления не найдены. Возможно, Avito включил защиту.");
                return;
            }

            foreach (var item in items)
            {
                string title = item.SelectSingleNode(".//h3")?.InnerText?.Trim();
                string price = item.SelectSingleNode(".//meta[@itemprop='price']")?.GetAttributeValue("content", "—");
                string link = item.SelectSingleNode(".//a")?.GetAttributeValue("href", "");

                if (!string.IsNullOrEmpty(link) && link.StartsWith("/"))
                    link = "https://www.avito.ru" + link;

                Console.WriteLine($"Название: {title}");
                Console.WriteLine($"Цена: {price} ₽");
                Console.WriteLine($"Ссылка: {link}");
                Console.WriteLine();
            }
        }*/
    }
}
