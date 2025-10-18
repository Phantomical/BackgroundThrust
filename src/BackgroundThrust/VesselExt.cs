namespace BackgroundThrust;

public static class VesselExt
{
    /// <summary>
    /// Get a the active <see cref="BackgroundThrustVessel"/> module for a vessel.
    /// This is somewhat faster than using <c>FindVesselModuleImplementing</c>.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static BackgroundThrustVessel GetBackgroundThrust(this Vessel v)
    {
        if (EventDispatcher.Instance is not null)
            return EventDispatcher.Instance.GetVesselModule(v);

        return v.FindVesselModuleImplementing<BackgroundThrustVessel>();
    }
}
