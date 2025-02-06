using HarmonyLib;
using Lod;
using Lod.ImageRecognition;
using Lod.TypeX4;
using ScreenRotate;
using UnityEngine;
using Input = Lod.Input;
using Logger = UnityEngine.Logger;

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

        [HarmonyPrefix, HarmonyPatch(typeof(ScreenRotater), "Rotate")]
        static bool Rotate(ref bool __result, uint DisplayNumber, ScreenRotater.Orientations Orientation) {
            if (Main.ConfigPreventAutoScreenRotate.Value) {
                __result = true;
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
        
    }
}