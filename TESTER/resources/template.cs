using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TESTER.resources
{
    class Template
    {
        output.Text =
                    $@"*1. Dane środowiska testowego:*
|Przeglądarka:|{browserType} wersja: {webengine}|
|Adres środowiska:|[{ip}]|
|Adres Bazy danych:|{DataManager.AdresBazyDanych}|
|NR Kompilacji AMMS:|{DataManager.NrKompilacji}|
|Data kompilacji AMMS:|{DataManager.DataKompilacji}|
|Numer rewizji AMMS:|{DataManager.NrRewizji}|

*2. Dane przypadku testowego:*
|Użytkownik/Hasło|{username}/{password}|";

if (!String.IsNullOrEmpty(idkjos)) output.Text +=
$@"
|IDK_JOS:|{idkjos}|";

if (!String.IsNullOrEmpty(nrpesel)) output.Text +=
$@"
|PESEL:|{nrpesel}|";

if (!String.IsNullOrEmpty(IdPacjenta)) output.Text +=
$@"
|ID_PAC:|{IdPacjenta}|";

if (!String.IsNullOrEmpty(IdOpieki)) output.Text +=
$@"
|ID_OPI:|{IdOpieki}|";

if (!String.IsNullOrEmpty(idPob)) output.Text +=
$@"
|ID_POB:|{idPob}|";
                    
if (!String.IsNullOrEmpty(IdZlec)) output.Text +=
$@"
|ID_ZLEC:|{IdZlec}|";

output.Text +=
$@"

*3. Kroki postępowania:*
|Ścieżka:|{sciezka}|

*4. Uzyskany rezultat*
{podsumowanie}
";
    }
}
