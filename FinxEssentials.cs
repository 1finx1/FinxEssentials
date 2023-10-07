using FinxEssentials;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Commands;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CombinedPlugin
{
    public class CombinedPlugin : RocketPlugin<CombinedConfig>, IDefaultable
    {

        protected override void Load()
        {
            Rocket.Core.Logging.Logger.Log("FinxEssentials has been loaded!");
            Configuration.Load();

            U.Events.OnPlayerConnected += OnPlayerConnected;
            UnturnedPlayerEvents.OnPlayerUpdateStamina += HandlePlayerStaminaUpdate;
            U.Events.OnPlayerConnected += HandlePlayerConnected;
        }

        private Dictionary<CSteamID, float> playerLastTpsRequest = new Dictionary<CSteamID, float>();

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            
            {

            }
        }


        protected override void Unload()
        {
            Rocket.Core.Logging.Logger.Log("CombinedPlugin has been unloaded!");
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            foreach (SteamPlayer player in Provider.clients)
            {
                player.player.enablePluginWidgetFlag(EPluginWidgetFlags.ShowStamina);
            }

        }



        [RocketCommand("repair", "Add health to the vehicle you are in.", "", AllowedCaller.Player)]
        
        public void RepairCommand(IRocketPlayer caller, string[] args)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            if (player.CurrentVehicle != null)
            {
                InteractableVehicle vehicle = player.CurrentVehicle;
                float currentHealth = vehicle.health;

                if (currentHealth >= vehicle.asset.health)
                {
                    UnturnedChat.Say(player, Translate("VehicleRepair"));
                    return;
                }

                float healthToAdd = Configuration.Instance.HealthToAdd;
                float healthDifference = vehicle.asset.health - currentHealth;
                float healthToRepair = Math.Min(healthDifference, healthToAdd);
                ushort healthToRepairUShort = (ushort)healthToRepair;

                vehicle.askRepair(healthToRepairUShort);
                UnturnedChat.Say(player, Translate("VehicleRepair", healthToRepairUShort));
            }
            else
            {
                UnturnedChat.Say(player, Translate("NotInVehicle"));
            }
        }





        [RocketCommand("ping", "Get the ping of the specified player.", "<SteamID>", AllowedCaller.Player)]
        

        public void PingCommand(IRocketPlayer caller, string[] args)
        {
            if (args.Length == 1)
            {
                string targetIdentifier = args[0];

                if (ulong.TryParse(targetIdentifier, out ulong steamId))
                {
                    GetPing(caller, steamId);
                }
                else
                {
                    GetPingByPlayerName(caller, targetIdentifier);
                }
            }
            else
            {
                UnturnedChat.Say(caller, Translate("ping_syntax"), UnityEngine.Color.red);
            }
        }

        private void GetPing(IRocketPlayer caller, ulong steamId)
        {
            SteamPlayer steamPlayer = PlayerTool.getSteamPlayer(new CSteamID(steamId));
            DisplayPing(caller, steamPlayer);
        }

        private void GetPingByPlayerName(IRocketPlayer caller, string playerName)
        {
            UnturnedPlayer targetPlayer = UnturnedPlayer.FromName(playerName);
            if (targetPlayer != null)
            {
                SteamPlayer steamPlayer = PlayerTool.getSteamPlayer(targetPlayer.CSteamID);
                DisplayPing(caller, steamPlayer);
            }
            else
            {
                UnturnedChat.Say(caller, Translate("player_unfound"), UnityEngine.Color.red);
            }
        }

        private void DisplayPing(IRocketPlayer caller, SteamPlayer steamPlayer)
        {
            if (steamPlayer != null)
            {
                float ping = steamPlayer.ping;

                if (ping >= 0)
                {
                    int formattedPing = Mathf.FloorToInt(ping * 1000); // Convert to integer
                    string pingMessage = Translate("ping_result", steamPlayer.playerID.characterName, formattedPing);
                    UnturnedChat.Say(caller, pingMessage, UnityEngine.Color.yellow);
                }
                else
                {
                    UnturnedChat.Say(caller, Translate("ping_retrieval_failed"), UnityEngine.Color.red);
                }
            }
            else
            {
                UnturnedChat.Say(caller, Translate("player_unfound"), UnityEngine.Color.red);
            }
        }



        [RocketCommand("refuel", "Refuel the vehicle you are in.", "", AllowedCaller.Player)]
    
        public void RefuelCommand(IRocketPlayer caller, string[] args)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            if (player.CurrentVehicle != null)
            {
                InteractableVehicle vehicle = player.CurrentVehicle;
                float maxFuel = vehicle.asset.fuel;
                int fuelToAdd = Configuration.Instance.DefaultFuelValue;
                ushort fuelToAddUShort = (ushort)fuelToAdd;

                vehicle.askFillFuel(fuelToAddUShort);
                UnturnedChat.Say(player, Translate("RefueledVehicle", fuelToAdd));
            }
            else
            {
                UnturnedChat.Say(player, Translate("NotInVehicle"));
            }
        }

        [RocketCommand("tps", "Display the server TPS.", "", AllowedCaller.Player)]
        
        public void TpsCommand(IRocketPlayer caller, string[] args)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            // Check when the player last requested TPS
            if (playerLastTpsRequest.ContainsKey(player.CSteamID))
            {
                float lastRequestTime = playerLastTpsRequest[player.CSteamID];
                float currentTime = Time.realtimeSinceStartup;

                // Only display TPS if at least 10 seconds have passed since the last request
                if (currentTime - lastRequestTime < 3f)
                {
                    UnturnedChat.Say(player, Translate("TpsCooldown"), UnityEngine.Color.red);
                    return;
                }
            }

            // Store the current time as the last TPS request time for the player
            playerLastTpsRequest[player.CSteamID] = Time.realtimeSinceStartup;

            // Display TPS to the player
            float tps = Provider.debugTPS;
            int formattedTps = Mathf.FloorToInt(tps);

            string tpsMessage = $"Server TPS: {formattedTps}";
            UnturnedChat.Say(player, tpsMessage, UnityEngine.Color.yellow);
        }
        public void LoadDefaults()
        {
            Configuration.Instance.HealthToAdd = 100.0f;
            Configuration.Instance.DefaultFuelValue = 100;
        }


        private void HandlePlayerStaminaUpdate(UnturnedPlayer player, byte stamina)
        {
            if (Configuration.Instance.EnableInfiniteStamina && stamina <= 50)
            {
                player.Player.life.serverModifyStamina(100);
            }
        }

        private void HandlePlayerConnected(UnturnedPlayer player)
        {
            if (!Configuration.Instance.EnableInfiniteStamina)
            {
                player.Player.enablePluginWidgetFlag(EPluginWidgetFlags.ShowStamina);
            }
            else
            {
                player.Player.disablePluginWidgetFlag(EPluginWidgetFlags.ShowStamina);
            }
        }


            public override TranslationList DefaultTranslations => new TranslationList()
            {
                { "VehicleRepair", "Vehicle Repair: Added {0} health to the vehicle." },
                { "NotInVehicle", "Not in a Vehicle: You are not in a vehicle." },
                { "RefueledVehicle", "Vehicle Refuel: Added {0} fuel to the vehicle." },
                { "TpsCooldown", "Please wait a moment before executing another TPS command." },
                { "ping_syntax", "Correct Syntax: /ping [SteamId] [Name]." },
                { "ping_result", "Ping of player {0}: {1} ms" },
                { "player_unfound", "Could not find specified player." },
                { "ping_retrieval_failed", "Could not pull specified players ping!" },
            
            };
    }
}
