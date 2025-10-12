using System.Collections.Generic;
using System.Reflection.Emit;
using BackgroundThrust.Utils;
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
