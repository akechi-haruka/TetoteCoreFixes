using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Lod;
using UnityEngine;
using Debug = Lod.Debug;

namespace Retote;

// ReSharper disable InconsistentNaming
public class GameplayRestorationPatches {
    [HarmonyPrefix, HarmonyPatch(typeof(MarkerChartPlayer), "CreateUpdater")]
    private static bool CreateUpdater(MarkerChartData.MarkerData markerData, ref MarkerUpdater __result) {
        if (!Plugin.ConfigEnable.Value) {
            return true;
        }

        MarkerUpdater markerUpdater = markerData.type switch {
            MarkerChartData.MarkerData.Type.Hit => new MarkerUpdater_Hit(),
            MarkerChartData.MarkerData.Type.Trace => new MarkerUpdater_Trace(),
            MarkerChartData.MarkerData.Type.Hold => new MarkerUpdater_Hold(),
            MarkerChartData.MarkerData.Type.Slash => new MarkerUpdater_Slash(),
            MarkerChartData.MarkerData.Type.Slash_SP => new MarkerUpdater_SpecialSlash(),
            MarkerChartData.MarkerData.Type.Trace_Fit => new MarkerUpdater_TraceFit(),
            MarkerChartData.MarkerData.Type.HighTouch => new MarkerUpdater_HighTouch(),
            MarkerChartData.MarkerData.Type.Pose => new MarkerUpdater_Pose(),
            MarkerChartData.MarkerData.Type.Slash_SP_Pose => new MarkerUpdater_SpecialSlashPose(),
            _ => null
        };

        if (markerUpdater == null) {
            Debug.LogError("Failed to create updater. marker.beat=" + markerData.time + ",id = " + markerData.id + ",type =" + markerData.type);
        }

        __result = markerUpdater;
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MarkerAssetDispenser), "GetMarkerAssetNames")]
    static bool GetMarkerAssetNames(ref List<Tuple<string, string>> __result) {
        if (!Plugin.ConfigEnable.Value) {
            return true;
        }

        List<Tuple<string, string>> list = new List<Tuple<string, string>>();

        string markerColorSetId = GameInstance.Instance.IngameContext.MarkerColorSetId;
        MarkerColorSetInfos markerColorSetInfoById = ConfigMaster.GetInstance().GetMarkerColorSetInfoById(markerColorSetId);

        foreach (object obj in Enum.GetValues(typeof(MarkerChartData.MarkerData.Type))) {
            MarkerChartData.MarkerData.Type type = (MarkerChartData.MarkerData.Type)obj;

            if (type == MarkerChartData.MarkerData.Type.Hit2Nodes || type == MarkerChartData.MarkerData.Type.Spark) {
                continue;
            }

            string text = type switch {
                MarkerChartData.MarkerData.Type.Hit => "HitMarker_",
                MarkerChartData.MarkerData.Type.Pose => "PoseMarker",
                MarkerChartData.MarkerData.Type.Slash_SP_Pose => "SpecialSlashMarker_Pose_",
                MarkerChartData.MarkerData.Type.Trace => "TraceMarker_",
                MarkerChartData.MarkerData.Type.Hold => "HoldMarker_",
                MarkerChartData.MarkerData.Type.Slash => "SlashMarker_",
                MarkerChartData.MarkerData.Type.Slash_SP => "SpecialSlashMarker_",
                MarkerChartData.MarkerData.Type.Trace_Fit => "TraceFitMarker_",
                MarkerChartData.MarkerData.Type.HighTouch => "HighTouchMarker_",
                _ => String.Empty
            };

            string text2 = text;
            if (type != MarkerChartData.MarkerData.Type.Pose) {
                text += markerColorSetInfoById.colorL;
                text2 += markerColorSetInfoById.colorR;
            }

            list.Add(new Tuple<string, string>("Marker/" + text, "Marker/" + text2));
        }

        __result = list;
        return false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MarkerAsset_Pose), "Setup")]
    private static void Setup(MusicTime currentTime, MarkerAsset_Pose __instance) {
        __instance.m_CountText.text = ""; // debug stuff
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MarkerAsset_Base.Simultaneity), "Hide")]
    private static bool Hide(MarkerAsset_Base.Simultaneity __instance) {
        __instance.simultaneity?.SetActive(false); // null fix
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MarkerUpdater_Pose), "UpdateInactive")]
    [SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified")]
    private static bool UpdateInactive(MusicTime currentTime, MarkerUpdater_Pose __instance) {
        if (__instance.IsGradingStart(currentTime)) { // .base copy
            if (__instance.m_MarkerData.length <= 1.0F) {
                __instance.m_MarkerData.length = 4.0F;
                Plugin.Logger.LogWarning("Pose marker length was incorrect!");
            }

            __instance.SetState(MarkerUpdater.State.Active, currentTime);
        }

        if (currentTime.Beat >= __instance.m_MarkerData.time - __instance.m_reactionOffsetTime[0] * currentTime.Numerator && !__instance.m_bCleanuped) {
            __instance.PlayReaction(0);
        }

        __instance.PlaySystemVoice(currentTime);
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MarkerUpdater_Pose), "UpdateActive")]
    [SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified")]
    private static bool UpdateActive(MusicTime currentTime, MarkerUpdater_Pose __instance) {
        if (currentTime.Beat >= __instance.m_MarkerData.time - __instance.m_reactionOffsetTime[1] * currentTime.Numerator && !__instance.m_bCleanuped) {
            __instance.PlayReaction(1);
        }

        __instance.PlaySystemVoice(currentTime);
        __instance.UpdateCircleBase(currentTime.Beat);
        __instance.UpdateCircleGauge(currentTime.Beat);


        // TODO: Judgements always succeed
        // m_bSuccess is normally set by OnRecgnitionFinished, which has no calls...
        // Lod.ImageRecognition.Recognizer has also been wiped.......
        if (currentTime.Beat >= __instance.m_MarkerData.time + (Plugin.ConfigFakeJudgementDelay.Value / 10F)) {
            Plugin.Logger.LogDebug("Hit fake judgement timing");
            __instance.m_bSuccess = true;
        }

        if (currentTime.Beat >= __instance.m_MarkerData.time && currentTime.Beat <= __instance.m_MarkerData.End && (__instance.m_bSuccess || !__instance.PoseRecognizable)) {
            Plugin.Logger.LogDebug("Pose is JUDGED (" + __instance.m_bSuccess + ")");
            InGameController inGameController = __instance.m_Player.StagePlayer.InGameContoller;
            if (inGameController != null && inGameController.IsEnableNotesInput) { // autoplay
                __instance.OnGraded(MarkerUpdater.Grade.Perfect, currentTime);
            } else {
                __instance.OnGraded(__instance.m_bSuccess ? MarkerUpdater.Grade.Perfect : MarkerUpdater.Grade.Miss, currentTime);
            }

            __instance.SetState(MarkerUpdater.State.Graded, currentTime);

            __instance.PoseMarkerAsset.ShowCircle(false);
        }

        __instance.m_bTimeOut = __instance.CheckTimeout(currentTime);
        if (__instance.m_bTimeOut) {
            Plugin.Logger.LogDebug("Pose is TIMEOUT");
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MarkerUpdater), "OnGraded")] // original excludes poses from combo gain
    static bool OnGraded(MarkerUpdater.Grade result, MusicTime currentTime, MarkerUpdater __instance) {
        if (__instance.m_Player.StagePlayer.InGameContoller.currentState == InGameController.State.Playing) {
            if (__instance.m_MarkerData.type == MarkerChartData.MarkerData.Type.Pose) {
                __instance.UpdateScore(result);
                int num = __instance.UpdateCombo(result);
                __instance.m_Asset.MyComboValue = num;
                __instance.UpdateGradeNum(result);
            }
        }

        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MarkerUpdater_Pose), "CalculatePlayVoiceTime")]
    static bool CalculatePlayVoiceTime(MarkerUpdater_Pose __instance) {
        float[] systemVoiceOffsetTime = __instance.PlayerStatus.Config.SystemVoiceOffsetTime;
        float time = __instance.m_MarkerData.time;
        for (int i = 0; i < __instance.m_voiceOffsetTime.Length; i++) {
            __instance.m_voiceOffsetTime[i] = time - (3 - i - 1) + __instance.m_Player.StagePlayer.currentMusicTime.BeatBySecond(systemVoiceOffsetTime[i]);
            Plugin.Logger.LogDebug("Voice time " + i + ": " + __instance.m_voiceOffsetTime[i]);
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MarkerUpdater_Pose), "CalculatePlayVoiceTime")]
    static bool CalculatePlayReactionTime(MarkerUpdater_Pose __instance) {
        __instance.m_reactionOffsetTime[0] = __instance.PlayerStatus.Config.HighTouch_ReactionTime_BeforePose;
        __instance.m_reactionOffsetTime[1] = __instance.PlayerStatus.Config.HighTouch_ReactionTime_Pose - 1;
        Plugin.Logger.LogDebug("Reaction time before pose: " + __instance.m_reactionOffsetTime[0]);
        Plugin.Logger.LogDebug("Reaction time at pose: " + __instance.m_reactionOffsetTime[1]);
        return false;
    }


    [HarmonyPrefix, HarmonyPatch(typeof(MarkerUpdater_Pose), "PlaySystemVoice")]
    [SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified")]
    static bool PlaySystemVoice(MusicTime currentTime, MarkerUpdater_Pose __instance) {
        for (int i = 0; i < __instance.m_voiceOffsetTime.Length; i++) {
            if (__instance.m_voiceOffsetTime[i] <= currentTime.Beat && !__instance.m_bCleanuped) {
                __instance.PlayVoice(i);
            }
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(MarkerUpdater_Pose), "PlayVoice")]
    private static bool PlayVoice(int voiceNo, MarkerUpdater_Pose __instance) {
        if (__instance.m_bPlayVoice[voiceNo]) {
            Plugin.Logger.LogDebug("Play voice " + voiceNo);
            string path = voiceNo switch {
                0 => "V00_ING_COUNT2",
                1 => "V00_ING_COUNT1",
                2 => "V00_ING_HAI",
                _ => null
            };

            if (path != null) {
                if (!GameInstance.Instance.LanguageManager.IsJapanese) {
                    path += "_EN";
                }

                GameInstance.Instance.SoundManager.PlayByCueId(path);
            }

            __instance.m_bPlayVoice[voiceNo] = false;
        }

        return false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MarkerAsset_Base), "CreateGradeEffect")]
    private static void CreateGradeEffect(MarkerAsset_Base __instance) {
        if (__instance.Updater.MarkerData.type != MarkerChartData.MarkerData.Type.Pose || __instance.Updater.CurrentGrade == MarkerUpdater.Grade.Miss) {
            return;
        }

        __instance.m_GradeAsset = __instance.Updater.Player.ReserveAsset(EffectDispenser.EffectType.HighTouch);
        __instance.m_GradeAsset.transform.localPosition = Vector3.zero;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MarkerChartData.MarkerData), "AppearAt")]
    [SuppressMessage("Method Declaration", "Harmony003:Harmony non-ref patch parameters modified")]
    private static void AppearAt(MusicTime currentTime, MarkerChartData.MarkerData __instance, ref float __result) {
        if (__instance.type != MarkerChartData.MarkerData.Type.Pose) {
            return;
        }

        float beforePoseTime = GameInstance.Instance.MasterManager.ConfigMaster.GetHighTouchReactionTime_BeforePose();
        float adjustedPoseTime = beforePoseTime * currentTime.GetNumeratorByBeat(__instance.time - beforePoseTime);
        __result = __instance.time - adjustedPoseTime;
    }
}