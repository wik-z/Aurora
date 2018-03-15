using Aurora.Devices.Layout;
using Aurora.Profiles;
using Aurora.Settings;
using CSScriptLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Linq;
using LEDINT = System.Int16;
using Aurora.Utils;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace Aurora.Devices
{
    public class DeviceContainer
    {
        //List of devices that the integration updates from
        public List<DeviceLayout> Devices { get; set; }

        public DeviceIntegration Device { get; set; }

        public BackgroundWorker Worker = new BackgroundWorker();
        public Thread UpdateThread { get; set; } = null;

        public CancellationTokenSource UpdateTaskCancellationTokenSource { get; set; } = null;

        private Tuple<BitmapLock, List<DeviceLayout>, bool> currentComp = null;
        private bool newFrame = false;

        public DeviceContainer(DeviceIntegration device)
        {
            this.Device = device;
            Worker.DoWork += WorkerOnDoWork;
            Worker.RunWorkerCompleted += (sender, args) =>
            {
                if (newFrame)
                    Worker.RunWorkerAsync();
            };
            Worker.WorkerSupportsCancellation = true;
        }

        private void WorkerOnDoWork(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            newFrame = false;
            UpdateTaskCancellationTokenSource = new CancellationTokenSource();
            Device.UpdateDevice(currentComp.Item1, currentComp.Item2, UpdateTaskCancellationTokenSource.Token,
                currentComp.Item3);
        }

        public void UpdateDevice(BitmapLock composition, bool forced = false)
        {
            UpdateTaskCancellationTokenSource?.Cancel();

            newFrame = true;
            currentComp = new Tuple<BitmapLock, List<DeviceLayout>, bool>(composition, Devices, forced);

            if (!Worker.IsBusy)
                Worker.RunWorkerAsync();
        }
    }

    public class DeviceManager : ObjectSettings<DeviceLayoutSettings>, IInit, IDisposable
    {
        private Dictionary<short, DeviceContainer> devices = new Dictionary<short, DeviceContainer>();

        public Dictionary<short, DeviceContainer> Devices { get { return devices; } }

        private bool anyInitialized = false;
        private bool retryActivated = false;
        private const int retryInterval = 5000;
        private const int retryAttemps = 15;
        private int retryAttemptsLeft = retryAttemps;
        private Thread retryThread;

        private bool _InitializeOnceAllowed = false;

        public int RetryAttempts
        {
            get
            {
                return retryAttemptsLeft;
            }
        }

        public event EventHandler NewDevicesInitialized;

        //TODO: Gotta make this be updated anytime the style changes for the DeviceLayout or a new device is added
        public List<DynamicDeviceLED> AllLEDS { get; private set; }

        private Grid _virtualLayout = new Grid();

        public Grid VirtualLayout
        {
            get {
                return _virtualLayout;
            }
        }

        public float canvas_width { get { return LayoutUtils.PixelToByte(this._virtualLayout.Width); } }

        public float canvas_height { get { return LayoutUtils.PixelToByte(this._virtualLayout.Height); } }
        
        public float canvas_width_center { get { return canvas_width / 2.0f; } }

        public float canvas_height_center { get { return canvas_height / 2.0f; } }

        public float editor_to_canvas { get { return 1f/(float)Global.Configuration.BitmapAccuracy; } }

        public float canvas_biggest { get { return canvas_width > canvas_height ? canvas_width : canvas_height; } }

        public DeviceManager()
        {

        }

        public bool Initialized { get; private set; }

        public bool Initialize()
        {
            if (Initialized)
                return true;

            #region Add Integrations
            AddDevice(new Devices.Logitech.LogitechDevice());         // Logitech Device
            AddDevice(new Devices.Corsair.CorsairDevice());           // Corsair Device
            AddDevice(new Devices.Razer.RazerDevice());               // Razer Device
            //devices.Add(new Devices.Roccat.RoccatDevice());             // Roccat Device
            AddDevice(new Devices.Clevo.ClevoDevice());               // Clevo Device
            AddDevice(new Devices.CoolerMaster.CoolerMasterDevice()); // CoolerMaster Device
            AddDevice(new Devices.AtmoOrbDevice.AtmoOrbDevice());     // AtmoOrb Ambilight Device
            AddDevice(new Devices.SteelSeries.SteelSeriesDevice());   // SteelSeries Device
            AddDevice(new Devices.OpenPixel.OpenPixelDevice());       // OpenPixel Device

            string devices_scripts_path = System.IO.Path.Combine(Global.ExecutingDirectory, "Scripts", "Devices");

            if (Directory.Exists(devices_scripts_path))
            {
                foreach (string device_script in Directory.EnumerateFiles(devices_scripts_path, "*.*"))
                {
                    try
                    {
                        string ext = Path.GetExtension(device_script);
                        switch (ext)
                        {
                            case ".py":
                                var scope = Global.PythonEngine.ExecuteFile(device_script);
                                dynamic main_type;
                                if (scope.TryGetVariable("main", out main_type))
                                {
                                    dynamic script = Global.PythonEngine.Operations.CreateInstance(main_type);

                                    DeviceIntegration scripted_device = new Devices.ScriptedDevice.ScriptedDevice(script);

                                    AddDevice(scripted_device);
                                }
                                else
                                    Global.logger.Error("Script \"{0}\" does not contain a public 'main' class", device_script);

                                break;
                            case ".cs":
                                System.Reflection.Assembly script_assembly = CSScript.LoadCodeFrom(device_script);
                                foreach (Type typ in script_assembly.ExportedTypes)
                                {
                                    dynamic script = Activator.CreateInstance(typ);

                                    DeviceIntegration scripted_device = new Devices.ScriptedDevice.ScriptedDevice(script);

                                    AddDevice(scripted_device);
                                }

                                break;
                            default:
                                Global.logger.Error("Script with path {0} has an unsupported type/ext! ({1})", device_script, ext);
                                break;
                        }
                    }
                    catch (Exception exc)
                    {
                        Global.logger.Error("An error occured while trying to load script {0}. Exception: {1}", device_script, exc);
                    }
                }
            }
            #endregion

            LoadSettings();

            InitDevices();

            Dictionary<byte, List<DeviceLayout>> allDevices = new Dictionary<byte, List<DeviceLayout>>();

            //Take out all the ones that specifically target an integration and give us all the ones that should target any
            foreach (var devType in this.Settings.Devices)
            {
                List<DeviceLayout> remaining = new List<DeviceLayout>();

                foreach (DeviceLayout dev in devType.Value)
                {
                    dev.PropertyChanged += this.deviceLayoutPropertyChanged;

                    if (dev.SelectedSDK != -1)
                    {
                        if (this.devices.ContainsKey(dev.SelectedSDK))
                        {
                            this.devices[dev.SelectedSDK].Devices.Add(dev);
                        }
                        else
                        {
                            //error
                            Global.logger.LogLine($"[Devices] DeviceLayout has SDK with `{dev.SelectedSDK}` selected, but it doesn't exist!");
                        }
                    }
                    else
                        remaining.Add(dev);

                    //Add event so that it updates the deviceID when needed
                    devType.Value.CollectionChanged += dev.Moved;

                    dev.Initialize();
                    
                    this._virtualLayout.Children.Add(dev.VirtualLayout);

                    foreach (var led in dev.VirtualGroup.grouped_keys)
                    {
                        this.AllLEDS.Add(new DynamicDeviceLED(led.tag, dev));
                    }
                }

                if (remaining.Count > 0)
                    allDevices.Add(devType.Key, remaining);
            }

            //Add devices that target all that support its type to every integration that supports it
            foreach (var integration in this.devices)
            {
                foreach (byte type in integration.Value.Device.SupportedDeviceTypes)
                {
                    if (allDevices.ContainsKey(type))
                    {
                        integration.Value.Devices.AddRange(allDevices[type]);
                    }
                }
            }

            Initialized = true;
            return Initialized;
        }

        private void deviceLayoutPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(DeviceLayout.SelectedSDK))) {
                //TODO: reassign the device to the currently selected sdk
                //need to case PropertyChangedEventArgs to PropertyChangedExEventArgs
            }
        }

        public void NewDevice(Type deviceLayoutType)
        {
            //TODO: Implement this
        }

        public BitmapRectangle GetBitmappingFromLED(DeviceLED led)
        {
            DeviceLayout device = GetDeviceFromDeviceLED(led);
            if (device != null)
            {
                Point offset = device.Location.ToPixel();
                BitmapRectangle rec;
                if (device.VirtualGroup.BitmapMap.TryGetValue(led.LedID, out rec))
                {
                    return rec.AddOffset(offset);
                }
                else
                {
                    //error
                }
            }

            return null;
        }

        public DeviceLayout GetDeviceFromDeviceLED(DeviceLED deviceLED)
        {
            ObservableCollection<DeviceLayout> devs;
            if (this.Settings.Devices.TryGetValue(deviceLED.DeviceTypeID, out devs))
            {
                if (deviceLED.DeviceID < devs.Count)
                {
                    return devs[deviceLED.DeviceID];
                }
                else
                {
                    //error
                }
            }
            else
            {
                //error
            }


            return null;
        }        

        public bool AddDevice(DeviceIntegration dev)
        {
            if (this.devices.ContainsKey(dev.DeviceID))
            {
                //exception
                Global.logger.LogLine($"[DeviceManager] Device with ID `{dev.DeviceID}` has already been registered!", Logging_Level.Error);
                return false;
            }

            this.devices.Add(dev.DeviceID, new DeviceContainer(dev));

            return true;
        }

        public void RegisterVariables()
        {
            //Register any variables
            foreach (var device in devices)
                Global.Configuration.VarRegistry.Combine(device.Value.Device.GetRegisteredVariables());
        }

        public void InitDevices()
        {
            int devicesToRetryNo = 0;
            foreach (KeyValuePair<short, DeviceContainer> deviceContainer in devices)
            {
                DeviceIntegration device = deviceContainer.Value.Device;
                if (device.IsInitialized() || Global.Configuration.devices_disabled.Contains(device.GetType()))
                    continue;

                if (device.Initialize())
                    anyInitialized = true;
                else
                    devicesToRetryNo++;

                Global.logger.Info("Device, " + device.GetDeviceName() + ", was" + (device.IsInitialized() ? "" : " not") + " initialized");
            }

            NewDevicesInitialized?.Invoke(this, new EventArgs());

            if (devicesToRetryNo > 0 && !retryActivated)
            {
                retryThread = new Thread(RetryInitialize);
                retryThread.Start();

                retryActivated = true;
            }

            _InitializeOnceAllowed = true;
        }

        private void RetryInitialize()
        {
            for (int try_count = 0; try_count < retryAttemps; try_count++)
            {
                Global.logger.Info("Retrying Device Initialization");
                int devicesAttempted = 0;
                bool _anyInitialized = false;
                foreach (KeyValuePair<short, DeviceContainer> deviceContainer in devices)
                {
                    DeviceIntegration device = deviceContainer.Value.Device;
                    if (device.IsInitialized() || Global.Configuration.devices_disabled.Contains(device.GetType()))
                        continue;

                    devicesAttempted++;
                    if (device.Initialize())
                        _anyInitialized = true;

                    Global.logger.Info("Device, " + device.GetDeviceName() + ", was" + (device.IsInitialized() ? "" : " not") + " initialized");
                }

                retryAttemptsLeft--;

                //We don't need to continue the loop if we aren't trying to initialize anything
                if (devicesAttempted == 0)
                    break;

                //There is only a state change if something suddenly becomes initialized
                if (_anyInitialized)
                {
                    NewDevicesInitialized?.Invoke(this, new EventArgs());
                    anyInitialized = true;
                }

                Thread.Sleep(retryInterval);
            }
        }

        public void InitializeOnce()
        {
            if (!anyInitialized && _InitializeOnceAllowed)
                Initialize();
        }

        public bool AnyInitialized()
        {
            return anyInitialized;
        }

        public DeviceIntegration[] GetInitializedDevices()
        {
            List<DeviceIntegration> ret = new List<DeviceIntegration>();

            foreach (var device in devices)
            {
                if (device.Value.Device.IsInitialized())
                {
                    ret.Add(device.Value.Device);
                }
            }

            return ret.ToArray();
        }

        public void Shutdown()
        {
            foreach (var device in devices)
            {
                if (device.Value.Device.IsInitialized())
                {
                    device.Value.Device.Shutdown();
                    Global.logger.Info("Device, " + device.Value.Device.GetDeviceName() + ", was shutdown");
                }
            }

            anyInitialized = false;
        }

        public void ResetDevices()
        {
            foreach (var device in devices)
            {
                if (device.Value.Device.IsInitialized())
                {
                    device.Value.Device.Reset();
                }
            }
        }

        public void UpdateDevices(BitmapLock composition, bool forced = false)
        {
            //Update deviceLayout colours
            foreach (var deviceGroups in this.Settings.Devices)
            {
                for (int i = 0; i < deviceGroups.Value.Count; i++)
                {
                    DeviceLayout device = deviceGroups.Value[i];
                    Point offset = device.Location.ToPixel();
                    Dictionary<LEDINT, Color> colours = new Dictionary<LEDINT, Color>();
                    foreach (KeyValuePair<LEDINT, BitmapRectangle> led in device.VirtualGroup.BitmapMap)
                    {
                        colours.Add(led.Key, composition.Bitmap.GetRegionColor(led.Value.AddOffset(offset)));
                    }

                    device.DeviceColours = new DeviceColorComposition()
                    {
                        deviceColours = colours,
                        keyBitmap = composition.Bitmap.Clone(device.VirtualGroup.BitmapRegion.AddOffset(offset), composition.Bitmap.PixelFormat)

                    };
                }
            }

            //Update devices with the overall composition
            foreach (KeyValuePair<short, DeviceContainer> deviceContainer in devices)
            {
                DeviceIntegration device = deviceContainer.Value.Device;

                if (device.IsInitialized())
                {
                    if (Global.Configuration.devices_disabled.Contains(device.GetType()))
                    {
                        //Initialized when it's supposed to be disabled? SMACK IT!
                        device.Shutdown();
                        continue;
                    }
                    
                    deviceContainer.Value.UpdateDevice(composition, forced);
                }
            }
        }

        public string GetDevices()
        {
            string devices_info = "";

            foreach (var device in devices)
                devices_info += device.Value.Device.GetDeviceDetails() + "\r\n";

            if (retryAttemptsLeft > 0)
                devices_info += "Retries: " + retryAttemptsLeft + "\r\n";

            return devices_info;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).

                    if (retryThread != null)
                    {
                        retryThread.Abort();
                        retryThread = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DeviceManager() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
