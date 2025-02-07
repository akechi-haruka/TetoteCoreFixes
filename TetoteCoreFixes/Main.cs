using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace TetoteCoreFixes {
    
    [BepInPlugin("eu.haruka.gmg.cf.teto", "Tetote Core Fixes", "1.0")]
    public class Main : BaseUnityPlugin {

        public static ConfigEntry<bool> ConfigSkipAeroBootCheck;
        public static ConfigEntry<bool> ConfigPreventAutoScreenRotate;
        public static ConfigEntry<bool> ConfigCameraDummy;
        public static ConfigEntry<bool> ConfigEndlessDates;
        public static ConfigEntry<bool> ConfigSomePrices;

        public static ManualLogSource Log;

        public void Awake() {

            Log = Logger;

            ConfigSkipAeroBootCheck = Config.Bind("General", "Skip Aero Boot Check", true, "Disables launching aeroBootCheckWnd.exe");
            ConfigPreventAutoScreenRotate = Config.Bind("General", "Prevent Screen Rotation", true, "Disables automatic screen rotation");
            ConfigCameraDummy = Config.Bind("General", "Camera Emulation", true, "Disable the camera requirement");
            ConfigEndlessDates = Config.Bind("General", "Disable End Dates", true, "Disable end dates of various things such as songs or the shop");
            ConfigSomePrices = Config.Bind("General", "Add prices to the shop", true, "Adds some random prices to the shop items. Better than having them all being zero I guess?");
            
            Harmony.CreateAndPatchAll(typeof(Patches), "eu.haruka.gmg.cf.teto");
        }

        public void Update() {
            
        }
    }
}