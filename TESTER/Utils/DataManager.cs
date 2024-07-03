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
                // Trim the trailing slash and "index.html" if present
                string linkWithoutIndex = link.TrimEnd('/');
                if (linkWithoutIndex.EndsWith("index.html"))
                {
                    linkWithoutIndex = linkWithoutIndex[..^"index.html".Length];
                }

                if (!Uri.IsWellFormedUriString(linkWithoutIndex, UriKind.Absolute))
                {
                    return (buildJsonLoaded, serviceJsonLoaded);
                }

                string sourceFilePath = "/mspa/anboot/build.json";
                string postPath = "/rest/shellService/prepareSystemInfoViewInfo?spinner=disabled";

                // Construct the full URI for GET request
                string fullLink = new Uri(new Uri(linkWithoutIndex), sourceFilePath).ToString();
                string postLink = new Uri(new Uri(linkWithoutIndex), postPath).ToString();

                using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

                // GET request to fetch build.json
                string buildJsonString = await client.GetStringAsync(fullLink);
                buildJson = JObject.Parse(buildJsonString);
                ProcessBuildJson(buildJson);
                buildJsonLoaded = true;

                // POST request to fetch serviceJson with a timeout of 3 seconds
                string postData = "{\"userId\":\"1\"}";
                StringContent content = new StringContent(postData, System.Text.Encoding.UTF8, "application/json");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                HttpResponseMessage response = await client.PostAsync(postLink, content, cts.Token);
                response.EnsureSuccessStatusCode(); // Throw if not a success code.

                string serviceJsonString = await response.Content.ReadAsStringAsync(cts.Token);
                serviceJson = JObject.Parse(serviceJsonString);
                ProcessServiceJson(serviceJson);
                serviceJsonLoaded = true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException error: {ex.Message}\n");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Request timed out.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Other error: {ex.Message}\n");
            }

            return (buildJsonLoaded, serviceJsonLoaded);
        }
    }
}
