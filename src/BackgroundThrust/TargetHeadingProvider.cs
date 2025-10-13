using BackgroundThrust.Utils;

namespace BackgroundThrust;

/// <summary>
/// A targetting module for a vessel. This controls in what direction the vessel
/// should orient itself and, optionally, allows overriding how applied thrust
/// is integrated into the orbit.
/// </summary>
public abstract class TargetHeadingProvider : DynamicallySerializable<TargetHeadingProvider>
{
    public Vessel Vessel;

    /// <summary>
    /// Get the current target heading for the vessel.
    /// </summary>
    /// <param name="module">
    ///   The <see cref="BackgroundThrustVessel" /> that this heading provider
    ///   is attached to.
    /// </param>
    /// <param name="UT">
    ///   The current universal time. May not necessarily be equal to
    ///   <c><see cref="Planetarium.GetUniversalTime"/>()</c>.
    /// </param>
    /// <returns>
    ///   A vector representing the target thrust direction, or <c>null</c> if
    ///   the vessel should stop thrust.
    /// </returns>
    public abstract Vector3d? GetTargetHeading(BackgroundThrustVessel module, double UT);

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
        var heading = vessel.transform.up;

        var deltaV = parameters.ComputeDeltaV();
        if (double.IsNaN(deltaV) || double.IsInfinity(deltaV))
        {
            LogUtil.Error("deltaV was infinite or NaN");
            return;
        }

        if (heading.sqrMagnitude == 0.0)
            return;

        vessel.orbit.Perturb(deltaV * (Vector3d)heading, parameters.StopUT);
    }

    public static TargetHeadingProvider Load(Vessel vessel, ConfigNode node) =>
        Load(node, provider => provider.Vessel = vessel);
}
