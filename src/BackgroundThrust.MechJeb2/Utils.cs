using System;
using System.Linq.Expressions;
using System.Reflection;
using HarmonyLib;
using MuMech;
using UnityEngine;

namespace BackgroundThrust.MechJeb2;

// Accessors for various mechjeb fields that have changed with devjeb being
// promoted to a CKAN release.
internal static class AccessUtils
{
    static readonly Func<MechJebCore, MechJebModuleAttitudeController> AttitudeControllerFunc =
        MakeMultiAttemptAccessor<MechJebCore, MechJebModuleAttitudeController>(
            "attitude", // <= 2.14.3
            "Attitude" // >= 2.15
        );

    static readonly Func<ComputerModule, bool> ComputerModuleEnabledFunc = MakeMultiAttemptAccessor<
        ComputerModule,
        bool
    >(
        "enabled", // <= 2.14.3
        "Enabled" // >= 2.15
    );

    static readonly Func<MechJebModuleAttitudeController, Quaternion> ControllerTargetAttitudeFunc =
        MakeTargetAttitudeFunc();

    static readonly Func<ComputerModule, Vessel> ComputerModuleVesselFunc =
        MakeMultiAttemptAccessor<ComputerModule, Vessel>(
            "vessel", // <= 2.14.3
            "Vessel" // 2.15
        );

    public static MechJebModuleAttitudeController GetAttitudeController(MechJebCore mechjeb)
    {
        if (mechjeb is null)
            return null;

        return AttitudeControllerFunc(mechjeb);
    }

    public static Quaternion GetControllerAttitudeTarget(
        MechJebModuleAttitudeController controller
    ) => ControllerTargetAttitudeFunc(controller);

    public static bool GetComputerModuleEnabled(ComputerModule module) =>
        ComputerModuleEnabledFunc(module);

    public static Vessel GetComputerModuleVessel(ComputerModule module) =>
        ComputerModuleVesselFunc(module);

    static Func<T, F> MakeMultiAttemptAccessor<T, F>(params string[] members)
    {
        MemberInfo member = null;
        foreach (var name in members)
        {
            member = GetFieldOrProperty(typeof(T), name);
            if (member is not null)
                break;
        }

        if (member is null)
            throw new Exception(
                $"Type {typeof(T).Name} does not have any of the requested fields or properties"
            );

        var memty = member switch
        {
            PropertyInfo property => property.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new Exception($"{member.Name} was not a field or property"),
        };
        if (!typeof(F).IsAssignableFrom(memty))
            throw new Exception($"Member type {memty.Name} cannot be assigned to {typeof(F).Name}");

        var param = Expression.Parameter(typeof(T));
        var lambda = Expression.Lambda<Func<T, F>>(
            Expression.MakeMemberAccess(param, member),
            param
        );

        return lambda.Compile();
    }

    static Quaternion ConvertQuaternionD(QuaternionD quat) => quat;

    static Func<MechJebModuleAttitudeController, Quaternion> MakeTargetAttitudeFunc()
    {
        var property = typeof(MechJebModuleAttitudeController).GetProperty(
            nameof(MechJebModuleAttitudeController.attitudeTarget)
        );

        var param = Expression.Parameter(typeof(MechJebModuleAttitudeController));
        Expression target = Expression.MakeMemberAccess(param, property);

        if (target.Type == typeof(QuaternionD))
        {
            target = Expression.Call(
                null,
                SymbolExtensions.GetMethodInfo(() => ConvertQuaternionD(default)),
                target
            );
        }

        return Expression
            .Lambda<Func<MechJebModuleAttitudeController, Quaternion>>(target, [param])
            .Compile();
    }

    static MemberInfo GetFieldOrProperty(Type type, string name) =>
        (MemberInfo)type.GetField(name) ?? type.GetProperty(name);
}
