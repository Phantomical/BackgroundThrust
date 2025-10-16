using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using KERBALISM;

namespace BackgroundThrust.Integration.Kerbalism.Patches;

[HarmonyPatch(typeof(VesselResources), nameof(VesselResources.Sync))]
internal static class VesselResources_Sync_Patch
{
    static readonly FieldInfo PtsField = typeof(ResourceInfo).GetField(
        "pts",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
    );

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ResourceInfo), "Amount", MethodType.Setter)]
    static void SetAmount(ResourceInfo info, double amount) { }

    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator gen
    )
    {
        var loadedField = typeof(Vessel).GetField("loaded");
        var matcher = new CodeMatcher(instructions, gen);
        matcher
            .MatchStartForward(
                new CodeMatch(inst =>
                {
                    if (inst.opcode != OpCodes.Ldfld)
                        return false;

                    if (inst.operand is not FieldInfo field)
                        return false;

                    return field == loadedField;
                })
            )
            .ThrowIfInvalid("Could not find load of Vessel.loaded")
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(
                    OpCodes.Call,
                    SymbolExtensions.GetMethodInfo(() => AddNullSyncSet(null, null))
                )
            );

        return matcher.Instructions();
    }

    static void AddNullSyncSet(Vessel v, VesselResources resources)
    {
        var info = resources.GetResource(v, KerbalismVesselInfoProvider.ThrustResourceName);
        var pts = (ResourceInfo.PriorityTankSets)PtsField.GetValue(info);
        var wrap = VirtualWrap.Instance;

        // If we don't do this then thrust will continue to accumulate forever,
        // eventually leading to floating point imprecision.
        SetAmount(info, 0.0);
        wrap.amount = info.Amount;

        pts.Add(wrap, 0);
    }

    public class VirtualWrap : ResourceInfo.Wrap
    {
        public static VirtualWrap Instance = new();

        public override double amount { get; set; }
        public override double maxAmount
        {
            get => double.PositiveInfinity;
            set { }
        }

        public override void Reset()
        {
            amount = 0.0;
        }
    }
}
