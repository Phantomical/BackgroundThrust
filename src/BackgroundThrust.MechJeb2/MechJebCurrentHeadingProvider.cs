using MuMech;

namespace BackgroundThrust.MechJeb2;

public class MechJebCurrentHeadingProvider : ICurrentHeadingProvider
{
    public TargetHeadingProvider GetCurrentHeading(BackgroundThrustVessel module)
    {
        var vessel = module.Vessel;
        var mechJeb = vessel.GetMasterMechJeb();
        if (mechJeb == null)
            return null;

        var attitude = AccessUtils.GetAttitudeController(mechJeb);
        if (!AccessUtils.GetComputerModuleEnabled(attitude))
            return null;

        return new SmartASS();
    }
}
