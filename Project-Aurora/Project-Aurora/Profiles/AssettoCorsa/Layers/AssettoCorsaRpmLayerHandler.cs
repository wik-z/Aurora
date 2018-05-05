using Aurora.EffectsEngine;
using Aurora.Profiles.AssettoCorsa.GSI;
using Aurora.Settings;
using Aurora.Settings.Layers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Aurora.Profiles.AssettoCorsa.Layers
{
    public class AssettoCorsaRpmLayerHandlerProperties : LayerHandlerProperties2Color<AssettoCorsaRpmLayerHandlerProperties>
    {
        public AssettoCorsaRpmLayerHandlerProperties() : base() { }

        public AssettoCorsaRpmLayerHandlerProperties(bool assign_default = false) : base(assign_default) { }

        public override void Default()
        {
            base.Default();
        }

    }

    public class AssettoCorsaRpmLayerHandler : LayerHandler<AssettoCorsaRpmLayerHandlerProperties>
    {
        public AssettoCorsaRpmLayerHandler() : base()
        {
            _ID = "AssettoCorsaRPM";
        }

        protected override UserControl CreateControl()
        {
            return new Control_AssettoCorsaRpmLayer(this);
        }

        public override EffectLayer Render(IGameState state)
        {
            EffectLayer rpm_layer = new EffectLayer("Assetto Corsa - RPM");

            if (state is GameState_AssettoCorsa)
            {
                GameState_AssettoCorsa ac_state = state as GameState_AssettoCorsa;

                //Update Rpms Layer
                if (ac_state.Rpms > 4000)
                {
                    Color rpm_color = Color.FromArgb(255, 0, 0);

                    rpm_layer.Fill(rpm_color);
                }
            }

            return rpm_layer;
        }

        public override void SetApplication(Application profile)
        {
            (Control as Control_AssettoCorsaRpmLayer).SetProfile(profile);
            base.SetApplication(profile);
        }
    }
}