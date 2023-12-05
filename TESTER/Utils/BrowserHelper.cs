using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace TESTER
{
    public static class BrowserHelper
    {
        public enum BrowserType
        {
            Firefox,
            Chrome,
            Edge
        }

        public static string GetBrowserVersion(BrowserType browserType)
        {
            string version = "Nie można odczytać wersji";

            try
            {
                switch (browserType)
                {
                    case BrowserType.Firefox:
                        version = GetVersionFromRegistry(@"SOFTWARE\mozilla.org\Mozilla", "CurrentVersion", Registry.LocalMachine);
                        break;

                    case BrowserType.Chrome:
                        string chromePath = GetChromePathFromRegistry();
                        version = !string.IsNullOrEmpty(chromePath) ? GetFileVersion(chromePath) : "Nie można odczytać wersji";
                        break;

                    case BrowserType.Edge:
                        version = GetVersionFromRegistry(@"Software\Microsoft\Edge\BLBeacon", "Version", Registry.CurrentUser);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return version;
        }

        private static string GetVersionFromRegistry(string keyName, string valueName, RegistryKey baseRegistryKey)
        {
            using (RegistryKey key = baseRegistryKey.OpenSubKey(keyName))
            {
                if (key != null)
                {
                    return key.GetValue(valueName)?.ToString() ?? "Nie można odczytać wersji";
                }
            }
            return "Nie można odczytać wersji";
        }

        private static string GetChromePathFromRegistry()
        {
            const string chromeRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(chromeRegistryKey))
            {
                if (key != null)
                {
                    return key.GetValue("") as string;
                }
            }
            return null;
        }

        private static string GetFileVersion(string filePath)
        {
            try
            {
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);
                return fileVersionInfo.FileVersion;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return "Nie można odczytać wersji";
        }
    }
}
