using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Interop;
using Newtonsoft.Json;
using TESTER.Utils;
using WpfAnimatedGif;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using System.CodeDom;


namespace TESTER
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Scroller _scroller;
        private bool isResizing = false;
        private Point lastMousePosition;


        public MainWindow()
        {

            InitializeComponent();
            _scroller = new Scroller(this);


            //update or create config
            ConfigHelper.CreateConfigFile();

            //Load settings
            Topmost = bool.Parse(ConfigHelper.ReadSetting("Topmost"));
            user.Text = ConfigHelper.ReadSetting("User");
            pwd.Text = ConfigHelper.ReadSetting("Password");
            browserComboBox.Text = ConfigHelper.ReadSetting("Browser");



            Credits.Text = this.Title.ToString() + " By: Szymon Bogus";


            AddMenuItem("Zapisz", MenuSave_Click);
            AddMenuItem("Zawsze na wierzchu", MenuAlwaysOnTop_Click);
            AddMenuItem("Sprawdź aktualizacje", MenuCheckUpdates_Click);
            AddMenuItem("Otwórz config", MenuOpenConfig_Click);
        }

        private void Menu_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Sprawdź, czy menu kontekstowe istnieje i nie jest już otwarte
            if (menu.ContextMenu != null && !menu.ContextMenu.IsOpen)
            {
                // Otwórz menu kontekstowe
                menu.ContextMenu.PlacementTarget = sender as Button;
                menu.ContextMenu.IsOpen = true;

                // Zablokuj dalsze przetwarzanie zdarzenia, aby uniknąć wywołania Click
                e.Handled = true;
            }
        }
        private void AddMenuItem(string header, RoutedEventHandler handler)
        {
            var menuItem = new MenuItem { Header = header };
            menuItem.Click += handler; // Przypisanie obsługi zdarzeń
            menu.ContextMenu.Items.Add(menuItem); // Dodawanie do menu kontekstowego
        }
        private void MenuSave_Click(object sender, RoutedEventArgs e)
        {
            ConfigHelper.SaveSetting("Browser", browserComboBox.Text);

            string key1 = "User";
            string value1 = user.Text;
            ConfigHelper.SaveSetting(key1, value1);

            string key2 = "Password";
            string value2 = pwd.Text;
            ConfigHelper.SaveSetting(key2, value2);
        }

        private void MenuAlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            // Logika dla "Zawsze na wierzchu" - przełączanie trybu zawsze na wierzchu
            var window = Application.Current.MainWindow;
            window.Topmost = !window.Topmost; // Przełączanie wartości
        }

        private void MenuCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            // Logika dla "Sprawdź aktualizacje"
        }

        private void MenuOpenConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Sprawdzenie, czy plik istnieje
                if (File.Exists(ConfigHelper.ConfigFilePath))
                {
                    // Otwarcie pliku w domyślnym edytorze tekstu
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = ConfigHelper.ConfigFilePath,
                        UseShellExecute = true // Wymagane dla .NET Core/.NET 5/6+
                    });
                }
                else
                {
                    MessageBox.Show($"Plik {ConfigHelper.ConfigFilePath} nie istnieje.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                // Obsługa błędów, np. wyświetlenie komunikatu
                MessageBox.Show($"Wystąpił problem podczas otwierania pliku: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void customResizeGrip_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                // Ustaw współczynnik skalowania dla szerokości i wysokości
                double newWidth = this.ActualWidth + (e.HorizontalChange);
                double newHeight = this.ActualHeight + (e.VerticalChange);

                // Ogranicz nowy rozmiar okna do maksymalnego rozmiaru ekranu
                double maxWidth = SystemParameters.WorkArea.Width;
                double maxHeight = SystemParameters.WorkArea.Height;

                // Sprawdź minimalne i maksymalne wymiary okna przed skalowaniem
                this.Width = Math.Max(this.MinWidth, Math.Min(newWidth, maxWidth));
                this.Height = Math.Max(this.MinHeight, Math.Min(newHeight, maxHeight));
            }
        }


        static string ExtractCaseData(string data, int index)
        {
            string[] lines = data.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(":"))
                {
                    if (index == 0 && lines[i].Contains("Identyfikator pacjenta:"))
                    {
                        return lines[i].Split(':')[1].Trim();
                    }
                    else if (index == 1 && lines[i].Contains("Identyfikator opieki:"))
                    {
                        return lines[i].Split(':')[1].Trim();
                    }
                    else if (index == 2 && lines[i].Contains("Identyfikator pobytu:"))
                    {
                        return lines[i].Split(':')[1].Trim();
                    }
                    else if (index == 3 && lines[i].Contains("Identyfikator zlecenia:"))
                    {
                        return lines[i].Split(':')[1].Trim();
                    }
                }
            }

            // Return an empty string if the specified index is out of bounds
            return "";

        }

        private async void autoUpdateOutput(object sender, RoutedEventArgs e)
        {
            if (ConfigHelper.ReadSetting("InstaFill") == "True")
            {
                updateOutput(sender, e);
            }

        }
        private async void updateOutput(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1);
            string ip = address.Text;
            string idkjos = jos.Text;
            string pacjent = pac.Text;
            string jednostka = jos.Text;
            string sciezka = path.Text;
            string opis = desc.Text;
            string nrpesel = pesel.Text;
            string podsumowanie = desc.Text;

            //stare uzupełnianie przeglądarki    string webengine = browser.Text.Replace(Environment.NewLine, " '
            string username = user.Text;
            string password = pwd.Text;

            //Dane przypadku testowego
            string IdPacjenta = ExtractCaseData(pacjent, 0); // Identyfikator pacjenta
            string IdOpieki = ExtractCaseData(pacjent, 1); // Identyfikator opieki
            string idPob = ExtractCaseData(pacjent, 2); // Identyfikator pobytu
            string IdZlec = ExtractCaseData(pacjent, 3); // Identyfikator zlecenia


            string webengine;
            string selectedBrowser;
            if (browserComboBox.SelectedItem != null)
            {
                // Pobierz tekst z wybranego elementu ComboBox
                selectedBrowser = ((ComboBoxItem)browserComboBox.SelectedItem).Content.ToString();

                // Konwertuj tekst na odpowiedni BrowserType
                if (Enum.TryParse(selectedBrowser, out BrowserHelper.BrowserType browserType))
                {
                    // Teraz możesz przekazać browserType do funkcji GetBrowserVersion
                    webengine = BrowserHelper.GetBrowserVersion(browserType);


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
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {

            if (ConfigHelper.ReadSetting("WarnOnExit") == "True")
            {
                var result = MessageBox.Show("Zamknięcie okna spowoduje utratę wprowadzonych danych", "Uwaga", MessageBoxButton.OKCancel, MessageBoxImage.None);

                if (result == MessageBoxResult.OK)
                {
                    // Logika zapisywania zmian
                    this.Close(); // Zamknij okno tylko jeśli użytkownik zdecydował się zapisać zmiany
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
                this.Close();
            }
            this.Close();

        }

        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(output.Text);
        }
        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && !textBox.IsKeyboardFocusWithin)
            {
                textBox.Focus();
                e.Handled = true; // Zapobiega dodatkowemu przetwarzaniu zdarzenia
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox?.SelectAll();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            address.Text = string.Empty;

            pac.Text = "Identyfikator pacjenta: \nIdentyfikator opieki: \nIdentyfikator pobytu: \nIdentyfikator zlecenia: ";

            pesel.Text = string.Empty;
            jos.Text = string.Empty;

            path.Text = string.Empty;
            desc.Text = string.Empty;


            DataManager.NrRewizji = string.Empty;
            DataManager.AdresBazyDanych = string.Empty;
            DataManager.NrKompilacji = string.Empty;
            DataManager.DataKompilacji = string.Empty;
            DataManager.AdresBazyDanych = string.Empty;


            ImageBehavior.SetAnimatedSource(ConnectionIndicator, null);

            output.Text = string.Empty;
        }


        private async void address_TextChanged(object sender, TextChangedEventArgs e)
        {
            DataManager.NrRewizji = string.Empty;
            DataManager.AdresBazyDanych = string.Empty;
            DataManager.NrKompilacji = string.Empty;
            DataManager.DataKompilacji = string.Empty;
            DataManager.AdresBazyDanych = string.Empty;

            // Ustawienie wskaźnika ładowania (spinner)
            var gifImage = new BitmapImage(new Uri("resources/spinner.gif", UriKind.RelativeOrAbsolute));
            ImageBehavior.SetAnimatedSource(ConnectionIndicator, gifImage);

            void SetAddressColumnSpan(bool isIconVisible)
            {
                Grid.SetColumnSpan(address, isIconVisible ? 1 : 2);
            }

            SetAddressColumnSpan(true);

            string baseLink = address.Text;
            if (!string.IsNullOrEmpty(baseLink))
            {
                try
                {
                    // Pobranie danych
                    var (buildJsonLoaded, serviceJsonLoaded) = await DataManager.ScrapeDataAsync(baseLink);
                    await Task.Delay(1000);

                    // Ustawienie odpowiedniego wskaźnika w zależności od stanu połączenia
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();

                    if (!buildJsonLoaded && !serviceJsonLoaded)
                    {
                        bitmap.UriSource = new Uri("resources/checkmark_red.png", UriKind.RelativeOrAbsolute);
                    }
                    else if (buildJsonLoaded && !serviceJsonLoaded)
                    {
                        bitmap.UriSource = new Uri("resources/checkmark_yellow.png", UriKind.RelativeOrAbsolute);
                        updateOutput(sender, e);
                    }
                    else if (buildJsonLoaded && serviceJsonLoaded)
                    {
                        bitmap.UriSource = new Uri("resources/checkmark_green.png", UriKind.RelativeOrAbsolute);
                        updateOutput(sender, e);
                    }

                    bitmap.EndInit();
                    ImageBehavior.SetAnimatedSource(ConnectionIndicator, bitmap);
                }
                catch (Exception ex)
                {
                    output.Text = $"Wystąpił nieoczekiwany błąd: {ex.Message}";
                    SetAddressColumnSpan(true);
                    ImageBehavior.SetAnimatedSource(ConnectionIndicator, new BitmapImage(new Uri("resources/checkmark_red.png", UriKind.RelativeOrAbsolute)));
                }
            }
            else
            {
                await Task.Delay(1000);
                ImageBehavior.SetAnimatedSource(ConnectionIndicator, null);
                SetAddressColumnSpan(false);
            }
        }
        private void ConnectionIndicator_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Wywołanie zdarzenia address_TextChanged
            address_TextChanged(sender, null);
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                // Przywracanie okna do normalnego rozmiaru
                this.WindowState = WindowState.Normal;
            }
            else
            {
                // Maksymalizacja okna
                this.WindowState = WindowState.Maximized;
            }
        }


        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}

