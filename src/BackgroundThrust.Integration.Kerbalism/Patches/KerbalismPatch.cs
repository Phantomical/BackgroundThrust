using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using KERBALISM;

namespace BackgroundThrust.Integration.Kerbalism.Patches;

[HarmonyPatch]
[HarmonyDebug]
internal static class Kerbalism_Patch
{
    const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    const BindingFlags Instance =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static double GetLastUpdateTimeBase(Guid vesselId) => Planetarium.GetUniversalTime();

    static Dictionary<int, ResourceInfo> GetResourcesBase(VesselResources v) => null;

    static double? MakeValue(double value) => value;

    static double? MakeNull() => null;

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Kerbalism_Patch), nameof(GetLastUpdateTimeBase))]
    internal static double? GetLastUpdateTime(Guid vesselId)
    {
#pragma warning disable CS8321 // Local function is declared but never used
        static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator gen
        )
        {
            var kerbalism = typeof(KERBALISM.Kerbalism);
            var unloadedData = kerbalism.GetNestedType("Unloaded_data");
            var dict = typeof(Dictionary<,>).MakeGenericType(typeof(Guid), unloadedData);
            var tryGetValueMethod = dict.GetMethod("TryGetValue");

            var unloadedField = kerbalism.GetField("unloaded", Static);
            var timeField = unloadedData.GetField("time", Instance);

            var value = gen.DeclareLocal(unloadedData);
            var labelF = gen.DefineLabel();

            // What we're trying to generate here is effectively
            //
            // double? GetLastUpdateTime(Guid vesselId)
            // {
            //    if (Kerbalism.unloaded.TryGetValue(vesselId, out Unloaded_data data))
            //      return data.time;
            //    return null;
            // }

            return
            [
                new CodeInstruction(OpCodes.Ldsfld, unloadedField),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldloca_S, (byte)value.LocalIndex),
                new CodeInstruction(OpCodes.Call, tryGetValueMethod),
                new CodeInstruction(OpCodes.Brfalse, labelF),
                // True branch
                new CodeInstruction(OpCodes.Ldloc_S, (byte)value.LocalIndex),
                new CodeInstruction(OpCodes.Ldfld, unloadedField),
                new CodeInstruction(
                    OpCodes.Call,
                    SymbolExtensions.GetMethodInfo(() => MakeValue(0.0))
                ),
                new CodeInstruction(OpCodes.Ret),
                // False branch
                new CodeInstruction(
                    OpCodes.Call,
                    SymbolExtensions.GetMethodInfo(() => MakeNull())
                ).WithLabels(labelF),
                new CodeInstruction(OpCodes.Ret),
            ];
        }
#pragma warning restore CS8321 // Local function is declared but never used

        return null;
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Kerbalism_Patch), nameof(GetResourcesBase))]
    internal static Dictionary<int, ResourceInfo> GetResources(VesselResources v)
    {
#pragma warning disable CS8321 // Local function is declared but never used
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> _)
        {
            var field = typeof(VesselResources).GetField("resources", Instance);

            return
            [
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, field),
                new CodeInstruction(OpCodes.Ret),
            ];
        }
#pragma warning restore CS8321 // Local function is declared but never used

        return null;
    }
}
