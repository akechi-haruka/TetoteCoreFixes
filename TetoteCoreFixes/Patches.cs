using System;
using System.IO;
using HarmonyLib;
using Lod;
using Lod.ImageRecognition;
using Lod.TypeX4;
using ScreenRotate;
using UnityEngine;
using UnityEngine.UI;
using Input = Lod.Input;
using Logger = UnityEngine.Logger;
using Random = System.Random;

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
                    if (Native.IsDiskFull(ex)) {
                        lastError = GWSafeFile.FILE_ERROR.ERROR_TOO_SMALL_FREE_SPACE;
                    } else {
                        lastError = GWSafeFile.FILE_ERROR.ERROR_ROLL_BACK;
                    }
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

        private static Random rand = new Random();

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
        
    }
}