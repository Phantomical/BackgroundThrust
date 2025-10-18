using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BackgroundThrust.Utils;
using HarmonyLib;
using PreFlightTests;

namespace BackgroundThrust.Patches;

[HarmonyPatch]
internal static class ModuleEngines_Patch
{
    const BindingFlags Instance =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static void EmptyMethod() { }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ModuleEngines_Patch), nameof(EmptyMethod))]
    public static float ApplyThrottleAdjustments(ModuleEngines _, float throttle)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var method = typeof(ModuleEngines).GetMethod(
                nameof(ApplyThrottleAdjustments),
                Instance
            );

            // We don't actually care about the original instructions. Instead,
            // we are just creating a wrapper that calls a private method
            // without the overhead of reflection.
            return CallMethod(method);
        }

        Transpiler(null);

        throw new NotImplementedException();
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ModuleEngines_Patch), nameof(EmptyMethod))]
    public static void UpdatePropellantStatus(ModuleEngines _, bool doGauge = true)
    {
        IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator gen
        )
        {
            var method = typeof(ModuleEngines).GetMethod(nameof(UpdatePropellantStatus), Instance);

            // We don't actually care about the original instructions. Instead,
            // we are just creating a wrapper that calls a private method
            // without the overhead of reflection.
            return CallMethod(method);
        }

        Transpiler(null, null);

        throw new NotImplementedException();
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ModuleEngines_Patch), nameof(EmptyMethod))]
    public static double RequiredPropellantMass(ModuleEngines _, float throttleAmount)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var method = typeof(ModuleEngines).GetMethod(nameof(RequiredPropellantMass), Instance);

            // We don't actually care about the original instructions. Instead,
            // we are just creating a wrapper that calls a private method
            // without the overhead of reflection.
            return CallMethod(method);
        }

        Transpiler(null);

        throw new NotImplementedException();
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ModuleEngines_Patch), nameof(EmptyMethod))]
    public static void ThrustUpdate(ModuleEngines _)
    {
        IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var method = typeof(ModuleEngines).GetMethod(nameof(ThrustUpdate), Instance);

            // We don't actually care about the original instructions. Instead,
            // we are just creating a wrapper that calls a private method
            // without the overhead of reflection.
            return CallMethod(method);
        }

        Transpiler(null);

        throw new NotImplementedException();
    }

    private static List<CodeInstruction> CallMethod(MethodBase method)
    {
        int pcount = method.GetParameters().Length;
        if (!method.IsStatic)
            pcount += 1;

        var insts = new List<CodeInstruction>(pcount + 2);
        for (int i = 0; i < pcount; ++i)
        {
            insts.Add(
                i switch
                {
                    0 => new CodeInstruction(OpCodes.Ldarg_0),
                    1 => new CodeInstruction(OpCodes.Ldarg_1),
                    2 => new CodeInstruction(OpCodes.Ldarg_2),
                    3 => new CodeInstruction(OpCodes.Ldarg_3),
                    _ => i <= 255
                        ? new CodeInstruction(OpCodes.Ldarg_S, (byte)i)
                        : new CodeInstruction(OpCodes.Ldarg, i),
                }
            );
        }

        if (method.IsVirtual)
            insts.Add(new CodeInstruction(OpCodes.Callvirt, method));
        else
            insts.Add(new CodeInstruction(OpCodes.Call, method));

        insts.Add(new CodeInstruction(OpCodes.Ret));

        return insts;
    }
}

[HarmonyPatch(typeof(ModuleEngines), "TimeWarping")]
internal static class ModuleEngines_TimeWarping_Patch
{
    static bool Prefix(ref bool __result)
    {
        // We actually want to run the vessel in warp. But only as part of
        // BackgroundThrust.PackedUpdate, not for regular FixedUpdate calls.
        if (BackgroundEngine.InPackedUpdate)
        {
            __result = false;
            return false;
        }

        return true;
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var deactivateLoopingFxMethod = SymbolExtensions.GetMethodInfo<ModuleEngines>(engines =>
            engines.DeactivateLoopingFX()
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

                    return method == deactivateLoopingFxMethod;
                })
            )
            .ThrowIfInvalid("Unable to find call to DeactivateLoopingFX()")
            .RemoveInstruction()
            .Insert(
                new CodeInstruction(
                    OpCodes.Call,
                    SymbolExtensions.GetMethodInfo(() => MaybeDeactivateLoopingFX(null))
                )
            );

        return matcher.Instructions();
    }

    static bool HasEnabledBackgroundEngine(ModuleEngines module)
    {
        if (!module.part.packed)
            return false;

        var engine = module.part.FindModuleImplementing<BackgroundEngine>();
        if (engine is null)
            return false;

        if (!engine.IsEnabled)
            return false;

        return true;
    }

    static void MaybeDeactivateLoopingFX(ModuleEngines module)
    {
        if (!HasEnabledBackgroundEngine(module))
            module.DeactivateLoopingFX();
    }
}

[HarmonyPatch(typeof(ModuleEngines), "ThrustUpdate")]
internal static class ModuleEngines_ThrustUpdate_Patch
{
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
                SymbolExtensions.GetMethodInfo(() => AddForceAtPosition(null, default, default))
            );

        return matcher.Instructions();
    }

    internal static void AddForceAtPosition(Part part, Vector3d force, Vector3d pos)
    {
        if (BackgroundEngine.InPackedUpdate)
            BackgroundEngine.ThrustAccumulator += force;
        else
            part.AddForceAtPosition(force, pos);
    }
}
