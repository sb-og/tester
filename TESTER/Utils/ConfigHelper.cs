using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static class ConfigHelper
{
    private const string ConfigFilePath = "config.json";

    private static Dictionary<string, string> DefaultSettings = new Dictionary<string, string>
    {
        { "User", "ADMIN" },
        { "Password", "ADMIN" },
        { "Topmost", "False" },
        { "Browser", "Edge" },
        { "Theme", "Dark" }  // Domyślny motyw ustawiony na "Dark"
        // Dodaj kolejne domyślne ustawienia w formie par klucz-wartość
    };

    private static Dictionary<string, string> CurrentSettings;

    public static void CreateConfigFile()
    {
        try
        {
            // Odczytaj aktualne ustawienia z pliku
            CurrentSettings = ReadSettings();

            // Sprawdź brakujące klucze i dodaj domyślne wartości
            foreach (var defaultSetting in DefaultSettings)
            {
                if (!CurrentSettings.ContainsKey(defaultSetting.Key))
                {
                    CurrentSettings[defaultSetting.Key] = defaultSetting.Value;
                }
            }

            // Zapisz zaktualizowane dane do pliku
            SaveSettings(CurrentSettings);

            Console.WriteLine("Plik konfiguracyjny został zaktualizowany pomyślnie.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił błąd podczas tworzenia/plik konfiguracyjnego: {ex.Message}");
        }
    }

    private static Dictionary<string, string> ReadSettings()
    {
        try
        {
            // Otwórz plik konfiguracyjny do odczytu
            string json = File.Exists(ConfigFilePath) ? File.ReadAllText(ConfigFilePath) : null;

            if (!string.IsNullOrEmpty(json))
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił błąd podczas odczytywania ustawień: {ex.Message}");
        }

        return new Dictionary<string, string>();
    }

    private static void SaveSettings(Dictionary<string, string> settings)
    {
        try
        {
            // Zapisz ustawienia do pliku JSON
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);

            Console.WriteLine("Ustawienia zostały zapisane pomyślnie.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił błąd podczas zapisywania ustawień: {ex.Message}");
        }
    }

    public static void SaveSetting(string key, string value)
    {
        try
        {
            // Zaktualizuj wartość dla podanego klucza
            CurrentSettings[key] = value;

            // Zapisz zaktualizowane dane z powrotem do pliku
            SaveSettings(CurrentSettings);

            Console.WriteLine($"Ustawienie {key} zostało zapisane pomyślnie.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił błąd podczas zapisywania ustawienia: {ex.Message}");
        }
    }

    public static string ReadSetting(string key)
    {
        try
        {
            // Odczytaj wartość dla podanego klucza
            if (CurrentSettings.ContainsKey(key))
            {
                return CurrentSettings[key];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił błąd podczas odczytywania ustawienia: {ex.Message}");
        }

        return null;
    }
}