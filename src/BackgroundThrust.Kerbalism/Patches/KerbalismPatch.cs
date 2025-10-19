using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using KERBALISM;

namespace BackgroundThrust.Kerbalism.Patches;

[HarmonyPatch]
internal static class Kerbalism_Patch
{
    const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    const BindingFlags Instance =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static double GetLastUpdateTimeBase(Guid vesselId) => Planetarium.GetUniversalTime();

    static Dictionary<int, ResourceInfo> GetResourcesBase(VesselResources v) => null;

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
            var unloadedData = kerbalism.GetNestedType(
                "Unloaded_data",
                BindingFlags.NonPublic | BindingFlags.Public
            );
            var dict = typeof(Dictionary<,>).MakeGenericType(typeof(Guid), unloadedData);
            var tryGetValueMethod = dict.GetMethod("TryGetValue");

            var unloadedField = kerbalism.GetField("unloaded", Static);
            var timeField = unloadedData.GetField("time", Instance);
            var ctor = typeof(double?).GetConstructor([typeof(double)]);

            var value = gen.DeclareLocal(unloadedData);
            var nullable = gen.DeclareLocal(typeof(double?));
            var exit = gen.DefineLabel();

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
                // double? nullable;
                // bool cond = Kerbalism.unloaded.TryGetValue(vesselId, out var data)
                new CodeInstruction(OpCodes.Ldsfld, unloadedField),
                new CodeInstruction(OpCodes.Ldarg_0),
                Ldloca(value),
                new CodeInstruction(OpCodes.Call, tryGetValueMethod),
                // if (!cond) goto exit;
                new CodeInstruction(OpCodes.Brfalse_S, exit),
                // if (data is null) goto exit;
                Ldloc(value),
                new CodeInstruction(OpCodes.Brfalse_S, exit),
                // nullable = data.time;
                Ldloca(nullable),
                Ldloc(value),
                new CodeInstruction(OpCodes.Ldfld, timeField),
                new CodeInstruction(OpCodes.Call, ctor),
                // exit:
                // return nullable;
                new CodeInstruction(OpCodes.Ldloc_S, (byte)nullable.LocalIndex).WithLabels(exit),
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

    private static CodeInstruction Ldloc(LocalBuilder local)
    {
        return local.LocalIndex switch
        {
            0 => new CodeInstruction(OpCodes.Ldloc_0),
            1 => new CodeInstruction(OpCodes.Ldloc_1),
            2 => new CodeInstruction(OpCodes.Ldloc_2),
            3 => new CodeInstruction(OpCodes.Ldloc_3),
            var i => i >= 0 && i < 256
                ? new CodeInstruction(OpCodes.Ldloc_S, (byte)i)
                : new CodeInstruction(OpCodes.Ldloc, i),
        };
    }

    private static CodeInstruction Ldloca(LocalBuilder local)
    {
        var i = local.LocalIndex;
        return i >= 0 && i < 256
            ? new CodeInstruction(OpCodes.Ldloca_S, (byte)i)
            : new CodeInstruction(OpCodes.Ldloca, i);
    }
}
