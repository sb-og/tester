using System;
using System.IO;
using System.Net;

class AmmsScraper
{
    static void Main(string[] args)
    {
        foreach (var link in args)
        {
            string modifiedLink = ModifyLink(link);

            if (!string.IsNullOrEmpty(modifiedLink))
            {
                string fullPath = GetFullPath(modifiedLink);

                if (!string.IsNullOrEmpty(fullPath))
                {
                    string fileContent = ReadFileContent(fullPath);

                    if (!string.IsNullOrEmpty(fileContent))
                    {
                        Console.WriteLine($"Zawartość pliku dla zmodyfikowanego linku '{modifiedLink}':\n{fileContent}\n");
                    }
                    else
                    {
                        Console.WriteLine($"Nie udało się odczytać zawartości pliku dla linku '{modifiedLink}'.\n");
                    }
                }
                else
                {
                    Console.WriteLine($"Nieprawidłowy format linku: '{modifiedLink}'.\n");
                }
            }
            else
            {
                Console.WriteLine($"Nieprawidłowy format linku: '{link}'.\n");
            }
        }
    }

    static string ModifyLink(string link)
    {
        // Zakłada, że link kończy się na "index.html"
        const string targetSuffix = "index.html";
        const string replacement = "mspa/anboot/build.json";

        if (link.EndsWith(targetSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return link.Substring(0, link.Length - targetSuffix.Length) + replacement;
        }
        else
        {
            return null; // Link nie spełnia oczekiwań
        }
    }

    static string GetFullPath(string link)
    {
        // Obsługa różnych formatów linków (tu można dodać dodatkowe przypadki)
        if (Uri.TryCreate(link, UriKind.Absolute, out Uri uri))
        {
            // Przyjęcie linku jako pełna ścieżka, jeśli jest już absolutny
            return uri.AbsoluteUri;
        }
        else
        {
            // Dopisanie ścieżki do pliku, zakładając, że jest to ścieżka względna
            return Path.Combine(Directory.GetCurrentDirectory(), link);
        }
    }

    static string ReadFileContent(string fullPath)
    {
        try
        {
            // Odczytanie zawartości pliku
            return File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas odczytu pliku '{fullPath}': {ex.Message}");
            return null;
        }
    }
}
