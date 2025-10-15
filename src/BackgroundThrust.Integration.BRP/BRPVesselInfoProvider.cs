using System;

namespace BackgroundThrust.Integration.BRP;

public class BRPVesselInfoProvider : StockVesselInfoProvider
{
    public override bool AllowBackground => true;

    public override double GetVesselMass(BackgroundThrustVessel module, double UT)
    {
        var vessel = module.Vessel;
        if (vessel.loaded)
            return base.GetVesselMass(module, UT);

        var info = EventDispatcher.Instance.GetVesselInfo(vessel);
        var dryMass = module.DryMass ?? vessel.totalMass;

        return dryMass + info.WetMass + info.WetMassRate * Math.Max(UT - info.LastUpdateUT, 0.0);
    }

    public override double GetVesselThrust(BackgroundThrustVessel module, double UT)
    {
        var vessel = module.Vessel;
        if (vessel.loaded)
            return base.GetVesselThrust(module, UT);

        // We already set the thrust field on changepoints, so no need to do
        // anything extra here.
        return module.Thrust;
    }
}
