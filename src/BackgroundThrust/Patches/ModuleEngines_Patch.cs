using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BackgroundThrust.Patches;

[HarmonyPatch(typeof(ModuleEngines), "TimeWarping")]
internal static class ModuleEngines_TimeWarping_Patch
{
    static bool Prefix(ModuleEngines __instance, ref bool __result)
    {
        if (!HighLogic.LoadedSceneIsFlight)
            return true;

        var part = __instance.part;
        if (!IsTimeWarping(part))
            return true;

        if (!BackgroundThrustVessel.IsThrustPermitted(__instance.vessel))
            return true;

        var bgengine = part.GetBackgroundEngine();
        if (bgengine is null)
            return true;

        if (!bgengine.IsEnabled)
            return true;

        // If there is an enabled BackgroundEngine module on this part then
        // we pretend that we aren't in time warp so that the engine runs
        // normally.
        __result = false;
        return false;
    }

    static bool IsTimeWarping(Part part)
    {
        return part.packed
            || (TimeWarp.CurrentRate > 1f && TimeWarp.WarpMode == TimeWarp.Modes.HIGH);
    }
}

[HarmonyPatch(typeof(ModuleEngines), "ThrustUpdate")]
internal static class ModuleEngines_ThrustUpdate_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var original = SymbolExtensions.GetMethodInfo<Part>(p => p.AddThermalFlux(0.0));
        var replacement = SymbolExtensions.GetMethodInfo<Part>(p =>
            AddThermalFluxAtLowWarp(p, 0.0)
        );

        var matcher = new CodeMatcher(instructions);

        matcher
            .MatchStartForward(new CodeMatch(OpCodes.Callvirt, original))
            .ThrowIfInvalid("Unable to find call to Part.AddThermalFlux")
            .SetInstruction(new CodeInstruction(OpCodes.Call, replacement));

        return matcher.Instructions();
    }

    static void AddThermalFluxAtLowWarp(Part part, double kilowatts)
    {
        // Avoid adding heat when the simulation has switched to analytical mode.
        if (TimeWarp.CurrentRate > PhysicsGlobals.ThermalMaxIntegrationWarp)
            return;

        part.AddThermalFlux(kilowatts);
    }
}
