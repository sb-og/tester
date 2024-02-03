﻿using System;
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
            this.ResizeMode = ResizeMode.CanMinimize;
            _scroller = new Scroller(this);


            //update or create config
            ConfigHelper.CreateConfigFile();

            //Load settings
            Topmost = bool.Parse(ConfigHelper.ReadSetting("Topmost"));
            //if (!Topmost) aot.Background= new SolidColorBrush(Colors.Transparent);
            //else aot.Background = new SolidColorBrush(Colors.Black);

            user.Text = ConfigHelper.ReadSetting("User");
            pwd.Text = ConfigHelper.ReadSetting("Password");
            browserComboBox.Text = ConfigHelper.ReadSetting("Browser");



            Credits.Text = this.Title.ToString() + " By: Szymon Bogus";

        }
        private void customResizeGrip_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                // Ustaw współczynnik skalowania dla szerokości i wysokości
                double widthScalingFactor = 0.5;
                double heightScalingFactor = 0.5;
                double newWidth = this.ActualWidth + (e.HorizontalChange * widthScalingFactor);
                double newHeight = this.ActualHeight + (e.VerticalChange * heightScalingFactor);

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
        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            output.Text = null;
            string ip = address.Text;
            string patient = pac.Text;
            string unit = jos.Text;
            string description = desc.Text;
            string nrpesel = pesel.Text;
            string testdesc = desc.Text;
            //stare uzupełnianie przeglądarki    string webengine = browser.Text.Replace(Environment.NewLine, " '
            string idkjos = jos.Text;
            string username = user.Text;
            string password = pwd.Text;
            string steps = path.Text;

            string webengine;
            string selectedBrowser;


            //Dane przypadku testowego
            string IdPacjenta = ExtractCaseData(patient, 0); // Identyfikator pacjenta
            string IdOpieki = ExtractCaseData(patient, 1); // Identyfikator opieki
            string idPob = ExtractCaseData(patient, 2); // Identyfikator pobytu
            string IdZlec = ExtractCaseData(patient, 3); // Identyfikator zlecenia



            // Pobierz bazowy link z pola tekstowego "address"

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
|Ścieżka:|{steps}|

*4. Uzyskany rezultat*
{description}
";
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveUserButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigHelper.SaveSetting("Browser", browserComboBox.Text);

            string key1 = "User";
            string value1 = user.Text;
            ConfigHelper.SaveSetting(key1, value1);

            string key2 = "Password";
            string value2 = pwd.Text;
            ConfigHelper.SaveSetting(key2, value2);
        }

        private void OnTopButton_Click(object sender, RoutedEventArgs e)
        {
            // Przełącz między Topmost i !Topmost
            Topmost = !Topmost;
            //if (!Topmost) aot.Background= new SolidColorBrush(Colors.Transparent);
            //else aot.Background = new SolidColorBrush(Colors.Black);

        }

        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(output.Text);
        }
        private async void ScrapeButton_Click(object sender, RoutedEventArgs e)
        {
            string baseLink = address.Text;
            await Shkrape.ScrapeDataAsync(baseLink);

            try
            {
                output.Text = Shkrape.buildJson.ToString();
                output.Text += Shkrape.serviceJson.ToString();
            }
            catch (Exception ex)
            {
                output.Text = $"Wystąpił nieoczekiwany błąd: {ex.Message}";
                return;
            }

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
            pesel.Text = string.Empty;
            jos.Text = string.Empty;
            pac.Text = "Identyfikator pacjenta: \nIdentyfikator opieki: \nIdentyfikator pobytu: \nIdentyfikator zlecenia: ";
            path.Text = string.Empty; 
            desc.Text = string.Empty;
            address.Text = string.Empty;
            ImageBehavior.SetAnimatedSource(ConnectionIndicator, null);
        }


        private async void address_TextChanged(object sender, TextChangedEventArgs e)
        {
            var gifImage = new BitmapImage();
            gifImage.BeginInit();
            gifImage.UriSource = new Uri("resources/spinner.gif", UriKind.RelativeOrAbsolute);
            gifImage.EndInit();

            void SetAddressColumnSpan(bool isIconVisible)
            {
                if (isIconVisible)
                {
                    Grid.SetColumnSpan(address, 1); // Jeśli ikona jest widoczna, ustaw ColumnSpan na 1
                }
                else
                {
                    Grid.SetColumnSpan(address, 2); // Jeśli ikona jest niewidoczna, ustaw ColumnSpan na 2
                }
            }
            SetAddressColumnSpan(true);
            ImageBehavior.SetAnimatedSource(ConnectionIndicator, gifImage);

            string baseLink = address.Text;
            if (baseLink != string.Empty)
            {
                try
                {
                    await Shkrape.ScrapeDataAsync(baseLink);
                    await Task.Delay(1000);
                    DataManager.ProcessBuildJson(Shkrape.buildJson);
                    DataManager.ProcessServiceJson(Shkrape.serviceJson);
                }
                catch (Exception ex)
                {
                    output.Text = $"Wystąpił nieoczekiwany błąd: {ex.Message}";
                    return;
                }

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                if (Shkrape.connected == false)
                {
                    SetAddressColumnSpan(true);
                    bitmap.UriSource = new Uri("resources/checkmark_red.png", UriKind.RelativeOrAbsolute);
                    ImageBehavior.SetAnimatedSource(ConnectionIndicator, bitmap);
                    
                }
                else if (Shkrape.connected == true)
                {
                    SetAddressColumnSpan(true);
                    bitmap.UriSource = new Uri("resources/checkmark_green.png", UriKind.RelativeOrAbsolute);
                    ImageBehavior.SetAnimatedSource(ConnectionIndicator, bitmap);
                    
                }
                bitmap.EndInit();
            }
            else
            {
                await Task.Delay(1000);
                ImageBehavior.SetAnimatedSource(ConnectionIndicator, null);
                address.Width = address.Width + ConnectionIndicator.Width;
                SetAddressColumnSpan(false);

            }
        }
    }
}

