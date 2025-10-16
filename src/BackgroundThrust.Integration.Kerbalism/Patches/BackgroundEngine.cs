using System.Collections.Generic;
using HarmonyLib;

namespace BackgroundThrust.Integration.Kerbalism.Patches;

[HarmonyPatch(typeof(BackgroundEngine), nameof(BackgroundEngine.OnSave))]
internal static class BackgroundEngine_OnSave_Patch
{
    static void Postfix(BackgroundEngine __instance, ConfigNode node) =>
        BackgroundEngineKerbalism.OnSave(__instance, node);
}

[HarmonyPatch(typeof(BackgroundEngine), nameof(BackgroundEngine.BackgroundUpdate))]
internal static class BackgroundEngine_BackgroundUpdate_Patch
{
    static void Postfix(
        Vessel v,
        ProtoPartSnapshot part_snapshot,
        ProtoPartModuleSnapshot module_snapshot,
        PartModule proto_part_module,
        Part proto_part,
        Dictionary<string, double> availableResources,
        List<KeyValuePair<string, double>> resourceChangeRequest,
        double elapsed_s,
        out string __result
    )
    {
        __result = BackgroundEngineKerbalism.BackgroundUpdate(
            v,
            part_snapshot,
            module_snapshot,
            proto_part_module,
            proto_part,
            availableResources,
            resourceChangeRequest,
            elapsed_s
        );
    }
}
