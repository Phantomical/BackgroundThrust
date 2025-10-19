using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RealFuels.Ullage;
using UnityEngine;

namespace BackgroundThrust.RealFuels.Patches;

[HarmonyPatch(typeof(UllageModule), nameof(UllageModule.FixedUpdate))]
internal static class UllageModule_FixedUpdate_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator gen
    )
    {
        var geeForceMethod = SymbolExtensions.GetMethodInfo(() =>
            FlightGlobals.getGeeForceAtPosition(default)
        );
        var tanksField = AccessTools.Field(typeof(UllageModule), "tanks");

        var matcher = new CodeMatcher(instructions, gen);

        // First we need to determine what local index `accel` is stored at.
        // We do this by looking at what variable the return value of
        // FlightGlobals.getGeeForceAtPosition gets stored in.
        matcher
            .MatchStartForward(new CodeMatch(OpCodes.Call, geeForceMethod))
            .ThrowIfInvalid("Unable to find call to FlightGlobals.getGeeForceAtPosition")
            .MatchStartForward(new CodeMatch(IsStloc))
            .ThrowIfInvalid("Unable to find subsequent store");

        var loc = GetStlocLocation(matcher.Instruction);

        // Now we need to inject a call to UpdateAcceleration somewhere in the
        // middle of the method after accel has been set to zero.
        //
        // We do this just before the method reads the UllageModule.tanks field.

        Vector3 dummy = default;

        matcher
            .MatchStartForward(new CodeMatch(OpCodes.Ldfld, tanksField))
            .ThrowIfInvalid("Unable to find ldfld UllageModule.tanks instruction")
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Ldloca, loc),
                new CodeInstruction(
                    OpCodes.Call,
                    SymbolExtensions.GetMethodInfo(() => UpdateAcceleration(null, ref dummy))
                )
            );

        return matcher.Instructions();
    }

    static bool IsStloc(CodeInstruction inst)
    {
        return inst.opcode == OpCodes.Stloc
            || inst.opcode == OpCodes.Stloc_S
            || inst.opcode == OpCodes.Stloc_0
            || inst.opcode == OpCodes.Stloc_1
            || inst.opcode == OpCodes.Stloc_2
            || inst.opcode == OpCodes.Stloc_3;
    }

    static int GetStlocLocation(CodeInstruction inst)
    {
        if (inst.opcode == OpCodes.Stloc_0)
            return 0;
        if (inst.opcode == OpCodes.Stloc_1)
            return 1;
        if (inst.opcode == OpCodes.Stloc_2)
            return 2;
        if (inst.opcode == OpCodes.Stloc_3)
            return 3;
        if (inst.opcode == OpCodes.Stloc_S)
            return (byte)inst.operand;
        if (inst.opcode == OpCodes.Stloc)
            return (int)inst.operand;

        throw new NotSupportedException("opcode was not a stloc variant");
    }

    static void UpdateAcceleration(UllageModule ullage, ref Vector3 accel)
    {
        var vessel = ullage.Vessel;
        if (!vessel.packed || !vessel.loaded)
            return;

        var module = vessel.GetBackgroundThrust();
        accel += module.Thrust.xzy / vessel.totalMass;
    }
}
