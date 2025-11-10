using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Lod;
using Lod.ChartEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utf8Json.Internal.DoubleConversion;

// ReSharper disable InconsistentNaming

namespace Tetote4K;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    private const int DEFAULT_HEIGHT = 1920;
    private const int DEFAULT_WIDTH = 1080;
    new static ManualLogSource Logger;

    static ConfigEntry<bool> ConfigEnable;
    static ConfigEntry<bool> ConfigIngameLowResDisable;
    static ConfigEntry<string> ConfigIngameShader;

    private static float ScreenScaleH = 1;
    private static float ScreenScaleW = 1;

    private static bool HasLoaded = false;

    private void Awake() {
        Logger = base.Logger;

        ConfigEnable = Config.Bind("General", "Enable", true, "Enables resolution adjustments (so stuff registers correctly on resolutions that are not 1080p)");
        ConfigIngameLowResDisable = Config.Bind("General", "Adjust Gameplay Resolution", true, "By default the game scales down rendering during gameplay. Enable this to apply the real resolution to gameplay. Requires to change the shader specified in the \"Shader\" setting.");
        ConfigIngameShader = Config.Bind("General", "Shader", "LOD_BG/InGameRenderShader", "Name of the shader to use ingame if \"Adjust Gameplay Resolution\" is set.");
        
        SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;

        Harmony.CreateAndPatchAll(typeof(Patches));
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private static void SceneManagerOnSceneLoaded(Scene arg0, LoadSceneMode arg1) {
        Logger.LogDebug("OnLoaded: " + arg0.name);
        if (arg0.name == "Attract") {
            HasLoaded = true;
        }
        
        if (!ConfigEnable.Value || !ConfigIngameLowResDisable.Value || arg0.name != "stg001_test" || !HasLoaded) {
            return;
        }

        GameObject go = GameObject.Find("InGameRenderCamera/BG");
        if (go == null) {
            Logger.LogError("Failed to find InGameRenderCamera/BG");
            return;
        }

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null) {
            Logger.LogError("Failed to find MeshRenderer");
            return;
        }

        if (mr.sharedMaterial == null) {
            Logger.LogError("Shared material is null!");
            return;
        }

        if (ConfigIngameShader.Value != "") {
            Shader target = Shader.Find(ConfigIngameShader.Value);
            if (target == null) {
                Logger.LogError("Target Shader not found! " + ConfigIngameShader.Value);
                return;
            }

            mr.sharedMaterial.shader = target;
        } else {
            mr.sharedMaterial.shader = null;
        }

        Logger.LogInfo("Successfully changed low resolution shader to " + ConfigIngameShader.Value);
    }

    public class Patches {
        
        // Set up note judgement values
        [HarmonyPostfix, HarmonyPatch(typeof(GameInstance), "OnSetupFinished")]
        static void OnSetupFinished() {
            if (ConfigEnable.Value) {
                Logger.LogInfo("Running resolution adjustment...");
                Logger.LogDebug("Game expected width: " + DEFAULT_WIDTH);
                Logger.LogDebug("Game current width: " + Screen.width);
                ScreenScaleW = (float)Screen.width / DEFAULT_WIDTH;
                Logger.LogInfo("Scale (w): " + ScreenScaleW);
                Logger.LogDebug("Game expected height: " + DEFAULT_HEIGHT);
                Logger.LogDebug("Game current height: " + Screen.height);
                ScreenScaleH = (float)Screen.height / DEFAULT_HEIGHT;
                Logger.LogInfo("Scale (h): " + ScreenScaleH);
                if (Math.Abs(1 - ScreenScaleH) > 0.01) {
                    AdjustJudgementScale(ScreenScaleH);
                } else {
                    Logger.LogInfo("Scale is correct (or similar) - no adjustments");
                }
            }
        }

        private static void AdjustJudgementScale(float scale) {
            foreach (DifficultySettingInfos setting in ConfigMaster.GetInstance().DifficultySettingInfos.Values) {
                setting.hitRadius0 = (int)(setting.hitRadius0 * scale);
                setting.hitRadius1 = (int)(setting.hitRadius1 * scale);
                setting.hitRadius2 = (int)(setting.hitRadius2 * scale);
                setting.hitRadius3 = (int)(setting.hitRadius3 * scale);
                setting.hitRadius4 = (int)(setting.hitRadius4 * scale);
                setting.hitRadius5 = (int)(setting.hitRadius5 * scale);
                setting.hitRadius6 = (int)(setting.hitRadius6 * scale);
            }
        }

        // this actually seems unused
        [HarmonyPostfix, HarmonyPatch(typeof(LowResolutionCamera), "GetResolutionSize")]
        static void GetResolutionSize(LowResolutionCamera.Resolution resolution, ref Vector2Int __result) {
            if (!ConfigEnable.Value || !ConfigIngameLowResDisable.Value) {
                return;
            }

            __result = new Vector2Int(Screen.width, Screen.height);
            Logger.LogInfo("Adjusting camera size to " + __result);
        }

        // Can anyone tell me who is more proficient in Unity why THIS screen in particular does weird things? Apparently the hand X position is in the negatives??
        [HarmonyPostfix, HarmonyPatch(typeof(LoginBonusController), "Update")]
        static void Update(LoginBonusController __instance) {
            if (!ConfigEnable.Value) {
                return;
            }
            
            if (__instance._TouchLeftHandSide != null) {
                Vector3 position = __instance._TouchLeftHandSide.transform.localPosition;
                position.x = -165 * (ScreenScaleW+ScreenScaleH);
                __instance._TouchLeftHandSide.transform.localPosition = position;
            }

            if (__instance._TouchRightHandSide != null) {
                Vector3 position2 = __instance._TouchRightHandSide.transform.localPosition;
                position2.x = 165 * (ScreenScaleW+ScreenScaleH);
                __instance._TouchRightHandSide.transform.localPosition = position2;
            }

        }

        #region Patched system message functions

        [HarmonyPrefix, HarmonyPatch(typeof(UISystemMessage), "ScrollStart")]
        static bool ScrollStart(bool isAdd, UISystemMessage __instance) {
            if (!ConfigEnable.Value) {
                return true;
            }

            __instance.isFirst = isAdd;
            if (!__instance.IsFixedToBottom) {
                __instance.messagePos = new Vector3(0f, __instance.isFirst ? Screen.height / 2F : UISystemMessage.bottom, 0f);
            } else {
                __instance.messagePos = new Vector3(0f, UISystemMessage.bottom, 0f);
            }

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UISystemMessage), "Update")]
        static bool Update(UISystemMessage __instance) {
            if (!ConfigEnable.Value) {
                return true;
            }

            if (GameInstance.Instance.LanguageManager != null && GameInstance.Instance.LanguageManager.CurrentLanguage != __instance.currentLanguage) {
                __instance.currentLanguage = GameInstance.Instance.LanguageManager.CurrentLanguage;
                __instance.isReady = false;
                __instance.messageActiveDirty = true;
            }

            if (!__instance.isReady && GameInstance.Instance.MasterManager.TextMaster != null) {
                for (int i = 0; i < __instance.textIDs.Length; i++) {
                    __instance.texts[i] = GameInstance.Instance.MasterManager.TextMaster.GetTextById(__instance.textIDs[i]);
                }

                __instance.isReady = true;
            }

            if (__instance.isReady && __instance.isActive) {
                __instance.UpdateMessageText();
                if (__instance.isScroll) {
                    __instance.messagePos.x -= Time.deltaTime * UISystemMessage.speed;
                    if (__instance.messagePos.x < -(Screen.width + __instance.message.preferredWidth)) {
                        __instance.ScrollStart();
                    }
                } else {
                    __instance.messagePos.x = -Screen.width + (Screen.width - __instance.message.preferredWidth) * 0.5f;
                }

                __instance.message.transform.localPosition = __instance.messagePos;
            }

            return false;
        }

        #endregion
    }
}