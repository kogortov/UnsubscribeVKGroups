using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace UnsubscribeVKGroups
{
    class Program
    {
        const string API_VERSION = "5.199";

        static async Task Main(string[] args)
        {
            Console.Title = "Unsubscribe VK Groups";

            Console.ForegroundColor = ConsoleColor.Blue;

            Console.WriteLine(@"
                                                                                         
 _____             _               _ _          _____ _____    _____                     
|  |  |___ ___ _ _| |_ ___ ___ ___|_| |_ ___   |  |  |  |  |  |   __|___ ___ _ _ ___ ___ 
|  |  |   |_ -| | | . |_ -|  _|  _| | . | -_|  |  |  |    -|  |  |  |  _| . | | | . |_ -|
|_____|_|_|___|___|___|___|___|_| |_|___|___|   \___/|__|__|  |_____|_| |___|___|  _|___|
                                                                                |_|      
");


            Console.WriteLine("Автор: Кирилл Когортов.");
            Console.WriteLine("Дата релиза: 02.08.2025");
            Console.ResetColor(); // вернуть обычный цвет

            Console.WriteLine("\nВведите id вашего VK-приложения:");
            string clientId = Console.ReadLine();

            string authUrl = $"https://oauth.vk.com/authorize?client_id={clientId}&display=page&redirect_uri=https://oauth.vk.com/blank.html&scope=groups,offline&response_type=token&v={API_VERSION}";

            Console.WriteLine("\nОткройте ссылку в браузере и авторизуйтесь:");
            Console.WriteLine(authUrl);

            Console.WriteLine("\nПосле авторизации скопируйте ссылку из адресной строки.");

            string accessToken = null;

            while (string.IsNullOrEmpty(accessToken))
            {

                string rawInput = ReadFullLine();
                accessToken = ExtractAccessToken(rawInput);

                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Не удалось извлечь access_token. Убедитесь, что вы вставили ССЫЛКУ, а не только токен.");
                    Console.ResetColor();
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nТокен получен. Начинаем отписку...\n");
            Console.ResetColor();

            var httpClient = new HttpClient();
            int offset = 0;
            int count = 1000;
            int totalUnsubscribed = 0;

            while (true)
            {
                var groups = await GetGroups(httpClient, accessToken, offset, count);
                var groupIds = groups["items"]?.ToObject<long[]>();
                if (groupIds == null || groupIds.Length == 0) break;

                foreach (var groupId in groupIds)
                {
                    bool success = await LeaveGroup(httpClient, accessToken, groupId);
                    if (success)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Отписался от группы ID: {groupId}");
                        Console.ResetColor();
                        totalUnsubscribed++;
                        await Task.Delay(350);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Ошибка при отписке от {groupId}");
                        Console.ResetColor();
                    }
                }

                offset += count;
            }

            Console.WriteLine($"Готово! Всего отписался от {totalUnsubscribed} групп.");

            Console.WriteLine("\nНажмите любую клавишу, чтобы выйти...");
            Console.ReadKey();
        }

        static async Task<JObject> GetGroups(HttpClient client, string token, int offset, int count)
        {
            string url = $"https://api.vk.com/method/groups.get?access_token={token}&v={API_VERSION}&extended=0&offset={offset}&count={count}";
            var response = await client.GetStringAsync(url);
            return JObject.Parse(response)["response"] as JObject;
        }

        static async Task<bool> LeaveGroup(HttpClient client, string token, long groupId)
        {
            string url = $"https://api.vk.com/method/groups.leave?access_token={token}&v={API_VERSION}&group_id={groupId}";
            var response = await client.GetStringAsync(url);
            var result = JObject.Parse(response);
            return result["response"]?.Value<int>() == 1;
        }


        static string ReadFullLine()
        {
            Console.WriteLine("Вставьте ссылку ниже, и нажмите Enter после вставки:");

            string input = "";
            ConsoleKeyInfo key;

            while (true)
            {
                key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                // Ctrl+V вставляет сразу несколько символов — поддерживается
                input += key.KeyChar;
                Console.Write(key.KeyChar); // отображаем символ
            }

            return input;
        }


        static string ExtractAccessToken(string input)
        {
            try
            {
                const string marker = "access_token=";
                int start = input.IndexOf(marker);
                if (start == -1) return null;

                start += marker.Length;
                string tail = input.Substring(start);

                int end = tail.IndexOf('&');
                if (end != -1)
                    tail = tail.Substring(0, end);

                return tail;
            }
            catch
            {
                return null;
            }
        }

    }
}
