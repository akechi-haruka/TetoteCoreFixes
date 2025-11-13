using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HarmonyLib;
using Lod;
using Lod.ImageRecognition;
using Lod.TestMode;
using Lod.TypeX4;
using UnityEngine;
using Random = System.Random;

// ReSharper disable InconsistentNaming

namespace TetoteCoreFixes {
    public class Patches {
        [HarmonyPrefix, HarmonyPatch(typeof(AeroBootCheck), "CheckAndReset")]
        static bool CheckAndReset(ref bool __result, bool outPutLog) {
            if (Main.ConfigSkipAeroBootCheck.Value) {
                __result = true;
                return false;
            }

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ScreenObserver), "Update")]
        static bool Rotate() {
            if (Main.ConfigPreventAutoScreenRotate.Value) {
                return false;
            }

            return true;
        }

        // aeroTap
        [HarmonyPrefix, HarmonyPatch(typeof(DepthCam2), "hasCameraProblems", MethodType.Getter)]
        public static bool hasCameraProblems(ref bool __result) {
            if (Main.ConfigCamera.Value == Main.CameraSetting.DisableCamera || Main.ConfigCamera.Value == Main.CameraSetting.ForceAeroTap) {
                __result = false;
            } else {
                return true;
            }

            return false;
        }

        // Intel
        [HarmonyPrefix, HarmonyPatch(typeof(DepthCamera), "hasCameraProblems", MethodType.Getter)]
        public static bool hasCameraProblems2(ref bool __result) {
            if (Main.ConfigCamera.Value == Main.CameraSetting.DisableCamera || Main.ConfigCamera.Value == Main.CameraSetting.ForceIntelRealSense) {
                __result = false;
            } else {
                return true;
            }

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DepthCamService), "current", MethodType.Getter)]
        public static void get_current(DepthCamService __instance, ref IDepthCam __result) {
            switch (Main.ConfigCamera.Value) {
                case Main.CameraSetting.ForceAeroTap:
                    __result = __instance.m_aeroTAP;
                    break;
                case Main.CameraSetting.ForceIntelRealSense:
                    __result = __instance.m_RealSense;
                    break;
                case Main.CameraSetting.DisableCamera:
                case Main.CameraSetting.Default:
                default:
                    // nop
                    break;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(DeviceCheck), "IsTouchPanelEnable")]
        public static bool IsTouchPanelEnable(ref bool __result) {
            switch (Main.ConfigTouchCheck.Value) {
                case Main.TouchSetting.Original:
                    return true;
                case Main.TouchSetting.AnyTouchscreen:
                    __result = Native.IsTouchEnabled();
                    return false;
                case Main.TouchSetting.Disabled:
                default:
                    __result = true;
                    return false;
            }
        }


        private static GWSafeFile.FILE_ERROR lastError = GWSafeFile.FILE_ERROR.ERROR_NONE;

        [HarmonyPrefix, HarmonyPatch(typeof(GWSafeFile), "WriteStr")]
        public static bool WriteStr(string _targetStr, string _baseName, int _MaxLog, bool _bUseCommit = false) {
            string nvram = Main.ConfigSafeFileDirectory.Value;
            if (!String.IsNullOrWhiteSpace(nvram)) {
                if (!Directory.Exists(nvram)) {
                    Main.Log.LogInfo("Created nvram directory");
                    try {
                        Directory.CreateDirectory(nvram);
                    } catch {
                        lastError = GWSafeFile.FILE_ERROR.ERROR_CREATE_DIRECTORY;
                        return false;
                    }
                }

                try {
                    File.WriteAllText(nvram + Path.DirectorySeparatorChar + _baseName, _targetStr);
                    lastError = GWSafeFile.FILE_ERROR.ERROR_NONE;
                } catch (Exception ex) {
                    lastError = Native.IsDiskFull(ex) ? GWSafeFile.FILE_ERROR.ERROR_TOO_SMALL_FREE_SPACE : GWSafeFile.FILE_ERROR.ERROR_ROLL_BACK;
                }

                return false;
            } else {
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GWSafeFile), "ReadStr")]
        public static bool ReadStr(ref string __result, string _baseName) {
            string nvram = Main.ConfigSafeFileDirectory.Value;
            if (!String.IsNullOrWhiteSpace(nvram)) {
                string file = nvram + Path.DirectorySeparatorChar + _baseName;

                try {
                    if (File.Exists(file)) {
                        __result = File.ReadAllText(file);
                        lastError = GWSafeFile.FILE_ERROR.ERROR_NONE;
                    } else {
                        __result = "";
                        lastError = GWSafeFile.FILE_ERROR.ERROR_NO_FILE;
                    }
                } catch {
                    lastError = GWSafeFile.FILE_ERROR.ERROR_ROLL_BACK;
                }

                return false;
            } else {
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GWSafeFile), "GetError")]
        public static bool GetError(ref GWSafeFile.FILE_ERROR __result) {
            string nvram = Main.ConfigSafeFileDirectory.Value;
            if (!String.IsNullOrWhiteSpace(nvram)) {
                __result = lastError;
                return false;
            } else {
                return true;
            }
        }

        private static readonly DateTimeOffset FAR_FUTURE = new DateTimeOffset(2090, 1, 1, 1, 1, 1, TimeSpan.Zero);

        [HarmonyPrefix]
        //[HarmonyPatch(typeof(LoginBonusScheduleInfo), "EndDate", MethodType.Getter)]
        [HarmonyPatch(typeof(MissionInfo), "EndDate", MethodType.Getter)]
        //[HarmonyPatch(typeof(EventInfo), "EndDate", MethodType.Getter)]
        [HarmonyPatch(typeof(MissionBase), "DisplayEndDateTime", MethodType.Getter)]
        [HarmonyPatch(typeof(StageInfo), "EndDate", MethodType.Getter)]
        [HarmonyPatch(typeof(StageLocationInfo), "EndDate", MethodType.Getter)]
        //[HarmonyPatch(typeof(CollaborationInfo), "EndDate", MethodType.Getter)]
        //[HarmonyPatch(typeof(InformationVideoInfo), "EndDate", MethodType.Getter)]
        [HarmonyPatch(typeof(MissionListElement), "EndDateTime", MethodType.Getter)]
        [HarmonyPatch(typeof(ShopItemInfo), "EndDateTime", MethodType.Getter)]
        [HarmonyPatch(typeof(MissionBase), "ValidityEndDateTime", MethodType.Getter)]
        public static bool EndDate(ref DateTimeOffset? __result) {
            if (Main.ConfigEndlessDates.Value) {
                __result = FAR_FUTURE;
                return false;
            }

            return true;
        }

        private static readonly Random rand = new Random();

        [HarmonyPostfix, HarmonyPatch(typeof(ShopItemInfo), MethodType.Constructor, typeof(Lod.ExcelData.ShopItemInfo))]
        static void Price(ShopItemInfo __instance, Lod.ExcelData.ShopItemInfo excelData) {
            if (Main.ConfigSomePrices.Value) {
                __instance.Price = (rand.Next(10) + 1) * 1000;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(InformationManager), "ClearInformations")]
        static bool ClearInformations() {
            if (Main.ConfigInformationBugfix.Value) {
                return false;
            }

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(InformationManager), "RequestGetInformations")]
        static bool RequestGetInformations(InformationManager __instance, bool attractOnly = false, Action onCompleted = null) {
            if (Main.ConfigInformationBugfix.Value) {
                __instance._informations.Clear();
                __instance.InformationUpdateDto = TimeUtils.GetCurrentDto();
                __instance.InformationUpdated = true;
            }

            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ArcadeIOManager), "GetSwitchOn")]
        static void GetSwitchOn(SwitchType switchType, ref bool __result) {
            switch (switchType) {
                case SwitchType.Coin:
                    __result = __result || Main.ConfigCoinKey.Value.IsPressed();
                    break;
                case SwitchType.Enter:
                    __result = __result || Main.ConfigEnterKey.Value.IsPressed();
                    break;
                case SwitchType.Select:
                    __result = __result || Main.ConfigSelectKey.Value.IsPressed();
                    break;
                case SwitchType.Service:
                    __result = __result || Main.ConfigServiceKey.Value.IsPressed();
                    break;
                case SwitchType.Test:
                    __result = __result || Main.ConfigTestKey.Value.IsPressed();
                    break;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ArcadeIOManager), "GetSwitchDown")]
        static void GetSwitchDown(SwitchType switchType, ref bool __result) {
            switch (switchType) {
                case SwitchType.Coin:
                    __result = __result || Main.ConfigCoinKey.Value.IsDown();
                    break;
                case SwitchType.Enter:
                    __result = __result || Main.ConfigEnterKey.Value.IsDown();
                    break;
                case SwitchType.Select:
                    __result = __result || Main.ConfigSelectKey.Value.IsDown();
                    break;
                case SwitchType.Service:
                    __result = __result || Main.ConfigServiceKey.Value.IsDown();
                    break;
                case SwitchType.Test:
                    __result = __result || Main.ConfigTestKey.Value.IsDown();
                    break;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ArcadeIOManager), "GetSwitchUp")]
        static void GetSwitchUp(SwitchType switchType, ref bool __result) {
            switch (switchType) {
                case SwitchType.Coin:
                    __result = __result || Main.ConfigCoinKey.Value.IsUp();
                    break;
                case SwitchType.Enter:
                    __result = __result || Main.ConfigEnterKey.Value.IsUp();
                    break;
                case SwitchType.Select:
                    __result = __result || Main.ConfigSelectKey.Value.IsUp();
                    break;
                case SwitchType.Service:
                    __result = __result || Main.ConfigServiceKey.Value.IsUp();
                    break;
                case SwitchType.Test:
                    __result = __result || Main.ConfigTestKey.Value.IsUp();
                    break;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TestModeMainMenu), "TestModeEnd")]
        static void TestModeEnd() {
            GameInstance.Instance.LanguageManager.SelectedLanguage = Main.ConfigDefaultLanguage.Value;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameInstance), "SetupCoroutine")]
        static IEnumerator SetupCoroutine(IEnumerator result) {
            // Run original enumerator code
            while (result.MoveNext())
                yield return result.Current;

            GameInstance.Instance.LanguageManager.SelectedLanguage = Main.ConfigDefaultLanguage.Value;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PartnerUtil), "IsSelectPartner")]
        static void IsSelectPartner(ref bool __result) {
            if (Main.ConfigDisablePartnerRandomization.Value) {
                __result = true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Calibration), "CalcCalibrationOffset")]
        static bool CalcCalibrationOffset(float calibratedStandingHeight, ref float __result) {
            float baseHeight = ConfigMaster.GetInstance().GetBaseHeight();
            float minHeight = baseHeight - ConfigMaster.GetCommonInt("HeightCalibrationLimit_Lower");
            float maxHeight = baseHeight + ConfigMaster.GetCommonInt("HeightCalibrationLimit_Upper");
            float clampedHeight = Mathf.Clamp(calibratedStandingHeight, minHeight, Math.Max(maxHeight, Main.ConfigPartnerMaxHeight.Value));
            float heightOffsetRatio = ConfigMaster.GetInstance().GetHeightOffsetRatio();
            __result = (clampedHeight - ConfigMaster.GetInstance().GetBaseHeight()) * heightOffsetRatio;
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerStatus), "isUpperPlayerHeight", MethodType.Getter)]
        static void isUpperPlayerHeight(ref bool __result) {
            if (Main.ConfigNoteTimeScaleAdjust.Value) {
                __result = GameInstance.Instance.IngameContext.PlayerHeight > Math.Max(175, Main.ConfigPartnerMaxHeight.Value);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayerStatus), "CalculateHeightRate")]
        static void CalculateHeightRate(PlayerStatus __instance) {
            if (__instance.isUpperPlayerHeight) {
                int num = GameInstance.Instance.IngameContext.PlayerHeight - Math.Max(176, Main.ConfigPartnerMaxHeight.Value + 1);
                float num3 = Mathf.Pow(24f, __instance.Config.Exponent_Lower) / (__instance.Config.PowMax_Lower - 1f);
                __instance.HeightRate = Mathf.Pow(num, __instance.Config.Exponent_Lower) / num3 + 1f;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ConfigMaster), "GetBaseHeight")]
        static void GetBaseHeight(ref int __result) {
            __result += Main.ConfigScreenPositionAdjust.Value;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ErrorDisplayObject), "Start")]
        static bool Start() {
            GameInstance.Instance.LanguageManager.SelectedLanguage = Main.ConfigErrorLanguage.Value;
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(LoginBonusProgressUseCase), "GetRecievableLoginBonusInfo")]
        static bool GetRecievableLoginBonusInfo(DateTimeOffset currentTime, ref LoginBonusScheduleInfo __result) {
            if (Main.ConfigEnableLoginBonus.Value != -1) {
                LoginBonusMaster instance = LoginBonusMaster.GetInstance();
                __result = instance.LoginBonusScheduleInfos.Values.ToArray()[Main.ConfigEnableLoginBonus.Value];
                __result.endDate = "2099/09/03 00:00";
                __result.EndDate = __result.ConvertStringToDateTimeOffset(__result.endDate);
                return false;
            }

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(AttractController), "Update")]
        static bool Update(AttractController __instance) {
            if (__instance.SceneEnding) {
                return false;
            }

            if (GameInstance.Instance.IsOnline != __instance.OldIsOnline) {
                __instance.OnIsOnlineUpdated(GameInstance.Instance.IsOnline);
                if (__instance.SubSceneNameBackup == string.Empty || __instance.SubSceneNameBackup == SceneDefinition.PlayDemo) {
                    __instance.CanPlayDemo = true;
                } else {
                    __instance.CanPlayDemo = false;
                }
            }

            if (!__instance.SceneEnding && !__instance.LoginRequested) {
                if (GameInstance.Instance.IsOnline && !Main.ConfigPreventCardReadOnAttract.Value) {
                    if (!GameInstance.Instance.ArcadeIOManager.icReader.IsReading) {
                        if (!__instance.NesicaEventAdded) {
                            GameInstance.Instance.ArcadeIOManager.icReader.OnReadEnded += __instance.OnNESiCAReadEnded;
                            __instance.NesicaEventAdded = true;
                        }

                        GameInstance.Instance.ArcadeIOManager.icReader.StartReading();
                    }
                } else if (GameInstance.Instance.ArcadeIOManager.icReader.IsReading) {
                    GameInstance.Instance.ArcadeIOManager.icReader.Clear();
                    __instance.NesicaEventAdded = false;
                }

                __instance.CautionTextOnline.SetActive(GameInstance.Instance.IsOnline);
                __instance.CautionTextOffline.SetActive(!GameInstance.Instance.IsOnline);
            }

            __instance.OldIsOnline = GameInstance.Instance.IsOnline;
            __instance.ClosedTextObject.SetActive(UIUtility.IsPlayLimitNow() && !UIUtility.IsPlayClosed());
            if (!__instance.HandlingStartNextSubScene && !__instance.SubSceneExists) {
                if (__instance.LoginRequested) {
                    __instance.LoginRequested = !__instance.RequestGoToLoginScene();
                    return false;
                }

                if (__instance.TitleSceneRequested) {
                    __instance.TitleSceneRequested = !__instance.RequestTitleSubScene();
                    return false;
                }

                __instance.StartNextSubScene();
            }

            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(TitleControllerBase), "DisplayTime", MethodType.Getter)]
        static void DisplayTime(ref float __result) {
            __result = Main.ConfigTitleScreenTimer.Value;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PlayDemoContext), "GeneratePlaylist")]
        static void GeneratePlaylist(PlayDemoContext __instance) {
            if (Main.ConfigDemonstrationNonNew.Value) {
                __instance.PlaylistStageIds.AddRange(StageDefinitionMaster.GetInstance().GetStageInfoValues(false, false).Select(s => s.id));
                __instance.PlaylistIndex = new Random().Next(0, __instance.PlaylistStageIds.Count);
            }
        }

        private static readonly NumberFormatInfo JAPANESE = CultureInfo.GetCultureInfoByIetfLanguageTag("ja").NumberFormat;

        [HarmonyPrefix, HarmonyPatch(typeof(NumberFormatInfo), "CurrentInfo", MethodType.Getter)]
        static bool CurrentInfo(ref NumberFormatInfo __result) {
            if (!Main.ConfigNumberFormatFix.Value) {
                return true;
            }

            __result = JAPANESE;

            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PreparationController), "PrepareCharacter")]
        private static IEnumerator PrepareCharacter(IEnumerator __result) {
            if (!Main.ConfigSkipPreloading.Value) {
                // Run original enumerator code
                while (__result.MoveNext())
                    yield return __result.Current;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIFooterWidget), "Start")]
        static void Start(UIFooterWidget __instance) {
            __instance.version.text = "Ver " + Application.version + " (CF " + Main.VERSION + ")";
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AttractController), "SelectMainCharacter")]
        static IEnumerator SelectMainCharacter(IEnumerator result, AttractController __instance) {
            if (!string.IsNullOrEmpty(__instance.MainCharacterSoundBankId)) {
                GameInstance.Instance.SoundManager.UnloadBankById(__instance.MainCharacterSoundBankId);
            }

            CharacterInfo characterInfo = GameInstance.Instance.ActiveCharacterInfos[__instance.MainCharacterTableIndex];
            if (GameInstance.Instance.CollaborationManager.ActiveCollaborationInfo == null) {
                GameInstance.Instance.CollaborationManager.SelectActiveCollaboration();
            }

            if (!Main.ConfigForceRandomAttractPartners.Value && GameInstance.Instance.CollaborationManager.ActiveCollaborationInfo != null) {
                foreach (CharacterInfo characterInfo2 in CharacterMaster.GetInstance().SortedAllCharacterInfos) {
                    if (characterInfo2.id == GameInstance.Instance.CollaborationManager.ActiveCollaborationInfo.CharacterId) {
                        characterInfo = characterInfo2;
                    }
                }
            }

            __instance.MainCharacterId = characterInfo.id;
            int num = AttractController.FirstVersionCharactorNum + (GameInstance.Instance.ActiveCharacterInfos.Count - AttractController.FirstVersionCharactorNum) * 5;
            __instance.MainCharacterTableIndex = global::UnityEngine.Random.Range(0, num);
            if (__instance.MainCharacterTableIndex >= AttractController.FirstVersionCharactorNum) {
                __instance.MainCharacterTableIndex = global::UnityEngine.Random.Range(AttractController.FirstVersionCharactorNum, GameInstance.Instance.ActiveCharacterInfos.Count);
            }

            __instance.MainCharacterSoundBankId = characterInfo.voiceCueSheet;
            GameInstance.Instance.SoundManager.LoadBankById(__instance.MainCharacterSoundBankId);
            yield return new WaitWhile(() => GameInstance.Instance.SoundManager.IsBankLoading());
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIEntryController), "OnUseDataConfirmClosed")]
        static bool OnUseDataConfirmClosed(UIEntryController __instance) {
            if (!Main.ConfigAlwaysShowMultiSelection.Value) {
                return true;
            }

            __instance.OnClosed();
            GameInstance.Instance.MenuManager.OpenGPPurchase(UIGPPurchaseWidget.Type.MultiPlay, __instance.OnPurchaced, __instance.GoToTitle, true);

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ConfigMaster), "GetMirrorModeCost")]
        static bool GetMirrorModeCost(ref int __result) {
            if (Main.ConfigMirrorFree.Value) {
                __result = 0;
                return false;
            }

            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SongSelectController), "SortStageInfoList")]
        static bool SortStageInfoList(SongSelectController __instance) {
            if (!Main.ConfigSortByNewIsSortByScore.Value || !__instance.sortToggle[1].isOn) {
                return false;
            }
            
            __instance.filteredStageInfos = (from m in __instance.filteredStageInfos
                orderby GetHighestChartScore(m), m.reading
                select m).ToList();

            if (__instance.orderToggle[1].isOn) {
                __instance.filteredStageInfos.Reverse();
            }

            return false;
        }

        private static int GetHighestChartScore(StageInfo stageInfo) {
            MusicRecordUseCase mr = GameInstance.Instance.Player.MusicRecordUseCase;
            List<int> scores = new List<int>();
            
            if (mr.IsPlayByChartId(stageInfo.chartIdEasy)) {
                scores.Add(mr.GetHighScore(stageInfo.chartIdEasy));                
            }
            if (mr.IsPlayByChartId(stageInfo.chartIdNormal)) {
                scores.Add(mr.GetHighScore(stageInfo.chartIdNormal));                
            }
            if (mr.IsPlayByChartId(stageInfo.chartIdHard)) {
                scores.Add(mr.GetHighScore(stageInfo.chartIdHard));                
            }
            if (mr.IsPlayByChartId(stageInfo.chartIdManiac)) {
                scores.Add(mr.GetHighScore(stageInfo.chartIdManiac));                
            }
            if (mr.IsPlayByChartId(stageInfo.chartIdConnect)) {
                scores.Add(mr.GetHighScore(stageInfo.chartIdConnect));                
            }

            return scores.Count > 0 ? scores.Max() : 0;
        }
    }
}