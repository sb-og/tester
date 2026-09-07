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
        private const int WmNcHitTest = 0x0084;
        private const int WmNcLeftButtonDown = 0x00A1;
        private const int WmExitSizeMove = 0x0232;
        private const int HtLeft = 10;
        private const int HtRight = 11;
        private const int HtTop = 12;
        private const int HtTopLeft = 13;
        private const int HtTopRight = 14;
        private const int HtBottom = 15;
        private const int HtBottomLeft = 16;
        private const int HtBottomRight = 17;
        private const double ResizeBorderThickness = 6;

        private readonly Scroller _scroller;
        private bool isWindowLayoutReady = false;
        private bool isManualResizeInProgress = false;
        private bool isCustomMaximized = false;
        private Rect normalWindowBoundsBeforeMaximize;
        private bool isResizing = false;
        private Point lastMousePosition;
        private bool requiresManualDatabaseAddress;


        public MainWindow()
        {

            InitializeComponent();
            SourceInitialized += MainWindow_SourceInitialized;
            _scroller = new Scroller(this, RestoreCustomMaximizedWindowForDrag, Window_DragCompleted);
            dbComboBox.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new TextChangedEventHandler(DatabaseComboBox_TextChanged));


            //update or create config
            ConfigHelper.CreateConfigFile();

            //Load settings
            Topmost = bool.Parse(ConfigHelper.ReadSetting("Topmost"));
            user.Text = ConfigHelper.ReadSetting("User");
            pwd.Text = ConfigHelper.ReadSetting("Password");
            browserComboBox.Text = ConfigHelper.ReadSetting("Browser");
            ApplyWindowOpacity(ConfigHelper.ReadSetting("WindowOpacity"), saveSetting: false);
            ApplySavedWindowSize();
            ApplySavedWindowPosition();

            Loaded += (_, _) =>
            {
                ApplySavedSplitterLayout();
                isWindowLayoutReady = true;
            };



            Credits.Text = this.Title.ToString() + " By: Szymon Bogus";


            AddBooleanSettingMenuItem("Zawsze na wierzchu", "Topmost", value => Topmost = value);
            AddBooleanSettingMenuItem("Automatyczne uzupełnianie", "InstaFill");
            AddBooleanSettingMenuItem("Ostrzegaj przy zamykaniu", "WarnOnExit");
            AddBooleanSettingMenuItem("Generuj brakujące puste pola", "GenerateEmptyFields");
            AddBooleanSettingMenuItem("Zachowaj rozmiar okna", "PreserveWindowSize", value =>
            {
                if (value)
                {
                    SaveWindowLayout(saveWindowSize: true, saveSplitterLayout: true);
                }
            });
            AddOpacityMenu();
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

        private void AddBooleanSettingMenuItem(string header, string settingKey, Action<bool>? applySetting = null)
        {
            bool isChecked = Boolean.TryParse(ConfigHelper.ReadSetting(settingKey), out bool value) && value;
            var menuItem = new MenuItem
            {
                Header = header,
                IsCheckable = true,
                IsChecked = isChecked
            };

            menuItem.Click += (_, _) =>
            {
                bool newValue = menuItem.IsChecked;
                ConfigHelper.SaveSetting(settingKey, newValue.ToString());
                applySetting?.Invoke(newValue);
            };

            menu.ContextMenu.Items.Add(menuItem);
        }

        private void AddOpacityMenu()
        {
            var opacityMenu = new MenuItem { Header = "Ustaw przezroczystość" };
            AddOpacityMenuItem(opacityMenu, "Wyłącz przezroczystość", 1.0);
            AddOpacityMenuItem(opacityMenu, "50%", 0.5);
            AddOpacityMenuItem(opacityMenu, "75%", 0.75);
            AddOpacityMenuItem(opacityMenu, "80%", 0.8);
            AddOpacityMenuItem(opacityMenu, "90%", 0.9);
            menu.ContextMenu.Items.Add(opacityMenu);
        }

        private void AddOpacityMenuItem(MenuItem parentMenu, string header, double opacity)
        {
            var menuItem = new MenuItem { Header = header };
            menuItem.Click += (_, _) => ApplyWindowOpacity(opacity.ToString(System.Globalization.CultureInfo.InvariantCulture), saveSetting: true);
            parentMenu.Items.Add(menuItem);
        }

        private void ApplyWindowOpacity(string? opacityValue, bool saveSetting)
        {
            if (!Double.TryParse(opacityValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double opacity))
            {
                opacity = 1.0;
            }

            Opacity = Math.Clamp(opacity, 0.0, 1.0);
            if (saveSetting)
            {
                ConfigHelper.SaveSetting("WindowOpacity", Opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        private void ApplySavedWindowSize()
        {
            if (ConfigHelper.ReadSetting("PreserveWindowSize") != "True")
            {
                return;
            }

            if (TryReadDoubleSetting("WindowWidth", out double savedWidth) && savedWidth >= MinWidth)
            {
                Width = savedWidth;
            }

            if (TryReadDoubleSetting("WindowHeight", out double savedHeight) && savedHeight >= MinHeight)
            {
                Height = savedHeight;
            }
        }

        private void ApplySavedWindowPosition()
        {
            if (ConfigHelper.ReadSetting("PreserveWindowSize") != "True")
            {
                return;
            }

            if (!TryReadDoubleSetting("WindowLeft", out double savedLeft) || !TryReadDoubleSetting("WindowTop", out double savedTop))
            {
                return;
            }

            double windowWidth = Width >= MinWidth ? Width : MinWidth;
            double windowHeight = Height >= MinHeight ? Height : MinHeight;
            bool isInsideVirtualScreen =
                savedLeft >= SystemParameters.VirtualScreenLeft &&
                savedTop >= SystemParameters.VirtualScreenTop &&
                savedLeft + windowWidth <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth &&
                savedTop + windowHeight <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;

            if (!isInsideVirtualScreen)
            {
                return;
            }

            Left = savedLeft;
            Top = savedTop;
        }

        private void ApplySavedSplitterLayout()
        {
            if (ConfigHelper.ReadSetting("PreserveWindowSize") != "True")
            {
                return;
            }

            if (!TryReadDoubleSetting("LeftColumnWidth", out double leftWidth) || !TryReadDoubleSetting("RightColumnWidth", out double rightWidth))
            {
                return;
            }

            if (leftWidth < leftContentColumn.MinWidth || rightWidth < rightContentColumn.MinWidth)
            {
                return;
            }

            leftContentColumn.Width = new GridLength(leftWidth, GridUnitType.Star);
            rightContentColumn.Width = new GridLength(rightWidth, GridUnitType.Star);
        }

        private void SaveWindowLayout(bool saveWindowSize, bool saveSplitterLayout)
        {
            if (!isWindowLayoutReady || ConfigHelper.ReadSetting("PreserveWindowSize") != "True")
            {
                return;
            }

            bool canSaveWindowBounds = WindowState == WindowState.Normal && !isCustomMaximized && !IsWindowSnappedToWorkArea();
            if (canSaveWindowBounds)
            {
                ConfigHelper.SaveSetting("WindowLeft", Left.ToString(System.Globalization.CultureInfo.InvariantCulture));
                ConfigHelper.SaveSetting("WindowTop", Top.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (saveWindowSize && canSaveWindowBounds)
            {
                ConfigHelper.SaveSetting("WindowWidth", ActualWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
                ConfigHelper.SaveSetting("WindowHeight", ActualHeight.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (saveSplitterLayout)
            {
                ConfigHelper.SaveSetting("LeftColumnWidth", leftContentColumn.ActualWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
                ConfigHelper.SaveSetting("RightColumnWidth", rightContentColumn.ActualWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        private static bool TryReadDoubleSetting(string key, out double value)
        {
            return Double.TryParse(ConfigHelper.ReadSetting(key), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private static bool AreClose(double first, double second)
        {
            return Math.Abs(first - second) <= 2;
        }

        private bool IsWindowSnappedToWorkArea()
        {
            Rect workArea = SystemParameters.WorkArea;
            bool isFullWorkArea =
                AreClose(Left, workArea.Left) &&
                AreClose(Top, workArea.Top) &&
                AreClose(ActualWidth, workArea.Width) &&
                AreClose(ActualHeight, workArea.Height);
            bool touchesLeftOrRight = AreClose(Left, workArea.Left) || AreClose(Left + ActualWidth, workArea.Right);
            bool touchesTopOrBottom = AreClose(Top, workArea.Top) || AreClose(Top + ActualHeight, workArea.Bottom);
            bool isHalfWidth = AreClose(ActualWidth, workArea.Width / 2);
            bool isHalfHeight = AreClose(ActualHeight, workArea.Height / 2);
            bool isFullHeight = AreClose(ActualHeight, workArea.Height);

            return isFullWorkArea ||
                (touchesLeftOrRight && isHalfWidth && isFullHeight) ||
                (touchesLeftOrRight && touchesTopOrBottom && isHalfWidth && isHalfHeight);
        }

        private void Window_DragCompleted()
        {
            SaveWindowLayout(saveWindowSize: false, saveSplitterLayout: false);
        }

        private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            SaveWindowLayout(saveWindowSize: false, saveSplitterLayout: true);
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            HwndSource? source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(WindowProc);
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmNcHitTest && WindowState == WindowState.Normal && !isCustomMaximized)
            {
                int hitTest = GetResizeHitTest(lParam);
                if (hitTest != 0)
                {
                    handled = true;
                    return new IntPtr(hitTest);
                }
            }
            else if (msg == WmNcLeftButtonDown && IsResizeHitTest(wParam.ToInt32()))
            {
                isManualResizeInProgress = true;
            }
            else if (msg == WmExitSizeMove && isManualResizeInProgress)
            {
                isManualResizeInProgress = false;
                SaveWindowLayout(saveWindowSize: true, saveSplitterLayout: true);
            }

            return IntPtr.Zero;
        }

        private int GetResizeHitTest(IntPtr lParam)
        {
            int x = unchecked((short)(lParam.ToInt64() & 0xFFFF));
            int y = unchecked((short)((lParam.ToInt64() >> 16) & 0xFFFF));
            Point cursorPosition = PointFromScreen(new Point(x, y));

            bool isLeft = cursorPosition.X <= ResizeBorderThickness;
            bool isRight = cursorPosition.X >= ActualWidth - ResizeBorderThickness;
            bool isTop = cursorPosition.Y <= ResizeBorderThickness;
            bool isBottom = cursorPosition.Y >= ActualHeight - ResizeBorderThickness;

            if (isTop && isLeft) return HtTopLeft;
            if (isTop && isRight) return HtTopRight;
            if (isBottom && isLeft) return HtBottomLeft;
            if (isBottom && isRight) return HtBottomRight;
            if (isLeft) return HtLeft;
            if (isRight) return HtRight;
            if (isTop) return HtTop;
            if (isBottom) return HtBottom;

            return 0;
        }

        private static bool IsResizeHitTest(int hitTest)
        {
            return hitTest >= HtLeft && hitTest <= HtBottomRight;
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
            bool generateEmptyFields = ConfigHelper.ReadSetting("GenerateEmptyFields") == "True";

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

                    var outputBuilder = new StringBuilder();
                    outputBuilder.AppendLine("*1. Dane środowiska testowego:*");
                    AppendOutputRow(outputBuilder, "Przeglądarka:", $"{browserType} wersja: {webengine}", generateEmptyFields);
                    AppendOutputRow(outputBuilder, "Adres środowiska:", ip, generateEmptyFields, value => $"[{value}]");
                    AppendOutputRow(outputBuilder, "Adres Bazy danych:", DataManager.AdresBazyDanych, generateEmptyFields);
                    AppendOutputRow(outputBuilder, "NR Kompilacji AMMS:", DataManager.NrKompilacji, generateEmptyFields);
                    AppendOutputRow(outputBuilder, "Data kompilacji AMMS:", DataManager.DataKompilacji, generateEmptyFields);
                    AppendOutputRow(outputBuilder, "Numer rewizji AMMS:", DataManager.NrRewizji, generateEmptyFields);

                    outputBuilder.AppendLine();
                    outputBuilder.AppendLine("*2. Dane przypadku testowego:*");
                    AppendOutputRow(outputBuilder, "Użytkownik/Hasło", $"{username}/{password}", generateEmptyFields, _ => $"{username}/{password}", username, password);
                    AppendOutputRow(outputBuilder, "IDK_JOS:", idkjos, generateEmptyFields);
                    AppendOutputRow(outputBuilder, "PESEL:", nrpesel, generateEmptyFields);
                    AppendOutputRow(outputBuilder, "ID_PAC:", IdPacjenta, generateEmptyFields);
                    AppendOutputRow(outputBuilder, "ID_OPI:", IdOpieki, generateEmptyFields);
                    AppendOutputRow(outputBuilder, "ID_POB:", idPob, generateEmptyFields);
                    AppendOutputRow(outputBuilder, "ID_ZLEC:", IdZlec, generateEmptyFields);

                    outputBuilder.AppendLine();
                    outputBuilder.AppendLine("*3. Kroki postępowania:*");
                    AppendOutputRow(outputBuilder, "Ścieżka:", sciezka, generateEmptyFields);

                    outputBuilder.AppendLine();
                    outputBuilder.AppendLine("*4. Uzyskany rezultat*");
                    if (generateEmptyFields || !String.IsNullOrWhiteSpace(podsumowanie))
                    {
                        outputBuilder.AppendLine(podsumowanie);
                    }

                    output.Text = outputBuilder.ToString();
                }
            }
        }

        private static void AppendOutputRow(StringBuilder outputBuilder, string label, string? value, bool generateEmptyFields, Func<string, string>? formatValue = null, params string?[] valuesToCheck)
        {
            bool hasValue = valuesToCheck.Length > 0
                ? valuesToCheck.Any(fieldValue => !String.IsNullOrWhiteSpace(fieldValue))
                : !String.IsNullOrWhiteSpace(value);

            if (!generateEmptyFields && !hasValue)
            {
                return;
            }

            string safeValue = value ?? String.Empty;
            string displayValue = formatValue?.Invoke(safeValue) ?? safeValue;
            outputBuilder.AppendLine($"|{label}|{displayValue}|");
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            SaveManualDatabaseAddress();
            updateOutput(sender, e);
        }

        private void DatabaseComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!requiresManualDatabaseAddress)
            {
                return;
            }

            DataManager.AdresBazyDanych = dbComboBox.Text.Trim();
            if (ConfigHelper.ReadSetting("InstaFill") == "True")
            {
                SaveManualDatabaseAddress();
                updateOutput(sender, e);
            }
        }

        private void SaveManualDatabaseAddress()
        {
            if (!requiresManualDatabaseAddress || String.IsNullOrWhiteSpace(dbComboBox.Text))
            {
                return;
            }

            DataManager.AdresBazyDanych = dbComboBox.Text.Trim();
            DatabaseAddressCache.Save(address.Text, DataManager.AdresBazyDanych);
        }

        private void DatabaseComboBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.UnicodeText, true))
            {
                return;
            }

            string? pastedText = e.DataObject.GetData(DataFormats.UnicodeText) as string;
            if (pastedText == null)
            {
                return;
            }

            dbComboBox.Text = String.Concat(pastedText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Trim();
            e.CancelCommand();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {

            if (ConfigHelper.ReadSetting("WarnOnExit") == "True")
            {
                if (!ShowCloseConfirmationDialog())
                {
                    return;
                }
            }

            this.Close();

        }

        private bool ShowCloseConfirmationDialog()
        {
            var dialog = new Window
            {
                Owner = this,
                Title = "Uwaga",
                Width = 390,
                Height = 160,
                MinWidth = 390,
                MinHeight = 160,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                ShowInTaskbar = false,
                Topmost = Topmost
            };

            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(10, 18, 31)),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromRgb(51, 66, 87))
            };

            var layout = new DockPanel();
            border.Child = layout;

            var titleBar = new Grid
            {
                Height = 20,
                Background = new SolidColorBrush(Color.FromRgb(18, 32, 54))
            };
            titleBar.ColumnDefinitions.Add(new ColumnDefinition());
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            titleBar.MouseLeftButtonDown += (_, _) => dialog.DragMove();
            DockPanel.SetDock(titleBar, Dock.Top);
            layout.Children.Add(titleBar);

            var title = new TextBlock
            {
                Text = "Uwaga",
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontSize = 12
            };
            titleBar.Children.Add(title);

            var closeButton = new Button
            {
                Content = "x",
                Width = 26,
                Height = 32,
                Margin = new Thickness(0, -9, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.Red,
                FontSize = 20
            };
            closeButton.Click += (_, _) => dialog.DialogResult = false;
            Grid.SetColumn(closeButton, 1);
            titleBar.Children.Add(closeButton);

            var content = new Grid { Margin = new Thickness(22, 18, 22, 18) };
            content.RowDefinitions.Add(new RowDefinition());
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.Children.Add(content);

            var message = new TextBlock
            {
                Text = "Zamknięcie okna spowoduje utratę wprowadzonych danych",
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
                FontSize = 13
            };
            content.Children.Add(message);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 18, 0, 0)
            };
            Grid.SetRow(buttons, 1);
            content.Children.Add(buttons);

            var okButton = CreateDialogButton("OK");
            okButton.Click += (_, _) => dialog.DialogResult = true;
            buttons.Children.Add(okButton);

            var cancelButton = CreateDialogButton("Anuluj");
            cancelButton.Margin = new Thickness(8, 0, 0, 0);
            cancelButton.Click += (_, _) => dialog.DialogResult = false;
            buttons.Children.Add(cancelButton);

            dialog.Content = border;
            return dialog.ShowDialog() == true;
        }

        private Button CreateDialogButton(string content)
        {
            return new Button
            {
                Content = content,
                MinWidth = 76,
                Height = 28,
                Padding = new Thickness(12, 0, 12, 0),
                Background = new SolidColorBrush(Color.FromRgb(37, 44, 61)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                BorderThickness = new Thickness(1),
                Foreground = Brushes.White
            };
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

        private void AnimateDatabaseComboBox(bool show)
        {
            var heightAnimation = new DoubleAnimation
            {
                To = show ? 26 : 0,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            dbComboBoxContainer.BeginAnimation(HeightProperty, heightAnimation);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            address.Text = string.Empty;
            requiresManualDatabaseAddress = false;
            dbComboBox.Text = String.Empty;
            AnimateDatabaseComboBox(false);

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
            requiresManualDatabaseAddress = false;
            dbComboBox.Text = String.Empty;
            AnimateDatabaseComboBox(false);
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
                    if (!String.Equals(baseLink, address.Text, StringComparison.Ordinal))
                    {
                        return;
                    }

                    requiresManualDatabaseAddress = serviceJsonLoaded && String.IsNullOrWhiteSpace(DataManager.AdresBazyDanych);
                    if (requiresManualDatabaseAddress)
                    {
                        dbComboBox.ItemsSource = DatabaseAddressCache.GetDatabaseAddresses();
                        if (DatabaseAddressCache.TryGet(baseLink, out string cachedDatabaseAddress))
                        {
                            dbComboBox.Text = cachedDatabaseAddress;
                            DataManager.AdresBazyDanych = cachedDatabaseAddress;
                        }
                    }

                    AnimateDatabaseComboBox(requiresManualDatabaseAddress);
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
            if (isCustomMaximized)
            {
                RestoreCustomMaximizedWindow();
            }
            else
            {
                MaximizeWindowToWorkArea();
            }
        }

        private void MaximizeWindowToWorkArea()
        {
            normalWindowBoundsBeforeMaximize = new Rect(Left, Top, ActualWidth, ActualHeight);
            Rect workArea = SystemParameters.WorkArea;

            isCustomMaximized = true;
            WindowState = WindowState.Normal;
            Left = workArea.Left;
            Top = workArea.Top;
            Width = workArea.Width;
            Height = workArea.Height;
            MaximizeIcon.Text = "🗗";
        }

        private void RestoreCustomMaximizedWindow()
        {
            isCustomMaximized = false;

            Width = Math.Max(MinWidth, normalWindowBoundsBeforeMaximize.Width);
            Height = Math.Max(MinHeight, normalWindowBoundsBeforeMaximize.Height);
            Left = normalWindowBoundsBeforeMaximize.Left;
            Top = normalWindowBoundsBeforeMaximize.Top;
            MaximizeIcon.Text = "🗖";
        }

        private bool RestoreCustomMaximizedWindowForDrag(Point mousePosition)
        {
            if (!isCustomMaximized)
            {
                return false;
            }

            double horizontalRatio = ActualWidth > 0 ? mousePosition.X / ActualWidth : 0.5;
            Point screenPosition = PointToScreen(mousePosition);
            double restoredWidth = Math.Max(MinWidth, normalWindowBoundsBeforeMaximize.Width);
            double restoredHeight = Math.Max(MinHeight, normalWindowBoundsBeforeMaximize.Height);

            isCustomMaximized = false;
            Width = restoredWidth;
            Height = restoredHeight;
            Left = screenPosition.X - restoredWidth * horizontalRatio;
            Top = SystemParameters.WorkArea.Top;
            MaximizeIcon.Text = "🗖";

            return true;
        }


        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}

