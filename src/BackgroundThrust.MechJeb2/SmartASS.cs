using System;
using System.Reflection;
using MuMech;
using UnityEngine;

namespace BackgroundThrust.MechJeb2;

public class SmartASS : TargetHeadingProvider
{
    static readonly Quaternion FrameShift = Quaternion.Euler(90f, 0f, 0f);

    MechJebModuleAttitudeController controller;

    [KSPField(isPersistant = true)]
    public AttitudeReference Attitude = AttitudeReference.INERTIAL;

    [KSPField(isPersistant = true)]
    public Quaternion AttitudeTarget;

    ManeuverNode Node
    {
        get
        {
            var nodes = Vessel?.patchedConicSolver?.maneuverNodes;
            if (nodes is null)
                return null;
            if (nodes.Count == 0)
                return null;
            return nodes[0];
        }
    }

    public ITargetable Target => Vessel.targetObject;

    public SmartASS() { }

    public override TargetHeading GetTargetHeading(double UT)
    {
        var ac = GetController();
        if (ac is not null)
        {
            if (!AccessUtils.GetComputerModuleEnabled(ac))
            {
                var module = Vessel.GetBackgroundThrust();
                module.SetTargetHeading(null);
            }

            Attitude = ac.attitudeReference;
            AttitudeTarget = ac.attitudeTarget;
        }

        var target = GetReferenceRotation() * AttitudeTarget * FrameShift;
        return new(target);
    }

    public override void OnInstalled()
    {
        var ac = GetController();
        if (ac is not null)
        {
            Attitude = ac.attitudeReference;
            AttitudeTarget = ac.attitudeTarget;
        }
    }

    protected override void OnSave(ConfigNode node)
    {
        base.OnSave(node);
    }

    MechJebModuleAttitudeController GetController()
    {
        if (!Vessel.loaded)
            return null;
        return controller ??= AccessUtils.GetAttitudeController(Vessel.GetMasterMechJeb());
    }

    Quaternion GetReferenceRotation()
    {
        var module = Vessel.GetBackgroundThrust();
        var transform = Vessel.ReferenceTransform;
        ManeuverNode node;
        Vector3 fwd;
        Vector3 up;

        Vector3d vesselUp = (Vessel.CoMD - Vessel.mainBody.position).normalized;
        Vector3d vesselFwd = transform.up;

        Vector3d thrust = module.Thrust.normalized;
        if (thrust == Vector3d.zero)
            thrust = vesselFwd;

        switch (Attitude)
        {
            case AttitudeReference.INERTIAL:
                return Quaternion.identity;

            case AttitudeReference.INERTIAL_COT:
                return Quaternion.FromToRotation(thrust, vesselFwd);

            case AttitudeReference.ORBIT:
                return Quaternion.LookRotation(Vessel.obt_velocity, vesselUp);

            case AttitudeReference.ORBIT_HORIZONTAL:
                return Quaternion.LookRotation(
                    Vector3d.Exclude(vesselUp, Vessel.obt_velocity.normalized),
                    transform.up
                );

            case AttitudeReference.SURFACE_NORTH:
                return GetSurfaceNorthRotation();

            case AttitudeReference.SURFACE_NORTH_COT:
                return GetSurfaceNorthRotation() * Quaternion.FromToRotation(thrust, vesselFwd);

            case AttitudeReference.SURFACE_VELOCITY:
                return Quaternion.LookRotation(GetSurfaceVelocity().normalized, vesselUp);

            case AttitudeReference.TARGET:
                if (Target is null)
                    return Quaternion.identity;

                fwd = (Target.GetTransform().position - Vessel.GetTransform().position).normalized;
                up = Vector3d.Cross(fwd, GetOrbitNormal());

                Vector3.OrthoNormalize(ref fwd, ref up);
                return Quaternion.LookRotation(fwd, up);

            case AttitudeReference.RELATIVE_VELOCITY:
                if (Target is null)
                    return Quaternion.identity;

                fwd = (Vessel.GetObtVelocity() - Target.GetObtVelocity()).normalized;
                up = Vector3d.Cross(fwd, GetOrbitNormal());

                Vector3.OrthoNormalize(ref fwd, ref up);
                return Quaternion.LookRotation(fwd, up);

            case AttitudeReference.TARGET_ORIENTATION:
                if (Target is null)
                    return Quaternion.identity;

                var tgt = Target.GetTransform();
                if (Target.GetTargetingMode() == VesselTargetModes.DirectionVelocityAndOrientation)
                    return Quaternion.LookRotation(tgt.forward, tgt.up);
                else
                    return Quaternion.LookRotation(tgt.up, tgt.forward);

            case AttitudeReference.MANEUVER_NODE:
                node = Node;
                if (node is null)
                    return Quaternion.identity;

                fwd = node.GetBurnVector(Vessel.orbit);
                up = Vector3d.Cross(fwd, GetOrbitNormal());

                Vector3.OrthoNormalize(ref fwd, ref up);
                return Quaternion.LookRotation(fwd, up);

            case AttitudeReference.MANEUVER_NODE_COT:
                node = Node;
                if (node is null)
                    return Quaternion.identity;

                fwd = node.GetBurnVector(Vessel.orbit);
                up = Vector3d.Cross(fwd, GetOrbitNormal());

                Vector3.OrthoNormalize(ref fwd, ref up);
                return Quaternion.LookRotation(thrust, vesselFwd)
                    * Quaternion.LookRotation(fwd, up);

            case AttitudeReference.SUN:
                var baseOrbit =
                    Vessel.mainBody == Planetarium.fetch.Sun
                        ? Vessel.orbit
                        : Vessel.orbit.TopParentOrbit();

                up = Vessel.CoMD - Planetarium.fetch.Sun.position;
                fwd = Vector3d.Cross(-baseOrbit.GetOrbitNormal().xzy.normalized, up);
                return Quaternion.LookRotation(fwd, up);

            case AttitudeReference.SURFACE_HORIZONTAL:
                return Quaternion.LookRotation(
                    Vector3d.Exclude(vesselUp, GetSurfaceVelocity()),
                    vesselUp
                );
        }

        return Quaternion.identity;
    }

    Quaternion GetSurfaceNorthRotation() =>
        Quaternion.LookRotation(Vessel.north, Vessel.CoMD - Vessel.mainBody.position);

    Vector3d GetSurfaceVelocity() => Vessel.obt_velocity - Vessel.mainBody.getRFrmVel(Vessel.CoMD);

    Vector3d GetOrbitNormal()
    {
        var up = (Vessel.CoMD - Vessel.mainBody.position).normalized;
        var radial = Vector3d.Exclude(Vessel.obt_velocity, up).normalized;
        return Vector3d.Cross(radial, Vessel.obt_velocity.normalized);
    }
}
