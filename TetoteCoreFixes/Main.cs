using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Lod;
using Lod.TypeX4;

namespace TetoteCoreFixes {
    
    [BepInPlugin("eu.haruka.gmg.cf.teto", "Tetote Core Fixes", "1.2")]
    public class Main : BaseUnityPlugin {

        public enum TouchSetting {
            Original,
            AnyTouchscreen,
            Disabled
        }

        public enum CameraSetting {
            Default,
            ForceAeroTap,
            ForceIntelRealSense,
            DisableCamera
        }

        public static ConfigEntry<bool> ConfigSkipAeroBootCheck;
        public static ConfigEntry<bool> ConfigPreventAutoScreenRotate;
        public static ConfigEntry<CameraSetting> ConfigCamera;
        public static ConfigEntry<bool> ConfigEndlessDates;
        public static ConfigEntry<bool> ConfigSomePrices;
        public static ConfigEntry<bool> ConfigInformationBugfix;
        public static ConfigEntry<TouchSetting> ConfigTouchCheck;
        public static ConfigEntry<string> ConfigSafeFileDirectory;
        public static ConfigEntry<KeyboardShortcut> ConfigServiceKey;
        public static ConfigEntry<KeyboardShortcut> ConfigTestKey;
        public static ConfigEntry<KeyboardShortcut> ConfigCoinKey;
        public static ConfigEntry<KeyboardShortcut> ConfigEnterKey;
        public static ConfigEntry<KeyboardShortcut> ConfigSelectKey;
        public static ConfigEntry<LanguageManager.Language> ConfigDefaultLanguage;

        public static ManualLogSource Log;

        public void Awake() {

            Log = Logger;

            ConfigPreventAutoScreenRotate = Config.Bind("General", "Prevent Screen Rotation", true, "Disables automatic screen rotation");
            ConfigEndlessDates = Config.Bind("General", "Disable End Dates", true, "Disable end dates of various things such as songs or the shop");
            ConfigSomePrices = Config.Bind("General", "Add prices to the shop", true, "Adds some random prices to the shop items. Better than having them all being zero I guess?");
            ConfigInformationBugfix = Config.Bind("General", "Bugfix for inaccessible information menu", true, "Fixes game deleting information data for some stupid reason");
            ConfigSafeFileDirectory = Config.Bind("General", "NVRAM Path", "nvram", "Sets the path to where backup data is written to. If this is empty, the original (encrypted) storage will be used.");
            ConfigDefaultLanguage = Config.Bind("General", "Default Language", LanguageManager.Language.English, "Sets the default game language to be displayed after boot and test menu.");
            
            ConfigTouchCheck = Config.Bind("Input", "Touchscreen Check Mode", TouchSetting.AnyTouchscreen, "Changes how the game checks for the touchscreen\n\n* Original: Checks for the specific touchscreen that comes with the cabinet.\n* AnyTouchscreen: Checks if any touchscreen is connected\n* Disabled: The check will always be OK");
            ConfigSkipAeroBootCheck = Config.Bind("Input", "Skip Aero Boot Check", true, "Disables launching aeroBootCheckWnd.exe");
            ConfigCamera = Config.Bind("Input", "Camera Mode", CameraSetting.DisableCamera, "Changes what camera the game uses.\n\n* Default: Default game logic\n*ForceAeroTap: Forces use of aeroTap\n* ForceIntelRealSense: Forces use of Intel RealSense\n* DisableCamera: No camera is used and the check is bypassed");
            ConfigServiceKey = Config.Bind("Input", "Extra Service Button", KeyboardShortcut.Empty, "Adds an extra keyboard/controller button to trigger the service button. (ttio input will still work)");
            ConfigTestKey = Config.Bind("Input", "Extra Test Button", KeyboardShortcut.Empty, "Adds an extra keyboard/controller button to trigger the test button. (ttio input will still work)");
            ConfigCoinKey = Config.Bind("Input", "Extra Coin Button", KeyboardShortcut.Empty, "Adds an extra keyboard/controller button to trigger the test button. (ttio input will still work)");
            ConfigEnterKey = Config.Bind("Input", "Extra Enter Button", KeyboardShortcut.Empty, "Adds an extra keyboard/controller button to trigger the test button. (ttio input will still work)");
            ConfigSelectKey = Config.Bind("Input", "Extra Select Button", KeyboardShortcut.Empty, "Adds an extra keyboard/controller button to trigger the test button. (ttio input will still work)");
            
            Harmony.CreateAndPatchAll(typeof(Patches), "eu.haruka.gmg.cf.teto");
        }

        public void Update() {
        }
    }
}