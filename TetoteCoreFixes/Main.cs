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

        public static ManualLogSource Log;

        public void Awake() {

            Log = Logger;

            ConfigSkipAeroBootCheck = Config.Bind("General", "Skip Aero Boot Check", true, "Disables launching aeroBootCheckWnd.exe");
            ConfigPreventAutoScreenRotate = Config.Bind("General", "Prevent Screen Rotation", true, "Disables automatic screen rotation");
            ConfigCameraDummy = Config.Bind("General", "Camera Emulation", true, "Disable the camera requirement");
            ConfigEndlessDates = Config.Bind("General", "Disable End Dates", true, "Disable end dates of various things such as songs or events");
            
            Harmony.CreateAndPatchAll(typeof(Patches), "eu.haruka.gmg.cf.teto");
        }

        public void Update() {
            
        }
    }
}