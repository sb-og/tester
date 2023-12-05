// Craper.cs

using System;
using System.IO;
using System.Net;

namespace TESTER
{
    public class Craper
    {
        public static string ScrapeData(string link)
        {
            // Inicjalizuj wynik funkcji
            string result = string.Empty;

            try
            {
                // Usuń ewentualne wystąpienie index.html z linku
                link = link.TrimEnd('/');
                if (link.EndsWith("index.html"))
                {
                    link = link.Substring(0, link.Length - "index.html".Length);
                }

                // Sprawdź, czy link jest prawidłowym adresem URL
                if (Uri.IsWellFormedUriString(link, UriKind.Absolute))
                {
                    // Ścieżka do pliku źródłowego strony
                    string sourceFilePath = "/mspa/anboot/build.json";
                    string post = "/rest/shellService/prepareSystemInfoViewInfo?spinner=disabled";

                    // Skorzystaj z Uri do poprawnego połączenia części linku
                    UriBuilder uriBuilder = new UriBuilder(link);
                    uriBuilder.Path += sourceFilePath;
                    string fullLink = uriBuilder.Uri.ToString();

                    result = fullLink;

                    // Odczytaj dane z linku
                    using (WebClient client = new WebClient())
                    {
                        string data = client.DownloadString(fullLink);
                        result = data;
                    }
                }
                else
                {
                    result = "Błąd: Podany link nie jest prawidłowym adresem URL.";
                }
            }
            catch (UriFormatException ex)
            {
                // Obsłuż błąd w przypadku nieprawidłowego adresu URL
                result = $"Błąd UriFormatException: {ex.Message}\n";
            }
            catch (Exception ex)
            {
                // Obsłuż inne błędy
                result = $"Inny błąd: {ex.Message}\n";
            }

            return result;
        }
    }
}
