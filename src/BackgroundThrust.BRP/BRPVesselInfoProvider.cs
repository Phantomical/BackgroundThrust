using System;
using BackgroundResourceProcessing;

namespace BackgroundThrust.BRP;

public class BRPVesselInfoProvider : VesselInfoProvider
{
    public override bool DisableOnZeroThrust => true;

    public override double GetVesselMass(BackgroundThrustVessel module, double UT)
    {
        var vessel = module.Vessel;
        var info = EventDispatcher.Instance.GetVesselInfo(vessel);
        var dryMass = module.DryMass ?? vessel.totalMass;

        return dryMass + info.WetMass + info.WetMassRate * Math.Max(UT - info.LastUpdateUT, 0.0);
    }

    public override double GetVesselThrust(BackgroundThrustVessel module, double UT)
    {
        var vessel = module.Vessel;
        var info = EventDispatcher.Instance.GetVesselInfo(vessel);
        return info.Thrust;
    }
}
