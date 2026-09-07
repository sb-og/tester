using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TESTER.Utils
{
    public static class DatabaseAddressCache
    {
        private const string CacheFilePath = "database-address-cache.json";

        public static bool TryGet(string environmentAddress, out string databaseAddress)
        {
            return ReadEntries().TryGetValue(NormalizeEnvironmentAddress(environmentAddress), out databaseAddress!);
        }

        public static IReadOnlyList<string> GetDatabaseAddresses()
        {
            return ReadEntries()
                .Values
                .Where(databaseAddress => !String.IsNullOrWhiteSpace(databaseAddress))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(databaseAddress => databaseAddress, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void Save(string environmentAddress, string databaseAddress)
        {
            string environmentKey = NormalizeEnvironmentAddress(environmentAddress);
            if (String.IsNullOrWhiteSpace(environmentKey) || String.IsNullOrWhiteSpace(databaseAddress))
            {
                return;
            }

            Dictionary<string, string> entries = ReadEntries();
            entries[environmentKey] = databaseAddress.Trim();
            File.WriteAllText(CacheFilePath, JsonConvert.SerializeObject(entries, Formatting.Indented));
        }

        private static Dictionary<string, string> ReadEntries()
        {
            try
            {
                if (!File.Exists(CacheFilePath))
                {
                    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                string json = File.ReadAllText(CacheFilePath);
                Dictionary<string, string>? entries = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                return entries == null
                    ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(entries, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas odczytu cache adresów baz danych: {ex.Message}");
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static string NormalizeEnvironmentAddress(string environmentAddress)
        {
            if (String.IsNullOrWhiteSpace(environmentAddress))
            {
                return String.Empty;
            }

            string normalizedAddress = environmentAddress.Trim();
            if (normalizedAddress.EndsWith("index.html", StringComparison.OrdinalIgnoreCase))
            {
                normalizedAddress = normalizedAddress[..^"index.html".Length];
            }

            return normalizedAddress.TrimEnd('/').ToLowerInvariant();
        }
    }
}