using System;
using BackgroundThrust.Kerbalism.Patches;
using KERBALISM;
using KSP.Localization;

namespace BackgroundThrust.Kerbalism;

using ResourceBroker = KERBALISM.ResourceBroker;
using ResourceCache = KERBALISM.ResourceCache;

public class KerbalismVesselInfoProvider : VesselInfoProvider
{
    public static ResourceBroker EngineBroker = ResourceBroker.GetOrCreate(
        "bt-engine",
        ResourceBroker.BrokerCategory.Unknown,
        Localizer.Format("#LOC_BT_Kerbalism_BrokerTitle")
    );

    public const string ThrustResourceName = "_BackgroundThrust";

    public override bool DisableOnZeroThrust => false;

    public override double GetVesselMass(BackgroundThrustVessel module, double UT)
    {
        var vessel = module.Vessel;
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

    public override double GetVesselThrust(BackgroundThrustVessel module, double UT)
    {
        var vessel = module.Vessel;
        return ResourceCache.GetResource(vessel, ThrustResourceName)?.AverageRate ?? 0.0;
    }
}
