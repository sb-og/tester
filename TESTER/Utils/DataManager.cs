using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TESTER.Utils
{
    public class DataManager
    {
        // Zmienne globalne
        public static string NrKompilacji;
        public static string DataKompilacji;
        public static string AdresBazyDanych;
        public static string NrRewizji;
        public static JObject buildJson;
        public static JObject serviceJson;

        // Metoda do przetwarzania buildJson
        public static void ProcessBuildJson(JObject buildJson)
        {
            try
            {
                // Pobieranie wartości z JSON-a i przypisywanie do zmiennych globalnych
                NrKompilacji = buildJson["buildNumber"].ToString();
                DataKompilacji = buildJson["buildDate"].ToString();
            }
            catch (Exception ex)
            {
                // Obsługa błędów
                Console.WriteLine($"Błąd podczas przetwarzania buildJson: {ex.Message}");
            }
        }

        // Metoda do przetwarzania serviceJson
        public static void ProcessServiceJson(JObject serviceJson)
        {
            try
            {
                // Pobieranie wartości z JSON-a i przypisywanie do zmiennych globalnych
                AdresBazyDanych = serviceJson["additionalData"]["AMMS_BIALA"]["return"].ToString();
                NrRewizji = serviceJson["additionalData"]["SVN_REVISION"]["return"].ToString();
            }
            catch (Exception ex)
            {
                // Obsługa błędów
                Console.WriteLine($"Błąd podczas przetwarzania serviceJson: {ex.Message}");
            }
        }

        // Metoda do pobierania danych z serwera
        public static async Task<(bool buildJsonLoaded, bool serviceJsonLoaded)> ScrapeDataAsync(string link)
        {
            bool buildJsonLoaded = false;
            bool serviceJsonLoaded = false;

            try
            {
                // Usunięcie tylko "index.html" jeśli jest obecne, pozostawiając resztę linku nienaruszoną
                if (link.EndsWith("index.html"))
                {
                    link = link[..^"index.html".Length];
                }

                // Sprawdzenie, czy link jest prawidłowym URI
                if (!Uri.IsWellFormedUriString(link, UriKind.Absolute))
                {
                    return (buildJsonLoaded, serviceJsonLoaded);
                }

                string sourceFilePath = "mspa/anboot/build.json";
                string postPath = "rest/shellService/prepareSystemInfoViewInfo?spinner=disabled";

                // Konstruowanie pełnych ścieżek do plików GET i POST
                string fullLink = new Uri(new Uri(link), sourceFilePath).ToString();
                string postLink = new Uri(new Uri(link), postPath).ToString();

                using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };

                // Pobieranie build.json
                string buildJsonString = await client.GetStringAsync(fullLink);
                buildJson = JObject.Parse(buildJsonString);
                ProcessBuildJson(buildJson);
                buildJsonLoaded = true;

                // Wysyłanie żądania POST do uzyskania serviceJson
                string postData = "{\"userId\":\"1\"}";
                StringContent content = new StringContent(postData, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(postLink, content);
                response.EnsureSuccessStatusCode();

                string serviceJsonString = await response.Content.ReadAsStringAsync();
                serviceJson = JObject.Parse(serviceJsonString);
                ProcessServiceJson(serviceJson);
                serviceJsonLoaded = true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException error: {ex.Message}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Other error: {ex.Message}\n");
            }

            return (buildJsonLoaded, serviceJsonLoaded);
        }
    }
}
