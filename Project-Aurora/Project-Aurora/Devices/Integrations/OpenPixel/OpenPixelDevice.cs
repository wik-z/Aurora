using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aurora.Settings;

namespace Aurora.Devices.OpenPixel
{
    public class OpenPixelDevice : DeviceIntegration
    {
        public string _ip;

        public int _port;

        public Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        private long lastUpdateTime = 0;

        public string GetDeviceDetails()
        {
            return "OpenPixel";
        }

        public string GetDeviceName()
        {
            return "OpenPixel";
        }

        public string GetDeviceUpdatePerformance()
        {
            return (_socket.Connected ? lastUpdateTime + " ms" : "");
        }

        public VariableRegistry GetRegisteredVariables()
        {
            return new VariableRegistry();
        }

        private bool ensureConnected()
        {
            if (_socket.Connected)
            {
                //Global.logger.LogLine("OpenPixel: Ensure Connected, already connected, doing nothing", Logging_Level.Info);
                return true;
            }

            else
            {
                try
                {
                    Global.logger.LogLine("OpenPixel: Ensure Connected, trying to connect...", Logging_Level.Info);
                    _socket.Ttl = 1;
                    IPAddress ip = IPAddress.Parse(_ip);
                    _socket.Connect(ip, _port);
                    Global.logger.LogLine("OpenPixel: Ensure Connected, ....success", Logging_Level.Info);
                    return true;
                }

                catch (SocketException e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
        }

        public bool Initialize()
        {
            //Set as options
            _ip = "127.0.0.1";
            _port = 7890;

            return ensureConnected();
        }


        public bool IsConnected()
        {
            return _socket.Connected;
        }

        public bool IsInitialized()
        {
            return _socket.Connected;
        }

        public bool IsKeyboardConnected()
        {
            throw new NotImplementedException();
        }

        public bool IsPeripheralConnected()
        {
            throw new NotImplementedException();
        }

        public bool Reconnect()
        {
            return ensureConnected();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            if (_socket.Connected)
                _socket.Disconnect(true);
        }

        public bool UpdateDevice(Dictionary<DeviceKeys, Color> keyColors, CancellationToken token, bool forced = false)
        {
            if (token.IsCancellationRequested) return false;

            // debug("put pixes: connecting");
            bool is_connected = ensureConnected();
            if (!is_connected)
            {
                //debug("Put pixels not connected. Ignoring these pixels.");
                return false;
            }
            int channel = 0;
            int count = 12;
            int len_hi_byte = count * 3 / 256;
            int len_low_byte = (count * 3) % 256;

            if (token.IsCancellationRequested) return false;

            List<byte> pieces = new List<byte>
            {
                Convert.ToByte(channel),
                Convert.ToByte(0),
                Convert.ToByte(len_hi_byte),
                Convert.ToByte(len_low_byte)
            };
            
            for(int item = 2; item <= 13; item++)
            {
                Color color = keyColors[(DeviceKeys)item];
                color = Color.FromArgb(255, Utils.ColorUtils.MultiplyColorByScalar(color, color.A / 255.0D));
                pieces.Add(color.R);
                pieces.Add(color.G);
                pieces.Add(color.B);
            }

            if (token.IsCancellationRequested) return false;

            return _socket.Send(pieces.ToArray()) == 0;
        }

        public bool UpdateDevice(DeviceColorComposition colorComposition, CancellationToken token, bool forced = false)
        {
            watch.Restart();

            bool update_result = UpdateDevice(colorComposition.deviceColours, token, forced);

            watch.Stop();
            lastUpdateTime = watch.ElapsedMilliseconds;

            return update_result;
        }
    }
}
