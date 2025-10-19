using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace BackgroundThrust.Patches;

[HarmonyPatch(typeof(ModuleEngines), nameof(ModuleEngines.FixedUpdate))]
internal static class ModuleEngines_FixedUpdate_Patch
{
    static bool Prefix(ModuleEngines __instance)
    {
        if (!HighLogic.LoadedSceneIsFlight)
            return true;

        if (BackgroundEngine.InPackedUpdate)
            return true;

        var part = __instance.part;
        if (!IsTimeWarping(part))
            return true;

        var bgengine = part.FindModuleImplementing<BackgroundEngine>();
        if (!bgengine.IsEnabled)
            return true;

        return false;
    }

    static bool IsTimeWarping(Part part)
    {
        return part.packed
            || (TimeWarp.CurrentRate > 1f && TimeWarp.WarpMode == TimeWarp.Modes.HIGH);
    }
}

[HarmonyPatch(typeof(ModuleEngines), "TimeWarping")]
internal static class ModuleEngines_TimeWarping_Patch
{
    static bool Prefix(ref bool __result)
    {
        // We actually want to run the vessel in warp. But only as part of
        // BackgroundThrust.PackedUpdate, not for regular FixedUpdate calls.
        if (BackgroundEngine.InPackedUpdate)
        {
            __result = false;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(ModuleEngines), "ThrustUpdate")]
internal static class ModuleEngines_ThrustUpdate_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var addForceAtPositionMethod = SymbolExtensions.GetMethodInfo(
            (Part part) => part.AddForceAtPosition(default, default)
        );

        var matcher = new CodeMatcher(instructions);
        matcher
            .MatchStartForward(
                new CodeMatch(inst =>
                {
                    if (inst.opcode != OpCodes.Callvirt)
                        return false;
                    if (inst.operand is not MethodInfo method)
                        return false;
                    return method == addForceAtPositionMethod;
                })
            )
            .ThrowIfInvalid("Unable to find call to Part.AddForceAtPosition")
            .Set(
                OpCodes.Call,
                SymbolExtensions.GetMethodInfo(() =>
                    BackgroundEngine.AddForceAtPosition(null, default, default)
                )
            );

        return matcher.Instructions();
    }
}
