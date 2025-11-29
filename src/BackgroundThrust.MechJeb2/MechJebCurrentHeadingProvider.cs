using MuMech;

namespace BackgroundThrust.MechJeb2;

public class MechJebCurrentHeadingProvider : ICurrentHeadingProvider
{
    public TargetHeadingProvider GetCurrentHeading(BackgroundThrustVessel module)
    {
        var mechJeb = module.Vessel.GetMasterMechJeb();
        if (mechJeb == null)
            return null;

        var attitude = AccessUtils.GetAttitudeController(mechJeb);
        if (attitude is null)
            return null;
        if (!AccessUtils.GetComputerModuleEnabled(attitude))
            return null;

        return new SmartASS();
    }
}
