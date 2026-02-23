using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BackgroundThrust.Patches;

/// <summary>
/// Allow saving and switching vessels while under acceleration. We do this by
/// overwriting the <c>ActiveVessel.geeForce &gt; 0.1</c> with
/// <c>ActiveVessel.geeForce &gt; double.PositiveInfinity</c>.
/// </summary>
[HarmonyPatch(typeof(FlightGlobals), nameof(FlightGlobals.ClearToSave), [typeof(bool)])]
internal static class FlightGlobals_ClearToSave_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator gen
    )
    {
        var geeForce = AccessTools.Field(typeof(Vessel), nameof(Vessel.geeForce));

        var matcher = new CodeMatcher(instructions, gen);
        matcher
            .MatchStartForward(
                new CodeMatch(OpCodes.Ldfld, geeForce),
                new CodeMatch(OpCodes.Ldc_R8, 0.1)
            )
            .ThrowIfInvalid("Unable to find geeForce > 0.1 comparison")
            .Advance(1)
            .SetOperandAndAdvance(double.PositiveInfinity);

        return matcher.Instructions();
    }
}
