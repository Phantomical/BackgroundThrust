using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SolverEngines;

namespace BackgroundThrust.SolverEngines.Patches;

[HarmonyPatch(typeof(ModuleEnginesSolver), nameof(ModuleEnginesSolver.FixedUpdate))]
internal static class ModuleEnginesSolver_FixedUpdate_Patch
{
    // Completely disable FixedUpdate if there is an enabled background engine
    // and we are in time warp.
    static bool Prefix(ModuleEnginesSolver __instance)
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
