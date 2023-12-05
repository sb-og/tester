using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTER
{
    internal static class ExtractData
    {
        public static string ExtractFrom(string envdata, int index)
        {
            string[] lines = envdata.Split('\n');
            string result = "";

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(":"))
                {
                    if (index == 0 && lines[i].Contains("Numer kompilacji:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                    else if (index == 1 && lines[i].Contains("Data kompilacji:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                    else if (index == 2 && lines[i].Contains("Nr rewizji:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                    else if (index == 3 && lines[i].Contains("Moduł:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                    else if (index == 4 && lines[i].Contains("Nr budowy modułu:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                    else if (index == 5 && lines[i].Contains("Rozdzielczość:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                    else if (index == 6 && lines[i].Contains("Tryb debug:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                    else if (index == 7 && lines[i].Contains("Adres bazy danych:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                    else if (index == 8 && lines[i].Contains("Adres szarej bazy danych:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                    else if (index == 9 && lines[i].Contains("Adres serwera aplikacji:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                    else if (index == 10 && lines[i].Contains("Adres stacji klienta:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                    else if (index == 11 && lines[i].Contains("EDM:"))
                    {
                        result = lines[i + 1].Trim();
                        break;
                    }
                }
            }

            return result;
        }
    }
}
