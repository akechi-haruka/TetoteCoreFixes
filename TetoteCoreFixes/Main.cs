using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Lod;
using Lod.TypeX4;

namespace TetoteCoreFixes {
    
    [BepInPlugin("eu.haruka.gmg.cf.teto", "Tetote Core Fixes", VERSION)]
    public class Main : BaseUnityPlugin {

        public const String VERSION = "1.6";

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
        public static ConfigEntry<bool> ConfigPreventCardReadOnAttract;
        public static ConfigEntry<int> ConfigTitleScreenTimer;
        public static ConfigEntry<bool> ConfigDemonstrationNonNew;
        public static ConfigEntry<bool> ConfigNumberFormatFix;
        public static ConfigEntry<bool> ConfigSkipPreloading;
        public static ConfigEntry<bool> ConfigForceRandomAttractPartners;
        public static ConfigEntry<bool> ConfigAlwaysShowMultiSelection;
        public static ConfigEntry<bool> ConfigMirrorFree;
        public static ConfigEntry<bool> ConfigSortByNewIsSortByScore;

        public static ManualLogSource Log;

        public void Awake() {

            Log = Logger;

            ConfigPreventAutoScreenRotate = Config.Bind("General", "Prevent Screen Rotation", true, "Disables automatic screen rotation");
            ConfigSafeFileDirectory = Config.Bind("General", "NVRAM Path", "nvram", "Sets the path to where backup data is written to. If this is empty, the original (encrypted) storage will be used.");
            ConfigNumberFormatFix = Config.Bind("General", "Force Japanese Number Format", true, "This fixes the numbers score display overlapping");
            
            
            ConfigEndlessDates = Config.Bind("EOL Fixes", "Disable End Dates", true, "Disable end dates of various things such as songs or the shop");
            ConfigDemonstrationNonNew = Config.Bind("EOL Fixes", "Fix Demonstration Play", true, "Allows any song for attract demonstration play, and not just \"new\" songs. There are no more new songs.");
            ConfigInformationBugfix = Config.Bind("EOL Fixes", "Bugfix for inaccessible information menu", true, "Fixes game deleting information data for some reason");
            ConfigSomePrices = Config.Bind("EOL Fixes", "Add prices to the shop", true, "Adds some random prices to the shop items. Better than having them all being zero I guess?");
            ConfigEnableLoginBonus = Config.Bind("EOL Fixes", "Enable Login Bonus", -1, new ConfigDescription("Enables the login bonus schedule with the given ID. -1 to disable.", new AcceptableValueRange<int>(-1, 10)));
            
            ConfigDefaultLanguage = Config.Bind("Mods", "Default Language", LanguageManager.Language.English, "Sets the default game language to be displayed after boot and test menu.");
            ConfigErrorLanguage = Config.Bind("Mods", "Error Language", LanguageManager.Language.English, "Sets the language of the error screen to the specified value.");
            ConfigDisablePartnerRandomization = Config.Bind("Mods", "Disable Partner Randomization", false, "Disables the partner randomization on first play.");
            ConfigTitleScreenTimer = Config.Bind("Mods", "Title Screen Timer", 10, new ConfigDescription("Sets the time limit until the title screen will move to demonstration on attract mode.", new AcceptableValueRange<int>(10, 999)));
            ConfigForceRandomAttractPartners = Config.Bind("Mods", "Force Random Attract Partner", true, "Always uses a random partner for attract/demonstration, and not the collaboration character if the server sets one.");
            ConfigSkipPreloading = Config.Bind("Mods", "Skip Preloading", false, "Skip preloading costumes. This will cause a severe delay when loading characters and costumes");
            ConfigPreventCardReadOnAttract = Config.Bind("Mods", "Disable Attract Mode Card Reading", false, "Disables the card reader on attract (but not on login)");
            ConfigAlwaysShowMultiSelection = Config.Bind("Mods", "Always show multiplay selector", false, "Always shows the multiplayer selection window, even if only 1 cab is currently detected");
            ConfigMirrorFree = Config.Bind("Mods", "Make Mirror Mode Free", false, "Makes mirror mode no longer cost coins");
            ConfigSortByNewIsSortByScore = Config.Bind("Mods", "Replace New Sort with Score Sort", true, "This replaces the \"New\" option in the Sort menu to sort by best score instead.");
            
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