using Newtonsoft.Json.Linq;
using System;

namespace TESTER.Utils
{
    public class DataManager
    {
        // Zmienne globalne
        public static string NrKompilacji;
        public static string DataKompilacji;

        public static string AdresBazyDanych;
        public static string NrRewizji;
        public static string IsUpToDate;


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

        // Metoda do przetwarzania serviceJson (do zaimplementowania w późniejszym etapie)
        public static void ProcessServiceJson(JObject serviceJson)
        {
            try
            {
                // Pobieranie wartości z JSON-a i przypisywanie do zmiennych globalnych
                AdresBazyDanych = serviceJson["additionalData"]["AMMS_BIALA"]["return"].ToString();
                NrRewizji = serviceJson["additionalData"]["SVN_REVISION"]["return"].ToString();
                IsUpToDate = serviceJson["status"].ToString();

                // ... (jeśli są inne dane, kontynuuj przypisywanie zmiennych globalnych)
            }
            catch (Exception ex)
            {
                // Obsługa błędów
                Console.WriteLine($"Błąd podczas przetwarzania serviceJson: {ex.Message}");
            }
        }

    }
}