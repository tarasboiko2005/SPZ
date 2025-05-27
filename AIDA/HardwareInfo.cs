namespace AIDA
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management;
    using System.Runtime.InteropServices;
    using OpenHardwareMonitor.Hardware;

    public class HardwareInfo
    {
        public class VideoControllerInfo
        {
            public string Name { get; set; }
            public string PNPDeviceID { get; set; }
            public string AdapterRAM { get; set; }
            public string DriverVersion { get; set; }
            public string CurrentHorizontalResolution { get; set; }
            public string CurrentVerticalResolution { get; set; }
        }

        public class GpuInfo
        {
            public string Name { get; set; }
            public float Temperature { get; set; }
            public float ClockSpeed { get; set; }
            public string DriverVersion { get; set; }
            public string AdapterRAM { get; set; }
        }

        public class MonitorInfo
        {
            public string Manufacturer { get; set; }
            public string Model { get; set; }
            public string SerialNumber { get; set; }
            public int PhysicalWidth { get; set; }
            public int PhysicalHeight { get; set; }
            public string PanelType { get; set; }
        }

        public class DisplaySettings
        {
            public string DeviceName { get; set; }
            public string Resolution { get; set; }
            public int RefreshRate { get; set; }
            public int ColorDepth { get; set; }
        }

        public List<VideoControllerInfo> GetVideoControllerInfo()
        {
            List<VideoControllerInfo> infos = new List<VideoControllerInfo>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                infos.Add(new VideoControllerInfo
                {
                    Name = obj["Name"]?.ToString() ?? "Unknown",
                    PNPDeviceID = obj["PNPDeviceID"]?.ToString(),
                    AdapterRAM = (Convert.ToUInt64(obj["AdapterRAM"] ?? 0) / 1024 / 1024).ToString() + " MB",
                    DriverVersion = obj["DriverVersion"]?.ToString() ?? "Unknown",
                    CurrentHorizontalResolution = obj["CurrentHorizontalResolution"]?.ToString() ?? "Unknown",
                    CurrentVerticalResolution = obj["CurrentVerticalResolution"]?.ToString() ?? "Unknown"
                });
            }
            return infos;
        }

        public static List<GpuInfo> GetGpus()
        {
            var gpus = new List<GpuInfo>();
            var computer = new Computer();
            computer.IsGpuEnabled = true;
            computer.Open(false);

            var hardwareInfo = new HardwareInfo();
            var videoControllers = hardwareInfo.GetVideoControllerInfo();
            int index = 0;

            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
                {
                    hardware.Update();
                    var gpu = new GpuInfo { Name = hardware.Name };

                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                            gpu.Temperature = sensor.Value ?? 0;
                        else if (sensor.SensorType == SensorType.Clock && sensor.Name == "GPU Core")
                            gpu.ClockSpeed = sensor.Value ?? 0;
                    }

                    if (index < videoControllers.Count)
                    {
                        gpu.DriverVersion = videoControllers[index].DriverVersion;
                        gpu.AdapterRAM = videoControllers[index].AdapterRAM;
                    }

                    gpus.Add(gpu);
                    index++;
                }
            }

            computer.Close();
            return gpus;
        }

        public static List<MonitorInfo> GetMonitors()
        {
            var monitors = new List<MonitorInfo>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorID");

            foreach (ManagementObject queryObj in searcher.Get())
            {
                var monitor = new MonitorInfo
                {
                    Manufacturer = DecodeUint16Array((ushort[])queryObj["ManufacturerName"]),
                    Model = DecodeUint16Array((ushort[])queryObj["UserFriendlyName"]),
                    SerialNumber = DecodeUint16Array((ushort[])queryObj["SerialNumberID"])
                };

                ManagementObjectSearcher sizeSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DesktopMonitor");
                foreach (ManagementObject sizeObj in sizeSearcher.Get())
                {
                    monitor.PhysicalWidth = Convert.ToInt32(sizeObj["ScreenWidth"] ?? 0);
                    monitor.PhysicalHeight = Convert.ToInt32(sizeObj["ScreenHeight"] ?? 0);
                    monitor.PanelType = sizeObj["DisplayType"]?.ToString() ?? "Unknown";
                }

                monitors.Add(monitor);
            }

            return monitors;
        }

        public static List<DisplaySettings> GetDisplaySettings()
        {
            var settings = new List<DisplaySettings>();
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                int displayIndex = 1;
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj.Properties["CurrentHorizontalResolution"]?.Value != null &&
                        obj.Properties["CurrentVerticalResolution"]?.Value != null)
                    {
                        settings.Add(new DisplaySettings
                        {
                            DeviceName = $"Display {displayIndex}",
                            Resolution = $"{obj["CurrentHorizontalResolution"]}x{obj["CurrentVerticalResolution"]}",
                            RefreshRate = Convert.ToInt32(obj["CurrentRefreshRate"] ?? 0),
                            ColorDepth = Convert.ToInt32(obj["CurrentBitsPerPixel"] ?? 0)
                        });
                        displayIndex++;
                    }
                }

                // Додатковий запит для уточнення інформації про монітори
                ManagementObjectSearcher monitorSearcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorID");
                var monitorIndex = 1;
                foreach (ManagementObject monitorObj in monitorSearcher.Get())
                {
                    if (settings.Count >= monitorIndex && settings[monitorIndex - 1].DeviceName == $"Display {monitorIndex}")
                    {
                        // Оновлення даних, якщо доступно
                        settings[monitorIndex - 1].DeviceName += $" ({DecodeUint16Array((ushort[])monitorObj["UserFriendlyName"])})";
                    }
                    monitorIndex++;
                }
            }
            catch (ManagementException ex)
            {
                Console.WriteLine($"WMI error in GetDisplaySettings: {ex.Message}");
            }
            return settings;
        }

        private static string DecodeUint16Array(ushort[] array)
        {
            if (array == null) return string.Empty;
            return new string(array.Select(c => (char)c).ToArray()).TrimEnd('\0');
        }
    }
}