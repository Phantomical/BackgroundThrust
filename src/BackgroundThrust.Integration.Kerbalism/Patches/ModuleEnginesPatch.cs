using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace BackgroundThrust.Integration.Kerbalism.Patches;

[HarmonyPatch]
internal static class ModuleEngines_Patch2
{
    const BindingFlags Instance =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    internal delegate double RequestPropellantDelegate(ModuleEngines engine, double mass);

    static double InvokeRequestPropellant(
        ModuleEngines engine,
        double mass,
        RequestPropellantDelegate func
    ) => func(engine, mass);

    static float CalculateThrustBase(
        ModuleEngines engine,
        RequestPropellantDelegate requestPropellant
    ) => 0f;

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ModuleEngines), nameof(ModuleEngines.RequestPropellant))]
    internal static float CalculateThrust(
        ModuleEngines engine,
        RequestPropellantDelegate requestPropellant
    )
    {
#pragma warning disable CS8321 // Local function is declared but never used
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var requestPropellantMethod = typeof(ModuleEngines).GetMethod(
                "RequestPropellant",
                Instance
            );
            var matcher = new CodeMatcher(instructions);

            matcher
                .MatchStartForward(
                    new CodeMatch(inst =>
                    {
                        if (inst.opcode != OpCodes.Call)
                            return false;

                        if (inst.operand is not MethodInfo method)
                            return false;

                        return method == requestPropellantMethod;
                    })
                )
                .ThrowIfInvalid("Unable to find call to RequestPropellant")
                .RemoveInstruction()
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_2),
                    new CodeInstruction(
                        OpCodes.Call,
                        SymbolExtensions.GetMethodInfo(() =>
                            InvokeRequestPropellant(null, 0.0, null)
                        )
                    )
                );

            return matcher.Instructions();
        }
#pragma warning restore CS8321 // Local function is declared but never used

        return 0f;
    }
}
