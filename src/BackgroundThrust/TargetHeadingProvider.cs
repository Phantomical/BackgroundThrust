using BackgroundThrust.Utils;

namespace BackgroundThrust;

public struct ThrustParameters
{
    /// <summary>
    /// The starting UT for thrust integration.
    /// </summary>
    public double StartUT;

    /// <summary>
    /// The ending UT for thrust integration.
    /// </summary>
    public double StopUT;

    /// <summary>
    /// The vessel mass at <see cref="StartUT"/>.
    /// </summary>
    public double StartMass;

    /// <summary>
    /// The vessel mass at <see cref="StopUT"/>.
    /// </summary>
    public double StopMass;

    /// <summary>
    /// The thrust emitted by the vessel between <see cref="StartUT"/>
    /// and <see cref="StopUT"/>.
    /// </summary>
    public double Thrust;

    public readonly double DeltaT => StopUT - StartUT;
}

/// <summary>
/// A targetting module for a vessel. This controls in what direction the vessel
/// should orient itself and, optionally, allows overriding how applied thrust
/// is integrated into the orbit.
/// </summary>
public abstract class TargetHeadingProvider : DynamicallySerializable<TargetHeadingProvider>
{
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

    public virtual void IntegrateThrust(BackgroundThrustVessel vessel, ThrustParameters parameters)
    {
        // throw new NotImplementedException();
    }

    public static TargetHeadingProvider Load(ConfigNode node)
    {
        return (TargetHeadingProvider)DynamicallySerializable<TargetHeadingProvider>.Load(node);
    }
}
