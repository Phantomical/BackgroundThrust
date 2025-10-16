using static Vessel;

namespace BackgroundThrust.Utils;

internal static class VesselExt
{
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
