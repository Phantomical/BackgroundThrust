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
        var setControlLockMethod = SymbolExtensions.GetMethodInfo(() =>
            InputLockManager.SetControlLock(ControlTypes.None, "")
        );

        var matcher = new CodeMatcher(instructions, gen);
        matcher
            .MatchStartForward(
                new CodeMatch(new CodeInstruction(OpCodes.Call, setControlLockMethod))
            )
            .ThrowIfInvalid("Unable to find call to InputLockManager.SetControlLock")
            .RemoveInstruction()
            .Insert(CodeInstruction.Call(() => SetControlLockFiltered(ControlTypes.None, "")));

        return matcher.Instructions();
    }

    static ControlTypes SetControlLockFiltered(ControlTypes controlTypes, string lockId)
    {
        const ControlTypes THROTTLE_MASK = ~(ControlTypes.THROTTLE | ControlTypes.THROTTLE_CUT_MAX);

        // LogUtil.Log($"Initial: 0x{(ulong)controlTypes:X16}");
        // LogUtil.Log($"Mask:    0x{(ulong)THROTTLE_MASK:X16}");
        // LogUtil.Log($"Final:   0x{(long)(controlTypes & THROTTLE_MASK):X16}");

        return InputLockManager.SetControlLock(controlTypes & THROTTLE_MASK, lockId);
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

        var module = vessel.FindVesselModuleImplementing<BackgroundThrustVessel>();
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
