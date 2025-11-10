using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

// ReSharper disable InconsistentNaming

namespace Retote;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("TetoteCustomMasterData")]
public class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger;

    public static ConfigEntry<bool> ConfigEnable;

    private void Awake() {
        Logger = base.Logger;

        ConfigEnable = Config.Bind("General", "Enable", true, "Enables ReToTe");

        Harmony.CreateAndPatchAll(typeof(GameplayRestorationPatches));
        //Harmony.CreateAndPatchAll(typeof(ResultRestorationPatches));
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
}