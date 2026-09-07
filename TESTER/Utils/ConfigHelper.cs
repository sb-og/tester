using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static class ConfigHelper
{
    public const string ConfigFilePath = "config.json";

    private static readonly Dictionary<string, string> DefaultSettings = new Dictionary<string, string>
    {
        { "User", "ADMIN" },
        { "Password", "ADMIN" },
        { "Topmost", "False" },
        { "Browser", "Edge" },
        { "WindowOpacity", "1.0" },
        { "InstaFill", "False" },
        { "WarnOnExit", "False" },
        { "GenerateEmptyFields", "True" },
        { "PreserveWindowSize", "True" },
        { "WindowWidth", "" },
        { "WindowHeight", "" },
        { "WindowLeft", "" },
        { "WindowTop", "" },
        { "LeftColumnWidth", "" },
        { "RightColumnWidth", "" }
        // Dodaj kolejne domyślne ustawienia w formie par klucz-wartość
    };

    private static Dictionary<string, string>? CurrentSettings;

    public static void CreateConfigFile()
    {
        try
        {
            var settings = GetSettings();

            // Zapisz zaktualizowane dane do pliku
            SaveSettings(settings);

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
            string? json = File.Exists(ConfigFilePath) ? File.ReadAllText(ConfigFilePath) : null;

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

    private static Dictionary<string, string> GetSettings()
    {
        if (CurrentSettings == null)
        {
            CurrentSettings = ReadSettings();
        }

        foreach (var defaultSetting in DefaultSettings)
        {
            if (!CurrentSettings.ContainsKey(defaultSetting.Key))
            {
                CurrentSettings[defaultSetting.Key] = defaultSetting.Value;
            }
        }

        return CurrentSettings;
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
            var settings = GetSettings();

            // Zaktualizuj wartość dla podanego klucza
            settings[key] = value;

            // Zapisz zaktualizowane dane z powrotem do pliku
            SaveSettings(settings);

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
            var settings = GetSettings();

            // Odczytaj wartość dla podanego klucza
            if (settings.ContainsKey(key))
            {
                return settings[key];
            }

            if (DefaultSettings.ContainsKey(key))
            {
                return DefaultSettings[key];
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił błąd podczas odczytywania ustawienia: {ex.Message}");
        }

        return String.Empty;
    }
}