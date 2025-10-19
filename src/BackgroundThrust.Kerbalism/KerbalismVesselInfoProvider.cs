using System;
using BackgroundThrust.Kerbalism.Patches;
using KERBALISM;
using KSP.Localization;

namespace BackgroundThrust.Kerbalism;

using ResourceBroker = KERBALISM.ResourceBroker;
using ResourceCache = KERBALISM.ResourceCache;

public class KerbalismVesselInfoProvider : StockVesselInfoProvider
{
    public static ResourceBroker EngineBroker = ResourceBroker.GetOrCreate(
        "bt-engine",
        ResourceBroker.BrokerCategory.Unknown,
        Localizer.Format("#LOC_BT_Kerbalism_BrokerTitle")
    );

    public const string ThrustResourceName = "_BackgroundThrust";

    public override bool AllowBackground => true;

    public override bool DisableOnZeroThrustInBackground => false;

    public override double GetVesselMass(BackgroundThrustVessel module, double UT)
    {
        var vessel = module.Vessel;
        if (vessel.loaded)
            return base.GetVesselMass(module, UT);

        var vd = vessel.KerbalismData();
        var dryMass = module.DryMass ?? vessel.totalMass;

        if (!vd.IsSimulated)
            return dryMass;

        var resources = ResourceCache.Get(vessel);
        var lastUpdate = Kerbalism_Patch.GetLastUpdateTime(vessel.id) ?? UT;
        var resList = Kerbalism_Patch.GetResources(resources);
        var deltaT = Math.Min(UT - lastUpdate, 0.0);

        double mass = dryMass;
        foreach (var resource in resList.Values)
        {
            var amount = UtilMath.Clamp(
                resource.Amount + resource.AverageRate * deltaT,
                0.0,
                resource.Capacity
            );
            var density =
                PartResourceLibrary.Instance.GetDefinition(resource.ResourceID)?.density ?? 0.0;

            mass += amount * density;
        }

        return mass;
    }

    public override Vector3d GetVesselThrust(BackgroundThrustVessel module, double UT)
    {
        var vessel = module.Vessel;
        if (vessel.loaded)
            return base.GetVesselThrust(module, UT);

        var thrust = ResourceCache.GetResource(vessel, ThrustResourceName)?.AverageRate ?? 0.0;
        return module.Heading * thrust;
    }
}
