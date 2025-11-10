extern alias CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CoreModule::UnityEngine;
using CoreModule::UnityEngine.SceneManagement;
using HarmonyLib;
using Lod;
using Lod.TypeX4;
using Utf8Json;
using Object = CoreModule::UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace TetoteCustomMasterData;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger;
    public static Dictionary<string, MarkerChartDataAsset> RedirectedCharts = new Dictionary<string, MarkerChartDataAsset>();

    public static ConfigEntry<bool> ConfigEnable;
    public static ConfigEntry<bool> ConfigPreventServerOverride;
    public static ConfigEntry<string> ConfigMasterDirectory;

    public void Awake() {
        Logger = base.Logger;

        ConfigEnable = Config.Bind("General", "Enable", true, "Enables loading custom master data files");
        ConfigPreventServerOverride = Config.Bind("General", "Prevent Server Override", true, "Prevents the game server from overriding custom master data files");
        ConfigMasterDirectory = Config.Bind("General", "Data Directory", "masterdata", "Specify a directory or full path where custom master data is located");

        if (!Directory.Exists(ConfigMasterDirectory.Value)) {
            Directory.CreateDirectory(ConfigMasterDirectory.Value);
        }

        Harmony.CreateAndPatchAll(typeof(Patches));
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    public class Patches {
        
        // Disable server data
        [HarmonyPrefix, HarmonyPatch(typeof(MasterManager), "UseServerMasterUpdate", MethodType.Getter)]
        private static bool UseServerMasterUpdate(ref bool __result) {
            if (ConfigPreventServerOverride.Value) {
                __result = false;
                return false;
            }

            return true;
        }

        // Load custom data
        [HarmonyPrefix, HarmonyPatch(typeof(GameInstance), "OnSetupFinished")]
        private static void OnSetupFinished() {
            if (ConfigEnable.Value) {
                Logger.LogInfo("Loading custom master data files...");

                foreach (string file in Directory.EnumerateFiles(ConfigMasterDirectory.Value)) {
                    Logger.LogInfo("Loading: " + file);
                    string fn = Path.GetFileName(file);

                    switch (fn) {
                        case "ChartInfos.json":
                            SetChartInfos(file);
                            break;
                        case "StageInfos.json":
                            SetStageInfos(file);
                            break;
                        case "CharacterInfos.json":
                            SetCharacterInfos(file);
                            break;
                        case "TextTable.json":
                            SetTextTable(file);
                            break;
                        default:
                            Logger.LogWarning("Unknown file type: " + fn);
                            break;
                    }
                }
            }
        }

        private static void SetChartInfos(string file) {
            StageDefinitionMaster.GetInstance().ChartInfos = TableItemBase.ToDictionary(JsonSerializer.Deserialize<ChartInfo[]>(File.ReadAllText(file)));
            Logger.LogDebug("Loaded " + StageDefinitionMaster.GetInstance().ChartInfos.Count + " record(s)");
        }

        private static void SetStageInfos(string file) {
            StageDefinitionMaster.GetInstance().StageInfos = TableItemBase.ToDictionary(JsonSerializer.Deserialize<StageInfo[]>(File.ReadAllText(file)));
            Logger.LogDebug("Loaded " + StageDefinitionMaster.GetInstance().StageInfos.Count + " record(s)");
        }

        private static void SetCharacterInfos(string file) {
            CharacterMaster cm = CharacterMaster.GetInstance();

            CharacterInfo[] table = JsonSerializer.Deserialize<CharacterInfo[]>(File.ReadAllText(file));
            cm.CharacterInfos = TableItemBase.ToDictionary(table);
            cm.SortedAllCharacterInfos = TableItemBase.ToReadOnlyList(table.OrderBy(info => info.sortIndex).ToArray());
            cm.SortedRegularCharacterInfos = cm.SortedAllCharacterInfos.Where(info => info.IsRegular).ToArray();

            Logger.LogDebug("Loaded " + cm.CharacterInfos.Count + " record(s)");
        }

        private static void SetTextTable(string file) {
            TextMaster.GetInstance().TextInfos = TableItemBase.ToDictionary(JsonSerializer.Deserialize<TextInfo[]>(File.ReadAllText(file)));
            Logger.LogDebug("Loaded " + TextMaster.GetInstance().TextInfos.Count + " record(s)");
        }

        private static void ReadChart(string file) {
            Logger.LogDebug("Loading custom chart: " + file);
            MarkerAssetWorkaround asset2 = JsonSerializer.Deserialize<MarkerAssetWorkaround>(File.ReadAllText(file));

            // unity shit workaround
            MarkerChartDataAsset asset = (MarkerChartDataAsset)ScriptableObject.CreateInstance(typeof(MarkerChartDataAsset));
            asset.charts = new MarkerChartData[asset2.charts.Length];
            for (int i = 0; i < asset2.charts.Length; i++) {
                MarkerChartData cd = new MarkerChartData();
                MarkerChartData2 cd2 = asset2.charts[i];
                cd.begin = cd2.begin;
                cd.type = cd2.type;
                cd.groupList = cd2.groupList;
                cd.m_Markers = cd2.m_Markers;
                asset.charts[i] = cd;
            }

            asset.eventData = new EventChartData {
                m_Events = asset2.eventData.m_Events,
                begin = asset2.eventData.begin,
                type = asset2.eventData.type,
                m_ReadyAnimationCode = asset2.eventData.m_ReadyAnimationCode,
                m_TempoMapCode = asset2.eventData.m_TempoMapCode
            };
            asset.markerNum = asset2.markerNum;
            asset.synchroScoreBase = asset2.synchroScoreBase;

            RedirectedCharts.Add(Path.GetFileNameWithoutExtension(file), asset);
        }

        // Make loading screen load custom charts
        [HarmonyPostfix, HarmonyPatch(typeof(PreparationController), "PreSetup")]
        private static IEnumerator PreSetup(IEnumerator __result, PreparationController __instance) {
            string chartDir = Path.Combine(ConfigMasterDirectory.Value, "charts");
            if (!Directory.Exists(chartDir)) {
                Directory.CreateDirectory(chartDir);
            }

            string[] customCharts = Directory.EnumerateFiles(chartDir).ToArray();

            IReadOnlyDictionary<string, CostumeInfo> costumeInfos = CharacterMaster.GetInstance().CostumeInfos;

            __instance._allNumText.text = (customCharts.Length + costumeInfos.Count + __instance._sceneNames.Length).ToString();
            __instance._loadedNumText.text = "0";

            Logger.LogInfo("Loading custom chart files...");
            foreach (string fn in customCharts) {
                if (fn.EndsWith(".json")) {
                    try {
                        ReadChart(fn);
                    } catch (Exception ex) {
                        Logger.LogError(ex);
                        GameInstance.Instance.ErrorManager.SetError(ERROR_CODE.FILE_HDD_ERROR);
                    }

                    yield return fn;
                } else {
                    Logger.LogWarning("Unknown file type: " + fn);
                }

                __instance.IncrementLoadedNum();
            }

            foreach (KeyValuePair<string, CostumeInfo> keyValuePair in costumeInfos) {
                yield return __instance.PrepareCharacter(keyValuePair.Value.characterId, keyValuePair.Value.id);
                __instance.IncrementLoadedNum();
            }

            foreach (string text in __instance._sceneNames) {
                yield return __instance.PrepareScene(text);
                __instance.IncrementLoadedNum();
            }

            __instance.DidFinishPreparing = true;
        }
        

        // Replace chart when loaded
        [HarmonyPrefix, HarmonyPatch(typeof(StagePlayer), "InitializeChartPlayData")]
        private static void InitializeChartPlayData(ref MarkerChartDataAsset chartAsset, StagePlayer __instance) {
            string file = __instance.m_StageDef.chartDataName;
            if (RedirectedCharts.TryGetValue(file, out MarkerChartDataAsset chart)) {
                __instance.m_MainChart = chartAsset = chart;
                Logger.LogMessage("Using overridden chart: " + file);
            }
        }
    }
}