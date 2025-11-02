using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Lod;
using Lod.TypeX4;

namespace TetoteCoreFixes {
    
    [BepInPlugin("eu.haruka.gmg.cf.teto", "Tetote Core Fixes", "1.4")]
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
        public static ConfigEntry<int> ConfigEnableLoginBonus;
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
        public static ConfigEntry<bool> ConfigDisablePartnerRandomization;
        public static ConfigEntry<int> ConfigPartnerMaxHeight;
        public static ConfigEntry<int> ConfigScreenPositionAdjust;
        public static ConfigEntry<bool> ConfigNoteTimeScaleAdjust;
        public static ConfigEntry<LanguageManager.Language> ConfigErrorLanguage;

        public static ManualLogSource Log;

        public void Awake() {

            Log = Logger;

            ConfigPreventAutoScreenRotate = Config.Bind("General", "Prevent Screen Rotation", true, "Disables automatic screen rotation");
            ConfigEndlessDates = Config.Bind("General", "Disable End Dates", true, "Disable end dates of various things such as songs or the shop");
            ConfigEnableLoginBonus = Config.Bind("General", "Enable Login Bonus", -1, new ConfigDescription("Enables the login bonus schedule with the given ID. -1 to disable.", new AcceptableValueRange<int>(-1, 10)));
            ConfigSomePrices = Config.Bind("General", "Add prices to the shop", true, "Adds some random prices to the shop items. Better than having them all being zero I guess?");
            ConfigInformationBugfix = Config.Bind("General", "Bugfix for inaccessible information menu", true, "Fixes game deleting information data for some stupid reason");
            ConfigSafeFileDirectory = Config.Bind("General", "NVRAM Path", "nvram", "Sets the path to where backup data is written to. If this is empty, the original (encrypted) storage will be used.");
            
            ConfigDefaultLanguage = Config.Bind("Mods", "Default Language", LanguageManager.Language.English, "Sets the default game language to be displayed after boot and test menu.");
            ConfigErrorLanguage = Config.Bind("Mods", "Error Language", LanguageManager.Language.English, "Sets the language of the error screen to the specified value.");
            ConfigDisablePartnerRandomization = Config.Bind("Mods", "Disable Partner Randomization", false, "Disables the partner randomization on first play.");
            
            ConfigPartnerMaxHeight = Config.Bind("Partner Height Mods", "Override Maximum Height (!)", 175, new ConfigDescription("Increase the maximum height from 175. Visual only, does not change notes.\n\n(!) CAUTION: In tests, going above 190 will make notes appear outside the screen!\nIf this is changed, make sure \"Adjust Note Circle Scale\" is also enabled!", new AcceptableValueRange<int>(175, 200)));
            ConfigNoteTimeScaleAdjust = Config.Bind("Partner Height Mods", "Adjust Note Circle Scale", false, new ConfigDescription("If the maximum height is set higher than 175, this will also cause note circles to no longer adjust for excessive height."));
            ConfigScreenPositionAdjust = Config.Bind("Partner Height Mods", "Bottom Screen Position Adjustment (!)", 0, new ConfigDescription("If the bottom edge of the touchscreen is not at ~61cm, modify the screen position with this setting, so the ingame height selector matches the real height.\n\n(!) Going above +15 will make notes appear outside the screen!", new AcceptableValueRange<int>(-50, 50)));
            
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