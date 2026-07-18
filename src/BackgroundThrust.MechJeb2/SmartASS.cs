using System;
using System.Reflection;
using MuMech;
using UnityEngine;

namespace BackgroundThrust.MechJeb2;

public class SmartASS : TargetHeadingProvider
{
    static readonly Quaternion FrameShift = Quaternion.Euler(90f, 0f, 0f);

    /// <summary>
    /// How far past the 90 degree boundary the thrust axis has to be before we
    /// change our mind about compensating for it. See <see cref="GetThrustForward"/>.
    /// </summary>
    const double BailOutThreshold = 1e-3;

    MechJebModuleAttitudeController controller;

    /// <summary>
    /// Whether we are currently giving up on the CoT correction. Derived state,
    /// deliberately not persisted - it re-latches on the first update after load.
    /// </summary>
    bool cotBailedOut;

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
            AttitudeTarget = AccessUtils.GetControllerAttitudeTarget(ac);
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
            AttitudeTarget = AccessUtils.GetControllerAttitudeTarget(ac);
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
        var transform = Vessel.ReferenceTransform;
        ManeuverNode node;
        Vector3 fwd;
        Vector3 up;

        Vector3d vesselUp = (Vessel.CoMD - Vessel.mainBody.position).normalized;
        Vector3d vesselFwd = transform.up;

        switch (Attitude)
        {
            case AttitudeReference.INERTIAL:
                return Quaternion.identity;

            case AttitudeReference.INERTIAL_COT:
                return GetCoTCorrection(vesselFwd);

            case AttitudeReference.ORBIT:
                return Quaternion.LookRotation(Vessel.obt_velocity, vesselUp);

            case AttitudeReference.ORBIT_HORIZONTAL:
                return Quaternion.LookRotation(
                    Vector3d.Exclude(vesselUp, Vessel.obt_velocity.normalized),
                    vesselUp
                );

            case AttitudeReference.SURFACE_NORTH:
                return GetSurfaceNorthRotation();

            case AttitudeReference.SURFACE_NORTH_COT:
                return GetCoTCorrection(vesselFwd) * GetSurfaceNorthRotation();

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
                    return Quaternion.LookRotation(tgt.up, tgt.right);

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
                return GetCoTCorrection(vesselFwd) * Quaternion.LookRotation(fwd, up);

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

    /// <summary>
    /// The rotation taking the thrust axis onto the control axis, as MechJeb
    /// composes it into the <c>*_COT</c> reference frames.
    /// </summary>
    Quaternion GetCoTCorrection(Vector3d vesselFwd) =>
        Quaternion.FromToRotation(GetThrustForward(vesselFwd), vesselFwd);

    /// <summary>
    /// Replicates MechJeb's <c>VesselState.thrustForward</c>, which is what the
    /// <c>*_COT</c> reference frames are built against.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// This is deliberately not the real thrust vector. MechJeb approximates the
    /// thrust axis geometrically, as the direction from the thrust-weighted centre
    /// of thrust towards the CoM, and then gives up entirely if that ends up more
    /// than 90 degrees away from the control axis. Using the true thrust vector
    /// instead makes us disagree with MechJeb by the full off-axis angle on craft
    /// with a sideways control point, which shows up as the vessel snapping to a
    /// different attitude the moment it goes on rails.
    /// </para>
    ///
    /// <para>
    /// We recompute this from live parts rather than reading
    /// <c>VesselState.thrustForward</c> because <c>VesselState.Update</c> bails out
    /// when <c>rootPart.rb</c> is null - which is exactly the packed case - so every
    /// field on it is stale for as long as we are the ones flying the vessel.
    /// </para>
    ///
    /// <para>
    /// Both vectors must come from the same instant. Pairing a thrust vector
    /// sampled in <c>FlightIntegrator.FixedUpdate</c> with a control axis read after
    /// <c>SetRotation</c> folds the frame-to-frame rotation delta into the
    /// correction, which then feeds back into the next target and diverges.
    /// </para>
    /// </remarks>
    Vector3d GetThrustForward(Vector3d vesselFwd)
    {
        // An unloaded vessel has no meaningful thrust transforms to average.
        if (!Vessel.loaded)
            return vesselFwd;

        Vector3d cot = Vector3d.zero;
        double scalar = 0.0;

        foreach (var engine in Vessel.FindPartModulesImplementing<ModuleEngines>())
        {
            if (!engine.EngineIgnited || !engine.isEnabled || !engine.isOperational)
                continue;

            for (int i = 0; i < engine.thrustTransforms.Count; ++i)
            {
                var weight = engine.finalThrust * engine.thrustTransformMultipliers[i];

                cot += weight * (Vector3d)engine.thrustTransforms[i].position;
                scalar += weight;
            }
        }

        // MechJeb leaves thrustForward at zero here, which makes its FromToRotation
        // an identity. Returning the control axis does the same thing.
        if (scalar <= 0.0)
            return vesselFwd;

        var thrustForward = (Vessel.CoMD - cot / scalar).normalized;
        var dot = Vector3d.Dot(thrustForward, vesselFwd);

        // MechJeb gives up past 90 degrees. Match that or we diverge from it.
        //
        // It compares against exactly zero, which is fine until the control point
        // is perpendicular to the thrust axis - a docking port mounted on the side
        // is the usual way to get there. The dot product is then mathematically
        // zero, so its sign is decided purely by rounding, and we cannot reproduce
        // MechJeb's rounding: it accumulates the centre of thrust in a frame offset
        // from ours by the floating origin (measured 34.6m apart), so the two
        // implementations land on opposite sides of zero. On the test craft MechJeb
        // got +2.6e-8 and we got -9.5e-9 from vectors that agree to six decimals.
        // A literal `< 0` would have us skip the correction while MechJeb applies a
        // full 90 degrees.
        //
        // So only bail when we are clearly past the boundary, and hysteresize the
        // decision. A vessel parked near 90 degrees must not be able to alternate
        // between identity and a 90 degree correction: MechJeb would merely wobble,
        // since its PID has a bounded slew rate, but we drive SetRotation, so it
        // would teleport and trip the oscillation guard in BackgroundThrustVessel,
        // which drops us to a fixed heading permanently.
        if (cotBailedOut ? dot < BailOutThreshold : dot < -BailOutThreshold)
        {
            cotBailedOut = true;
            return vesselFwd;
        }

        cotBailedOut = false;
        return thrustForward;
    }

    Quaternion GetSurfaceNorthRotation() =>
        Quaternion.LookRotation(Vessel.north, Vessel.CoMD - Vessel.mainBody.position);

    Vector3d GetSurfaceVelocity() => Vessel.obt_velocity - Vessel.mainBody.getRFrmVel(Vessel.CoMD);

    Vector3d GetOrbitNormal()
    {
        var up = (Vessel.CoMD - Vessel.mainBody.position).normalized;
        var radial = Vector3d.Exclude(Vessel.obt_velocity, up).normalized;
        // Negated to match MechJeb's VesselState.normalPlus, which is what the attitude
        // reference frames below are built against.
        return -Vector3d.Cross(radial, Vessel.obt_velocity.normalized);
    }
}
