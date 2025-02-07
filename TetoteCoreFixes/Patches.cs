using System;
using System.IO;
using HarmonyLib;
using Lod;
using Lod.ImageRecognition;
using Lod.TypeX4;
using ScreenRotate;
using UnityEngine;
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
        
        [HarmonyPrefix, HarmonyPatch(typeof(DepthCam2), "hasCameraProblems", MethodType.Getter)]
        public static bool hasCameraProblems(ref bool __result) {
            if (Main.ConfigCameraDummy.Value) {
                __result = false;
            } else {
                return true;
            }
            return false;
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(DepthCamera), "hasCameraProblems", MethodType.Getter)]
        public static bool hasCameraProblems2(ref bool __result) {
            if (Main.ConfigCameraDummy.Value) {
                __result = false;
            } else {
                return true;
            }
            return false;
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(DeviceCheck), "IsTouchPanelEnable")]
        public static bool IsTouchPanelEnable(ref bool __result) {
            // TODO: check winapi for touchpanel
            __result = true;
            return false;
        }


        [HarmonyPrefix, HarmonyPatch(typeof(GWSafeFile), "WriteStr")]
        public static bool WriteStr(string _targetStr, string _baseName, int _MaxLog, bool _bUseCommit = false) {
            if (!Directory.Exists("nvram")) {
                Main.Log.LogInfo("Created nvram directory");
                Directory.CreateDirectory("nvram");
            }
            File.WriteAllText("nvram\\"+_baseName, _targetStr);
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GWSafeFile), "ReadStr")]
        public static bool ReadStr(ref string __result, string _baseName) {
            if (File.Exists("nvram\\"+_baseName)) {
                __result = File.ReadAllText("nvram\\" + _baseName);
            } else {
                __result = "";
            }

            return false;
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

        [HarmonyPrefix, HarmonyPatch(typeof(ShopItemInfo), "Price", MethodType.Getter)]
        static bool Price(ShopItemInfo __instance) {
            if (Main.ConfigSomePrices.Value) {
                if (__instance.Price == 0) {
                    __instance.Price = (rand.Next(10) + 1) * 1000;
                }
            }

            return true;
        }
        
    }
}