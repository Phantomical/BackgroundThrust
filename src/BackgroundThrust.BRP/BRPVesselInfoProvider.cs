using System;
using BackgroundResourceProcessing;

namespace BackgroundThrust.BRP;

public class BRPVesselInfoProvider : StockVesselInfoProvider
{
    public override bool AllowBackground => true;

    public override bool DisableOnZeroThrustInBackground => true;

    public override double GetVesselMass(BackgroundThrustVessel module, double UT)
    {
        var vessel = module.Vessel;
        if (vessel.loaded)
            return base.GetVesselMass(module, UT);

        var info = EventDispatcher.Instance.GetVesselInfo(vessel);
        var dryMass = module.DryMass ?? vessel.totalMass;

        return dryMass + info.WetMass + info.WetMassRate * Math.Max(UT - info.LastUpdateUT, 0.0);
    }

    public override Vector3d GetVesselThrust(BackgroundThrustVessel module, double UT)
    {
        var vessel = module.Vessel;
        if (vessel.loaded)
            return base.GetVesselThrust(module, UT);

        var info = EventDispatcher.Instance.GetVesselInfo(vessel);
        return module.Heading * info.Thrust;
    }
}
