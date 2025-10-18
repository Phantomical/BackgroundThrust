using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BackgroundThrust.Patches;

[HarmonyPatch(typeof(TimeWarp), "setRate")]
internal static class TimeWarp_SetRate_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator gen
    )
    {
        const ControlTypes THROTTLE_MASK = ~(ControlTypes.THROTTLE | ControlTypes.THROTTLE_CUT_MAX);

        var setControlLockMethod = SymbolExtensions.GetMethodInfo(() =>
            InputLockManager.SetControlLock(ControlTypes.None, "")
        );

        var lockId = gen.DeclareLocal(typeof(string));

        var matcher = new CodeMatcher(instructions, gen);
        matcher
            .MatchStartForward(
                new CodeMatch(new CodeInstruction(OpCodes.Call, setControlLockMethod))
            )
            .ThrowIfInvalid("Unable to find call to InputLockManager.SetControlLock")
            .MatchEndBackwards(new CodeMatch(OpCodes.Ldstr, "TimeWarpLock"))
            .ThrowIfInvalid("Unable to find ldstr instruction")
            .Insert(
                new CodeInstruction(OpCodes.Ldc_I8, unchecked((long)THROTTLE_MASK)),
                new CodeInstruction(OpCodes.And)
            );

        return matcher.Instructions();
    }
}

[HarmonyPatch(typeof(TimeWarp), "getMaxOnRailsRateIdx")]
internal static class TimeWarp_GetMaxOnRailsRateIdx_Patch
{
    static bool ShouldAllowWarpWhileAccelerating(TimeWarp timeWarp)
    {
        var vessel = FlightGlobals.ActiveVessel;
        if (vessel == null)
            return false;

        if (timeWarp.current_rate_index > timeWarp.maxPhysicsRate_index)
            return true;

        var module = vessel.GetBackgroundThrust();
        if (module.Throttle != 0.0)
            return true;

        return false;
    }

    static void Prefix(TimeWarp __instance, out double __state)
    {
        __state = TimeWarp.GThreshold;

        if (ShouldAllowWarpWhileAccelerating(__instance))
            TimeWarp.GThreshold = double.PositiveInfinity;
    }

    static void Finalizer(double __state) => TimeWarp.GThreshold = __state;
}
