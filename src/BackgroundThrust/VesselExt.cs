using static Vessel;

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

    internal static bool IsOrbiting(this Vessel vessel)
    {
        return vessel.situation switch
        {
            Situations.ORBITING => true,
            Situations.ESCAPING => true,
            Situations.SUB_ORBITAL => true,
            _ => false,
        };
    }
}
