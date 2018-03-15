using Aurora.Settings;
using Aurora.Settings.Layers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Aurora.Settings
{
    public abstract class Settings : NotifyPropertyChangedEx, ICloneable
    {
        public object Clone()
        {
            string str = JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Binder = Aurora.Utils.JSONUtils.SerializationBinder });

            return JsonConvert.DeserializeObject(
                    str,
                    this.GetType(),
                    new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace, TypeNameHandling = TypeNameHandling.All, Binder = Aurora.Utils.JSONUtils.SerializationBinder }
                    );
        }
    }

    public class ScriptSettings : Settings
    {
        #region Private Properties
        private KeySequence _Keys;

        private bool _Enabled = false;

        private bool _ExceptionHit = false;

        private Exception _Exception = null;
        #endregion

        #region Public Properties
        public KeySequence Keys { get { return _Keys; } set { var old = _Keys; _Keys = value; InvokePropertyChanged(_Keys, value); } }

        public bool Enabled { get { return _Enabled; }
            set {
                var old = _Enabled;
                _Enabled = value;
                if (value)
                {
                    ExceptionHit = false;
                    Exception = null;
                }
                InvokePropertyChanged(old, value);
            }
        }

        [JsonIgnore]
        public bool ExceptionHit { get { return _ExceptionHit; } set { var old = _ExceptionHit; _ExceptionHit = value; InvokePropertyChanged(old, value); } }

        [JsonIgnore]
        public Exception Exception { get { return _Exception; } set { var old = _Exception;  _Exception = value; InvokePropertyChanged(old, value); } }
        #endregion

        public ScriptSettings(dynamic script)
        {
            if (script?.DefaultKeys != null && script?.DefaultKeys is KeySequence)
                Keys = script.DefaultKeys;
        }
    }

    public class ApplicationProfile : Settings, IDisposable
    {
        #region Private Properties
        private string _ProfileName = "";

        private Keybind _triggerKeybind;

        private Dictionary<string, ScriptSettings> _ScriptSettings;

        private ObservableCollection<Layer> _Layers;
        #endregion

        #region Public Properties
        public string ProfileName { get { return _ProfileName; } set { var old = _ProfileName; _ProfileName = value; InvokePropertyChanged(old, value); } }

        public Keybind TriggerKeybind { get { return _triggerKeybind; } set { var old = _triggerKeybind; _triggerKeybind = value; InvokePropertyChanged(old, value); } }

        [JsonIgnore]
        public string ProfileFilepath { get; set; }

        public Dictionary<string, ScriptSettings> ScriptSettings { get { return _ScriptSettings; } set { var old = _ScriptSettings; _ScriptSettings = value; InvokePropertyChanged(old, value); } }

        public ObservableCollection<Layer> Layers { get { return _Layers; } set { var old = _ScriptSettings; _Layers = value; InvokePropertyChanged(old, value); } }
        #endregion

        public ApplicationProfile()
        {
            this.Reset();
        }

        public virtual void Reset()
        {
            _Layers = new ObservableCollection<Layer>();
            _ScriptSettings = new Dictionary<string, Aurora.Settings.ScriptSettings>();
            _triggerKeybind = new Keybind();
        }

        public virtual void Dispose()
        {
            foreach (Layer l in _Layers)
                l.Dispose();
        }
    }
}
