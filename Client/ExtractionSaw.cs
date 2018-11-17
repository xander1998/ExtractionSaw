using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Newtonsoft.Json;
using NativeUI;
using Client.Models;

namespace Client
{
    public class ExtractionSaw : BaseScript
    {

        // Constant Variables
        const string SawModel = "prop_tool_consaw";
        const string AnimDict = "weapons@heavy@minigun";
        const string AnimName = "idle_2_aim_right_med";
        const string ParticleDict = "des_fib_floor";
        const string ParticleName = "ent_ray_fbi5a_ramp_metal_imp";

        // Main Variables
        public bool ScriptReady = false;
        public List<SawVehicle> SawVehicles = new List<SawVehicle>();
        public List<string> SawPeds = new List<string>();
        public int CutTimer = 10;
        public Entity CurrentSaw = null;
        public int CurrentSawNetHandle = 0;
        public bool CurrentSawActive = false;
        public Vehicle vehicleBeingCut;

        // Keyholding
        public bool KeyHolding = false;
        public int KeyHoldStartTime = 0;

        // Inital BaseScript Call
        public ExtractionSaw()
        {
            EventHandlers.Add("ExtractionSaw:InitializePlayer", new Action<string>(InitializeScript));
            EventHandlers.Add("ExtractionSaw:StartSawSync", new Action<int>(StartSaw));
            EventHandlers.Add("ExtractionSaw:StopSawSync", new Action<int>(StopSaw));
            EventHandlers.Add("ExtractionSaw:RemoteDoorCut", new Action<int, int>(RemoteDoorCut));
            Tick += OnTick;

            // Initialize Client
            TriggerServerEvent("ExtractionSaw:GetInitializedData");
        }

        // Initializes Client Start Configurations
        private void InitializeScript(string _vehiclesConfig)
        {
            SawVehicles = JsonConvert.DeserializeObject<List<SawVehicle>>(_vehiclesConfig);
            ScriptReady = true;
        }

        // Sync event for syncing particles to other clients
        private async void StartSaw(int sawid)
        {
            var SawObject = API.NetToObj(sawid);
            API.RequestNamedPtfxAsset(ParticleDict);
            while (!API.HasNamedPtfxAssetLoaded(ParticleDict))
            {
                await Delay(0);
            }
            API.UseParticleFxAssetNextCall(ParticleDict);
            API.StartParticleFxNonLoopedOnEntity(ParticleName, SawObject, -0.715f, 0.005f, 0f, 0f, 25f, 25f, 0.75f, false, false, false);
        }

        // Sync event for syncing to remove particles to other clients
        private async void StopSaw(int sawid)
        {
            var SawObject = API.NetToObj(sawid);
            await Delay(150);
            API.RemoveParticleFxFromEntity(SawObject);
        }

        // Fixes players in the vehicle after the door cut
        private void RemoteDoorCut(int player, int doorIndex)
        {
            Vehicle vehicle = new Vehicle(LocalPlayer.Character.CurrentVehicle.Handle);
            vehicle.Doors[(VehicleDoorIndex)doorIndex].Break(true);
        }

        // Handles cutting | saw storage and grab | saw pickup and drop
        private async Task OnTick()
        {
            await Task.FromResult(0);

            if (ScriptReady)
            {
                if (CurrentSaw == null)
                {

                    Vehicle truck = GetVehicleInArea();

                    // Grabbing saw from vehicle
                    if (truck != null)
                    {
                        var CheckedVehicle = CheckCastedVehicle(truck);
                        if (CheckedVehicle != null)
                        {
                            World.DrawMarker(MarkerType.HorizontalCircleSkinny, truck.GetOffsetPosition(CheckedVehicle.MarkerPos), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), System.Drawing.Color.FromArgb(255, 0, 0), false, false, true, null, null, false);
                            if (World.GetDistance(LocalPlayer.Character.Position, truck.GetOffsetPosition(CheckedVehicle.MarkerPos)) <= 1.2f)
                            {
                                Screen.DisplayHelpTextThisFrame($"~INPUT_PICKUP~ Retrieve Saw");
                                if (Game.IsControlJustPressed(1, Control.Pickup))
                                {
                                    await RetrieveSaw();
                                }
                            }
                        }
                    }

                    // Pickup Saw From Ground
                    var LocalPedPos = LocalPlayer.Character.Position;
                    var closestSaw = API.GetClosestObjectOfType(LocalPedPos.X, LocalPedPos.Y, LocalPedPos.Z, 5f, (uint)API.GetHashKey(SawModel), false, true, true);
                    var closestSawPos = API.GetEntityCoords(closestSaw, false);
                    var closestSawDistance = World.GetDistance(LocalPedPos, API.GetEntityCoords(closestSaw, false));

                    if (closestSaw != 0 && !LocalPlayer.Character.IsInVehicle() && API.GetEntityAttachedTo(closestSaw) == 0)
                    {
                        World.DrawMarker(MarkerType.HorizontalCircleSkinny, closestSawPos, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), System.Drawing.Color.FromArgb(255, 0, 0), false, false, true, null, null, false);
                        if (closestSawDistance <= 1.2f)
                        {
                            Screen.DisplayHelpTextThisFrame($"~INPUT_PICKUP~ Pickup Saw");
                            if (Game.IsControlJustPressed(1, Control.Pickup))
                            {
                                await PickupSaw(closestSaw);
                            }
                        }
                    }

                }
                else
                {

                    Game.DisableControlThisFrame(1, Control.Attack);
                    Game.DisableControlThisFrame(1, Control.Attack2);
                    Game.DisableControlThisFrame(1, Control.Jump);
                    Game.DisableControlThisFrame(1, Control.Aim);
                    Game.DisableControlThisFrame(1, Control.Enter);
                    if (LocalPlayer.Character.Weapons.Current.Hash != WeaponHash.Unarmed)
                    {
                        API.SetCurrentPedWeapon(LocalPlayer.Character.Handle, (uint)WeaponHash.Unarmed, true);
                    }

                    Vehicle truck = GetVehicleInArea();
                    Vehicle vehicle = GetVehicleFromCast();

                    // Storing saw to vehicle
                    if (truck != null)
                    {
                        var CheckedVehicle = CheckCastedVehicle(truck);
                        if (CheckedVehicle != null)
                        {
                            World.DrawMarker(MarkerType.HorizontalCircleSkinny, truck.GetOffsetPosition(CheckedVehicle.MarkerPos), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), System.Drawing.Color.FromArgb(255, 0, 0), false, false, true, null, null, false);
                            if (World.GetDistance(LocalPlayer.Character.Position, truck.GetOffsetPosition(CheckedVehicle.MarkerPos)) <= 1.2f)
                            {
                                Screen.DisplayHelpTextThisFrame($"~INPUT_PICKUP~ Store Saw");
                                if (Game.IsControlJustPressed(1, Control.Pickup))
                                {
                                    StoreSaw();
                                }
                            }
                        }
                    }
                    else
                    {
                        // Dropping Saw
                        if (Game.IsControlJustPressed(1, Control.Detonate))
                        {
                            await DropSaw();
                        }
                    }

                    // Cutting Logic
                    if (KeyHolding && GetClosestDoorBone(vehicleBeingCut) != (VehicleDoorIndex)(-1) && !vehicleBeingCut.Doors[GetClosestDoorBone(vehicleBeingCut)].IsBroken)
                    {
                        if (Game.IsDisabledControlPressed(1, Control.Attack))
                        {
                            if (Game.GameTime > KeyHoldStartTime)
                            {
                                KeyHolding = false;
                                KeyHoldStartTime = 0;
                                TriggerServerEvent("ExtractionSaw:StopSawCutSync", CurrentSawNetHandle);
                                VehicleDoorIndex doorIndex = GetClosestDoorBone(vehicle);
                                CutDoor(vehicleBeingCut, doorIndex);
                            }
                            BarTimerBar bar = new BarTimerBar("Cutting Door");
                            bar.Percentage = 1f - (float)(((KeyHoldStartTime - Game.GameTime) / 1000) * 0.1m) - 0.1f;
                            bar.Draw(0);
                        }
                        else
                        {
                            KeyHoldStartTime = 0;
                            KeyHolding = false;
                            TriggerServerEvent("ExtractionSaw:StopSawCutSync", CurrentSawNetHandle);
                        }
                    }
                    else
                    {
                        if (Game.IsDisabledControlPressed(1, Control.Attack) && GetClosestDoorBone(vehicle) != (VehicleDoorIndex)(-1))
                        {
                            KeyHoldStartTime = Game.GameTime + (CutTimer * 1000);
                            vehicleBeingCut = vehicle;
                            KeyHolding = true;
                        }
                    }

                    // Saw Particle Syncing
                    if (KeyHolding)
                    {
                        LoopParticles();
                    }

                }
            }
        }

        // Keeps the particles going during the cut
        private void LoopParticles()
        {
            TriggerServerEvent("ExtractionSaw:StartSawCutSync", CurrentSawNetHandle);
        }

        // Create Saw Method
        private async Task RetrieveSaw()
        {
            await LocalPlayer.Character.Task.PlayAnimation(AnimDict, AnimName, -8f, 8f, -1, AnimationFlags.StayInEndFrame | AnimationFlags.UpperBodyOnly | AnimationFlags.AllowRotation, 8f);
            Vector3 PlayerPos = LocalPlayer.Character.GetOffsetPosition(new Vector3(0f, 1f, 0f));
            CurrentSaw = await World.CreateProp(SawModel, PlayerPos, true, true);
            CurrentSawNetHandle = API.NetworkGetNetworkIdFromEntity(CurrentSaw.Handle);
            CurrentSaw.AttachTo(LocalPlayer.Character.Bones[Bone.SKEL_R_Hand], new Vector3(0.095f, 0f, 0f), new Vector3(125f, 155f, 55f));
        }

        // Store the saw back into the truck
        private void StoreSaw()
        {
            CurrentSaw.Detach();
            CurrentSaw.Delete();
            LocalPlayer.Character.Task.ClearAnimation(AnimDict, AnimName);
            CurrentSaw = null;
        }

        // Pickup saw from the ground
        private async Task PickupSaw(int newSaw)
        {
            API.NetworkRequestControlOfEntity(newSaw);
            int delayTime = 0;
            while (!API.NetworkHasControlOfEntity(newSaw))
            {
                await Delay(0);
                delayTime += delayTime + 1;
                if (delayTime > 5000)
                {
                    return;
                }
            }
            API.SetEntityAsMissionEntity(newSaw, true, true);
            API.DeleteEntity(ref newSaw);
            await RetrieveSaw();
        }

        // Drop saw on the ground
        private async Task DropSaw()
        {
            CurrentSaw.Detach();
            LocalPlayer.Character.Task.ClearAnimation(AnimDict, AnimName);
            var PlayerOffset = LocalPlayer.Character.GetOffsetPosition(new Vector3(0f, 1f, 100f));
            CurrentSaw.Position = new Vector3(PlayerOffset.X, PlayerOffset.Y, CurrentSaw.Position.Z - CurrentSaw.HeightAboveGround);
            await Delay(100);
            API.FreezeEntityPosition(CurrentSaw.Handle, true);
            CurrentSaw = null;
        }

        // Get vehicle from raycast
        private Vehicle GetVehicleFromCast()
        {
            RaycastResult VehicleCast;
            Vector3 PlayerPos = LocalPlayer.Character.Position;
            Vector3 PlayerOffsetPos = LocalPlayer.Character.GetOffsetPosition(new Vector3(0f, 3f, 0f));
            VehicleCast = World.RaycastCapsule(PlayerPos, PlayerOffsetPos, 0.2f, IntersectOptions.Everything, LocalPlayer.Character);

            if (VehicleCast.DitHitEntity)
            {
                if (API.IsEntityAVehicle(VehicleCast.HitEntity.Handle))
                {
                    return new Vehicle(VehicleCast.HitEntity.Handle);
                }
            }
            return null;
        }

        // Get vehicle in area
        private Vehicle GetVehicleInArea()
        {
            Vector3 PlayerPos = LocalPlayer.Character.Position;
            Vehicle vehicle;
            foreach (SawVehicle veh in SawVehicles)
            {
                var truck = API.GetClosestVehicle(LocalPlayer.Character.Position.X, LocalPlayer.Character.Position.Y, LocalPlayer.Character.Position.Z, 5f, (uint) API.GetHashKey(veh.Model), 70);
                if (truck != 0)
                {
                    vehicle = new Vehicle(truck);
                    return vehicle;
                }
            }
            return null;
        }

        // Check vehicle is a saw available truck
        private SawVehicle CheckCastedVehicle(Vehicle vehicle)
        {
            foreach (SawVehicle model in SawVehicles)
            {
                if (API.GetHashKey(model.Model) == API.GetEntityModel(vehicle.Handle))
                {
                    return model;
                }
            }
            return null;
        }

        // Cutting Door
        private void CutDoor(Vehicle vehicle, VehicleDoorIndex doorIndex)
        {
            if (IsVehiclePlayerControlled(vehicle))
            {
                List<object> passengers = GetVehiclePlayers(vehicle);
                int newDoorIndex = GetVehicleDoorEnumIndex(doorIndex);
                TriggerServerEvent("ExtractionSaw:SyncDoorCut", passengers, newDoorIndex);
            }
            else
            {
                vehicle.Doors[doorIndex].Break(true);
            }
        }

        // Get Closest Door To Remove
        private VehicleDoorIndex GetClosestDoorBone(Vehicle vehicle)
        {

            if (vehicle == null)
            {
                return (VehicleDoorIndex)(-1);
            }

            var FrontDriverDoorBone = vehicle.Bones["door_dside_f"];
            var RearDriverDoorBone = vehicle.Bones["door_dside_r"];
            var FrontPassengerDoorBone = vehicle.Bones["door_pside_f"];
            var RearPassengerDoorBone = vehicle.Bones["door_pside_r"];
            var LocalPedPos = LocalPlayer.Character.Position;

            VehicleDoorIndex ClosestDoorIndex = VehicleDoorIndex.FrontLeftDoor;
            EntityBone ClosestBone = FrontDriverDoorBone;

            // Checking For Closest Door
            if (World.GetDistance(LocalPedPos, ClosestBone.Position) > World.GetDistance(LocalPedPos, RearDriverDoorBone.Position))
            {
                ClosestBone = RearDriverDoorBone;
                ClosestDoorIndex = VehicleDoorIndex.BackLeftDoor;
            }
            else if (World.GetDistance(LocalPedPos, ClosestBone.Position) > World.GetDistance(LocalPedPos, FrontPassengerDoorBone.Position))
            {
                ClosestBone = FrontPassengerDoorBone;
                ClosestDoorIndex = VehicleDoorIndex.FrontRightDoor;
            }
            else if (World.GetDistance(LocalPedPos, ClosestBone.Position) > World.GetDistance(LocalPedPos, RearPassengerDoorBone.Position))
            {
                ClosestBone = RearPassengerDoorBone;
                ClosestDoorIndex = VehicleDoorIndex.BackRightDoor;
            }

            if (World.GetDistance(LocalPedPos, ClosestBone.Position) < 1.3f)
            {
                return ClosestDoorIndex;
            }
            else
            {
                return (VehicleDoorIndex)(-1);
            }
        }

        // Find Vehicle Controller
        private List<object> GetVehiclePlayers(Vehicle vehicle)
        {
            int seats = API.GetVehicleModelNumberOfSeats((uint) API.GetEntityModel(vehicle.Handle));
            List<object> players = new List<object>();

            for (var seat = -1; seat < seats - 1; seat ++)
            {
                for (var player = 0; player < 64; player++)
                {
                    if (API.GetPlayerPed(player) == vehicle.GetPedOnSeat((VehicleSeat)seat).Handle)
                    {
                        int playerid = API.GetPlayerServerId(player);
                        if (playerid != 0)
                        {
                            players.Add(playerid);
                        }
                    }
                }
            }

            return players;
        }

        // Returns if the vehicle has players or not
        private bool IsVehiclePlayerControlled(Vehicle vehicle)
        {
            int seats = API.GetVehicleModelMaxNumberOfPassengers((uint)vehicle.GetHashCode());
            for (var seat = -1; seat < seats; seat++)
            {
                Ped seatedPed = vehicle.GetPedOnSeat((VehicleSeat)seat);
                if (seatedPed != null)
                {
                    if (seatedPed.IsPlayer)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Returns VehicleDoorIndex Enum Integer
        private int GetVehicleDoorEnumIndex(VehicleDoorIndex doorIndex)
        {
            Debug.WriteLine($"Door Index: {doorIndex}");

            if (doorIndex == VehicleDoorIndex.FrontLeftDoor)
            {
                return 0;
            }
            else if (doorIndex == VehicleDoorIndex.FrontRightDoor)
            {
                return 1;
            }
            else if (doorIndex == VehicleDoorIndex.BackLeftDoor)
            {
                return 2;
            }
            else if (doorIndex == VehicleDoorIndex.BackRightDoor)
            {
                return 3;
            }
            return -1;
        }

    }
}