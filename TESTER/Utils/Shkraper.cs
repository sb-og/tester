using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TESTER.Utils
{
    public class Shkrape
    {
        public static bool? connected = null;
        public static JObject buildJson;
        public static JObject serviceJson;

        public static async Task ScrapeDataAsync(string link)
        {
            try
            {
                string linkWithoutIndex = link.TrimEnd('/');
                if (linkWithoutIndex.EndsWith("index.html"))
                {
                    linkWithoutIndex = linkWithoutIndex.Substring(0, linkWithoutIndex.Length - "index.html".Length);
                }

                if (Uri.IsWellFormedUriString(linkWithoutIndex, UriKind.Absolute))
                {
                    string sourceFilePath = "/mspa/anboot/build.json";
                    string postPath = "/rest/shellService/prepareSystemInfoViewInfo?spinner=disabled";

                    UriBuilder uriBuilder = new UriBuilder(linkWithoutIndex);
                    uriBuilder.Path += sourceFilePath;
                    string fullLink = uriBuilder.Uri.ToString();

                    using (HttpClient client = new HttpClient())
                    {
                        // Ustaw timeout na 30 sekund
                        client.Timeout = TimeSpan.FromSeconds(30);

                        string buildJsonString = await client.GetStringAsync(fullLink);
                        buildJson = JObject.Parse(buildJsonString);
                    }

                    string postLink = linkWithoutIndex + postPath;
                    string postData = "{\"userId\":\"1\"}";

                    using (HttpClient client = new HttpClient())
                    {
                        // Ustaw timeout na 30 sekund
                        client.Timeout = TimeSpan.FromSeconds(30);

                        StringContent content = new StringContent(postData);
                        content.Headers.ContentType.MediaType = "application/json";
                        string serviceJsonString = await client.PostAsync(postLink, content).Result.Content.ReadAsStringAsync();
                        serviceJson = JObject.Parse(serviceJsonString);
                    }
                    connected = true;
                }
                else
                {
                    connected=false;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Błąd HttpRequestException: {ex.Message}\n");
                connected = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Inny błąd: {ex.Message}\n");
                connected = false;
            }
        }
    }
}
