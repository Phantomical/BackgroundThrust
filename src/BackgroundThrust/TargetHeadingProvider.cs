using System;
using System.CodeDom;
using System.Numerics;
using BackgroundThrust.Utils;
using UnityEngine;
using static BackgroundThrust.Utils.MathUtil;

namespace BackgroundThrust;

/// <summary>
/// A targetting module for a vessel. This controls in what direction the vessel
/// should orient itself and, optionally, allows overriding how applied thrust
/// is integrated into the orbit.
/// </summary>
///
/// <remarks>
/// <para>
/// There are some things you need to keep in mind when writing a new heading
/// provider:
/// <list type="number">
///  <item>
///   It must have a zero-argument constructor. The deserialization code will
///   use the default constructor and then call <c>OnLoad</c>.
///  </item>
///  <item>
///   If <see cref="GetTargetHeading"/> returns <c>null</c> then the vessel
///   will cut thrust and remove the target heading, as well as printing a
///   message to the screen about the maneuver being complete.
///  </item>
/// </list>
/// </para>
///
/// <para>
/// If you have a planned trajectory that involves multiple different burns
/// then you will need to create and add a new <see cref="TargetHeadingProvider"/>
/// at the start of each individual burn in the sequence.
/// </para>
/// </remarks>
public abstract class TargetHeadingProvider : DynamicallySerializable<TargetHeadingProvider>
{
    /// <summary>
    /// The vessel that this heading provider is controlling.
    /// </summary>
    public Vessel Vessel;

    /// <summary>
    /// Get the current target heading for the vessel.
    /// </summary>
    /// <param name="UT">
    ///   The current universal time. May not necessarily be equal to
    ///   <c><see cref="Planetarium.GetUniversalTime"/>()</c>.
    /// </param>
    /// <returns>
    ///   A vector representing the target thrust direction.
    /// </returns>
    public abstract TargetHeading GetTargetHeading(double UT);

    /// <summary>
    /// Update the orbit of the current vessel to account for the change in
    /// velocity of the vessel.
    /// </summary>
    /// <param name="module"></param>
    /// <param name="parameters"></param>
    ///
    /// <remarks>
    /// <para>
    /// This provides an extension point for heading providers that have
    /// special knowledge of how the heading vector will change over time to
    /// apply a custom change to the orbit that takes that into account.
    /// </para>
    ///
    /// <para>
    /// The default implementation effectively applies all the thrust at the
    /// current UT.
    /// </para>
    /// </remarks>
    public virtual void IntegrateThrust(BackgroundThrustVessel module, ThrustParameters parameters)
    {
        var heading = module.Heading;

        var deltaV = parameters.ComputeDeltaV();
        if (!IsFinite(deltaV))
        {
            LogUtil.Error("deltaV was infinite or NaN");
            return;
        }

        var mag2 = heading.sqrMagnitude;
        if (mag2 == 0.0 || !IsFinite(mag2))
            return;

        Vessel.orbit.Perturb(deltaV * heading, parameters.StopUT);
    }

    public static TargetHeadingProvider Load(Vessel vessel, ConfigNode node) =>
        Load(node, provider => provider.Vessel = vessel);

    internal static new void RegisterAll() =>
        DynamicallySerializable<TargetHeadingProvider>.RegisterAll();
}

public struct TargetHeading(QuaternionD orientation)
{
    public static readonly TargetHeading Invalid = default;

    /// <summary>
    /// The orientation of the vessel. An identity quaternion is equivalent to
    /// forward laying on the X axis and up on the Y axis.
    ///
    /// Note that the heading vector of the ship is <c>transform.up</c>, not
    /// <c>transform.forward</c>.
    /// </summary>
    public QuaternionD Orientation = orientation;

    public TargetHeading(Transform transform)
        : this(transform.rotation) { }

    public TargetHeading(Vector3d forward)
        : this(forward, Vector3d.up) { }

    public TargetHeading(Vector3d forward, Vessel vessel)
        : this(forward, vessel.ReferenceTransform?.up ?? Vector3d.up) { }

    public TargetHeading(Vector3d forward, Vector3d up)
        : this(QuaternionD.LookRotation(forward, up)) { }

    public readonly bool IsValid()
    {
        var q = Orientation;
        var mag2 = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;

        if (double.IsNaN(mag2) || double.IsInfinity(mag2))
            return false;
        if (Math.Abs(mag2) < 1e-4)
            return false;

        return true;
    }
}
