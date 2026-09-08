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
        private const string DefaultPatientIdentifiers = "Identyfikator pacjenta: \nIdentyfikator opieki: \nIdentyfikator pobytu: \nIdentyfikator zlecenia: ";
        private const string DefaultUserName = "ADMIN";
        private const string DefaultPassword = "ADMIN";
        private const string DefaultBrowser = "Edge";

        private readonly Scroller _scroller;
        private bool isWindowLayoutReady = false;
        private bool isManualResizeInProgress = false;
        private bool isCustomMaximized = false;
        private bool isOutputPanelCollapsed;
        private Rect normalWindowBoundsBeforeMaximize;
        private GridLength expandedLeftColumnWidth;
        private GridLength expandedRightColumnWidth;
        private bool isResizing = false;
        private Point lastMousePosition;
        private bool requiresManualDatabaseAddress;
        private bool isUsingCachedDatabaseAddress;
        private int databaseComboBoxAnimationVersion;
        private MenuItem? warnOnExitMenuItem;
        private MenuItem? saveEntireStateMenuItem;
        private MenuItem? compactOutputModeMenuItem;
        private MenuItem? instaFillMenuItem;
        private string? restoredDatabaseAddress;


        public MainWindow()
        {

            InitializeComponent();
            SourceInitialized += MainWindow_SourceInitialized;
            Closing += MainWindow_Closing;
            MouseEnter += (_, _) => UpdateWindowOpacity();
            MouseLeave += (_, _) => UpdateWindowOpacity();
            Activated += (_, _) => UpdateWindowOpacity();
            Deactivated += (_, _) => UpdateWindowOpacity();
            _scroller = new Scroller(this, RestoreCustomMaximizedWindowForDrag, Window_DragCompleted);
            dbComboBox.AddHandler(System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent, new TextChangedEventHandler(DatabaseComboBox_TextChanged));


            //update or create config
            ConfigHelper.CreateConfigFile();

            //Load settings
            Topmost = bool.Parse(ConfigHelper.ReadSetting("Topmost"));
            user.Text = ConfigHelper.ReadSetting("User");
            pwd.Text = ConfigHelper.ReadSetting("Password");
            browserComboBox.Text = ConfigHelper.ReadSetting("Browser");
            ApplyConfiguredWindowOpacity();
            ApplySavedWindowSize();
            ApplySavedWindowPosition();

            Loaded += (_, _) =>
            {
                ApplySavedSplitterLayout();
                isWindowLayoutReady = true;
                RestoreSavedApplicationState();
                RestoreCompactOutputMode();
            };



            Credits.Text = this.Title.ToString() + " By: Szymon Bogus";


            AddBooleanSettingMenuItem("Zawsze na wierzchu", "Topmost", value => Topmost = value);
            instaFillMenuItem = AddBooleanSettingMenuItem("Automatyczne uzupełnianie", "InstaFill");
            AddAutoHideMenu();
            warnOnExitMenuItem = AddBooleanSettingMenuItem("Ostrzegaj przy zamykaniu", "WarnOnExit");
            AddBooleanSettingMenuItem("Generuj brakujące puste pola", "GenerateEmptyFields");
            AddCompactOutputModeMenuItem();
            AddBooleanSettingMenuItem("Zachowaj rozmiar okna", "PreserveWindowSize", value =>
            {
                if (value)
                {
                    SaveWindowLayout(saveWindowSize: true, saveSplitterLayout: true);
                }
            });
            AddOpacityMenu();
            AddSaveMenu();
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

        private void MenuContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu contextMenu)
            {
                return;
            }

            contextMenu.Opacity = 0;
            contextMenu.RenderTransformOrigin = new Point(0, 0);
            contextMenu.RenderTransform = new ScaleTransform(0.96, 0.96);
            var animation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120));
            contextMenu.BeginAnimation(OpacityProperty, animation);
            ((ScaleTransform)contextMenu.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(120)));
            ((ScaleTransform)contextMenu.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(120)));
        }

        private void SectionHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: FrameworkElement sectionContent })
            {
                bool isCollapsing = sectionContent.Visibility == Visibility.Visible;
                SetSectionVisibility(sectionContent, !isCollapsing);

                if (isCollapsing && (sender != testEnvironmentHeader || !isUsingCachedDatabaseAddress))
                {
                    ((Button)sender).Foreground = Brushes.White;
                }
            }
        }

        private void AddMenuItem(string header, RoutedEventHandler handler)
        {
            var menuItem = new MenuItem { Header = header, StaysOpenOnClick = true };
            menuItem.Click += handler; // Przypisanie obsługi zdarzeń
            menu.ContextMenu.Items.Add(menuItem); // Dodawanie do menu kontekstowego
        }

        private MenuItem AddBooleanSettingMenuItem(string header, string settingKey, Action<bool>? applySetting = null)
        {
            bool isChecked = Boolean.TryParse(ConfigHelper.ReadSetting(settingKey), out bool value) && value;
            var menuItem = new MenuItem
            {
                Header = header,
                IsCheckable = true,
                IsChecked = isChecked,
                StaysOpenOnClick = true
            };

            menuItem.Click += (_, _) =>
            {
                bool newValue = menuItem.IsChecked;
                ConfigHelper.SaveSetting(settingKey, newValue.ToString());
                applySetting?.Invoke(newValue);
            };

            menu.ContextMenu.Items.Add(menuItem);
            return menuItem;
        }

        private void AddAutoHideMenu()
        {
            var autoHideMenu = new MenuItem { Header = "Autoukrywanie" };
            AddAutoHideSettingMenuItem(autoHideMenu, "Automatycznie ukryj dane środowiska", "AutoHideTestEnvironmentDetails");
            AddAutoHideSettingMenuItem(autoHideMenu, "Automatycznie ukryj dane przypadku", "AutoHideTestCaseDetails");
            AddAutoHideSettingMenuItem(autoHideMenu, "Automatycznie ukryj opis ścieżki", "AutoHidePathDetails");
            menu.ContextMenu.Items.Add(autoHideMenu);
        }

        private static void AddAutoHideSettingMenuItem(MenuItem parentMenu, string header, string settingKey)
        {
            bool isChecked = ConfigHelper.ReadSetting(settingKey) == "True";
            var menuItem = new MenuItem
            {
                Header = header,
                IsCheckable = true,
                IsChecked = isChecked,
                StaysOpenOnClick = true
            };
            menuItem.Click += (_, _) => ConfigHelper.SaveSetting(settingKey, menuItem.IsChecked.ToString());
            parentMenu.Items.Add(menuItem);
        }

        private void AddCompactOutputModeMenuItem()
        {
            compactOutputModeMenuItem = new MenuItem
            {
                Header = "Tryb kompaktowy",
                IsCheckable = true,
                IsChecked = ConfigHelper.ReadSetting("AutoSave") == "True"
                    && ConfigHelper.ReadSetting("CompactOutputMode") == "True",
                StaysOpenOnClick = true
            };
            compactOutputModeMenuItem.Click += (_, _) => SetOutputPanelCompactMode(compactOutputModeMenuItem.IsChecked);
            menu.ContextMenu.Items.Add(compactOutputModeMenuItem);
        }

        private void AddOpacityMenu()
        {
            var opacityMenu = new MenuItem { Header = "Ustaw przezroczystość" };
            var autoOpacityMenuItem = new MenuItem { Header = "Auto", StaysOpenOnClick = true };
            autoOpacityMenuItem.Click += (_, _) => ApplyDefaultAutoWindowOpacity();
            opacityMenu.Items.Add(autoOpacityMenuItem);
            AddOpacityMenuItem(opacityMenu, "50%", 0.5);
            AddOpacityMenuItem(opacityMenu, "75%", 0.75);
            AddOpacityMenuItem(opacityMenu, "80%", 0.8);
            AddOpacityMenuItem(opacityMenu, "90%", 0.9);
            AddOpacityMenuItem(opacityMenu, "Wyłącz przezroczystość", 1.0);
            menu.ContextMenu.Items.Add(opacityMenu);
        }

        private void AddOpacityMenuItem(MenuItem parentMenu, string header, double opacity)
        {
            var menuItem = new MenuItem { Header = header, StaysOpenOnClick = true };
            menuItem.Click += (_, _) => ApplyManualWindowOpacity(opacity);
            parentMenu.Items.Add(menuItem);
        }

        private void ApplyDefaultAutoWindowOpacity()
        {
            ConfigHelper.SaveSetting("OpacityBase", "0.2");
            ConfigHelper.SaveSetting("OpacityFocus", "0.5");
            ConfigHelper.SaveSetting("OpacityHover", "0.3");
            ApplyConfiguredWindowOpacity();
        }

        private void AddSaveMenu()
        {
            var saveMenu = new MenuItem { Header = "Zapis" };
            var saveUserMenuItem = new MenuItem { Header = "Zapisz użytkownika", StaysOpenOnClick = true };
            saveUserMenuItem.Click += (_, _) => SaveUser();
            saveMenu.Items.Add(saveUserMenuItem);

            var saveBrowserMenuItem = new MenuItem { Header = "Zapisz przeglądarkę", StaysOpenOnClick = true };
            saveBrowserMenuItem.Click += (_, _) => SaveBrowser();
            saveMenu.Items.Add(saveBrowserMenuItem);

            bool autoSaveEnabled = Boolean.TryParse(ConfigHelper.ReadSetting("AutoSave"), out bool value) && value;
            var autoSaveMenuItem = new MenuItem
            {
                Header = "Autozapis",
                IsCheckable = true,
                IsChecked = autoSaveEnabled,
                StaysOpenOnClick = true
            };
            autoSaveMenuItem.Click += (_, _) => ConfigHelper.SaveSetting("AutoSave", autoSaveMenuItem.IsChecked.ToString());
            saveMenu.Items.Add(autoSaveMenuItem);

            bool saveEntireStateEnabled = autoSaveEnabled && ConfigHelper.ReadSetting("SaveEntireState") == "True";
            saveEntireStateMenuItem = new MenuItem
            {
                Header = "Zapisuj cały stan",
                IsCheckable = true,
                IsChecked = saveEntireStateEnabled,
                IsEnabled = autoSaveEnabled,
                StaysOpenOnClick = true,
                ToolTip = autoSaveEnabled ? null : "Włącz autozapis, aby zapisywać cały stan."
            };
            ToolTipService.SetShowOnDisabled(saveEntireStateMenuItem, true);
            saveEntireStateMenuItem.Click += (_, _) =>
            {
                ConfigHelper.SaveSetting("SaveEntireState", saveEntireStateMenuItem.IsChecked.ToString());
                UpdateWarnOnExitAvailability(saveEntireStateMenuItem.IsChecked);
            };
            saveMenu.Items.Add(saveEntireStateMenuItem);

            autoSaveMenuItem.Click += (_, _) => UpdateSaveEntireStateAvailability(autoSaveMenuItem.IsChecked);
            UpdateWarnOnExitAvailability(saveEntireStateEnabled);

            menu.ContextMenu.Items.Add(saveMenu);
        }

        private void UpdateSaveEntireStateAvailability(bool autoSaveEnabled)
        {
            if (saveEntireStateMenuItem == null)
            {
                return;
            }

            saveEntireStateMenuItem.IsEnabled = autoSaveEnabled;
            saveEntireStateMenuItem.ToolTip = autoSaveEnabled ? null : "Włącz autozapis, aby zapisywać cały stan.";
            if (!autoSaveEnabled)
            {
                saveEntireStateMenuItem.IsChecked = false;
                ConfigHelper.SaveSetting("SaveEntireState", "False");
            }

            UpdateWarnOnExitAvailability(saveEntireStateMenuItem.IsChecked);
        }

        private void UpdateWarnOnExitAvailability(bool saveEntireStateEnabled)
        {
            if (warnOnExitMenuItem == null)
            {
                return;
            }

            warnOnExitMenuItem.IsEnabled = !saveEntireStateEnabled;
            warnOnExitMenuItem.ToolTip = saveEntireStateEnabled ? "Opcja jest niedostępna podczas zapisywania całego stanu." : null;
            ToolTipService.SetShowOnDisabled(warnOnExitMenuItem, true);
            if (saveEntireStateEnabled)
            {
                warnOnExitMenuItem.IsChecked = false;
                ConfigHelper.SaveSetting("WarnOnExit", "False");
            }
        }

        private void SaveUser()
        {
            ConfigHelper.SaveSetting("User", user.Text);
        }

        private void SaveBrowser()
        {
            ConfigHelper.SaveSetting("Browser", browserComboBox.Text);
        }

        private void SaveApplicationState()
        {
            ConfigHelper.SaveSetting("Password", pwd.Text);
            ConfigHelper.SaveSetting("SavedAddress", address.Text);
            ConfigHelper.SaveSetting("SavedDatabaseAddress", dbComboBox.Text);
            ConfigHelper.SaveSetting("SavedPesel", pesel.Text);
            ConfigHelper.SaveSetting("SavedJos", jos.Text);
            ConfigHelper.SaveSetting("SavedPatient", pac.Text);
            ConfigHelper.SaveSetting("SavedPath", path.Text);
            ConfigHelper.SaveSetting("SavedDescription", desc.Text);
            ConfigHelper.SaveSetting("SavedOutput", output.Text);
        }

        private void RestoreSavedApplicationState()
        {
            if (ConfigHelper.ReadSetting("AutoSave") != "True" || ConfigHelper.ReadSetting("SaveEntireState") != "True")
            {
                return;
            }

            pwd.Text = ConfigHelper.ReadSetting("Password");
            pesel.Text = ConfigHelper.ReadSetting("SavedPesel");
            jos.Text = ConfigHelper.ReadSetting("SavedJos");
            pac.Text = ConfigHelper.ReadSetting("SavedPatient");
            path.Text = ConfigHelper.ReadSetting("SavedPath");
            desc.Text = ConfigHelper.ReadSetting("SavedDescription");
            output.Text = ConfigHelper.ReadSetting("SavedOutput");
            restoredDatabaseAddress = ConfigHelper.ReadSetting("SavedDatabaseAddress");
            address.Text = ConfigHelper.ReadSetting("SavedAddress");
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ConfigHelper.ReadSetting("AutoSave") != "True")
            {
                return;
            }

            SaveUser();
            SaveBrowser();
            if (ConfigHelper.ReadSetting("SaveEntireState") == "True")
            {
                SaveApplicationState();
            }
        }

        private void ApplyManualWindowOpacity(double opacity)
        {
            ConfigHelper.SaveSetting("OpacityBase", opacity.ToString(System.Globalization.CultureInfo.InvariantCulture));
            ConfigHelper.SaveSetting("OpacityFocus", null);
            ConfigHelper.SaveSetting("OpacityHover", null);
            ApplyConfiguredWindowOpacity();
        }

        private void ApplyConfiguredWindowOpacity()
        {
            if (TryGetAutoOpacityValues(out double[] opacityValues))
            {
                UpdateWindowOpacity(opacityValues);
                return;
            }

            BeginAnimation(OpacityProperty, null);
            Opacity = TryParseOpacityValue(ConfigHelper.ReadSetting("OpacityBase"), out double opacity) ? opacity : 1.0;
        }

        private void UpdateWindowOpacity()
        {
            if (TryGetAutoOpacityValues(out double[] opacityValues))
            {
                UpdateWindowOpacity(opacityValues);
            }
        }

        private void UpdateWindowOpacity(double[] opacityValues)
        {
            double targetOpacity = opacityValues[0];
            if (IsActive)
            {
                targetOpacity += opacityValues[1];
            }

            if (IsMouseOver)
            {
                targetOpacity += opacityValues[2];
            }

            targetOpacity = Math.Clamp(targetOpacity, 0.0, 1.0);

            BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = targetOpacity,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            });
        }

        private static bool TryGetAutoOpacityValues(out double[] opacityValues)
        {
            if (!TryParseOpacityValue(ConfigHelper.ReadSetting("OpacityBase"), out double baseOpacity)
                || !TryParseOpacityValue(ConfigHelper.ReadSetting("OpacityFocus"), out double focusOpacity)
                || !TryParseOpacityValue(ConfigHelper.ReadSetting("OpacityHover"), out double hoverOpacity))
            {
                opacityValues = Array.Empty<double>();
                return false;
            }

            opacityValues = new[] { baseOpacity, focusOpacity, hoverOpacity };
            return true;
        }

        private static bool TryParseOpacityValue(string? opacityValue, out double opacity)
        {
            if (!Double.TryParse(opacityValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out opacity))
            {
                return false;
            }

            opacity = Math.Clamp(opacity, 0.0, 1.0);
            return true;
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
            if (!isOutputPanelCollapsed)
            {
                SaveWindowLayout(saveWindowSize: false, saveSplitterLayout: true);
            }
        }

        private void GridSplitter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetOutputPanelCompactMode(!isOutputPanelCollapsed);
        }

        private void SetOutputPanelCompactMode(bool isCompactMode)
        {
            if (isCompactMode)
            {
                ConfigHelper.SaveSetting("InstaFill", "True");
                HideOutputPanel();
            }
            else
            {
                ShowOutputPanel();
            }

            if (ConfigHelper.ReadSetting("AutoSave") == "True")
            {
                ConfigHelper.SaveSetting("CompactOutputMode", isCompactMode.ToString());
            }

            if (compactOutputModeMenuItem != null)
            {
                compactOutputModeMenuItem.IsChecked = isCompactMode;
            }

            if (instaFillMenuItem != null)
            {
                instaFillMenuItem.IsChecked = true;
                instaFillMenuItem.IsEnabled = !isCompactMode;
                instaFillMenuItem.ToolTip = isCompactMode
                    ? "Opcja jest zawsze włączona w trybie kompaktowym."
                    : null;
            }
        }

        private void HideOutputPanel()
        {
            expandedLeftColumnWidth = leftContentColumn.Width;
            expandedRightColumnWidth = rightContentColumn.Width;
            isOutputPanelCollapsed = true;
            outputPanel.Visibility = Visibility.Collapsed;
            expandedOutputActions.Visibility = Visibility.Collapsed;
            compactOutputActions.Visibility = Visibility.Visible;
            compactOutputActions.Opacity = 0;
            compactOutputActions.RenderTransform = new TranslateTransform(12, 0);
            compactOutputActions.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
            ((TranslateTransform)compactOutputActions.RenderTransform).BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(160)));
            rightContentColumn.MinWidth = 32;
            rightContentColumn.Width = new GridLength(32);
            leftContentColumn.Width = new GridLength(1, GridUnitType.Star);
        }

        private void ShowOutputPanel()
        {
            isOutputPanelCollapsed = false;
            outputPanel.Visibility = Visibility.Visible;
            outputPanel.Opacity = 0;
            outputPanel.RenderTransform = new TranslateTransform(12, 0);
            outputPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
            ((TranslateTransform)outputPanel.RenderTransform).BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(160)));
            expandedOutputActions.Visibility = Visibility.Visible;
            compactOutputActions.Visibility = Visibility.Collapsed;
            rightContentColumn.MinWidth = 150;
            leftContentColumn.Width = expandedLeftColumnWidth;
            rightContentColumn.Width = expandedRightColumnWidth;
        }

        private void RestoreCompactOutputMode()
        {
            if (ConfigHelper.ReadSetting("AutoSave") == "True"
                && ConfigHelper.ReadSetting("CompactOutputMode") == "True")
            {
                SetOutputPanelCompactMode(true);
            }
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
            CollapseCompletedSections();

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
                CollapseCompletedSections();
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

        private void Jos_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox || !e.DataObject.GetDataPresent(DataFormats.UnicodeText, true))
            {
                return;
            }

            if (e.DataObject.GetData(DataFormats.UnicodeText) is string pastedText)
            {
                textBox.SelectedText = pastedText;
                e.CancelCommand();
            }
        }

        private void TestEnvironmentDetails_LostKeyboardFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(CollapseCompletedSections);
        }

        private void SectionDetails_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(CollapseCompletedSections);
        }

        private void TestCaseInput_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(CollapseCompletedSections, System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void CollapseCompletedSections()
        {
            if (ConfigHelper.ReadSetting("AutoHideTestEnvironmentDetails") == "True")
            {
                CollapseTestEnvironmentDetails();
            }

            if (ConfigHelper.ReadSetting("AutoHideTestCaseDetails") == "True")
            {
                CollapseTestCaseDetails();
            }

            if (ConfigHelper.ReadSetting("AutoHidePathDetails") == "True")
            {
                CollapsePathDetails();
            }
        }

        private void CollapseTestEnvironmentDetails()
        {
            if (!String.IsNullOrWhiteSpace(address.Text)
                && !String.IsNullOrWhiteSpace(user.Text)
                && !String.IsNullOrWhiteSpace(pwd.Text)
                && !String.IsNullOrWhiteSpace(browserComboBox.Text)
                && !String.IsNullOrWhiteSpace(DataManager.AdresBazyDanych))
            {
                testEnvironmentHeader.Foreground = isUsingCachedDatabaseAddress
                    ? Brushes.Gold
                    : requiresManualDatabaseAddress ? Brushes.Gold : Brushes.LimeGreen;
                SetSectionVisibility(testEnvironmentDetails, false);
            }
        }

        private void CollapseTestCaseDetails()
        {
            bool havePatientIdentifiersChanged = !String.Equals(
                pac.Text.Replace("\r\n", "\n").TrimEnd(),
                DefaultPatientIdentifiers.TrimEnd(),
                StringComparison.Ordinal);

            if (!String.IsNullOrWhiteSpace(pesel.Text)
                && !String.IsNullOrWhiteSpace(jos.Text)
                && havePatientIdentifiersChanged
                && !pesel.IsKeyboardFocusWithin
                && !jos.IsKeyboardFocusWithin
                && !pac.IsKeyboardFocusWithin)
            {
                testCaseHeader.Foreground = Brushes.LimeGreen;
                SetSectionVisibility(testCaseDetails, false);
            }
        }

        private void CollapsePathDetails()
        {
            if (path is not null
                && pathDetails is not null
                && !path.IsKeyboardFocusWithin
                && !String.IsNullOrWhiteSpace(path.Text))
            {
                pathHeader.Foreground = Brushes.LimeGreen;
                SetSectionVisibility(pathDetails, false);
            }
        }

        private static void SetSectionVisibility(FrameworkElement section, bool isVisible)
        {
            if (isVisible)
            {
                if (section.Visibility == Visibility.Visible)
                {
                    return;
                }

                section.Visibility = Visibility.Visible;
                section.ClearValue(HeightProperty);
                section.Measure(new Size(section.ActualWidth, Double.PositiveInfinity));
                double targetHeight = section.DesiredSize.Height;
                section.Height = 0;
                var expandAnimation = new DoubleAnimation(0, targetHeight, TimeSpan.FromMilliseconds(160));
                expandAnimation.Completed += (_, _) => section.ClearValue(HeightProperty);
                section.BeginAnimation(HeightProperty, expandAnimation);
                return;
            }

            if (section.Visibility != Visibility.Visible)
            {
                return;
            }

            var collapseAnimation = new DoubleAnimation(section.ActualHeight, 0, TimeSpan.FromMilliseconds(140));
            collapseAnimation.Completed += (_, _) =>
            {
                section.BeginAnimation(HeightProperty, null);
                section.ClearValue(HeightProperty);
                section.Visibility = Visibility.Collapsed;
            };
            section.BeginAnimation(HeightProperty, collapseAnimation);
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
                Text = "Zamknięcie okna może spowodować utratę wprowadzonych danych",
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

        private void ClearTestEnvironmentSection_Click(object sender, RoutedEventArgs e)
        {
            requiresManualDatabaseAddress = false;
            isUsingCachedDatabaseAddress = false;
            address.Text = String.Empty;
            dbComboBox.Text = String.Empty;
            browserComboBox.Text = DefaultBrowser;
            user.Text = ConfigHelper.ReadSetting("User");
            pwd.Text = ConfigHelper.ReadSetting("Password");
            DataManager.NrKompilacji = String.Empty;
            DataManager.DataKompilacji = String.Empty;
            DataManager.NrRewizji = String.Empty;
            DataManager.AdresBazyDanych = String.Empty;
            AnimateDatabaseComboBox(false);
            ImageBehavior.SetAnimatedSource(ConnectionIndicator, null);
            SetSectionVisibility(testEnvironmentDetails, true);
            testEnvironmentHeader.Foreground = Brushes.White;
            testEnvironmentHeader.ToolTip = null;
        }

        private void ClearTestCaseSection_Click(object sender, RoutedEventArgs e)
        {
            pesel.Text = String.Empty;
            jos.Text = String.Empty;
            pac.Text = DefaultPatientIdentifiers;
            SetSectionVisibility(testCaseDetails, true);
            testCaseHeader.Foreground = Brushes.White;
        }

        private void ClearPathSection_Click(object sender, RoutedEventArgs e)
        {
            path.Text = String.Empty;
            SetSectionVisibility(pathDetails, true);
            pathHeader.Foreground = Brushes.White;
        }

        private void AnimateDatabaseComboBox(bool show)
        {
            int animationVersion = ++databaseComboBoxAnimationVersion;
            dbComboBoxContainer.Visibility = Visibility.Visible;
            var heightAnimation = new DoubleAnimation
            {
                To = show ? 26 : 0,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            if (!show)
            {
                heightAnimation.Completed += (_, _) =>
                {
                    if (animationVersion == databaseComboBoxAnimationVersion)
                    {
                        dbComboBoxContainer.Visibility = Visibility.Collapsed;
                    }
                };
            }

            dbComboBoxContainer.BeginAnimation(HeightProperty, heightAnimation);
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            address.Text = string.Empty;
            requiresManualDatabaseAddress = false;
            isUsingCachedDatabaseAddress = false;
            UpdateCachedDatabaseAddressIndicator();
            dbComboBox.Text = String.Empty;
            AnimateDatabaseComboBox(false);

            pac.Text = DefaultPatientIdentifiers;

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
            SetSectionVisibility(testEnvironmentDetails, true);
            SetSectionVisibility(testCaseDetails, true);
            SetSectionVisibility(pathDetails, true);
            testEnvironmentHeader.Foreground = Brushes.White;
            testCaseHeader.Foreground = Brushes.White;
            pathHeader.Foreground = Brushes.White;
        }


        private async void address_TextChanged(object sender, TextChangedEventArgs e)
        {
            requiresManualDatabaseAddress = false;
            isUsingCachedDatabaseAddress = false;
            UpdateCachedDatabaseAddressIndicator();
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

                    bool hasIncompleteServiceData = String.IsNullOrWhiteSpace(DataManager.AdresBazyDanych)
                        || String.IsNullOrWhiteSpace(DataManager.NrRewizji);
                    bool hasCachedDatabaseAddress = DatabaseAddressCache.TryGet(baseLink, out string cachedDatabaseAddress);
                    requiresManualDatabaseAddress = serviceJsonLoaded && String.IsNullOrWhiteSpace(DataManager.AdresBazyDanych);
                    if (requiresManualDatabaseAddress)
                    {
                        dbComboBox.ItemsSource = DatabaseAddressCache.GetDatabaseAddresses();
                        if (!String.IsNullOrWhiteSpace(restoredDatabaseAddress))
                        {
                            dbComboBox.Text = restoredDatabaseAddress;
                            DataManager.AdresBazyDanych = restoredDatabaseAddress;
                        }
                        else if (hasCachedDatabaseAddress)
                        {
                            dbComboBox.Text = cachedDatabaseAddress;
                            DataManager.AdresBazyDanych = cachedDatabaseAddress;
                            isUsingCachedDatabaseAddress = true;
                        }
                    }
                    else if (!serviceJsonLoaded && hasCachedDatabaseAddress)
                    {
                        DataManager.AdresBazyDanych = cachedDatabaseAddress;
                        isUsingCachedDatabaseAddress = true;
                    }

                    restoredDatabaseAddress = null;
                    UpdateCachedDatabaseAddressIndicator();

                    AnimateDatabaseComboBox(requiresManualDatabaseAddress);
                    await Task.Delay(1000);

                    // Ustawienie odpowiedniego wskaźnika w zależności od stanu połączenia
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();

                    if (!buildJsonLoaded && !serviceJsonLoaded && !isUsingCachedDatabaseAddress)
                    {
                        bitmap.UriSource = new Uri("resources/checkmark_red.png", UriKind.RelativeOrAbsolute);
                    }
                    else if (!buildJsonLoaded || !serviceJsonLoaded || hasIncompleteServiceData)
                    {
                        bitmap.UriSource = new Uri("resources/checkmark_yellow.png", UriKind.RelativeOrAbsolute);
                        updateOutput(sender, e);
                        CollapseCompletedSections();
                    }
                    else
                    {
                        bitmap.UriSource = new Uri("resources/checkmark_green.png", UriKind.RelativeOrAbsolute);
                        updateOutput(sender, e);
                        CollapseCompletedSections();
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

        private void UpdateCachedDatabaseAddressIndicator()
        {
            testEnvironmentHeader.Foreground = isUsingCachedDatabaseAddress ? Brushes.Gold : Brushes.White;
            testEnvironmentHeader.ToolTip = isUsingCachedDatabaseAddress
                ? $"zapamiętana baza: {DataManager.AdresBazyDanych}"
                : null;
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

