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
            SingIn("user", "user");
            Console.Read();
        }

        public static void SingIn(string Login,string Password)
        {
            string url = "http://localhost/ajax/login.php";
            Debug.WriteLine($"Выполняем запрос: {url}");
            HttpWebRequest request= (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.CookieContainer= new CookieContainer();
            string postData = $"login={Login}&password={Password}";
            byte[] Data= Encoding.ASCII.GetBytes(postData);
            request.ContentLength = Data.Length;
            using (var stream=request.GetRequestStream())
            {
                stream.Write(Data, 0, Data.Length);
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Debug.WriteLine($"Статус выполнения: {response.StatusCode}");
            string responseFromServer= new StreamReader(response.GetResponseStream()).ReadToEnd();  
            Console.WriteLine(responseFromServer);
        }
    }
}
