using System;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Aurora.Settings;
using Newtonsoft.Json;
using Aurora.Profiles.AssettoCorsa.Layers;
using System.Collections.Generic;

namespace Aurora.Profiles.AssettoCorsa
{
    public class AssettoCorsa : Application
    {
        public AssettoCorsa()
            : base(new LightEventConfig
            {
                Name = "Assetto Corsa",
                ID = "assettocorsa",
                ProcessNames = new[] { "AssettoCorsa.exe" },
                ProfileType = typeof(AssettoCorsaProfile),
                OverviewControlType = typeof(Control_AssettoCorsa),
                GameStateType = typeof(GSI.GameState_AssettoCorsa),
                SettingsType = typeof(FirstTimeApplicationSettings),
                Event = new GameEvent_Generic(),
                IconURI = "Resources/bf3_64x64.png"
            })
        {
            var extra = new List<LayerHandlerEntry> {
                new LayerHandlerEntry("AssettoCorsaRPM", "Assetto Corsa RPM", typeof(AssettoCorsaRpmLayerHandler)),
            };

            Global.LightingStateManager.RegisterLayerHandlers(extra, false);

            foreach (var entry in extra)
            {
                Config.ExtraAvailableLayers.Add(entry.Key);
            }
        }
    }
}
