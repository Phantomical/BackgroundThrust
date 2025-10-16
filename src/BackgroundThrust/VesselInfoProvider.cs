namespace BackgroundThrust;

public abstract class VesselInfoProvider
{
    /// <summary>
    /// Whether this info provider works in the background. If <c>false</c>
    /// then the vessel will not allow thrust in the background.
    /// </summary>
    public abstract bool AllowBackground { get; }

    /// <summary>
    /// Whether the <see cref="BackgroundThrust"/> module should disable itself
    /// if <see cref="GetVesselThrust"/> returns 0.
    /// </summary>
    ///
    /// <remarks>
    /// This is generally more efficient but may not be compatible with all
    /// background processing implementations.
    /// </remarks>
    public virtual bool DisableOnZeroThrustInBackground => true;

    /// <summary>
    /// Get the current mass of the vessel.
    /// </summary>
    /// <param name="module"></param>
    /// <param name="UT">The current UT.</param>
    /// <returns></returns>
    ///
    /// <remarks>
    /// <paramref name="UT"/> will always be equivalent to
    /// <see cref="Planetarium.GetUniversalTime"/> when the vessel is loaded
    /// but may vary somewhat when processing unloaded vessels.
    /// </remarks>
    public abstract double GetVesselMass(BackgroundThrustVessel module, double UT);

    /// <summary>
    /// Get the current thrust of the vessel.
    /// </summary>
    /// <param name="module"></param>
    /// <param name="UT">The current UT.</param>
    /// <returns></returns>
    ///
    /// <remarks>
    /// <para>
    /// The implementation of this method only needs to handle packed and
    /// unloaded vessels. It will never be called when the vessel is unpacked.
    /// You can ignore any other source of thrust besides that produced by
    /// <see cref="BackgroundEngine"/> modules.
    /// </para>
    ///
    /// <para>
    /// <paramref name="UT"/> will always be equivalent to
    /// <see cref="Planetarium.GetUniversalTime"/> when the vessel is loaded
    /// but may vary somewhat when processing unloaded vessels.
    /// </para>
    /// </remarks>
    public abstract Vector3d GetVesselThrust(BackgroundThrustVessel module, double UT);
}

public class StockVesselInfoProvider : VesselInfoProvider
{
    public override bool AllowBackground => false;

    public override double GetVesselMass(BackgroundThrustVessel module, double UT) =>
        module.Vessel.totalMass;

    public override Vector3d GetVesselThrust(BackgroundThrustVessel module, double UT)
    {
        Vector3d thrust = Vector3d.zero;
        foreach (var engine in module.Engines)
            thrust += engine.Thrust;
        return thrust;
    }
}
