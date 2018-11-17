using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Server
{
    public class Server : BaseScript
    {

        // Initial BaseScript Call
        public Server()
        {
            EventHandlers.Add("ExtractionSaw:GetInitializedData", new Action<Player>(GetInitializedData));
            EventHandlers.Add("ExtractionSaw:StartSawCutSync", new Action<Player, int>(StartSawCutSync));
            EventHandlers.Add("ExtractionSaw:StopSawCutSync", new Action<Player, int>(StopSawCutSync));
            EventHandlers.Add("ExtractionSaw:SyncDoorCut", new Action<Player, List<object>, int>(SyncDoorCut));
        }

        // Get Script Config Data
        private void GetInitializedData([FromSource] Player player)
        {
            string VehiclesConfig = API.LoadResourceFile(API.GetCurrentResourceName(), "/configs/vehicles.json");
            player.TriggerEvent("ExtractionSaw:InitializePlayer", VehiclesConfig);
        }

        // Sync Cutting Particles Add
        private void StartSawCutSync([FromSource] Player player, int sawid)
        {
            TriggerClientEvent("ExtractionSaw:StartSawSync", sawid);
        }

        // Sync Cutting Particles Delete
        private void StopSawCutSync([FromSource] Player player, int sawid)
        {
            TriggerClientEvent("ExtractionSaw:StopSawSync", sawid);
        }

        // Sync Vehicle Door Cutting
        private void SyncDoorCut([FromSource] Player player, List<object> passengers, int doorIndex)
        {
            Debug.WriteLine(doorIndex.ToString());
            Debug.WriteLine($"Passenger Count: {passengers.Count}");
            foreach (object passenger in passengers)
            {
                TriggerClientEvent("ExtractionSaw:RemoteDoorCut", passenger, doorIndex);
            }
        }
    }
}
