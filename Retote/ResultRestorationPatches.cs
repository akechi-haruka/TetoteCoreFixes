using System.Collections.Generic;
using HarmonyLib;
using Lod;
using Lod.Timeline;

namespace Retote;

// ReSharper disable InconsistentNaming
public class ResultRestorationPatches {
    [HarmonyPostfix, HarmonyPatch(typeof(ResultController), "StepTable", MethodType.Getter)]
    private static void StepTable(ref IReadOnlyList<ResultViewType> __result) {
        __result = new List<ResultViewType> {
            ResultViewType.SuccessAndFailure,
            ResultViewType.Score,
            ResultViewType.PoseCard,
            ResultViewType.Event,
            ResultViewType.Collaboration,
            ResultViewType.Mission,
            ResultViewType.Continue
        };
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ResultController), "InitializeViewControlEvents")]
    private static void InitializeViewControlEvents(ResultController __instance) {
        ResultPoseCardView pcv = __instance.gameObject.AddComponent<ResultPoseCardView>();
        pcv.Context = __instance.Context;
        __instance.ViewControlEvents.Add(ResultViewType.PoseCard, new ResultController.ViewControlEvent {
            CheckSkip = () => false,
            Enter = () => {
                Plugin.Logger.LogDebug("Entering PoseCardView");
            },
            Exit = () => {
                Plugin.Logger.LogDebug("Leaving PoseCardView");
            }
        });
        __instance.ResultViewControllers.Add(ResultViewType.PoseCard, pcv);
    }
}