namespace AIDA
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Management;
    using System.IO;
    using System.Threading;
    using System.Globalization;
    using System.ComponentModel;
    using System.Diagnostics;

    public partial class MainWindow : Window
    {
        private enum FilterType { All, Ok, NotOk }
        private FilterType currentFilterType = FilterType.All;
        private string currentSortProperty = "DeviceName";
        private ListSortDirection currentSortDirection = ListSortDirection.Ascending;
        private List<HardwareInfo.GpuInfo> gpus;
        private List<HardwareInfo.MonitorInfo> monitors;
        private List<HardwareInfo.DisplaySettings> displaySettings;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetLanguage("en");
            PopulateDevicesTree();
            PopulateDriversList();
        }

        private void SetLanguage(string culture)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
            DevicesTabItem.Header = Properties.EngUI.DevicesTab;
            DriversTabItem.Header = Properties.EngUI.DriversTab;
            DiagnosticsTabItem.Header = Properties.EngUI.DiagnosticsTab;
            SettingsTabItem.Header = Properties.EngUI.SettingsTab;
            HelpTabItem.Header = Properties.EngUI.HelpTab;
           
            HelpTextBlock.Text = Properties.EngUI.HelpText;
            
        }

        private void PopulateDevicesTree()
        {
            gpus = HardwareInfo.GetGpus();
            monitors = HardwareInfo.GetMonitors();
            displaySettings = HardwareInfo.GetDisplaySettings();

            DevicesTree.Items.Clear();

            var graphicsCardsItem = new TreeViewItem { Header = Properties.EngUI.DevicesTab };
            foreach (var gpu in gpus)
            {
                var gpuItem = new TreeViewItem { Header = gpu.Name };
                gpuItem.Items.Add(new TreeViewItem { Header = $"Temperature: {gpu.Temperature} °C" });
                gpuItem.Items.Add(new TreeViewItem { Header = $"Clock Speed: {gpu.ClockSpeed} MHz" });
                gpuItem.Items.Add(new TreeViewItem { Header = $"Driver Version: {gpu.DriverVersion}" });
                gpuItem.Items.Add(new TreeViewItem { Header = $"Memory: {gpu.AdapterRAM}" });
                graphicsCardsItem.Items.Add(gpuItem);
            }
            DevicesTree.Items.Add(graphicsCardsItem);

            var monitorsItem = new TreeViewItem { Header = "Monitors" };
            foreach (var monitor in monitors)
            {
                var monitorItem = new TreeViewItem { Header = monitor.Model };
                monitorItem.Items.Add(new TreeViewItem { Header = $"Manufacturer: {monitor.Manufacturer}" });
                monitorItem.Items.Add(new TreeViewItem { Header = $"Serial Number: {monitor.SerialNumber}" });
                monitorItem.Items.Add(new TreeViewItem { Header = $"Physical Size: {monitor.PhysicalWidth} x {monitor.PhysicalHeight} cm" });
                monitorItem.Items.Add(new TreeViewItem { Header = $"Panel Type: {monitor.PanelType}" });
                monitorsItem.Items.Add(monitorItem);
            }
            DevicesTree.Items.Add(monitorsItem);

            var displaySettingsItem = new TreeViewItem { Header = "Display Settings" };
            for (int i = 0; i < displaySettings.Count; i++)
            {
                var setting = displaySettings[i];
                var displayItem = new TreeViewItem { Header = $"Display {i + 1}" };
                displayItem.Items.Add(new TreeViewItem { Header = $"Resolution: {setting.Resolution}" });
                displayItem.Items.Add(new TreeViewItem { Header = $"Refresh Rate: {setting.RefreshRate} Hz" });
                displayItem.Items.Add(new TreeViewItem { Header = $"Color Depth: {setting.ColorDepth} bits" });
                displaySettingsItem.Items.Add(displayItem);
            }
            DevicesTree.Items.Add(displaySettingsItem);
        }

        private void PopulateDriversList()
        {
            DriversList.ItemsSource = null;
            var drivers = new List<DriverInfo>();

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPSignedDriver WHERE DeviceClass = 'DISPLAY'");
            foreach (ManagementObject mo in searcher.Get())
            {
                string deviceName = mo["DeviceName"]?.ToString() ?? "Unknown";
                string driverVersion = mo["DriverVersion"]?.ToString() ?? "Unknown";
                string driverDate = mo["DriverDate"]?.ToString() ?? "Unknown";
                string status = GetDeviceStatus(mo["DeviceID"]?.ToString());

                drivers.Add(new DriverInfo
                {
                    DeviceName = deviceName,
                    DriverVersion = driverVersion,
                    DriverDate = driverDate,
                    Status = status
                });
            }

            var view = CollectionViewSource.GetDefaultView(drivers);
            view.SortDescriptions.Add(new SortDescription(currentSortProperty, currentSortDirection));
            view.Filter = o =>
            {
                var driver = o as DriverInfo;
                if (currentFilterType == FilterType.All) return true;
                if (currentFilterType == FilterType.Ok) return driver.Status == Properties.EngUI.StatusOK;
                return driver.Status == Properties.EngUI.StatusNotOK;
            };
            DriversList.ItemsSource = view;
        }

        private string GetDeviceStatus(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
                return "Unknown";

            try
            {
                ManagementObjectSearcher entitySearcher = new ManagementObjectSearcher($"SELECT * FROM Win32_PnPEntity WHERE DeviceID = '{deviceId.Replace("\\", "\\\\")}'");
                foreach (ManagementObject obj in entitySearcher.Get())
                {
                    uint errorCode = Convert.ToUInt32(obj["ConfigManagerErrorCode"] ?? 0);
                    return errorCode == 0 ? Properties.EngUI.StatusOK : Properties.EngUI.StatusNotOK;
                }
            }
            catch
            {
                return "Not Found";
            }
            return "Not Found";
        }

        private void ShowColorBars_Click(object sender, RoutedEventArgs e)
        {
            var window = new TestPatternWindow("ColorBars");
            window.Show();
        }

        private void ShowGrayscale_Click(object sender, RoutedEventArgs e)
        {
            var window = new TestPatternWindow("Grayscale");
            window.Show();
        }

        private void ShowGrid_Click(object sender, RoutedEventArgs e)
        {
            var window = new TestPatternWindow("Grid");
            window.Show();
        }

        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ms-settings:windowsupdate",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Win32Exception ex)
            {
                // Логування або повідомлення користувачу
                MessageBox.Show($"Не вдалося відкрити сторінку оновлень: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDevicesToText_Click(object sender, RoutedEventArgs e)
        {
            string exportText = $"{Properties.EngUI.DevicesTab}\n\n";

            exportText += "Graphics Cards:\n";
            foreach (var gpu in gpus)
            {
                exportText += $"Name: {gpu.Name}\n";
                exportText += $"Temperature: {gpu.Temperature} °C\n";
                exportText += $"Clock Speed: {gpu.ClockSpeed} MHz\n";
                exportText += $"Driver Version: {gpu.DriverVersion}\n";
                exportText += $"Memory: {gpu.AdapterRAM}\n\n";
            }

            exportText += "Monitors:\n";
            foreach (var monitor in monitors)
            {
                exportText += $"Model: {monitor.Model}\n";
                exportText += $"Manufacturer: {monitor.Manufacturer}\n";
                exportText += $"Serial Number: {monitor.SerialNumber}\n";
                exportText += $"Physical Size: {monitor.PhysicalWidth} x {monitor.PhysicalHeight} cm\n";
                exportText += $"Panel Type: {monitor.PanelType}\n\n";
            }

            exportText += "Display Settings:\n";
            for (int i = 0; i < displaySettings.Count; i++)
            {
                var setting = displaySettings[i];
                exportText += $"Display {i + 1}:\n";
                exportText += $"Resolution: {setting.Resolution}\n";
                exportText += $"Refresh Rate: {setting.RefreshRate} Hz\n";
                exportText += $"Color Depth: {setting.ColorDepth} bits\n\n";
            }

            File.WriteAllText("devices_info.txt", exportText);
            MessageBox.Show("Information exported to devices_info.txt");
        }

        private void ExportDevicesToHtml_Click(object sender, RoutedEventArgs e)
        {
            string report = "<html><body><h1>" + Properties.EngUI.DevicesTab + "</h1>";

            report += "<h2>Graphics Cards</h2><ul>";
            foreach (var gpu in gpus)
            {
                report += "<li>" + gpu.Name + "<ul>";
                report += "<li>Temperature: " + gpu.Temperature + " °C</li>";
                report += "<li>Clock Speed: " + gpu.ClockSpeed + " MHz</li>";
                report += "<li>Driver Version: " + gpu.DriverVersion + "</li>";
                report += "<li>Memory: " + gpu.AdapterRAM + "</li>";
                report += "</ul></li>";
            }
            report += "</ul>";

            report += "<h2>Monitors</h2><ul>";
            foreach (var monitor in monitors)
            {
                report += "<li>" + monitor.Model + "<ul>";
                report += "<li>Manufacturer: " + monitor.Manufacturer + "</li>";
                report += "<li>Serial Number: " + monitor.SerialNumber + "</li>";
                report += "<li>Physical Size: " + monitor.PhysicalWidth + " x " + monitor.PhysicalHeight + " cm</li>";
                report += "<li>Panel Type: " + monitor.PanelType + "</li>";
                report += "</ul></li>";
            }
            report += "</ul>";

            report += "<h2>Display Settings</h2><ul>";
            for (int i = 0; i < displaySettings.Count; i++)
            {
                var setting = displaySettings[i];
                report += "<li>Display " + (i + 1) + "<ul>";
                report += "<li>Resolution: " + setting.Resolution + "</li>";
                report += "<li>Refresh Rate: " + setting.RefreshRate + " Hz</li>";
                report += "<li>Color Depth: " + setting.ColorDepth + " bits</li>";
                report += "</ul></li>";
            }
            report += "</ul></body></html>";

            File.WriteAllText("devices_info.html", report);
            MessageBox.Show("Information exported to devices_info.html");
        }

        private void ExportDriversToText_Click(object sender, RoutedEventArgs e)
        {
            string report = $"{Properties.EngUI.DriversTab}\n";
            foreach (DriverInfo driver in DriversList.Items)
            {
                report += $"{driver.DeviceName}\t{driver.DriverVersion}\t{driver.DriverDate}\t{driver.Status}\n";
            }
            File.WriteAllText("drivers_info.txt", report);
            MessageBox.Show("Drivers exported to drivers_info.txt");
        }

        private void ExportDriversToHtml_Click(object sender, RoutedEventArgs e)
        {
            string report = "<html><body><h1>" + Properties.EngUI.DriversTab + "</h1><table border='1'>";
            report += "<tr><th>" + Properties.EngUI.DeviceColumn + "</th><th>" + Properties.EngUI.DriverVersionColumn + "</th><th>" + Properties.EngUI.DriverDateColumn + "</th><th>" + Properties.EngUI.StatusColumn + "</th></tr>";
            foreach (DriverInfo driver in DriversList.Items)
            {
                report += $"<tr><td>{driver.DeviceName}</td><td>{driver.DriverVersion}</td><td>{driver.DriverDate}</td><td>{driver.Status}</td></tr>";
            }
            report += "</table></body></html>";
            File.WriteAllText("drivers_info.html", report);
            MessageBox.Show("Drivers exported to drivers_info.html");
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string culture = selectedItem.Tag.ToString();
                SetLanguage(culture);
                PopulateDevicesTree();
                PopulateDriversList();
            }
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selected = selectedItem.Content.ToString();
                if (selected == Properties.EngUI.All)
                    currentFilterType = FilterType.All;
                else if (selected == Properties.EngUI.StatusOK)
                    currentFilterType = FilterType.Ok;
                else if (selected == Properties.EngUI.StatusNotOK)
                    currentFilterType = FilterType.NotOk;
                PopulateDriversList();
            }
        }

        private void UpdateDatabase_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Database update not implemented in this version.");
        }
    }
}