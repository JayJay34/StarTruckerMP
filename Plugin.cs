using BepInEx.Unity.IL2CPP;
using BepInEx;
using Object = UnityEngine.Object;
using HarmonyLib;
using System;
using BepInEx.Logging;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP.UnityEngine;

namespace StarTruckMP;

[BepInPlugin(pluginGuid, pluginName, pluginVersion)]
public class StarTruckMP : BasePlugin
{
    public const string pluginGuid = "StarTruckMP";
    public const string pluginName = "Star Trucker MP";
    public const string pluginVersion = "0.1";
    internal static new ManualLogSource Log;
    public static ConfigEntry<string> IPAddress;
    public static ConfigEntry<int> MoveUpdate;
    public static ConfigEntry<UnityEngine.KeyCode> joinKey;
    public static ConfigEntry<UnityEngine.KeyCode> hostKey;


    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"Plugin {pluginGuid} is loaded!");
        IPAddress = Config.Bind("Server Info", "ServerIP", "127.0.0.1:7777", "IP Address to Join");
        MoveUpdate = Config.Bind("Server Info", "MovementUpdate", 100, "Movement update frequencey in ms");
        joinKey = Config.Bind("Keybinds", "JoinKey", UnityEngine.KeyCode.LeftBracket, "Set the Key to press for joining the listed IP");
        hostKey = Config.Bind("Keybinds", "HostKey", UnityEngine.KeyCode.RightBracket, "Set the Key to press for hosting a server");
        Harmony.CreateAndPatchAll(typeof(TruckClient));
    }

    [HarmonyPatch]
    public class TruckClient
    {
        [HarmonyPatch(typeof(PauseController), nameof(Update), new Type[] { })]
        [HarmonyPostfix]
        public static void Update()
        {
            StarTruckServer.StarTruckServer.Update();
            StarTruckClient.StarTruckClient.Update();
            StarTruckServer.StarTruckServer.FixedUpdate();
            StarTruckClient.StarTruckClient.FixedUpdate();
        }

        [HarmonyPatch(typeof(CustomizationState), nameof(CustomizationState.EquipLivery))]
        [HarmonyPostfix]
        public static void EquipLivery(string itemId)
        {
            StarTruckClient.StarTruckClient.equipLivery(itemId);
        }

        [HarmonyPatch(typeof(SectorPersistence), nameof(SectorPersistence.OnArrivedAtSector))]
        [HarmonyPostfix]
        public static void OnArrivedAtSector(Object sender, EventArgs eventArgs)
        {
            StarTruckClient.StarTruckClient.OnArrivedAtSector();
        }
    }
}
