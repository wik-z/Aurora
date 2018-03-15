using Aurora.Devices;
using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aurora.Utils;
using SharpDX.RawInput;
using Aurora.Devices.Layout;

namespace Aurora.Settings
{
    public sealed class KeyRecorder : IDisposable
    {
	    private readonly InputEvents inputEvents;
	    private String recordingType = "";
        private bool isSingleKey = false;
        private List<DeviceLED> recordedKeys = new List<DeviceLED>();
        public delegate void RecordingFinishedHandler(DeviceLED[] resulting_keys);
        public event RecordingFinishedHandler FinishedRecording;

        public KeyRecorder(InputEvents inputEvents)
        {
	        this.inputEvents = inputEvents;
	        Reset();

	        inputEvents.KeyUp += InputEventsOnKeyUp;
        }
        
        private void InputEventsOnKeyUp(object sender, KeyboardInputEventArgs e)
        {
            if (IsRecording())
            {
                KeyboardDeviceLayout.KeyboardKeys key = e.GetDeviceKey();

                if(key != KeyboardDeviceLayout.KeyboardKeys.NONE)
                {
                    DeviceLED devLed = KeyboardDeviceLayout.GetDeviceLED((short)key);
                    if (HasRecorded(devLed))
                        RemoveKey(devLed);
                    else
                        AddKey(devLed);
                }
            }
        }

        public void AddKey(DeviceLED key)
        {
            if (!IsRecording())
                return;

            if (!HasRecorded(key))
            {
                recordedKeys.Add(key);

                if(isSingleKey)
                {
                    StopRecording();
                }
            }
        }

        public void RemoveKey(DeviceLED key)
        {
            if (!IsRecording())
                return;

            if (HasRecorded(key))
            {
                recordedKeys.Remove(key);
            }
        }

        public bool HasRecorded(DeviceLED key)
        {
            return recordedKeys.Contains(key);
        }

        public DeviceLED[] GetKeys()
        {
            return recordedKeys.ToArray();
        }

        public void StartRecording(String type, bool isSingleKey = false)
        {
            Reset();

            recordingType = type;
            this.isSingleKey = isSingleKey;
        }

        public void StopRecording()
        {
            recordingType = "";
            isSingleKey = false;

            if (FinishedRecording != null)
            {
                FinishedRecording(GetKeys());
            }
        }

        public bool IsRecording(String type = "")
        {
            if(String.IsNullOrWhiteSpace(type))
            {
                return !String.IsNullOrWhiteSpace(recordingType);
            }
            else
            {
                return recordingType.Equals(type);
            }
        }

        public string GetRecordingType()
        {
            return recordingType;
        }

        public void Reset()
        {
            recordingType = "";
            isSingleKey = false;
            recordedKeys = new List<DeviceLED>();
        }

        public bool IsSingleKey()
        {
            return isSingleKey;
        }

	    private bool disposed;

	    public void Dispose()
	    {
		    if (!disposed)
		    {
			    disposed = true;
			    inputEvents.KeyUp -= InputEventsOnKeyUp;
			}
		}
    }
}
