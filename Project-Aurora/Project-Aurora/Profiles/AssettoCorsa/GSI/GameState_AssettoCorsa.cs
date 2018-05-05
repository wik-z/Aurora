using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Aurora.Profiles;
using ACSM = AssettoCorsaSharedMemory;

namespace Aurora.Profiles.AssettoCorsa.GSI
{
    /// <summary>
    /// A class representing various information retaining to Game State Integration of Assetto Corsa
    /// </summary>
    public class GameState_AssettoCorsa : GameState<GameState_AssettoCorsa>
    {
        private GameState_AssettoCorsa _Previously;

        public int Rpms;

        public GameState_AssettoCorsa()
        {
            ACSM.AssettoCorsa ac = new ACSM.AssettoCorsa();

            ac.PhysicsUpdated += ac_PhysicsUpdated;
        }

        private void ac_PhysicsUpdated(object sender, ACSM.PhysicsEventArgs e)
        {
            Rpms = e.Physics.Rpms;
        }
    }
}
