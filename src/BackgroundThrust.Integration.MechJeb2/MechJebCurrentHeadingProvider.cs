using MuMech;

namespace BackgroundThrust.Integration.MechJeb2;

public class MechJebCurrentHeadingProvider : ICurrentHeadingProvider
{
    public TargetHeadingProvider GetCurrentHeading(BackgroundThrustVessel module)
    {
        var vessel = module.Vessel;
        var mechJeb = vessel.GetMasterMechJeb();
        if (mechJeb == null)
            return null;

        var attitude = mechJeb.attitude;
        if (!attitude.enabled)
            return null;

        return null;
    }
}
