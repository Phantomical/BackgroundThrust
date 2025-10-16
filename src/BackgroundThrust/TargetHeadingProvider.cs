using System.Collections.Generic;
using System.Linq;
using BackgroundThrust.Utils;

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
    ///   A vector representing the target thrust direction, or <c>null</c> if
    ///   the vessel should stop thrust.
    /// </returns>
    public abstract Vector3d? GetTargetHeading(double UT);

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
        var vessel = module.Vessel;
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

        vessel.orbit.Perturb(deltaV * heading, parameters.StopUT);
    }

    public static TargetHeadingProvider Load(Vessel vessel, ConfigNode node) =>
        Load(node, provider => provider.Vessel = vessel);

    internal static new void RegisterAll() =>
        DynamicallySerializable<TargetHeadingProvider>.RegisterAll();

    private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}
