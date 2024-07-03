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
                // Trim the trailing slash and "index.html" if present
                string linkWithoutIndex = link.TrimEnd('/');
                if (linkWithoutIndex.EndsWith("index.html"))
                {
                    linkWithoutIndex = linkWithoutIndex[..^"index.html".Length];
                }

                if (!Uri.IsWellFormedUriString(linkWithoutIndex, UriKind.Absolute))
                {
                    connected = false;
                    return;
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

                // POST request to fetch serviceJson
                string postData = "{\"userId\":\"1\"}";
                await Task.Delay(1000000);
                StringContent content = new StringContent(postData, System.Text.Encoding.UTF8, "application/json");
                string serviceJsonString = await client.PostAsync(postLink, content).Result.Content.ReadAsStringAsync();
                serviceJson = JObject.Parse(serviceJsonString);

                connected = true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException error: {ex.Message}\n");
                connected = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Other error: {ex.Message}\n");
                connected = false;
            }

        }
    }
}
