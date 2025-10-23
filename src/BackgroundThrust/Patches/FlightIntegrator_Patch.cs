using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace BackgroundThrust.Patches;

[HarmonyPatch(typeof(FlightIntegrator), "FixedUpdate")]
internal static class FlightIntegrator_FixedUpdate_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator gen
    )
    {
        var packed = GetFieldInfo((Part part) => part.packed);
        var integrate = SymbolExtensions.GetMethodInfo(() => IntegratePacked(null));

        var matcher = new CodeMatcher(instructions, gen);
        matcher
            .MatchStartForward(new CodeMatch(OpCodes.Ldfld, packed))
            .ThrowIfInvalid("Unable to find field access to Part.packed")
            .MatchStartForward(new CodeMatch(OpCodes.Brfalse_S))
            .ThrowIfInvalid("Unable to find subsequent brfalse instruction")
            .Advance(1)
            .Insert(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Call, integrate),
                new CodeMatch(OpCodes.Ret)
            );

        return matcher.Instructions();
    }

    static void IntegratePacked(FlightIntegrator fi)
    {
        var vessel = fi.Vessel;
        var thrust = Vector3d.zero;

        foreach (var part in vessel.parts)
        {
            thrust += part.force;
            foreach (var force in part.forces)
                thrust += force.force;

            part.force.Zero();
            part.torque.Zero();
            part.forces.Clear();
        }

        var module = vessel.GetBackgroundThrust();
        if (module is null)
            return;

        module.Thrust = thrust;
    }

    static FieldInfo GetFieldInfo(Expression expr)
    {
        if (expr is not LambdaExpression lambda)
            throw new ArgumentException("expected a lambda expression");

        if (lambda.Body is not MemberExpression member)
            throw new ArgumentException("expected a member access");

        if (member.Member is not FieldInfo field)
            throw new ArgumentException("expected a field access");

        return field;
    }
}
