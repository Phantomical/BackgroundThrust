namespace BackgroundThrust;

/// <summary>
/// An interface to provide information about a vessel in the background.
/// It is only called for unloaded vessels.
/// </summary>
public abstract class VesselInfoProvider
{
    /// <summary>
    /// Whether the <see cref="BackgroundThrust"/> module should disable itself
    /// if <see cref="GetVesselThrust"/> returns 0.
    /// </summary>
    ///
    /// <remarks>
    /// This is generally more efficient but may not be compatible with all
    /// background processing implementations.
    /// </remarks>
    public virtual bool DisableOnZeroThrust => true;

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
    public abstract double GetVesselThrust(BackgroundThrustVessel module, double UT);
}
